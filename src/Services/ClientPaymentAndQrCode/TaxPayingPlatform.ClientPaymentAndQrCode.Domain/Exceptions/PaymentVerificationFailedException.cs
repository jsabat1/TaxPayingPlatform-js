namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

public sealed class PaymentVerificationFailedException : DomainException
{
    public Guid InstructionId { get; }
    public string? AttemptedReceiptReference { get; }

    public PaymentVerificationFailedException(Guid instructionId, string? attemptedReceiptReference, string reason)
        : base(
            code: "payment_instruction.verification_failed",
            message: $"Payment verification failed for instruction '{instructionId}': {reason}")
    {
        InstructionId = instructionId;
        AttemptedReceiptReference = attemptedReceiptReference;
    }
}