namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;

public enum TaxPaymentStatus
{
    Pending = 0,
    QrCodeGenerated = 1,
    QrCodeDisplayed = 2,
    TransferInitiated = 3,
    Paid = 4,
    Overdue = 5
}