using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Stockroom.Domain.Enums;

namespace Stockroom.Api.Swagger;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddStockroomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Stockroom API",
                Version = "v1",
                Description = "Portfolio demo API for inventory and orders. Authenticate with POST /api/auth/login, then send the access token as a Bearer token."
            });
            options.MapType<OrderStatus>(() => StringEnum<OrderStatus>());
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token returned by POST /api/auth/login."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static OpenApiSchema StringEnum<TEnum>()
        where TEnum : struct, Enum
    {
        return new OpenApiSchema
        {
            Type = "string",
            Enum = Enum.GetNames<TEnum>().Select(name => (IOpenApiAny)new OpenApiString(name)).ToList()
        };
    }
}
