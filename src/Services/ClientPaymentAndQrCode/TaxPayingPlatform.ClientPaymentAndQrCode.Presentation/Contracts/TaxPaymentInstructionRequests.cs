namespace TaxPayingPlatform.ClientPaymentAndQrCode.Presentation.Contracts;

public sealed record CreateTaxPaymentInstructionRequest(
    Guid ClientId,
    Guid MonthlyTaxSettlementId,
    decimal ObligationAmount,
    string MicroAccountNumber,
    string TaxAuthorityCode,
    DateTimeOffset PaymentDeadline);

public sealed record InitiateBankTransferRequest(
    string BankingApplicationName,
    DateTimeOffset? InitiatedAt = null);

public sealed record ConfirmTaxPaymentRequest(
    string ReceiptReference,
    DateTimeOffset? ConfirmedAt = null);

public sealed record MarkOverdueRequest(
    DateTimeOffset? CurrentTime = null);