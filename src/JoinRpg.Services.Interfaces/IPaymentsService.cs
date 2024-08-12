using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Payment methods to be used for making payments
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Traditional payments using bank cards, Samsung pay, Apple pay...
    /// </summary>
    BankCard,

    /// <summary>
    /// Payments using Fast Payments System via QR Code (Russia only)
    /// </summary>
    FastPaymentsSystem,
}

/// <summary>
/// Base class for payment requests
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// Database Id of a payer
    /// </summary>
    public int PayerId { get; set; }

    /// <summary>
    /// How much to pay
    /// </summary>
    public int Money { get; set; }

    /// <summary>
    /// Payment method to use
    /// </summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Comment added by payer
    /// </summary>
    public string? CommentText { get; set; }

    /// <summary>
    /// Date and time of the operation
    /// </summary>
    public DateTime OperationDate { get; set; }
}

/// <summary>
/// Payment request for game fee
/// </summary>
public class ClaimPaymentRequest : PaymentRequest
{
    /// <summary>
    /// Database Id of a project
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Database Id of a claim
    /// </summary>
    public int ClaimId { get; set; }

    /// <summary>
    /// true to initiate an original recurrent payment and create a new instance of the <see cref="RecurrentPayment"/> class
    /// </summary>
    public bool Recurrent { get; set; }

    /// <summary>
    /// Id of a <see cref="RecurrentPayment"/> when new payment must be derived from an exited recurrent payment.
    /// </summary>
    public int? FromRecurrentPaymentId { get; set; }

    /// <summary>
    /// true to initiate a refund. Not compatible with <see cref="Recurrent"/>
    /// </summary>
    public bool Refund { get; set; }

    /// <summary>
    /// When <see cref="Refund"/> is true, must contain Id of a <see cref="FinanceOperation"/> to refund.
    /// </summary>
    public int? FinanceOperationToRefundId { get; set; }
}

/// <summary>
/// Base class for payment results
/// </summary>
public class PaymentContext
{
    /// <summary>
    /// true if payment was accepted
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Payment request description built by bank API
    /// </summary>
    public PaymentRequestDescriptor RequestDescriptor { get; set; }
}

/// <summary>
/// Result of claim payment
/// </summary>
public class ClaimPaymentContext : PaymentContext
{
}

/// <summary>
/// Base class for payment results
/// </summary>
public class PaymentResultContext
{
    /// <summary>
    /// true if payment was successfully made
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Database Id of a finance operation
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Bank response info
    /// </summary>
    public BankResponseInfo BankResponse { get; set; }
}

public class FastPaymentsSystemMobilePaymentContext
{
    public ICollection<FpsBank>? Banks { get; set; }

    public string QrCodeUrl { get; set; }

    public int Amount { get; set; }

    public string Details { get; set; }

    public int ProjectId { get; set; }

    public int ClaimId { get; set; }

    public int OperationId { get; set; }

    public FpsPlatform ExpectedPlatform { get; set; }
}

/// <summary>
/// Payments service
/// </summary>
public interface IPaymentsService
{
    /// <summary>
    /// Creates finance operation and returns payment context
    /// </summary>
    /// <param name="request">Payment request</param>
    Task<ClaimPaymentContext> InitiateClaimPaymentAsync(ClaimPaymentRequest request);

    /// <summary>
    /// Creates finance operation and returns payment context for the Fast Payments System mobile payment.
    /// </summary>
    /// <param name="request">Payment request</param>
    /// <param name="platform">Desired platform</param>
    Task<FastPaymentsSystemMobilePaymentContext> InitiateFastPaymentsSystemMobilePaymentAsync(ClaimPaymentRequest request, FpsPlatform platform);

    Task<FastPaymentsSystemMobilePaymentContext> GetFastPaymentsSystemMobilePaymentContextAsync(
        int projectId,
        int claimId,
        int operationId,
        FpsPlatform platform);

    /// <summary>
    /// Updates status of a proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    /// <param name="orderId">Finance operation Id</param>
    Task UpdateClaimPaymentAsync(int projectId, int claimId, int orderId);

    /// <summary>
    /// Updates status of the last proposed payment
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    Task UpdateLastClaimPaymentAsync(int projectId, int claimId);

    /// <summary>
    /// Sets recurrent payment to cancelled internally and tries to cancel on the bank side.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="recurrentPaymentId">Id of a recurrent payment</param>
    /// <returns>true when payment was successfully cancelled, false otherwise, null when it was not even possible to start</returns>
    Task<bool?> CancelRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId);

    /// <summary>
    /// Tries to charge <paramref name="amount"/> money using payment method assigned with a specified recurrent payment.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="recurrentPaymentId">Id of a recurrent payment</param>
    /// <param name="amount">How much money to charge. If null, will be taken as much as was charged in the first payment.</param>
    /// <param name="internalCall">When true, access will not be verified.</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> or null when it was even not possible to initiate operation.</returns>
    /// <remarks>
    /// Payments are typically asynchronous operations. It is necessary to update its state
    /// a little bit later using <see cref="UpdateClaimPaymentAsync"/>.
    /// </remarks>
    Task<FinanceOperation?> PerformRecurrentPaymentAsync(int projectId, int claimId, int recurrentPaymentId, int? amount, bool internalCall = false);

    /// <summary>
    /// Tries to make a refund of a specified payment.
    /// </summary>
    /// <param name="projectId">Id of a project</param>
    /// <param name="claimId">Id of a claim</param>
    /// <param name="operationId">Id of a payment to refund.</param>
    /// <returns>An instance of the <see cref="FinanceOperation"/> that represents the refund.</returns>
    Task<FinanceOperation> RefundAsync(int projectId, int claimId, int operationId);

    /// <summary>
    /// Tries to create an url to open payment details in external system using the external payment key of a payment.
    /// </summary>
    /// <param name="externalPaymentKey">A key returned from an external payment system.</param>
    /// <returns></returns>
    string? GetExternalPaymentUrl(string? externalPaymentKey);
}
