using System.Text.Json;
using Microsoft.OpenApi;
using NB.Omc.Cubo.Contracts;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Project.Api;

internal sealed class UnexpectedErrorExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ExamplesController)
            || context.MethodInfo.Name != nameof(ExamplesController.GetUnexpectedError))
        {
            return;
        }

        var example = ApiResponseFactory.Failure<object?>(
            "INTERNAL_ERROR",
            "An unexpected error occurred.",
            ApiErrorTypes.Technical,
            correlationId: "demo-error-123");

        operation.Responses!["500"].Content!["application/json"].Example =
            JsonSerializer.SerializeToNode(example, JsonSerializerOptions.Web);
    }
}
