namespace TaxPayingPlatform.ClientPaymentAndQrCode.Application.DTOs;

public sealed record EncodedBankingQrCodeDto(
    string MicroAccountNumber,
    decimal Amount,
    string TaxAuthorityCode,
    string Payload,
    DateTimeOffset GeneratedAt);

public sealed record BankTransferAttemptDto(
    Guid AttemptId,
    string BankingApplicationName,
    DateTimeOffset InitiatedAt,
    bool IsConfirmed);

public sealed record TaxPaymentInstructionDto(
    Guid InstructionId,
    Guid ClientId,
    Guid MonthlyTaxSettlementId,
    decimal ObligationAmount,
    string MicroAccountNumber,
    string TaxAuthorityCode,
    DateTimeOffset PaymentDeadline,
    string Status,
    bool IsQrCodeDisplayed,
    EncodedBankingQrCodeDto? EncodedQrCode,
    IReadOnlyList<BankTransferAttemptDto> TransferAttempts,
    string? PaymentReceiptReference,
    DateTimeOffset? ConfirmedAt);