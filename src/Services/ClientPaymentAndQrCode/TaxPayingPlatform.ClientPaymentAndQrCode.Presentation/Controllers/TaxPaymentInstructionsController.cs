using Microsoft.AspNetCore.Mvc;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.DTOs;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Services;
using TaxPayingPlatform.ClientPaymentAndQrCode.Domain.Exceptions;
using TaxPayingPlatform.ClientPaymentAndQrCode.Presentation.Contracts;

namespace TaxPayingPlatform.ClientPaymentAndQrCode.Presentation.Controllers;
//automatyczne validation
[ApiController]
[Route("api/tax-payment-instructions")]
public sealed class TaxPaymentInstructionsController : ControllerBase
{
    private readonly ITaxPaymentInstructionService _service;

    public TaxPaymentInstructionsController(ITaxPaymentInstructionService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<TaxPaymentInstructionDto>> CreateInstruction(
        [FromBody] CreateTaxPaymentInstructionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateInstructionAsync(
                request.ClientId,
                request.MonthlyTaxSettlementId,
                request.ObligationAmount,
                request.MicroAccountNumber,
                request.TaxAuthorityCode,
                request.PaymentDeadline,
                cancellationToken);
//201
            return CreatedAtAction(
                nameof(GetInstruction),
                new { instructionId = created.InstructionId },
                created);
        }
        catch (ArgumentException ex)
        {//400
            return BadRequest(new { code = "validation.invalid_argument", message = ex.Message });
        }
    }

    [HttpGet("{instructionId:guid}")]
    public async Task<ActionResult<TaxPaymentInstructionDto>> GetInstruction(
        Guid instructionId,
        CancellationToken cancellationToken)
    {
        var instruction = await _service.GetInstructionAsync(instructionId, cancellationToken);
        if (instruction is null)
        {
            return NotFound(new
            {
                code = "payment_instruction.not_found",
                message = $"Tax payment instruction '{instructionId}' was not found."
            });
        }

        return Ok(instruction);
    }

    [HttpPost("{instructionId:guid}/qr-code")]
    public async Task<ActionResult<TaxPaymentInstructionDto>> GenerateBankingPaymentQrCode(
        Guid instructionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.GenerateBankingPaymentQrCodeAsync(instructionId, cancellationToken);
            return Ok(updated);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
    }

    [HttpPost("{instructionId:guid}/initiate-transfer")]
    public async Task<ActionResult<TaxPaymentInstructionDto>> InitiateBankTransferViaQrCode(
        Guid instructionId,
        [FromBody] InitiateBankTransferRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.InitiateBankTransferViaQrCodeAsync(
                instructionId,
                request.BankingApplicationName,
                request.InitiatedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);

            return Ok(updated);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { code = "validation.invalid_argument", message = ex.Message });
        }
    }

    [HttpPost("{instructionId:guid}/confirm-payment")]
    public async Task<ActionResult<TaxPaymentInstructionDto>> ConfirmTaxPaymentCompletion(
        Guid instructionId,
        [FromBody] ConfirmTaxPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.ConfirmTaxPaymentCompletionAsync(
                instructionId,
                request.ReceiptReference,
                request.ConfirmedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);

            return Ok(updated);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
    }

    [HttpPost("{instructionId:guid}/mark-overdue")]
    public async Task<ActionResult<TaxPaymentInstructionDto>> MarkTaxObligationAsOverdue(
        Guid instructionId,
        [FromBody] MarkOverdueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.MarkTaxObligationAsOverdueAsync(
                instructionId,
                request.CurrentTime ?? DateTimeOffset.UtcNow,
                cancellationToken);

            return Ok(updated);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
    }

    private ActionResult MapDomainException(DomainException ex) => ex switch
    {
        TaxPaymentInstructionNotFoundException => NotFound(new { code = ex.Code, message = ex.Message }),
        QrCodeGenerationFailedException qrEx => BadRequest(new
        {
            code = qrEx.Code,
            message = qrEx.Message,
            attemptedAmount = qrEx.AttemptedAmount,
            attemptedMicroAccount = qrEx.AttemptedMicroAccount
        }),
        TaxPaymentDeadlineExpiredException deadlineEx => UnprocessableEntity(new
        {
            code = deadlineEx.Code,
            message = deadlineEx.Message,
            instructionId = deadlineEx.InstructionId,
            paymentDeadline = deadlineEx.PaymentDeadline,
            attemptedAt = deadlineEx.AttemptedAt
        }),
        PaymentVerificationFailedException verifyEx => UnprocessableEntity(new
        {
            code = verifyEx.Code,
            message = verifyEx.Message,
            instructionId = verifyEx.InstructionId,
            attemptedReceiptReference = verifyEx.AttemptedReceiptReference
        }),
        _ => UnprocessableEntity(new { code = ex.Code, message = ex.Message })
    };
}