using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Project.Api.Models;

namespace Project.Api.DbContexts
{
    public class DbContextProject(DbContextOptions<DbContextProject> options) : DbContext(options)
    {
        public DbSet<Process> Processes => Set<Process>();
        public DbSet<HistoryProcess> Processes_History => Set<HistoryProcess>();
        public DbSet<ValidationRequest> ValidationRequests => Set<ValidationRequest>();
        public DbSet<ValidationRequestHistory> ValidationRequests_History => Set<ValidationRequestHistory>();

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var snapshots = new List<IAuditHistory>();
            try
            {
                StageAuditHistory(snapshots);
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch
            {
                DetachSnapshots(snapshots);
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshots = new List<IAuditHistory>();
            try
            {
                StageAuditHistory(snapshots);
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            catch
            {
                DetachSnapshots(snapshots);
                throw;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Allocate the final ID before staging the insert snapshot.
            AddTables(modelBuilder);
            AddHistoryTables(modelBuilder);

            ProcessTableEntities(modelBuilder);
        }
        private static void AddTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Process>().Property(process => process.Id).UseHiLo("ProcessIds");
            modelBuilder.Entity<ValidationRequest>().Property(vr => vr.Id).UseHiLo("ValidationRequestId");

        }

        private static void AddHistoryTables(ModelBuilder modelBuilder)
        {
            #region ProcessHistory
            modelBuilder.Entity<HistoryProcess>().ToTable("Process_History");
            modelBuilder.Entity<HistoryProcess>().HasKey(history => history.HistoryId);
            modelBuilder.Entity<HistoryProcess>().Property(history => history.HistoryId)
                .HasColumnName("HistoryProcessId");
            modelBuilder.Entity<HistoryProcess>()
                .HasIndex(history => new { history.ProcessId, history.HistoryId });
            #endregion


            #region ValidationRequestHistory
            modelBuilder.Entity<ValidationRequestHistory>().ToTable("ValidationRequest_History");
            modelBuilder.Entity<ValidationRequestHistory>().HasKey(history => history.HistoryId);
            modelBuilder.Entity<ValidationRequestHistory>()
                .HasIndex(history => new { history.ValidationRequestId, history.HistoryId });
            #endregion
        }

        private static void ProcessTableEntities(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes().ToList())
            {
                var audit = entity.ClrType.GetCustomAttribute<AuditableAttribute>();
                if (audit == null)
                    continue;

                var history = modelBuilder.Entity(audit.HistoryType).Metadata;
                var renamedKey = audit.SourceKeyProperty == null
                    ? null
                    : entity.FindPrimaryKey()!.Properties.Single();

                foreach (var property in entity.GetProperties())
                {
                    var historyName = property == renamedKey ? audit.SourceKeyProperty! : property.Name;
                    if (history.FindProperty(historyName)?.ClrType != property.ClrType)
                        throw new InvalidOperationException(
                            $"{audit.HistoryType.Name} must map {entity.ClrType.Name}.{property.Name} as {historyName}.");
                }
            }
        }

        private void StageAuditHistory(List<IAuditHistory> snapshots)
        {
            var changes = ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var change in changes)
            {
                var audit = change.Metadata.ClrType.GetCustomAttribute<AuditableAttribute>();
                if (audit == null)
                    continue;

                var values = change.State == EntityState.Deleted ? change.OriginalValues : change.CurrentValues;
                var snapshot = audit.CreateHistory();
                snapshots.Add(snapshot);
                var snapshotValues = values.Properties.ToDictionary(property => property.Name, property => values[property]);
                if (audit.SourceKeyProperty is { } sourceKeyProperty)
                {
                    var key = change.Metadata.FindPrimaryKey()!.Properties.Single().Name;
                    snapshotValues.Remove(key, out var sourceId);
                    snapshotValues.Add(sourceKeyProperty, sourceId);
                }
                Entry((object)snapshot).CurrentValues.SetValues(snapshotValues);
                Add((object)snapshot);
            }
        }

        private void DetachSnapshots(List<IAuditHistory> snapshots)
        {
            foreach (var snapshot in snapshots)
                Entry((object)snapshot).State = EntityState.Detached;
        }
    }
}
