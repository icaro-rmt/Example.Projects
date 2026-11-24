using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NB.Omc.Cubo.Contracts;

public static class ApiCorrelationContext
{
	private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

	public static string? Current
	{
		get => CurrentCorrelationId.Value;
		internal set => CurrentCorrelationId.Value = value;
	}
}

public static class ApiCorrelationId
{
	public static string Resolve(HttpContext context)
	{
		if (context.Request.Headers.TryGetValue(ApiResponseFactory.CorrelationHeader, out var headerValue)
			&& !string.IsNullOrWhiteSpace(headerValue))
		{
			return headerValue.ToString().Trim();
		}

		return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
	}
}

public static class ApiApplicationId
{
    public static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(ApiResponseFactory.ApplicationHeader, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim();
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}

public sealed class CorrelationIdMiddleware(
	RequestDelegate next,
	ILogger<CorrelationIdMiddleware> logger)
{
	public async Task InvokeAsync(HttpContext context)
	{
		var correlationId = ApiCorrelationId.Resolve(context);
        var applicationId = ApiApplicationId.Resolve(context);
		var previousCorrelationId = ApiCorrelationContext.Current;
		ApiCorrelationContext.Current = correlationId;
		context.Response.Headers[ApiResponseFactory.CorrelationHeader] = correlationId;
		context.Response.Headers[ApiResponseFactory.ApplicationHeader] = applicationId;

		using var loggingScope = logger.BeginScope(new Dictionary<string, object>
		{
			["CorrelationId"] = correlationId,
            ["ApplicationId"] = applicationId
        });

		try
		{
			await next(context);
		}
		finally
		{
			ApiCorrelationContext.Current = previousCorrelationId;
		}
	}
}

public sealed class CorrelationIdStartupFilter : IStartupFilter
{
	public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
	{
		return app =>
		{
			app.UseMiddleware<CorrelationIdMiddleware>();
			next(app);
		};
	}
}

public static class CorrelationServiceCollectionExtensions
{
	public static IServiceCollection AddApiCorrelation(this IServiceCollection services)
	{
		services.AddSingleton<IStartupFilter, CorrelationIdStartupFilter>();
		return services;
	}
}
