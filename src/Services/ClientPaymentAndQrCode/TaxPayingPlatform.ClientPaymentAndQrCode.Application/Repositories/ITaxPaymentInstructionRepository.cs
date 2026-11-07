using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Application.Repositories;

public interface ITaxPaymentInstructionRepository
{
    Task<TaxPaymentInstruction?> GetByIdAsync(Guid instructionId, CancellationToken cancellationToken = default);
    Task SaveAsync(TaxPaymentInstruction instruction, CancellationToken cancellationToken = default);
}