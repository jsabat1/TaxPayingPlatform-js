namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Entities;

public sealed class BankTransferAttempt
{
    public Guid Id { get; }
    public string BankingApplicationName { get; }
    public DateTimeOffset InitiatedAt { get; }
    public bool IsConfirmed { get; private set; }

    internal BankTransferAttempt(string bankingApplicationName, DateTimeOffset initiatedAt)
    {
        if (string.IsNullOrWhiteSpace(bankingApplicationName))
        {
            throw new ArgumentException("Banking application name cannot be empty.", nameof(bankingApplicationName));
        }

        Id = Guid.NewGuid();
        BankingApplicationName = bankingApplicationName.Trim();
        InitiatedAt = initiatedAt;
        IsConfirmed = false;
    }

    internal void MarkAsConfirmed()
    {
        IsConfirmed = true;
    }
}