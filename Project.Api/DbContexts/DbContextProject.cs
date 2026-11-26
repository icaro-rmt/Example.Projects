using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Project.Api.Models;

namespace Project.Api.DbContexts;

public class DbContextProject(DbContextOptions<DbContextProject> options) : DbContext(options)
{
    internal static string HistoryTableNameBuilder(string entityName) => entityName + "_History";
    internal static string ParentIdColumnNameBuilder(string entityName) => entityName + "Id";
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<ValidationRequest> ValidationRequests => Set<ValidationRequest>();

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
            var parentId = ParentIdColumnNameBuilder(entityName);

            var history = modelBuilder.SharedTypeEntity<Dictionary<string, object>>(tableName)
                .HasNoKey()
                .ToTable(tableName, source.GetSchema());

            foreach (var property in source.GetProperties())
            {
                var column = property == key ? parentId : property.GetColumnName(sourceTable);
                if (column is null)
                    continue;

                history.IndexerProperty(property.ClrType, column).IsRequired(!property.IsNullable);
            }

            var historyId = history.IndexerProperty<long>("HistoryId").UseIdentityByDefaultColumn();
            // Keep the existing physical column name for Process history.
            if (source.ClrType == typeof(Process))
                historyId.HasColumnName("HistoryProcessId");

            history.IndexerProperty<DateTime>("HistoryCreationDate").HasDefaultValueSql("statement_timestamp()");
            history.HasIndex(parentId, "HistoryCreationDate");
        }
    }
}
