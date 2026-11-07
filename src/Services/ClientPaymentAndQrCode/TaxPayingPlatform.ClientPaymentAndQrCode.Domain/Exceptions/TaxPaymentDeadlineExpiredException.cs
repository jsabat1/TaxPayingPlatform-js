namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

public sealed class TaxPaymentDeadlineExpiredException : DomainException
{
    public Guid InstructionId { get; }
    public DateTimeOffset PaymentDeadline { get; }
    public DateTimeOffset AttemptedAt { get; }

    public TaxPaymentDeadlineExpiredException(Guid instructionId, DateTimeOffset paymentDeadline, DateTimeOffset attemptedAt)
        : base(
            code: "payment_instruction.deadline_expired",
            message: $"Tax payment deadline ({paymentDeadline:O}) expired before confirmation attempt at ({attemptedAt:O}).")
    {
        InstructionId = instructionId;
        PaymentDeadline = paymentDeadline;
        AttemptedAt = attemptedAt;
    }
}