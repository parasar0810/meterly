using Meterly.Application;
using Meterly.Infrastructure;
using Meterly.Api.Middleware;
using Meterly.Infrastructure.Jobs;

using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// ============================================================
// Program.cs — The application entry point
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICES (Dependency Injection registration)
// ============================================================

// Allow services (like CurrentUserService) to read the HTTP Context
builder.Services.AddHttpContextAccessor();

// Our application layer: MediatR + FluentValidation
builder.Services.AddApplication();

// Our infrastructure layer: EF Core + PostgreSQL + Auth Services
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] 
                     ?? throw new InvalidOperationException("JWT Secret is not configured.");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Meterly",
            ValidAudience = "MeterlyClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

// ASP.NET Core controller support
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Meterly API",
        Version = "v1",
        Description = "Multi-tenant SaaS billing and subscription platform API"
    });

    // Configure Swagger to send JWT token
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {token}"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// BUILD THE APP
// ============================================================

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Meterly API v1");
        options.RoutePrefix = "swagger"; 
    });
}

app.UseHttpsRedirection();

// Authentication + Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Routes requests to the appropriate controller action
app.MapControllers();

// ============================================================
// Phase 12: Hangfire Dashboard + Recurring Jobs
// ============================================================
// The Hangfire Dashboard is a built-in web UI showing:
//   - Queued, processing, succeeded, and failed jobs
//   - Job history and retry counts
//   - The recurring job schedule
// In production you would restrict this behind authentication/authorization.
// For development, we expose it at /hangfire freely.
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Meterly Job Dashboard",
    // Allow all in Development; in production, restrict with IsDashboardAuthorizationFilter
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

// Register recurring jobs after the app is built so DI is fully configured.
// RecurringJob.AddOrUpdate is idempotent — calling it again just updates the schedule.
RecurringJob.AddOrUpdate<DunningJob>(
    recurringJobId: "dunning-sweep",
    methodCall: job => job.RunAsync(CancellationToken.None),
    // Run every hour — fine-grained enough for billing while not hammering the DB
    // Format: "0 * * * *" = at minute 0 of every hour
    cronExpression: Cron.Hourly(),
    queue: "dunning");

// Quick sanity check: log that the app is starting
app.Logger.LogInformation("Meterly API starting in {Environment} environment",
    app.Environment.EnvironmentName);

app.Run();

// ============================================================
// WHY PARTIAL CLASS?
// Making Program a partial class allows integration tests to reference
// it as the entry point for WebApplicationFactory<Program>.
// This is a common pattern in .NET integration tests.
// ============================================================
public partial class Program { }
