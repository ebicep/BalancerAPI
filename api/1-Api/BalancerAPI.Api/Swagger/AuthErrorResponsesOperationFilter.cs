using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BalancerAPI.Api.Swagger;

internal sealed class AuthErrorResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized — invalid or missing API key.",
            Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema }
            }
        });

        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden — authenticated client lacks permission for this operation.",
            Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema }
            }
        });
    }
}
