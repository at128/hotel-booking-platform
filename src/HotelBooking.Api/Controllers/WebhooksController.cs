using Asp.Versioning;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Checkout.Commands.HandlePaymentWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
public sealed class WebhooksController(
    ISender sender,
    IPaymentGateway paymentGateway,
    ILogger<WebhooksController> logger)
    : ControllerBase
{
    private const string StripeSignatureHeader = "Stripe-Signature";

    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
            rawPayload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers[StripeSignatureHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Stripe webhook received without Stripe-Signature header");
            return BadRequest("Missing Stripe-Signature header.");
        }

        var parsed = await paymentGateway.ParseWebhookAsync(rawPayload, signature, ct);

        if (!parsed.IsSignatureValid)
        {
            logger.LogWarning("Stripe webhook signature validation failed");
            return BadRequest("Invalid webhook signature.");
        }

        logger.LogInformation("Stripe webhook received: {EventType}", parsed.EventType);

        try
        {
            var result = await sender.Send(new HandlePaymentWebhookCommand(parsed), ct);

            if (result.IsError)
            {
                logger.LogWarning(
                    "Webhook handler returned error for event {EventType}: {ErrorCode} - {ErrorDescription}",
                    parsed.EventType,
                    result.TopError.Code,
                    result.TopError.Description);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Webhook request was canceled while processing event {EventType}. Acknowledging to avoid retries.",
                parsed.EventType);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception while processing webhook event {EventType}",
                parsed.EventType);

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }
}