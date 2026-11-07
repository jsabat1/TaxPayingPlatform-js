using TaxPayingPlatform.ClientPaymentAndQrCode.Application.DTOs;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Application.Services;

public interface ITaxPaymentInstructionService
{
    Task<TaxPaymentInstructionDto> CreateInstructionAsync(
        Guid clientId,
        Guid monthlyTaxSettlementId,
        decimal obligationAmount,
        string microAccountNumber,
        string taxAuthorityCode,
        DateTimeOffset paymentDeadline,
        CancellationToken cancellationToken = default);

    Task<TaxPaymentInstructionDto?> GetInstructionAsync(
        Guid instructionId,
        CancellationToken cancellationToken = default);

    Task<TaxPaymentInstructionDto> GenerateBankingPaymentQrCodeAsync(
        Guid instructionId,
        CancellationToken cancellationToken = default);

    Task<TaxPaymentInstructionDto> InitiateBankTransferViaQrCodeAsync(
        Guid instructionId,
        string bankingApplicationName,
        DateTimeOffset initiatedAt,
        CancellationToken cancellationToken = default);

    Task<TaxPaymentInstructionDto> ConfirmTaxPaymentCompletionAsync(
        Guid instructionId,
        string receiptReference,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken = default);

    Task<TaxPaymentInstructionDto> MarkTaxObligationAsOverdueAsync(
        Guid instructionId,
        DateTimeOffset currentTime,
        CancellationToken cancellationToken = default);
}