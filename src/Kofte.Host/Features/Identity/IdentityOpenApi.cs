using System.Text.Json.Nodes;
using Kofte.Platform.Identity;
using Microsoft.OpenApi;

namespace Kofte.Host.Features.Identity;

internal static class IdentityOpenApi
{
    internal static bool IsOperation(string? id) =>
        id is "identityBrowserContext" or "identityLogin" or "identityCallback" or "identityCurrent" or
            "identityEnrollFirstAdministrator";

    internal static void Configure(OpenApiDocument document, IdentityConfiguration configuration)
    {
        var components = document.Components ??= new();
        var schemas = components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        schemas["BrowserContextV1"] = Object(new()
        {
            ["version"] = Literal("1"),
            ["identityMode"] = Mode(),
            ["csrfToken"] = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 }
        });
        schemas["CurrentIdentityV1"] = Object(new()
        {
            ["version"] = Literal("1"),
            ["userId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid",
                Pattern = "^(?!00000000-0000-0000-0000-000000000000$)[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$"
            },
            ["displayName"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, MaxLength = 128 },
            ["identityMode"] = Mode(),
            ["isInitialAdministrator"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
        });
        schemas["IdentityProblemV1"] = Object(new()
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Pattern = "^urn:kofte:problem:identity_" },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
            ["code"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = new[] { "identity_request_invalid", "identity_unauthenticated", "identity_forbidden",
                    "identity_enrollment_unavailable", "identity_admission_exceeded", "identity_unavailable",
                    "identity_outcome_uncertain" }.Select(value => (JsonNode)JsonValue.Create(value)!).ToList()
            },
            ["correlationId"] = new OpenApiSchemaReference("CorrelationId", document)
        });
        schemas["EnrollmentResultV1"] = Object(new()
        {
            ["version"] = Literal("1"),
            ["status"] = Literal("enrolled"),
            ["correlationId"] = new OpenApiSchemaReference("CorrelationId", document)
        });
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["kofteSession"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = configuration.Mode == IdentityMode.Oidc ? "__Host-kofte-session" : "kofte-dev-session",
            Description = "Opaque host session cookie; no Authorization or bearer-token credential."
        };
        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null) throw new InvalidOperationException("identity_openapi_operation_missing");
            foreach (var operation in path.Operations.Values)
            {
                if (!IsOperation(operation.OperationId)) continue;
                operation.Tags?.Clear();
                operation.Security = operation.OperationId is "identityCurrent" or "identityEnrollFirstAdministrator"
                    ? [new() { [new OpenApiSecuritySchemeReference("kofteSession", document)] = [] }]
                    : [];
                operation.Responses = new();
                foreach (var status in new[] { 400, 401, 403, 429, 503 })
                    operation.Responses[status.ToString(System.Globalization.CultureInfo.InvariantCulture)] =
                        Response(document, "IdentityProblemV1", "Fixed sanitized identity failure", "application/problem+json");
                if (operation.OperationId is "identityBrowserContext" or "identityCurrent")
                    operation.Responses["200"] = Response(document,
                        operation.OperationId == "identityCurrent" ? "CurrentIdentityV1" : "BrowserContextV1",
                        "Verified self identity or public browser continuity");
                else if (operation.OperationId == "identityEnrollFirstAdministrator")
                {
                    operation.Responses["200"] = Response(document, "EnrollmentResultV1",
                        "Known committed enrollment; retrieve current identity to verify designation");
                    operation.Responses["409"] = Response(document, "IdentityProblemV1",
                        "Enrollment unavailable without disclosing nominee or ceremony history", "application/problem+json");
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new()
                            {
                                Schema = Object(new()
                                {
                                    ["authorization"] = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String,
                                        MinLength = 43,
                                        MaxLength = 43,
                                        Pattern = "^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$",
                                        WriteOnly = true
                                    }
                                })
                            }
                        }
                    };
                    operation.Parameters =
                    [
                        new OpenApiParameter
                        {
                            Name = "Origin", In = ParameterLocation.Header, Required = true,
                            Description = "Exact configured public origin",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                        },
                        new OpenApiParameter
                        {
                            Name = "X-Kofte-CSRF", In = ParameterLocation.Header, Required = true,
                            Description = "Fresh identity-bound antiforgery request token with its paired cookie",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    ];
                }
                else
                {
                    operation.Responses["303"] = new OpenApiResponse
                    {
                        Description = operation.OperationId == "identityLogin"
                            ? "OIDC-mode challenge to the configured authorization endpoint"
                            : "Known committed login; admitted local return only",
                        Headers = Headers(document)
                    };
                    if (operation.OperationId == "identityLogin")
                        operation.RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/x-www-form-urlencoded"] = new()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        AdditionalPropertiesAllowed = false,
                                        Properties = new Dictionary<string, IOpenApiSchema>
                                        {
                                            ["returnPath"] = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.String,
                                                Enum = [JsonValue.Create("/"), JsonValue.Create("/identity")],
                                                Default = JsonValue.Create("/identity")
                                            },
                                            ["__RequestVerificationToken"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                        }
                                    }
                                }
                            }
                        };
                }
            }
        }
    }

    private static OpenApiSchema Object(Dictionary<string, IOpenApiSchema> properties) => new()
    {
        Type = JsonSchemaType.Object,
        AdditionalPropertiesAllowed = false,
        Required = properties.Keys.ToHashSet(StringComparer.Ordinal),
        Properties = properties
    };

    private static OpenApiSchema Literal(string value) => new() { Type = JsonSchemaType.String, Const = value };
    private static OpenApiSchema Mode() => new()
    {
        Type = JsonSchemaType.String,
        Enum = [JsonValue.Create("oidc"), JsonValue.Create("development")]
    };

    private static Dictionary<string, IOpenApiHeader> Headers(OpenApiDocument document) => new()
    {
        ["Cache-Control"] = new OpenApiHeaderReference("NoStore", document),
        ["Pragma"] = new OpenApiHeader { Required = true, Schema = Literal("no-cache") },
        ["Referrer-Policy"] = new OpenApiHeader { Required = true, Schema = Literal("no-referrer") }
    };

    private static OpenApiResponse Response(OpenApiDocument document, string schema, string description,
        string mediaType = "application/json") => new()
        {
            Description = description,
            Headers = Headers(document),
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [mediaType] = new() { Schema = new OpenApiSchemaReference(schema, document) }
            }
        };
}
