using System.Text;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;

using Stockroom.Api.Middleware;
using Stockroom.Api.Swagger;
using Stockroom.Application;
using Stockroom.Application.Options;
using Stockroom.Infrastructure;
using Stockroom.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var jwt = JwtOptions.Read(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, jwt);
builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddStockroomSwagger();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins("http://localhost:5280", "https://localhost:5280")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
await DatabaseInitializer.ApplyAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stockroom API");
    options.DocumentTitle = "Stockroom API";
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", (IConfiguration configuration) => Results.Ok(new
{
    status = "ok",
    provider = configuration["DatabaseProvider"] ?? DatabaseProviders.Sqlite
}));
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
