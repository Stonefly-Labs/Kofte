using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Kofte.Host.Features.Bootstrap;

public static class BootstrapOpenApi
{
    public static void AddBootstrapOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddOperationTransformer((operation, context, token) =>
            {
                if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                    throw new InvalidOperationException("bootstrap_openapi_access_invalid");
                operation.Security = [];
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer((document, context, token) =>
            {
                document.Info = new()
                {
                    Title = "Köfte Bootstrap API",
                    Version = "1.0.0",
                    Description = "Local-only, non-sensitive bootstrap contract. No identity, domain operation or module/MCP interface."
                };
                var config = context.ApplicationServices.GetRequiredService<BootstrapConfiguration>();
                document.Servers = [new() { Url = config.BackendOrigin.GetLeftPart(UriPartial.Authority) }];
                document.Security = [];
                document.Tags = null;
                var components = document.Components ??= new();
                if (components.Schemas is null || !components.Schemas.TryGetValue("BootstrapStatus", out var status))
                    throw new InvalidOperationException("bootstrap_openapi_status_missing");
                components.Schemas.Remove("BootstrapStatus");
                components.Schemas["CorrelationId"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Pattern = "^[0-9a-f]{32}$"
                };
                foreach (var (name, state, component) in new[]
                {
                    ("LiveStatus", "live", "host"), ("ReadyStatus", "ready", "storage"),
                    ("UnavailableStatus", "unavailable", "")
                })
                {
                    var properties = new Dictionary<string, IOpenApiSchema>(status.Properties ??
                        throw new InvalidOperationException("bootstrap_openapi_status_missing"));
                    var schema = new OpenApiSchema
                    {
                        Type = status.Type,
                        Required = new HashSet<string> { "version", "state", "component", "correlationId" },
                        AdditionalPropertiesAllowed = false,
                        Properties = properties
                    };
                    properties["version"] = Literal("1");
                    properties["state"] = Literal(state);
                    properties["component"] = component.Length != 0 ? Literal(component) : new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = [JsonValue.Create("host"), JsonValue.Create("storage")]
                    };
                    properties["correlationId"] = new OpenApiSchemaReference("CorrelationId", document);
                    components.Schemas[name] = schema;
                }
                components.Headers = new Dictionary<string, IOpenApiHeader>
                {
                    ["NoStore"] = new OpenApiHeader
                    {
                        Required = true,
                        Description = "Do not cache diagnostic responses",
                        Schema = Literal("no-store")
                    }
                };
                components.Responses = new Dictionary<string, IOpenApiResponse>
                {
                    ["InvalidHealthRequest"] = new OpenApiResponse
                    {
                        Description = "Unsupported query or request body; empty sanitized response",
                        Headers = NoStore(document)
                    }
                };
                foreach (var path in document.Paths.Values)
                {
                    if (path.Operations is null) throw new InvalidOperationException("bootstrap_openapi_operation_missing");
                    foreach (var operation in path.Operations.Values)
                    {
                        if (operation.Responses is null) throw new InvalidOperationException("bootstrap_openapi_response_missing");
                        operation.Tags?.Clear();
                        var live = operation.OperationId switch
                        {
                            "bootstrapLiveness" => true,
                            "bootstrapReadiness" => false,
                            _ => throw new InvalidOperationException("bootstrap_openapi_operation_invalid")
                        };
                        operation.Responses["200"] = StatusResponse(document, live ? "LiveStatus" : "ReadyStatus",
                            live ? "Process can serve the liveness request" : "Configured platform store is usable");
                        if (!live) operation.Responses["503"] = StatusResponse(document, "UnavailableStatus",
                            "Startup incomplete or platform store unavailable; no provider fallback");
                        operation.Responses["400"] = new OpenApiResponseReference("InvalidHealthRequest", document);
                        operation.Responses["405"] = new OpenApiResponse
                        {
                            Description = "Unsupported method; empty response body",
                            Headers = NoStore(document)
                        };
                    }
                }
                document.Paths["/openapi/v1.json"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new()
                        {
                            OperationId = "bootstrapOpenApi",
                            Summary = "Fetch this versioned contract in Development only",
                            Security = [],
                            Responses = new()
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Description = "OpenAPI description, available only in Development",
                                    Headers = NoStore(document),
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        ["application/json"] = new()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Object,
                                                Required = new HashSet<string> { "openapi", "info", "paths" },
                                                Properties = new Dictionary<string, IOpenApiSchema>
                                                {
                                                    ["openapi"] = Literal("3.1.1"),
                                                    ["info"] = new OpenApiSchema
                                                    {
                                                        Type = JsonSchemaType.Object,
                                                        Required = new HashSet<string> { "title", "version" },
                                                        Properties = new Dictionary<string, IOpenApiSchema>
                                                        {
                                                            ["title"] = Literal("Köfte Bootstrap API"),
                                                            ["version"] = Literal("1.0.0")
                                                        }
                                                    },
                                                    ["paths"] = new OpenApiSchema { Type = JsonSchemaType.Object }
                                                }
                                            }
                                        }
                                    }
                                },
                                ["400"] = new OpenApiResponseReference("InvalidHealthRequest", document),
                                ["404"] = new OpenApiResponse
                                {
                                    Description = "Documentation route absent outside Development; empty response body"
                                },
                                ["405"] = new OpenApiResponse
                                {
                                    Description = "Unsupported method; empty response body",
                                    Headers = NoStore(document)
                                }
                            }
                        }
                    }
                };
                return Task.CompletedTask;
            });
        });
    }

    private static OpenApiSchema Literal(string value) => new()
    {
        Type = JsonSchemaType.String,
        Const = value
    };

    private static Dictionary<string, IOpenApiHeader> NoStore(OpenApiDocument document) => new()
    {
        ["Cache-Control"] = new OpenApiHeaderReference("NoStore", document)
    };

    private static OpenApiResponse StatusResponse(OpenApiDocument document, string schema, string description) => new()
    {
        Description = description,
        Headers = NoStore(document),
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new() { Schema = new OpenApiSchemaReference(schema, document) }
        }
    };
}
