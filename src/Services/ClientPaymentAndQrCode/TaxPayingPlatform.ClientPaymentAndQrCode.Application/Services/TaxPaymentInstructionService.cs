using TaxPayingPlatform.ClientPaymentAndQrCode.Application.DTOs;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Repositories;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Application.Services;
//tylko traffic, orchestration
public sealed class TaxPaymentInstructionService : ITaxPaymentInstructionService
{
    private readonly ITaxPaymentInstructionRepository _repository;
//dependency injection, fake mock repository for testing
    public TaxPaymentInstructionService(ITaxPaymentInstructionRepository repository)
    {
        _repository = repository;
    }
//task<> - cos co sie stanie w przyszlosci, returnuje <taskpeymentinstructiondto>, await zanim database operation sie wykona
    public async Task<TaxPaymentInstructionDto> CreateInstructionAsync(
        Guid clientId,
        Guid monthlyTaxSettlementId,
        decimal obligationAmount,
        string microAccountNumber,
        string taxAuthorityCode,
        DateTimeOffset paymentDeadline,
        CancellationToken cancellationToken = default)
    {
        var instruction = new TaxPaymentInstruction(
            clientId,
            monthlyTaxSettlementId,
            obligationAmount,
            microAccountNumber,
            taxAuthorityCode,
            paymentDeadline);

        await _repository.SaveAsync(instruction, cancellationToken); //zapisz do repository
        return MapToDto(instruction);//map to dto
    }

    public async Task<TaxPaymentInstructionDto?> GetInstructionAsync(
        Guid instructionId,
        CancellationToken cancellationToken = default)
    {
        var instruction = await _repository.GetByIdAsync(instructionId, cancellationToken); //database null = throw exception
        return instruction is null ? null : MapToDto(instruction);
    }

    public async Task<TaxPaymentInstructionDto> GenerateBankingPaymentQrCodeAsync(
        Guid instructionId,
        CancellationToken cancellationToken = default)
    {
        var instruction = await GetExistingInstructionOrThrowAsync(instructionId, cancellationToken);//load z database

        instruction.GenerateBankingPaymentQrCode();//business logic

        await _repository.SaveAsync(instruction, cancellationToken);//save to database
        return MapToDto(instruction);//return read model
    }

    public async Task<TaxPaymentInstructionDto> InitiateBankTransferViaQrCodeAsync(
        Guid instructionId,
        string bankingApplicationName,
        DateTimeOffset initiatedAt,
        CancellationToken cancellationToken = default)
    {
        var instruction = await GetExistingInstructionOrThrowAsync(instructionId, cancellationToken);

        instruction.DisplayTaxPaymentQrCode();
        instruction.InitiateBankTransferViaQrCode(bankingApplicationName, initiatedAt);

        await _repository.SaveAsync(instruction, cancellationToken);
        return MapToDto(instruction);
    }

    public async Task<TaxPaymentInstructionDto> ConfirmTaxPaymentCompletionAsync(
        Guid instructionId,
        string receiptReference,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken = default)
    {
        var instruction = await GetExistingInstructionOrThrowAsync(instructionId, cancellationToken);

        instruction.ConfirmTaxPaymentCompletion(receiptReference, confirmedAt);

        await _repository.SaveAsync(instruction, cancellationToken);
        return MapToDto(instruction);
    }

    public async Task<TaxPaymentInstructionDto> MarkTaxObligationAsOverdueAsync(
        Guid instructionId,
        DateTimeOffset currentTime,
        CancellationToken cancellationToken = default)
    {
        var instruction = await GetExistingInstructionOrThrowAsync(instructionId, cancellationToken);

        instruction.MarkTaxObligationAsOverdue(currentTime);

        await _repository.SaveAsync(instruction, cancellationToken);
        return MapToDto(instruction);
    }

    private async Task<TaxPaymentInstruction> GetExistingInstructionOrThrowAsync(
        Guid instructionId,
        CancellationToken cancellationToken)
    {
        var instruction = await _repository.GetByIdAsync(instructionId, cancellationToken);
        return instruction ?? throw new TaxPaymentInstructionNotFoundException(instructionId);
    }

    private static TaxPaymentInstructionDto MapToDto(TaxPaymentInstruction instruction) =>
        new(
            instruction.Id,
            instruction.ClientId,
            instruction.MonthlyTaxSettlementId,
            instruction.ObligationAmount,
            instruction.MicroAccountNumber,
            instruction.TaxAuthorityCode,
            instruction.PaymentDeadline,
            instruction.Status.ToString(),
            instruction.IsQrCodeDisplayed,
            instruction.EncodedQrCode is null
                ? null
                : new EncodedBankingQrCodeDto(
                    instruction.EncodedQrCode.MicroAccountNumber,
                    instruction.EncodedQrCode.Amount,
                    instruction.EncodedQrCode.TaxAuthorityCode,
                    instruction.EncodedQrCode.Payload,
                    instruction.EncodedQrCode.GeneratedAt),
            instruction.TransferAttempts
                .Select(a => new BankTransferAttemptDto(a.Id, a.BankingApplicationName, a.InitiatedAt, a.IsConfirmed))
                .ToList(),
            instruction.PaymentReceiptReference,
            instruction.ConfirmedAt);
}