using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;
using Xunit;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Tests;

public class TaxPaymentInstructionTests
{
    private const string ValidMicroAccount = "12345678901234567890123456";
    private static readonly DateTimeOffset FutureDeadline = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void GenerateBankingPaymentQrCode_WhenValidDataProvided_GeneratesEncodedQrCodeAndUpdatesStatus()
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            clientId: Guid.NewGuid(),
            monthlyTaxSettlementId: Guid.NewGuid(),
            obligationAmount: 1450.75m,
            microAccountNumber: ValidMicroAccount,
            taxAuthorityCode: "US-VAT7",
            paymentDeadline: FutureDeadline);

        // Act
        instruction.GenerateBankingPaymentQrCode();

        // Assert
        Assert.NotNull(instruction.EncodedQrCode);
        Assert.Equal(ValidMicroAccount, instruction.EncodedQrCode.MicroAccountNumber);
        Assert.Equal(1450.75m, instruction.EncodedQrCode.Amount);
        Assert.Contains(ValidMicroAccount, instruction.EncodedQrCode.Payload);
        Assert.Contains("145075", instruction.EncodedQrCode.Payload);
        Assert.Equal(TaxPaymentStatus.QrCodeGenerated, instruction.Status);
    }

    [Fact]
    public void DisplayAndInitiateBankTransferViaQrCode_WhenQrGenerated_RecordsDisplayAndAddsTransferAttempt()
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            800.00m,
            ValidMicroAccount,
            "ZUS-DRA",
            FutureDeadline);
        instruction.GenerateBankingPaymentQrCode();

        // Act
        instruction.DisplayTaxPaymentQrCode();
        instruction.InitiateBankTransferViaQrCode("mBank Mobile", FutureDeadline.AddDays(-2));

        // Assert
        Assert.True(instruction.IsQrCodeDisplayed);
        Assert.Equal(TaxPaymentStatus.TransferInitiated, instruction.Status);
        var attempt = Assert.Single(instruction.TransferAttempts);
        Assert.Equal("mBank Mobile", attempt.BankingApplicationName);
        Assert.False(attempt.IsConfirmed);
    }

    [Fact]
    public void ConfirmTaxPaymentCompletion_WhenWithinDeadlineAndValidReceipt_MarksTaxObligationAsPaid()
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2100.00m,
            ValidMicroAccount,
            "US-PIT5",
            FutureDeadline);
        instruction.GenerateBankingPaymentQrCode();
        instruction.InitiateBankTransferViaQrCode("ING Mobile", FutureDeadline.AddDays(-3));
        var confirmationTime = FutureDeadline.AddDays(-1);

        // Act
        instruction.ConfirmTaxPaymentCompletion("RECEIPT-ZUS-2026-9981", confirmationTime);

        // Assert
        Assert.Equal(TaxPaymentStatus.Paid, instruction.Status);
        Assert.Equal("RECEIPT-ZUS-2026-9981", instruction.PaymentReceiptReference);
        Assert.Equal(confirmationTime, instruction.ConfirmedAt);
        var attempt = Assert.Single(instruction.TransferAttempts);
        Assert.True(attempt.IsConfirmed);
    }

    [Fact]
    public void MarkTaxObligationAsOverdue_WhenDeadlineExpiredWithoutConfirmation_MarksAsOverdue()
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            500.00m,
            ValidMicroAccount,
            "US-VAT7",
            FutureDeadline);
        instruction.GenerateBankingPaymentQrCode();
        var afterDeadline = FutureDeadline.AddHours(2);

        // Act
        instruction.MarkTaxObligationAsOverdue(afterDeadline);

        // Assert
        Assert.Equal(TaxPaymentStatus.Overdue, instruction.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100.50)]
    public void GenerateBankingPaymentQrCode_WhenObligationAmountIsZeroOrNegative_ThrowsQrCodeGenerationFailedException(decimal invalidAmount)
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalidAmount,
            ValidMicroAccount,
            "US-VAT7",
            FutureDeadline);

        // Act
        var ex = Assert.Throws<QrCodeGenerationFailedException>(() =>
            instruction.GenerateBankingPaymentQrCode());

        // Assert
        Assert.Equal("payment_instruction.qr_code_generation_failed", ex.Code);
        Assert.Equal(invalidAmount, ex.AttemptedAmount);
        Assert.Equal(ValidMicroAccount, ex.AttemptedMicroAccount);
        Assert.Null(instruction.EncodedQrCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567890123456789012345A")]
    public void GenerateBankingPaymentQrCode_WhenMicroAccountFormatInvalid_ThrowsQrCodeGenerationFailedException(string invalidMicroAccount)
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            300.00m,
            invalidMicroAccount,
            "US-VAT7",
            FutureDeadline);

        // Act
        var ex = Assert.Throws<QrCodeGenerationFailedException>(() =>
            instruction.GenerateBankingPaymentQrCode());

        // Assert
        Assert.Equal("payment_instruction.qr_code_generation_failed", ex.Code);
        Assert.Equal(300.00m, ex.AttemptedAmount);
        Assert.Equal(invalidMicroAccount, ex.AttemptedMicroAccount);
    }

    [Fact]
    public void ConfirmTaxPaymentCompletion_WhenPaymentDeadlineExpired_ThrowsTaxPaymentDeadlineExpiredException()
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            950.00m,
            ValidMicroAccount,
            "ZUS-DRA",
            FutureDeadline);
        instruction.GenerateBankingPaymentQrCode();
        var lateConfirmationTime = FutureDeadline.AddMinutes(15);

        // Act
        var ex = Assert.Throws<TaxPaymentDeadlineExpiredException>(() =>
            instruction.ConfirmTaxPaymentCompletion("RECEIPT-LATE-001", lateConfirmationTime));

        // Assert
        Assert.Equal("payment_instruction.deadline_expired", ex.Code);
        Assert.Equal(instruction.Id, ex.InstructionId);
        Assert.Equal(FutureDeadline, ex.PaymentDeadline);
        Assert.Equal(lateConfirmationTime, ex.AttemptedAt);
        Assert.NotEqual(TaxPaymentStatus.Paid, instruction.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    public void ConfirmTaxPaymentCompletion_WhenReceiptReferenceIsInvalid_ThrowsPaymentVerificationFailedException(string invalidReceipt)
    {
        // Arrange
        var instruction = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            950.00m,
            ValidMicroAccount,
            "ZUS-DRA",
            FutureDeadline);
        instruction.GenerateBankingPaymentQrCode();

        // Act
        var ex = Assert.Throws<PaymentVerificationFailedException>(() =>
            instruction.ConfirmTaxPaymentCompletion(invalidReceipt, FutureDeadline.AddDays(-1)));

        // Assert
        Assert.Equal("payment_instruction.verification_failed", ex.Code);
        Assert.Equal(instruction.Id, ex.InstructionId);
        Assert.Equal(invalidReceipt, ex.AttemptedReceiptReference);
    }
}