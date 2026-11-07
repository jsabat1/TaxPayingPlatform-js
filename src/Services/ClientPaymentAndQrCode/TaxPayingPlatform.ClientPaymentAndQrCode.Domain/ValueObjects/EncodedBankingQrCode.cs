using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.ValueObjects;

public sealed record EncodedBankingQrCode
{
    public const int RequiredMicroAccountLength = 26;

    public string MicroAccountNumber { get; }
    public decimal Amount { get; }
    public string TaxAuthorityCode { get; }
    public string Payload { get; }
    public DateTimeOffset GeneratedAt { get; }

    internal EncodedBankingQrCode(string microAccountNumber, decimal amount, string taxAuthorityCode, DateTimeOffset generatedAt)
    {
        if (amount <= 0)
        {
            throw new QrCodeGenerationFailedException(amount, microAccountNumber, "Tax obligation amount must be greater than zero.");
        }

        var normalizedAccount = microAccountNumber?.Trim() ?? string.Empty;
        if (normalizedAccount.Length != RequiredMicroAccountLength || !normalizedAccount.All(char.IsDigit))
        {
            throw new QrCodeGenerationFailedException(amount, microAccountNumber ?? string.Empty, "Polish tax microaccount must consist of exactly 26 digits.");
        }

        if (string.IsNullOrWhiteSpace(taxAuthorityCode))
        {
            throw new QrCodeGenerationFailedException(amount, normalizedAccount, "Tax authority code cannot be empty.");
        }

        MicroAccountNumber = normalizedAccount;
        Amount = amount;
        TaxAuthorityCode = taxAuthorityCode.Trim().ToUpperInvariant();
        GeneratedAt = generatedAt;

        var amountInGrosze = (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        Payload = $"||PL|{MicroAccountNumber}|{amountInGrosze:D6}|{TaxAuthorityCode}|||";
    }
}