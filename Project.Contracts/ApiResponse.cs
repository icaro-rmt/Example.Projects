namespace NB.Omc.Cubo.Contracts;

public sealed class ApiResponse<T>
{
	public ApiMetadata Meta { get; init; } = new();

	public bool Success { get; init; }

	public IReadOnlyCollection<ApiError> Errors { get; init; } = [];

	public T? Data { get; init; }
}

public sealed class ApiMetadata
{
	public string ApplicationID { get; set; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;

	public DateTimeOffset Timestamp { get; init; }

	public string Version { get; init; } = ApiResponseFactory.Version;
}

public sealed class ApiError
{
	public string Type { get; init; } = string.Empty;

	public string Code { get; init; } = string.Empty;

	public string Message { get; init; } = string.Empty;

	public string? Field { get; init; }
}

public static class ApiErrorTypes
{
	public const string Warning = "WARNING";
	public const string Business = "BUSINESS";
	public const string Database = "DATABASE";
	public const string Infrastructure = "INFRASTRUCTURE";
	public const string File = "FILE";
	public const string Timeout = "TIMEOUT";
	public const string Broker = "BROKER";
	public const string Technical = "TECHNICAL";
}

public sealed class PaginatedResult<T>
{
	public IReadOnlyCollection<T> Items { get; init; } = [];

	public PaginationMetadata Pagination { get; init; } = new();
}

public sealed class PaginationMetadata
{
	public int Page { get; init; }

	public int PageSize { get; init; }

	public int TotalItems { get; init; }

	public int TotalPages { get; init; }
}
