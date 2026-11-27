using Microsoft.AspNetCore.Mvc;
using NB.Omc.Cubo.Contracts;
using Project.Api.DbContexts;
using Project.Api.Models;

namespace Project.Api;

[ApiController]
[Route("api/validation-requests")]
public sealed class ValidationRequestsController(DbContextProject db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ValidationRequest>>> Get(int id, CancellationToken cancellationToken)
    {
        var validationRequest = await db.ValidationRequests.FindAsync([id], cancellationToken);
        if (validationRequest is null)
            return ValidationRequestNotFound();

        return Ok(ApiResponseFactory.Success(validationRequest));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ValidationRequest>>> Create(
        ValidationRequestRequest request, CancellationToken cancellationToken)
    {
        var validationRequest = new ValidationRequest
        {
            CreationDate = DateTime.UtcNow,
            CreationUserId = request.UserId
        };
        ApplyChanges(validationRequest, request, validationRequest.CreationDate);

        await db.ValidationRequests.AddAsync(validationRequest, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = validationRequest.Id }, ApiResponseFactory.Success(validationRequest));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ValidationRequest>>> Update(
        int id, ValidationRequestRequest request, CancellationToken cancellationToken)
    {
        var validationRequest = await db.ValidationRequests.FindAsync([id], cancellationToken);
        if (validationRequest is null)
            return ValidationRequestNotFound();

        ApplyChanges(validationRequest, request, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(validationRequest));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var validationRequest = await db.ValidationRequests.FindAsync([id], cancellationToken);
        if (validationRequest is null)
            return ValidationRequestNotFound();

        db.ValidationRequests.Remove(validationRequest);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<ValidationRequest[]>>> GetHistory(
        int id, CancellationToken cancellationToken)
    {
        var history = await db.GetHistoryAsync<ValidationRequest, long>(id, cancellationToken);

        return Ok(ApiResponseFactory.Success(history));
    }

    private NotFoundObjectResult ValidationRequestNotFound() => NotFound(
        ApiResponseFactory.Failure<ValidationRequest>("VALIDATION_REQUEST_NOT_FOUND", "The validation request was not found."));

    private static void ApplyChanges(ValidationRequest validationRequest, ValidationRequestRequest request, DateTime updatedAt)
    {
        validationRequest.DocumentId = request.DocumentId;  
        validationRequest.UpdateDate = updatedAt;
        validationRequest.UpdateUser = request.UserId;
    }
}

public sealed record ValidationRequestRequest(
    string DocumentId,
    int UserId);
