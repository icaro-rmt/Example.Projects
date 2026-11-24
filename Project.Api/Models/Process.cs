using Project.Api.Models;

namespace Project.Api.Models
{
    public abstract class ProcessFields
    {
        public int ProcessTypeId { get; set; }
        public int ProcessStatusId { get; set; }
        public string ClientNIF { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public string ClientSector { get; set; } = null!;
        public string ClientCountryCode { get; set; } = null!;
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public int CreationUserId { get; set; }
        public int UpdateUser { get; set; }
        public string Comments { get; set; } = null!;
    }

    [Auditable<HistoryProcess>(SourceKeyProperty = nameof(HistoryProcess.ProcessId))]
    public class Process : ProcessFields
    {
        public int Id { get; set; }
    }
    public class HistoryProcess : ProcessFields, IAuditHistory
    {
        public long HistoryId { get; set; }
        public int ProcessId { get; set; }
    }
}

