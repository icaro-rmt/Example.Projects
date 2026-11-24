namespace Project.Api.Models
{
    public abstract class ValidationRequestFields
    {
        public string DocumentId { get; set; } = null!;
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public int CreationUserId { get; set; }
        public int UpdateUser { get; set; }
    
    }

    [Auditable<ValidationRequestHistory>(SourceKeyProperty = nameof(ValidationRequestHistory.ValidationRequestId))]
    public class ValidationRequest : ValidationRequestFields
    {
        public long Id { get; set; }

    }

    public class ValidationRequestHistory : ValidationRequestFields, IAuditHistory
    {
        public long ValidationRequestId { get; set; }
        public long HistoryId { get ; set; }
    }
   
}
