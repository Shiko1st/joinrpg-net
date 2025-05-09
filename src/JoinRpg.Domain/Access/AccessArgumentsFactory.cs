using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Domain.Access;

public enum CharacterAccessMode
{
    Usual,
    Print,
    SendClaim
}

public static class AccessArgumentsFactory
{
    public static AccessArguments Create(Character character, int? userId)
    {
        ArgumentNullException.ThrowIfNull(character);

        return new AccessArguments(
            MasterAccess: character.HasMasterAccess(userId),
            PlayerAccessToCharacter: character.HasPlayerAccess(userId),
            PlayerAccesToClaim: character.ApprovedClaim?.HasPlayerAccesToClaim(userId) ?? false,
            EditAllowed: character.Project.Active,
            Published: character.Project.Details.PublishPlot
            );
    }

    public static AccessArguments Create(Character character, ICurrentUserAccessor user, CharacterAccessMode mode = CharacterAccessMode.Usual)
    {
        ArgumentNullException.ThrowIfNull(character);
        return mode switch
        {
            CharacterAccessMode.Usual => Create(character, user?.UserIdOrDefault),
            CharacterAccessMode.Print => CreateForPrint(character, user?.UserIdOrDefault),
            CharacterAccessMode.SendClaim => CreateForAdd(character, user?.UserIdOrDefault),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// Для печати мы используем режим показа от имени игрока, даже если у нас есть мастерские права
    /// </summary>
    public static AccessArguments CreateForPrint(Character character, int? userId)
    {
        ArgumentNullException.ThrowIfNull(character);

        return new AccessArguments(
              MasterAccess: false,
              // Not a "player visible", because it could be master that asks to view as player
              PlayerAccessToCharacter: character.HasAnyAccess(userId),
              PlayerAccesToClaim: character.ApprovedClaim?.HasAccess(userId, ExtraAccessReason.Player) ?? false,
              EditAllowed: character.Project.Active,
              Published: character.Project.Details.PublishPlot
              );
    }

    /// <summary>
    /// Для добавления заявки нужен особый режим, где у пользователя нет доступа к персонажу (точно), а вот доступ к заявке есть, несмотря на то что заявки нет
    /// </summary>
    public static AccessArguments CreateForAdd(Character character, int? userId)
    {
        return new AccessArguments(
          character.HasMasterAccess(userId),
          PlayerAccessToCharacter: false,
          PlayerAccesToClaim: true,
          EditAllowed: true,
          Published: false
          );
    }

    public static AccessArguments Create(Claim claim, int? userId)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return new AccessArguments(
            MasterAccess: claim.HasMasterAccess(userId),
            PlayerAccessToCharacter: claim.Character?.HasPlayerAccess(userId) ?? false,
            PlayerAccesToClaim: claim.HasPlayerAccesToClaim(userId),
            EditAllowed: claim.Project.Active,
            Published: claim.Project.Details.PublishPlot
            );
    }
}
