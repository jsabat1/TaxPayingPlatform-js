namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

public sealed class QrCodeGenerationFailedException : DomainException
{
    public decimal AttemptedAmount { get; }
    public string AttemptedMicroAccount { get; }

    public QrCodeGenerationFailedException(decimal attemptedAmount, string attemptedMicroAccount, string reason)
        : base(
            code: "payment_instruction.qr_code_generation_failed",
            message: $"QR code generation failed for amount '{attemptedAmount:F2}' and microaccount '{attemptedMicroAccount}': {reason}")
    {
        AttemptedAmount = attemptedAmount;
        AttemptedMicroAccount = attemptedMicroAccount;
    }
}