namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Entities;
//child entity do tax payment instruction aggreagate
public sealed class BankTransferAttempt
{
    public Guid Id { get; }
    public string BankingApplicationName { get; }
    public DateTimeOffset InitiatedAt { get; }
    public bool IsConfirmed { get; private set; }
//internal, only tax payment instruction can create this
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
//flag, only aggregate root can mark as confirmed
    internal void MarkAsConfirmed()
    {
        IsConfirmed = true;
    }
}