# Automatic audit history with EF Core

`DbContextProject.SaveChangesAsync` stages one history row for each added, modified, or deleted entity marked with `AuditableAttribute`. The history row stores a snapshot of mapped scalar values, including any timestamp or user fields the caller has populated. The override copies those fields as-is. If you need to identify the operation or the actor who performed a deletion, extend the history model and populate that metadata explicitly.

## How a save works

1. `SaveChangesAsync` checks cancellation and calls `StageAuditHistory` before the base EF Core save. The synchronous `SaveChanges` override uses the same staging method.
2. `StageAuditHistory` collects tracked entries in the `Added`, `Modified`, and `Deleted` states, then looks for the audit attribute on each entity type. Each audited entry produces one snapshot per save call.
3. For inserts and updates, the snapshot uses `CurrentValues`. For deletes, it uses `OriginalValues`, the values EF recorded when tracking began or last accepted changes.
4. The attribute creates a history instance. If `SourceKeyProperty` is set, the staging code copies the source primary key to that named history property. The context marks the history instance as `Added`.
5. The base save persists both sets of changes in the same call. If staging or saving throws, the override detaches the history instances it staged and rethrows. A subsequent save can stage new snapshots from the source entries still pending.

The audit hook requires tracked entity changes and a call to one of the context's save methods. `ExecuteUpdateAsync`, `ExecuteDeleteAsync`, and direct SQL bypass the hook. The history table needs its own generated key because a source row can produce multiple snapshots.

Implementation lives in [DbContextProject.cs](DbContexts/DbContextProject.cs) and [Auditing.cs](Models/Auditing.cs).

## Add a new auditable entity

Use [ValidationRequest.cs](Models/ValidationRequest.cs) as a working example. The example below defines a separate history entity for a new `WorkItem` model.

```csharp
public abstract class WorkItemFields
{
    public string Title { get; set; } = null!;
}

[Auditable<WorkItemHistory>(SourceKeyProperty = nameof(WorkItemHistory.WorkItemId))]
public class WorkItem : WorkItemFields
{
    public int Id { get; set; }
}

public class WorkItemHistory : WorkItemFields, IAuditHistory
{
    public long HistoryId { get; set; }
    public int WorkItemId { get; set; }
}
```

1. Put both model classes in `Project.Api.Models`. The history class must implement `IAuditHistory` and have a public parameterless constructor. A shared base class helps keep mapped fields in sync. Every mapped source property needs a history property with the same CLR type and name, except the primary key when renamed through `SourceKeyProperty`. That option expects a single-property primary key.
2. Add `DbSet<WorkItem> WorkItems => Set<WorkItem>()` and `DbSet<WorkItemHistory> WorkItems_History => Set<WorkItemHistory>()` to the context.
3. Add the mappings below to `AddTables` and `AddHistoryTables` respectively. Assign the final source key before staging history. This project uses PostgreSQL HiLo for that, with a named sequence per source entity. On the async write path, await `AddAsync` before calling `SaveChangesAsync`.

   ```csharp
   // AddTables
   modelBuilder.Entity<WorkItem>().Property(item => item.Id).UseHiLo("WorkItemIds");

   // AddHistoryTables
   var history = modelBuilder.Entity<WorkItemHistory>();
   history.HasKey(item => item.HistoryId);
   history.HasIndex(item => new { item.WorkItemId, item.HistoryId });
   ```

4. Run `dotnet ef dbcontext info` from the API project to check design-time model creation. Scaffold the migration with `dotnet ef migrations add AddWorkItemAudit`. Review the generated migration for the source table, HiLo sequence, history table, and history index. For an existing table, preserve established table and column names through explicit EF mappings where needed. `HistoryProcess.HistoryId`, for example, maps to the existing `HistoryProcessId` column.
5. Apply the reviewed migration to a development database. Insert, update, then delete a sample row, saving after each operation. Query history by the original source ID and order by `HistoryId`. Verify that all three snapshots share the source ID, the update reflects the new values, and the delete captures the values present when the entity was loaded for deletion.

## Debug missing or incorrect history

| Symptom | Where to look |
| --- | --- |
| `<HistoryType> must map <Entity>.<Property> as <Name>.` | Check `ProcessTableEntities` in `OnModelCreating`. Confirm the named history property is mapped and has the exact source CLR type. An `int` source key requires an `int` history reference. Add or map newly introduced source fields on the history model as well. |
| EF reports that the history entity requires a primary key | Verify that `AddHistoryTables` explicitly calls `HasKey` for `HistoryId`. |
| An insert snapshot has the wrong source ID | Break in `StageAuditHistory` and inspect the source key in `CurrentValues` and its `IsTemporary` flag. Confirm the source key is assigned before saving and that the history property named in `SourceKeyProperty` receives it. |
| A save succeeds but no history row appears | Check that the change passes through `DbContextProject.SaveChangesAsync` or `SaveChanges`, the source appears in `ChangeTracker.Entries()` in a supported state, and the concrete entity type has the audit attribute. Then check the history table mapping and the database targeted by the query. |
| A delete snapshot contains defaults or stale values | Inspect `OriginalValues` before saving. Load the full source row in the same context before removing it so the snapshot has the persisted values available. |
| A save throws after staging history | Inspect the original exception for missing migrations, database constraints, or concurrency failures. Check that `DetachSnapshots` detached the newly staged entries. Resolve the underlying failure before retrying the save. |
| A migration proposes unrelated history schema changes | Compare the history table name, primary key column, and index against the previous model snapshot. EF derives the migration from the current model and that snapshot. |

For callers that pass `acceptAllChangesOnSuccess: false`, follow the caller's transaction and change acceptance lifecycle before saving again. Entries can otherwise remain pending and be processed again on a later save.
