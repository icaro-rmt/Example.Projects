using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Project.Api.Models;

namespace Project.Api.DbContexts;

public class DbContextProject(DbContextOptions<DbContextProject> options) : DbContext(options)
{
    internal static string HistoryTableNameBuilder(string entityName) => entityName + "_History";
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<ValidationRequest> ValidationRequests => Set<ValidationRequest>();

    public Task<TEntity[]> GetHistoryAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class, IAuditing, new()
    {
        var source = Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not mapped.");
        var table = StoreObjectIdentifier.Table(source.GetTableName()!, source.GetSchema());
        var key = source.FindPrimaryKey()!.Properties.Single();
        if (key.ClrType != typeof(TKey))
            throw new ArgumentException($"The history key for {source.ClrType.Name} must be {key.ClrType.Name}.", nameof(id));

        var keyColumn = key.GetColumnName(table)!;
        return Set<Dictionary<string, object>>(HistoryTableNameBuilder(source.ClrType.Name))
            .AsNoTracking()
            .Where(row => object.Equals(EF.Property<TKey>(row, keyColumn), id))
            .OrderBy(row => EF.Property<long>(row, "HistoryId"))
            .Select(CreateHistoryProjection<TEntity>(source, table))
            .ToArrayAsync(cancellationToken);
    }

    private static Expression<Func<Dictionary<string, object>, TEntity>> CreateHistoryProjection<TEntity>(
        IEntityType source, StoreObjectIdentifier table) where TEntity : new()
    {
        var row = Expression.Parameter(typeof(Dictionary<string, object>), "row");
        var bindings = new List<MemberBinding>();

        foreach (var property in source.GetProperties())
        {
            if (property.GetColumnName(table) is not { } column)
                continue;

            var member = property.PropertyInfo?.SetMethod is not null
                ? (MemberInfo)property.PropertyInfo
                : property.FieldInfo;
            if (member is null || member is FieldInfo { IsInitOnly: true })
                continue;

            var value = Expression.Call(typeof(EF), nameof(EF.Property), [property.ClrType],
                row, Expression.Constant(column));
            bindings.Add(Expression.Bind(member, value));
        }

        return Expression.Lambda<Func<Dictionary<string, object>, TEntity>>(
            Expression.MemberInit(Expression.New(typeof(TEntity)), bindings), row);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Process>().Property(process => process.Id).UseHiLo("ProcessIds");
        modelBuilder.Entity<ValidationRequest>().Property(request => request.Id).UseHiLo("ValidationRequestId");

        var sources = modelBuilder.Model.GetEntityTypes()
            .Where(entity => typeof(IAuditing).IsAssignableFrom(entity.ClrType))
            .ToArray();

        foreach (var source in sources)
        {
            var entityName = source.ClrType.Name;
            var tableName = HistoryTableNameBuilder(entityName);
            var sourceTable = StoreObjectIdentifier.Table(source.GetTableName()!, source.GetSchema());
            var key = source.FindPrimaryKey()!.Properties.Single();
            var parentId = key.GetColumnName(sourceTable)!;

            var history = modelBuilder.SharedTypeEntity<Dictionary<string, object>>(tableName)
                .ToTable(tableName, source.GetSchema());

            foreach (var property in source.GetProperties())
            {
                var column = property.GetColumnName(sourceTable);
                if (column is null)
                    continue;

                history.IndexerProperty(property.ClrType, column).IsRequired(!property.IsNullable);
            }

            history.IndexerProperty<long>("HistoryId").UseIdentityByDefaultColumn();
            history.HasKey("HistoryId");
            history.IndexerProperty<DateTime>("HistoryCreationDate");
            history.HasIndex(parentId, "HistoryId");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var snapshots = new List<EntityEntry>();
        try
        {
            StageHistory(snapshots);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch
        {
            DetachHistory(snapshots);
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshots = new List<EntityEntry>();
        try
        {
            StageHistory(snapshots);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            DetachHistory(snapshots);
            throw;
        }
    }

    private void StageHistory(List<EntityEntry> snapshots)
    {
        var changes = ChangeTracker.Entries<IAuditing>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        var stagedAt = DateTime.UtcNow;

        foreach (var change in changes)
        {
            var source = change.Metadata;
            var sourceTable = StoreObjectIdentifier.Table(source.GetTableName()!, source.GetSchema());
            var key = source.FindPrimaryKey()!.Properties.Single();
            if (change.Property(key.Name).IsTemporary)
                throw new InvalidOperationException($"Audit history requires a final key for {source.ClrType.Name}.");

            var values = change.State == EntityState.Deleted ? change.OriginalValues : change.CurrentValues;
            var snapshot = new Dictionary<string, object>
            {
                ["HistoryCreationDate"] = stagedAt
            };
            foreach (var property in values.Properties)
            {
                if (property.GetColumnName(sourceTable) is { } column)
                    snapshot.Add(column, values[property]!);
            }

            snapshots.Add(Set<Dictionary<string, object>>(HistoryTableNameBuilder(source.ClrType.Name)).Add(snapshot));
        }
    }

    private static void DetachHistory(List<EntityEntry> snapshots)
    {
        foreach (var snapshot in snapshots)
            snapshot.State = EntityState.Detached;
    }
}
