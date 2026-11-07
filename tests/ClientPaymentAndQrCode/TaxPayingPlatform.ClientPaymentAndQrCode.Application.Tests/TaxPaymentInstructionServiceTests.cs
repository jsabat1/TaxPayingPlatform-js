using Moq;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Repositories;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Services;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Aggregates;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;
using Xunit;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Application.Tests;

public class TaxPaymentInstructionServiceTests
{
    private const string ValidMicroAccount = "12345678901234567890123456";
    private static readonly DateTimeOffset FutureDeadline = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);

    private readonly Mock<ITaxPaymentInstructionRepository> _repositoryMock = new();
    private readonly TaxPaymentInstructionService _sut;

    public TaxPaymentInstructionServiceTests()
    {
        _sut = new TaxPaymentInstructionService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateInstructionAsync_WhenValidParameters_CreatesAndSavesInstruction()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();

        // Act
        var result = await _sut.CreateInstructionAsync(
            clientId,
            settlementId,
            1200.00m,
            ValidMicroAccount,
            "US-VAT7",
            FutureDeadline);

        // Assert
        Assert.NotEqual(Guid.Empty, result.InstructionId);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(1200.00m, result.ObligationAmount);
        Assert.Equal("Pending", result.Status);

        _repositoryMock.Verify(
            r => r.SaveAsync(It.Is<TaxPaymentInstruction>(i => i.ClientId == clientId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateBankingPaymentQrCodeAsync_WhenInstructionExists_GeneratesQrCodeAndCallsSaveAsync()
    {
        // Arrange
        var existing = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1500.00m,
            ValidMicroAccount,
            "ZUS-DRA",
            FutureDeadline);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.GenerateBankingPaymentQrCodeAsync(existing.Id);

        // Assert
        Assert.NotNull(result.EncodedQrCode);
        Assert.Equal("QrCodeGenerated", result.Status);
        _repositoryMock.Verify(
            r => r.SaveAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmTaxPaymentCompletionAsync_WhenValid_MarksAsPaidAndCallsSaveAsync()
    {
        // Arrange
        var existing = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1500.00m,
            ValidMicroAccount,
            "ZUS-DRA",
            FutureDeadline);
        existing.GenerateBankingPaymentQrCode();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.ConfirmTaxPaymentCompletionAsync(
            existing.Id,
            "RECEIPT-ZUS-778899",
            FutureDeadline.AddDays(-1));

        // Assert
        Assert.Equal("Paid", result.Status);
        Assert.Equal("RECEIPT-ZUS-778899", result.PaymentReceiptReference);
        _repositoryMock.Verify(
            r => r.SaveAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateBankingPaymentQrCodeAsync_WhenInstructionDoesNotExist_ThrowsNotFoundAndNeverSaves()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxPaymentInstruction?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TaxPaymentInstructionNotFoundException>(
            () => _sut.GenerateBankingPaymentQrCodeAsync(missingId));

        Assert.Equal(missingId, ex.InstructionId);
        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<TaxPaymentInstruction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmTaxPaymentCompletionAsync_WhenDeadlineExpired_PropagatesDomainExceptionAndNeverSaves()
    {
        // Arrange
        var existing = new TaxPaymentInstruction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1500.00m,
            ValidMicroAccount,
            "US-PIT5",
            FutureDeadline);
        existing.GenerateBankingPaymentQrCode();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act & Assert
        await Assert.ThrowsAsync<TaxPaymentDeadlineExpiredException>(
            () => _sut.ConfirmTaxPaymentCompletionAsync(
                existing.Id,
                "RECEIPT-LATE-999",
                FutureDeadline.AddHours(5)));

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<TaxPaymentInstruction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}