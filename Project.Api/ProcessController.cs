using Microsoft.AspNetCore.Mvc;
using NB.Omc.Cubo.Contracts;
using Project.Api.DbContexts;
using Project.Api.Models;

namespace Project.Api;

[ApiController]
[Route("api/processes")]
public sealed class ProcessController(DbContextProject db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<Process>>> Get(int id, CancellationToken cancellationToken)
    {
        var process = await db.Processes.FindAsync([id], cancellationToken);
        if (process is null)
            return ProcessNotFound();

        return Ok(ApiResponseFactory.Success(process));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Process>>> Create(
        ProcessRequest request, CancellationToken cancellationToken)
    {
        var process = new Process
        {
            CreationDate = DateTime.UtcNow,
            CreationUserId = request.UserId
        };
        ApplyChanges(process, request, process.CreationDate);

        await db.Processes.AddAsync(process, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = process.Id }, ApiResponseFactory.Success(process));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<Process>>> Update(
        int id, ProcessRequest request, CancellationToken cancellationToken)
    {
        var process = await db.Processes.FindAsync([id], cancellationToken);
        if (process is null)
            return ProcessNotFound();

        ApplyChanges(process, request, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(process));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var process = await db.Processes.FindAsync([id], cancellationToken);
        if (process is null)
            return ProcessNotFound();

        db.Processes.Remove(process);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<Process[]>>> GetHistory(
        int id, CancellationToken cancellationToken)
    {
        var history = await db.GetHistoryAsync<Process, int>(id, cancellationToken);

        return Ok(ApiResponseFactory.Success(history));
    }

    private NotFoundObjectResult ProcessNotFound() => NotFound(
        ApiResponseFactory.Failure<Process>("PROCESS_NOT_FOUND", "The process was not found."));

    private static void ApplyChanges(Process process, ProcessRequest request, DateTime updatedAt)
    {
        process.ProcessTypeId = request.ProcessTypeId;
        process.ProcessStatusId = request.ProcessStatusId;
        process.ClientNIF = request.ClientNIF;
        process.ClientName = request.ClientName;
        process.ClientSector = request.ClientSector;
        process.ClientCountryCode = request.ClientCountryCode;
        process.Comments = request.Comments;
        process.UpdateDate = updatedAt;
        process.UpdateUser = request.UserId;
    }
}

public sealed record ProcessRequest(
    int ProcessTypeId,
    int ProcessStatusId,
    string ClientNIF,
    string ClientName,
    string ClientSector,
    string ClientCountryCode,
    string Comments,
    int UserId);
