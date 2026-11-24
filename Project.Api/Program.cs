using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NB.Omc.Cubo.Contracts;
using Project.Api;
using Project.Api.DbContexts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DbContextProject>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProcessDatabase")));

builder.Services.AddApiCorrelation();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API Response Examples",
        Version = "v1"
    });
    options.OperationFilter<UnexpectedErrorExampleFilter>();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Project.Api.xml"));
});
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new UnprocessableEntityObjectResult(
            ApiResponseFactory.FromModelState(context.ModelState));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "Unexpected error while handling the request.");

    var response = ApiResponseFactory.Failure<object?>(
        "INTERNAL_ERROR", "An unexpected error occurred.", ApiErrorTypes.Technical);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.Headers[ApiResponseFactory.CorrelationHeader] = response.Meta.CorrelationId;
    await context.Response.WriteAsJsonAsync(response);
}));

app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("./v1/swagger.json", "API Response Examples v1"));

app.MapControllers();
app.Run();
