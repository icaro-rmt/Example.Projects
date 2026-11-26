namespace Project.Api.Models;

public class ValidationRequest : IAuditing
{
    public long Id { get; set; }
    public string DocumentId { get; set; } = null!;
    public DateTime CreationDate { get; set; }
    public DateTime UpdateDate { get; set; }
    public int CreationUserId { get; set; }
    public int UpdateUser { get; set; }
}
