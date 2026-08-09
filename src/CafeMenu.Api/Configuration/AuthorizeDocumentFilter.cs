using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CafeMenu.Api.Configuration;

public sealed class AuthorizeDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Paths is null)
        {
            return;
        }

        foreach (var apiDescription in context.ApiDescriptions)
        {
            if (AllowsAnonymous(apiDescription) || !RequiresAuthorization(apiDescription))
            {
                continue;
            }

            var path = GetSwaggerPath(apiDescription);
            if (path is null || !swaggerDoc.Paths.TryGetValue(path, out var pathItem))
            {
                continue;
            }

            if (pathItem.Operations is null)
            {
                continue;
            }

            var httpMethod = GetHttpMethod(apiDescription);
            if (httpMethod is null || !pathItem.Operations.TryGetValue(httpMethod, out var operation))
            {
                continue;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(CreateBearerSecurityRequirement(swaggerDoc));
        }
    }

    private static bool AllowsAnonymous(ApiDescription apiDescription)
    {
        return apiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
    }

    private static bool RequiresAuthorization(ApiDescription apiDescription)
    {
        return apiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
    }

    private static string? GetSwaggerPath(ApiDescription apiDescription)
    {
        if (string.IsNullOrWhiteSpace(apiDescription.RelativePath))
        {
            return null;
        }

        var relativePath = apiDescription.RelativePath;
        var queryStringStart = relativePath.IndexOf('?', StringComparison.Ordinal);
        if (queryStringStart >= 0)
        {
            relativePath = relativePath[..queryStringStart];
        }

        return $"/{relativePath.TrimStart('/')}";
    }

    private static HttpMethod? GetHttpMethod(ApiDescription apiDescription)
    {
        return string.IsNullOrWhiteSpace(apiDescription.HttpMethod)
            ? null
            : new HttpMethod(apiDescription.HttpMethod);
    }

    private static OpenApiSecurityRequirement CreateBearerSecurityRequirement(OpenApiDocument swaggerDoc)
    {
        return new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    JwtBearerDefaults.AuthenticationScheme,
                    swaggerDoc,
                    externalResource: null)
            ] = new List<string>()
        };
    }
}
