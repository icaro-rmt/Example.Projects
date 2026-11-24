using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NB.Omc.Cubo.Contracts;

public static class ApiResponseFactory
{
	public const string Version = "1.0";
	public const string CorrelationHeader = "X-Correlation-Id";
	public const string ApplicationHeader = "X-App-Id";

    public static ApiResponse<T> Success<T>(T? data, string? correlationId = null)
	{
		return new ApiResponse<T>
		{
			Meta = CreateMetadata(correlationId),
			Success = true,
			Data = data
		};
	}

	public static ApiResponse<T> Failure<T>(
		string code,
		string message,
		string type = ApiErrorTypes.Business,
		string? field = null,
		string? correlationId = null)
	{
		return Failure<T>(
			new[]
			{
				new ApiError
				{
					Type = type,
					Code = code,
					Message = message,
					Field = field
				}
			},
			correlationId);
	}

	public static ApiResponse<T> Failure<T>(
		IEnumerable<ApiError> errors,
		string? correlationId = null)
	{
		var errorList = errors.ToArray();
		if (errorList.Length == 0)
		{
			throw new ArgumentException("At least one API error is required.", nameof(errors));
		}

		return new ApiResponse<T>
		{
			Meta = CreateMetadata(correlationId),
			Success = false,
			Errors = errorList,
			Data = default
		};
	}

	public static ApiResponse<object?> FromModelState(ModelStateDictionary modelState)
	{
		var errors = modelState
			.SelectMany(pair => pair.Value?.Errors.Select(error => new ApiError
			{
				Type = ApiErrorTypes.Business,
				Code = "INVALID_FIELD",
				Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
					? "The supplied value is invalid."
					: error.ErrorMessage,
				Field = pair.Key
			}) ?? Enumerable.Empty<ApiError>())
			.ToArray();

		return Failure<object?>(errors.Length == 0
			? new[]
			{
				new ApiError
				{
					Type = ApiErrorTypes.Business,
					Code = "INVALID_REQUEST",
					Message = "The request is invalid."
				}
			}
			: errors);
	}

	private static ApiMetadata CreateMetadata(string? correlationId)
	{
		return new ApiMetadata
		{
			CorrelationId = correlationId
				?? ApiCorrelationContext.Current
				?? Activity.Current?.TraceId.ToString()
				?? Guid.NewGuid().ToString("N"),
			Timestamp = DateTimeOffset.UtcNow
		};
	}
}
