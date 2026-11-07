using System.Collections.Concurrent;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Repositories;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Presentation.Infrastructure;

public sealed class InMemoryTaxPaymentInstructionRepository : ITaxPaymentInstructionRepository
{
    private readonly ConcurrentDictionary<Guid, TaxPaymentInstruction> _storage = new();

    public Task<TaxPaymentInstruction?> GetByIdAsync(Guid instructionId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(instructionId, out var instruction);
        return Task.FromResult(instruction);
    }

    public Task SaveAsync(TaxPaymentInstruction instruction, CancellationToken cancellationToken = default)
    {
        _storage[instruction.Id] = instruction;
        return Task.CompletedTask;
    }
}