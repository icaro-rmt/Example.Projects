using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var history = await db.Set<Dictionary<string, object>>("Process_History")
            .Where(entry => EF.Property<int>(entry, "Id") == id)
            .OrderBy(entry => EF.Property<long>(entry, "HistoryId"))
            .Select(entry => new Process
            {
                Id = EF.Property<int>(entry, nameof(Process.Id)),
                ProcessTypeId = EF.Property<int>(entry, nameof(Process.ProcessTypeId)),
                ProcessStatusId = EF.Property<int>(entry, nameof(Process.ProcessStatusId)),
                ClientNIF = EF.Property<string>(entry, nameof(Process.ClientNIF)),
                ClientName = EF.Property<string>(entry, nameof(Process.ClientName)),
                ClientSector = EF.Property<string>(entry, nameof(Process.ClientSector)),
                ClientCountryCode = EF.Property<string>(entry, nameof(Process.ClientCountryCode)),
                CreationDate = EF.Property<DateTime>(entry, nameof(Process.CreationDate)),
                UpdateDate = EF.Property<DateTime>(entry, nameof(Process.UpdateDate)),
                CreationUserId = EF.Property<int>(entry, nameof(Process.CreationUserId)),
                UpdateUser = EF.Property<int>(entry, nameof(Process.UpdateUser)),
                Comments = EF.Property<string>(entry, nameof(Process.Comments))
            })
            .ToArrayAsync(cancellationToken);

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
