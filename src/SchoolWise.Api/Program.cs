using System.Data.Common;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SchoolWise.Api.Infrastructure;
using SchoolWise.Application;
using SchoolWise.Infrastructure;
using SchoolWise.Infrastructure.Persistence;
using SchoolWise.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

SetConfigurationAlias("DATABASE_URL", "ConnectionStrings:Default");
SetConfigurationAlias("JWT_SECRET", "Jwt:Secret");
SetConfigurationAlias("JWT_ISSUER", "Jwt:Issuer");
SetConfigurationAlias("JWT_AUDIENCE", "Jwt:Audience");
SetConfigurationAlias("FRONTEND_ORIGINS", "Frontend:Origins");

builder.Host.UseSerilog((_, logger) => logger
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication(builder.Configuration["AutoMapper:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SchoolWise.Application.Common.ICurrentUser, CurrentUser>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
if (builder.Environment.IsProduction()
    && (string.IsNullOrWhiteSpace(builder.Configuration["JWT_ISSUER"])
        || string.IsNullOrWhiteSpace(builder.Configuration["JWT_AUDIENCE"])))
{
    throw new InvalidOperationException("JWT_ISSUER and JWT_AUDIENCE must be explicitly configured in Production.");
}
if (Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");
}
if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
});

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "600";
        await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests. Try again in ten minutes.")
            .ExecuteAsync(context.HttpContext);
    };
    options.AddPolicy("verification-redeem", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("verification-issue", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("review-write", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var frontendOriginsSetting = builder.Configuration["Frontend:Origins"];
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(frontendOriginsSetting))
{
    throw new InvalidOperationException("FRONTEND_ORIGINS must be explicitly configured in Production.");
}

var frontendOrigins = (frontendOriginsSetting
        ?? builder.Configuration["Frontend:Origin"]
        ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (frontendOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one Frontend:Origins value must be configured.");
}
if (builder.Environment.IsProduction()
    && frontendOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || uri.Scheme != Uri.UriSchemeHttps
        || !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Every Production frontend origin must be an HTTPS origin without a path.");
}
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SchoolWise API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        if (app.Environment.IsProduction())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000";
        }

        return Task.CompletedTask;
    });
    await next();
});

if (args.Contains("--migrate-only", StringComparer.Ordinal))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "SchoolWise API v1"));
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/ready", async Task<IResult> (
    SchoolWiseDbContext db,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (DbException exception)
    {
        logger.LogWarning(exception, "Database readiness check failed.");
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    if (app.Environment.IsDevelopment())
    {
        await initializer.SeedDevelopmentDataAsync();
    }
    else
    {
        await initializer.SeedInitialAdminAsync();
    }
}

app.Run();

void SetConfigurationAlias(string environmentVariable, string configurationKey)
{
    var value = builder.Configuration[environmentVariable];
    if (!string.IsNullOrWhiteSpace(value))
    {
        builder.Configuration[configurationKey] = value;
    }
}

public partial class Program { }
