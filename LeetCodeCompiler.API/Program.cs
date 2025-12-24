using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/leetcode-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "LeetCode Compiler API", 
        Version = "v1",
        Description = "API for LeetCode-style coding problems and tests"
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 🚀 OPTIMIZED: Add memory cache for execution results
builder.Services.AddMemoryCache();

// 🔧 DEVELOPMENT MODE: Simplified CORS for faster development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins for easier testing
        policy
                .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        }
        else
        {
            // In production, use specific origins for security
            policy
                .WithOrigins(
                    "http://localhost:3000", // React development
                    "http://localhost:8081", // React Native
                    "http://192.168.0.239:8081", // React Native on network
                    "https://yourdomain.com" // Production domain - update this
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// Configure database with connection pooling and retry policy
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        "Server=localhost;Database=LeetCode;Trusted_Connection=True;TrustServerCertificate=True;",
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
}, ServiceLifetime.Scoped);

// Add Redis caching (optional - can be added later)
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = "localhost:6379";
//     options.InstanceName = "LeetCodeAPI";
// });

// 🔧 DEVELOPMENT MODE: Disable rate limiting for faster development
if (!builder.Environment.IsDevelopment())
{
    // Production rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiting
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Code execution rate limiting (more restrictive)
    options.AddPolicy("CodeExecution", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10, // 10 executions per minute per user
                Window = TimeSpan.FromMinutes(1)
            }));

    // API rate limiting
    options.AddPolicy("ApiLimiter", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200, // 200 requests per minute per user
                Window = TimeSpan.FromMinutes(1)
            }));
});
}

// 🔧 DEVELOPMENT MODE: Disable JWT Authentication for faster development
if (builder.Environment.IsDevelopment())
{
    // Skip authentication entirely in development
    builder.Services.AddAuthentication();
    
    // Allow all requests in development
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("CodeExecution", policy => policy.RequireAssertion(_ => true));
        options.AddPolicy("ProblemAccess", policy => policy.RequireAssertion(_ => true));
        options.AddPolicy("AdminAccess", policy => policy.RequireAssertion(_ => true));
    });
}
else
{
    // Production JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "YourMainAppIssuer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "YourMainAppAudience",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourMainAppSecretKey"))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Production authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CodeExecution", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("ProblemAccess", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminAccess", policy => 
            policy.RequireClaim("user_type_id", "25", "26", "27"));
});
}

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("code-execution", () => HealthCheckResult.Healthy("Code execution service is healthy"));

// Configure container pool options
builder.Services.Configure<ContainerPoolOptions>(builder.Configuration.GetSection("ContainerPool"));

// Register services
builder.Services.AddScoped<IActivityTrackingService, ActivityTrackingService>();
builder.Services.AddScoped<ICodingTestService, CodingTestService>();
builder.Services.AddScoped<IPracticeTestService, PracticeTestService>();
builder.Services.AddSingleton<IContainerPoolService, ContainerPoolService>();
builder.Services.AddScoped<StudentProfileService>();

// Register execution services
builder.Services.AddScoped<PythonExecutionService>();
builder.Services.AddScoped<JavaScriptExecutionService>();
builder.Services.AddScoped<JavaExecutionService>();
builder.Services.AddScoped<CppExecutionService>();
builder.Services.AddScoped<CExecutionService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add HTTP client for external API calls
builder.Services.AddHttpClient("StudentProfileAPI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout for external API
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add CORS
app.UseCors();

// Enable HTTPS redirection for security
// app.UseHttpsRedirection();
// Enable HTTPS redirection for security (disabled in development to prevent CORS issues)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// 🔧 DEVELOPMENT MODE: Skip security headers in development for easier testing
if (!app.Environment.IsDevelopment())
{
    // Add security headers in production
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
}

// 🔧 DEVELOPMENT MODE: Skip rate limiting in development
if (!app.Environment.IsDevelopment())
{
app.UseRateLimiter();
}

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

// Configure the application to listen on all network interfaces
app.Urls.Clear();
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://0.0.0.0:5081"); // Listen on all interfaces
    app.Urls.Add("https://0.0.0.0:7169"); // Listen on all interfaces
}
else
{
    app.Urls.Add("http://192.168.0.239:5081");
    app.Urls.Add("https://192.168.0.239:7169");
}

try
{
    Log.Information("Starting LeetCode Compiler API");
    
    // Initialize container pools
    using (var scope = app.Services.CreateScope())
    {
        var containerPool = scope.ServiceProvider.GetRequiredService<IContainerPoolService>();
        await containerPool.InitializePoolsAsync();
        Log.Information("Container pools initialized successfully");
    }
    
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
