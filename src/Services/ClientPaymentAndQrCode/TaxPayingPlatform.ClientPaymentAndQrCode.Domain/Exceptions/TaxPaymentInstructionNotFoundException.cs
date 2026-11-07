namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

public sealed class TaxPaymentInstructionNotFoundException : DomainException
{
    public Guid InstructionId { get; }

    public TaxPaymentInstructionNotFoundException(Guid instructionId)
        : base(
            code: "payment_instruction.not_found",
            message: $"Tax payment instruction with ID '{instructionId}' was not found.")
    {
        InstructionId = instructionId;
    }
}