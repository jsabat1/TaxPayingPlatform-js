using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Entities;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.ValueObjects;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;
//aggregate root:
public sealed class TaxPaymentInstruction
{
    public const int MinimumReceiptReferenceLength = 5;
//
    private readonly List<BankTransferAttempt> _transferAttempts = new();

    public Guid Id { get; }
    public Guid ClientId { get; }
    public Guid MonthlyTaxSettlementId { get; }
    public decimal ObligationAmount { get; private set; }
    public string MicroAccountNumber { get; private set; }
    public string TaxAuthorityCode { get; private set; }
    public DateTimeOffset PaymentDeadline { get; private set; }
    public TaxPaymentStatus Status { get; private set; }
    public EncodedBankingQrCode? EncodedQrCode { get; private set; }
    public bool IsQrCodeDisplayed { get; private set; }
    public string? PaymentReceiptReference { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
//read only
    public IReadOnlyCollection<BankTransferAttempt> TransferAttempts => _transferAttempts.AsReadOnly();

    public TaxPaymentInstruction(
        Guid clientId,
        Guid monthlyTaxSettlementId,
        decimal obligationAmount,
        string microAccountNumber,
        string taxAuthorityCode,
        DateTimeOffset paymentDeadline)
    {
        //validate inputs on creation
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId cannot be empty.", nameof(clientId));

        if (monthlyTaxSettlementId == Guid.Empty)
            throw new ArgumentException("MonthlyTaxSettlementId cannot be empty.", nameof(monthlyTaxSettlementId));

        Id = Guid.NewGuid();
        ClientId = clientId;
        MonthlyTaxSettlementId = monthlyTaxSettlementId;
        ObligationAmount = obligationAmount;
        MicroAccountNumber = microAccountNumber ?? string.Empty;
        TaxAuthorityCode = taxAuthorityCode ?? string.Empty;
        PaymentDeadline = paymentDeadline;
        Status = TaxPaymentStatus.Pending;
    }
//exception nie generuje jak juz zaplacony
    public void GenerateBankingPaymentQrCode()
    {
        if (Status == TaxPaymentStatus.Paid)
        {
            throw new PaymentVerificationFailedException(Id, PaymentReceiptReference, "Cannot regenerate QR code for an already paid tax obligation.");
        }
//value object, validation in contructor
        EncodedQrCode = new EncodedBankingQrCode(
            MicroAccountNumber,
            ObligationAmount,
            TaxAuthorityCode,
            DateTimeOffset.UtcNow);

        MicroAccountNumber = EncodedQrCode.MicroAccountNumber;
        TaxAuthorityCode = EncodedQrCode.TaxAuthorityCode;
        Status = TaxPaymentStatus.QrCodeGenerated;
    }

    public void DisplayTaxPaymentQrCode()
    {
        if (EncodedQrCode is null)
        {
            throw new PaymentVerificationFailedException(Id, null, "Cannot display tax payment QR code before it is generated.");
        }

        IsQrCodeDisplayed = true;
        if (Status == TaxPaymentStatus.QrCodeGenerated)
        {
            Status = TaxPaymentStatus.QrCodeDisplayed;
        }
    }

    public void InitiateBankTransferViaQrCode(string bankingApplicationName, DateTimeOffset initiatedAt)
    {
        if (EncodedQrCode is null)
        {
            throw new PaymentVerificationFailedException(Id, null, "Cannot initiate bank transfer via QR code before QR code is generated.");
        }

        if (initiatedAt > PaymentDeadline || Status == TaxPaymentStatus.Overdue)
        {
            throw new TaxPaymentDeadlineExpiredException(Id, PaymentDeadline, initiatedAt);
        }

        var attempt = new BankTransferAttempt(bankingApplicationName, initiatedAt);
        _transferAttempts.Add(attempt);
        Status = TaxPaymentStatus.TransferInitiated;
    }

    public void ConfirmTaxPaymentCompletion(string receiptReference, DateTimeOffset confirmedAt)
    {
        if (EncodedQrCode is null)
        {
            throw new PaymentVerificationFailedException(Id, receiptReference, "Cannot confirm payment before generating banking payment QR code.");
        }

        if (Status == TaxPaymentStatus.Paid)
        {
            throw new PaymentVerificationFailedException(Id, receiptReference, "Tax obligation is already marked as paid.");
        }

        if (confirmedAt > PaymentDeadline || Status == TaxPaymentStatus.Overdue)
        {
            throw new TaxPaymentDeadlineExpiredException(Id, PaymentDeadline, confirmedAt);
        }

        if (string.IsNullOrWhiteSpace(receiptReference) || receiptReference.Trim().Length < MinimumReceiptReferenceLength)
        {
            throw new PaymentVerificationFailedException(
                Id,
                receiptReference,
                $"A valid ZUS/US payment receipt reference (minimum {MinimumReceiptReferenceLength} characters) is required.");
        }

        PaymentReceiptReference = receiptReference.Trim();
        ConfirmedAt = confirmedAt;
        Status = TaxPaymentStatus.Paid;

        var latestAttempt = _transferAttempts.LastOrDefault();
        latestAttempt?.MarkAsConfirmed();
    }

    public void MarkTaxObligationAsOverdue(DateTimeOffset currentTime)
    {
        if (Status == TaxPaymentStatus.Paid)
        {
            throw new PaymentVerificationFailedException(Id, PaymentReceiptReference, "Cannot mark an already paid tax obligation as overdue.");
        }

        if (currentTime <= PaymentDeadline)
        {
            throw new PaymentVerificationFailedException(Id, null, "Cannot mark tax obligation as overdue before the payment deadline has expired.");
        }

        Status = TaxPaymentStatus.Overdue;
    }
}