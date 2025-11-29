using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository : INotificationRepository
{
    private readonly Counter<int> lostRaceCounter;
    private readonly Counter<int> successRaceCounter;
    private readonly NotificationsDataDbContext dbContext;
    private readonly ILogger<NotificationsRepository> logger;

    private readonly IExecutionStrategy executionStrategy;

    private readonly string lockRequestSql;

    public NotificationsRepository(
        NotificationsDataDbContext dbContext,
        ILogger<NotificationsRepository> logger,
        IMeterFactory meterFactory)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        var meter = meterFactory.Create("JoinRpg.Dal.Notifications.Repository");
        lostRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_lost_races");
        successRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_success_races");
        executionStrategy = dbContext.Database.CreateExecutionStrategy();

        lockRequestSql = BuildLockRequestSql();
    }

    private string BuildLockRequestSql()
    {
        var channelsTableName = dbContext.NotificationMessageChannels.EntityType.GetTableName();
        Debug.Assert(channelsTableName is not null);
        var channelStatusPropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.NotificationMessageStatus))
            .GetColumnName();
        Debug.Assert(channelStatusPropName is not null);
        var channelTypePropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.Channel))
            .GetColumnName();
        Debug.Assert(channelTypePropName is not null);
        var momentPropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.SendAfter))
            .GetColumnName();
        Debug.Assert(momentPropName is not null);

        return $"SELECT * FROM {channelsTableName} ch"
            + $"\nWHERE {channelTypePropName} = {{0}} AND {channelStatusPropName} = {{1}} AND CURRENT_TIMESTAMP > {momentPropName}"
            + "\nLIMIT 1"
            + "\nLOCK FOR UPDATE SKIP LOCKED";
    }

    async Task INotificationRepository.InsertNotifications(NotificationMessageCreateDto[] notifications)
    {
        var moment = DateTimeOffset.UtcNow;
        foreach (var x in notifications)
        {
            var message = new NotificationMessage()
            {
                Body = x.Body.Contents!,
                Header = x.Header,
                InitiatorAddress = x.InitiatorAddress,
                InitiatorUserId = x.Initiator.Value,
                RecipientUserId = x.Recipient.Value,
                NotificationMessageChannels = [
                    .. x.Channels.Select(c => new NotificationMessageChannel()
                    {
                        Channel = c.Channel,
                        ChannelSpecificValue = c.ChannelSpecificValue,
                        NotificationMessageStatus = NotificationMessageStatus.Queued,
                        NotificationMessage = null!,
                        Attempts = 0,
                        SendAfter = moment,
                    })
                    ]
            };
            _ = dbContext.Notifications.Add(message);
        }
        _ = await dbContext.SaveChangesAsync();
    }

    Task INotificationRepository.MarkSendingFailed(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Failed);

    Task INotificationRepository.MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Queued, attempts, sendAfter);

    Task INotificationRepository.MarkSendingSucceeded(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Sent);

    private record struct SelectMessageData(NotificationChannel Channel);

    private async Task<AddressedNotificationMessageDto?> InternalSelectNextNotificationAsync(SelectMessageData data, CancellationToken cancellationToken)
    {
        var candidate = await dbContext.NotificationMessageChannels
            .FromSqlRaw(lockRequestSql, data.Channel, NotificationMessageStatus.Queued)
            .Include(static e => e.NotificationMessage)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            // No messages left
            lostRaceCounter.Add(1);
            return null;
        }

        await SetStatus(
            candidate.NotificationMessageChannelId,
            candidate.Channel,
            from: candidate.NotificationMessageStatus,
            to: NotificationMessageStatus.Sending,
            attempts: candidate.Attempts + 1);

        successRaceCounter.Add(1);

        return CreateNotificationMessageDto(candidate);
    }

    Task<AddressedNotificationMessageDto?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        return executionStrategy.ExecuteInTransactionAsync(
            new SelectMessageData(channel),
            InternalSelectNextNotificationAsync,
            (_, _) => Task.FromResult(false));
    }

    private static AddressedNotificationMessageDto CreateNotificationMessageDto(NotificationMessageChannel candidate)
    {
        return new AddressedNotificationMessageDto
        {
            Id = new NotificationId(candidate.NotificationMessageId),
            Channel = new NotificationChannelDto(candidate.Channel, candidate.ChannelSpecificValue),
            Attempts = candidate.Attempts,
            Body = new DataModel.MarkdownString(candidate.NotificationMessage.Body),
            Header = candidate.NotificationMessage.Header,
            Initiator = new(candidate.NotificationMessage.InitiatorUserId),
            InitiatorAddress = new(candidate.NotificationMessage.InitiatorAddress),
            Recipient = new(candidate.NotificationMessage.RecipientUserId),
        };
    }

    private async Task SetStatus(
        int messageId,
        NotificationChannel channel,
        NotificationMessageStatus from,
        NotificationMessageStatus to,
        int? attempts = null,
        DateTimeOffset? sendAfter = null)
    {
        if (to == NotificationMessageStatus.Sending && !attempts.HasValue)
        {
            throw new ArgumentNullException(nameof(attempts));
        }

        Expression<Func<SetPropertyCalls<NotificationMessageChannel>, SetPropertyCalls<NotificationMessageChannel>>> updateExpression
            = to switch
            {
                NotificationMessageStatus.Sending => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.Attempts, attempts.GetValueOrDefault()),
                NotificationMessageStatus.Queued when attempts.HasValue => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.SendAfter, sendAfter ?? DateTimeOffset.UtcNow)
                    .SetProperty(static channel => channel.Attempts, attempts.Value),
                NotificationMessageStatus.Queued => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.SendAfter, sendAfter ?? DateTimeOffset.UtcNow),
                _ => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to),
            };

        var totalRows = await dbContext
            .NotificationMessageChannels
            .Where(ch => ch.NotificationMessageId == messageId)
            .ForChannelAndStatus(channel, from)
            .ExecuteUpdateAsync(updateExpression);

        // Both the following cases are unexpected due to row-level lock or provided search criteria, so we can throw.
        switch (totalRows)
        {
            case 0:
                throw new DbUpdateConcurrencyException($"Failed to change status of channel {channel} from {from} to {to} of message {messageId}");
            case > 1:
                throw new DbUpdateConcurrencyException($"Unexpected number ({totalRows}) of updated notification channels {channel} from {from} to {to} of message {messageId}");
        }
    }
}
