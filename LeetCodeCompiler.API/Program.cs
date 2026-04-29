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
const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
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
            // Production: browser CORS requires the *page's* origin (scheme + host + port), not the API URL.
            // Add more via env on EC2: CORS_ALLOWED_ORIGINS="http://localhost:5173,https://app.example.com"
            var productionOrigins = new List<string>
            {
                "http://localhost:8081",
                "http://192.168.0.101:8081", // Vite default
                "http://127.0.0.1:8081",
                "https://preplacementtest.com",
                "https://www.preplacementtest.com"
            };

            static bool IsOrigin(string? o) =>
                !string.IsNullOrWhiteSpace(o) && Uri.TryCreate(o.Trim(), UriKind.Absolute, out var u)
                && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

            var fromEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                foreach (var o in fromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (IsOrigin(o))
                        productionOrigins.Add(o.Trim());
            }

            var fromConfig = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (fromConfig != null)
            {
                foreach (var o in fromConfig)
                    if (IsOrigin(o))
                        productionOrigins.Add(o.Trim());
            }

            policy
                .WithOrigins(productionOrigins.Distinct(StringComparer.Ordinal).ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// Configure database with connection pooling and retry policy
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
var dbConnectionFromConfiguration = false;
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(dbConnectionString))
        throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is required in Production");
}
else if (string.IsNullOrWhiteSpace(dbConnectionString))
{
    dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    dbConnectionFromConfiguration = true;
}

if (dbConnectionFromConfiguration)
    Log.Information("Database connection string loaded from configuration (ConnectionStrings:DefaultConnection)");
else
    Log.Information("Database connection string loaded from environment variable DB_CONNECTION_STRING");

if (dbConnectionString is null || string.IsNullOrWhiteSpace(dbConnectionString))
    throw new InvalidOperationException("Database connection string is not configured properly");

string connectionString = dbConnectionString;

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
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
    {
        // Always allow CORS preflight requests to pass through
        if (HttpMethods.IsOptions(httpContext.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter("cors-preflight");
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

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
        options.AddPolicy("TestSetterOnly", policy => policy.RequireAssertion(_ => true));
        options.AddPolicy("AnyAuthenticated", policy => policy.RequireAssertion(_ => true));
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
    options.AddPolicy("TestSetterOnly", policy =>
            policy.RequireClaim("user_type_id", "0", "25", "26", "27", "28", "29", "30"));
    options.AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());
});
}

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("code-execution", () => HealthCheckResult.Healthy("Code execution service is healthy"));

// Configure container pool options (appsettings ContainerPool, or env ContainerPool__PythonPoolSize, etc.)
builder.Services.Configure<ContainerPoolOptions>(builder.Configuration.GetSection("ContainerPool"));
builder.Services.PostConfigure<ContainerPoolOptions>(opts =>
{
    static void Apply(string? value, Action<int> set)
    {
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out var n) && n >= 0)
            set(n);
    }

    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_PYTHON_POOL_SIZE"), v => opts.PythonPoolSize = v);
    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_JAVASCRIPT_POOL_SIZE"), v => opts.JavaScriptPoolSize = v);
    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_JAVA_POOL_SIZE"), v => opts.JavaPoolSize = v);
    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_CPP_POOL_SIZE"), v => opts.CppPoolSize = v);
    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_C_POOL_SIZE"), v => opts.CPoolSize = v);
    Apply(Environment.GetEnvironmentVariable("CONTAINER_POOL_DEFAULT_POOL_SIZE"), v => opts.DefaultPoolSize = v);
});

// Register services
builder.Services.AddScoped<IActivityTrackingService, ActivityTrackingService>();
builder.Services.AddScoped<ICodingTestService, CodingTestService>();
builder.Services.AddScoped<IPracticeTestService, PracticeTestService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
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

app.UseRouting();

// Add CORS before auth so preflight OPTIONS can pass
app.UseCors(FrontendCorsPolicy);

// HTTPS redirection disabled: Production Docker/EC2 listens on HTTP only; terminate TLS at ALB/nginx.
// app.UseHttpsRedirection();
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
    // 0.0.0.0 for Docker/EC2; ASPNETCORE_URLS can override if set
    app.Urls.Add("http://0.0.0.0:5081");
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
