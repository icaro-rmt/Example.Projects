namespace Project.Api.Models
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AuditableAttribute<THistory> : AuditableAttribute
    where THistory : class, IAuditHistory, new()
    {
        public override Type HistoryType => typeof(THistory);

        public override IAuditHistory CreateHistory() => new THistory();
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public abstract class AuditableAttribute : Attribute
    {
        public abstract Type HistoryType { get; }

        public abstract IAuditHistory CreateHistory();

        public string? SourceKeyProperty { get; set; }
    }

    public interface IAuditHistory
    {
        public long HistoryId{ get; set; }
    }
}
