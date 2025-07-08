using API.DataAccess.Repositories.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ProductAPI.Business.Mappings;
using ProductAPI.Business.Services.Implementations;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.DataAccess.Context;
using ProductAPI.DataAccess.Repositories.Implementations;
using ProductAPI.DataAccess.Repositories.Interfaces;
using ProductAPI.DataAccess.UnitOfWork;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ===== LOGGING CONFIGURATION =====
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/myproject-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ===== DATABASE CONFIGURATION =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ProductAPI.DataAccess"));

    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===== REPOSITORY PATTERN DEPENDENCIES =====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();


// Generic repositories are handled by UnitOfWork, but can be registered separately if needed
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ===== BUSINESS SERVICES =====
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ===== AUTOMAPPER CONFIGURATION =====
builder.Services.AddAutoMapper(typeof(MappingProfiles).Assembly);


// ===== CONTROLLERS & API CONFIGURATION =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

        // Add enum string conversion
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ===== API VERSIONING =====
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("apiVersion"),
        new HeaderApiVersionReader("X-Version"),
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// ===== SWAGGER/OPENAPI CONFIGURATION =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Product API",
        Version = "v1",
        Description = "Web API with EF Core",
    });


    // Add enum descriptions
    c.SchemaFilter<EnumSchemaFilter>();

    // Add authorization header
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// ===== CORS CONFIGURATION =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        policy
            .WithOrigins("https://myproject.com", "https://www.myproject.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization");
    });
});

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// ===== PERFORMANCE & CACHING =====
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ===== RATE LIMITING =====
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "Fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });
});

// ===== HTTP CLIENT =====
builder.Services.AddHttpClient();

// ===== PROBLEM DETAILS =====
builder.Services.AddProblemDetails();

// ===== BUILD APPLICATION =====
var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====

// Exception handling - must be first
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts(); // HTTPS Strict Transport Security
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Request logging
app.UseSerilogRequestLogging();

// HTTPS redirection
app.UseHttpsRedirection();

// Response compression
app.UseResponseCompression();

// Response caching
app.UseResponseCaching();

// CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

// Rate limiting
app.UseRateLimiter();

// Authentication & Authorization (when implemented)
// app.UseAuthentication();
// app.UseAuthorization();

// Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductAPI API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}

// Health checks
app.UseHealthChecks("/health");
app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
{
    Predicate = check => check.Tags.Contains("ready")
});
app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
{
    Predicate = _ => false
});

// API Controllers
app.MapControllers();

// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment())
    {
        // Auto-migrate in development
        try
        {
            context.Database.Migrate();
            Log.Information("Database migrated successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while migrating database");
        }
    }
}

// ===== SEED DATA (Optional) =====
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            await SeedData.InitializeAsync(scope.ServiceProvider);
            Log.Information("Database seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while seeding database");
        }
    }
}

// ===== START APPLICATION =====
try
{
    Log.Information("Starting ProductAPI API...");
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

// ===== HELPER CLASSES =====

/// <summary>
/// Enum schema filter for Swagger documentation
/// </summary>
public class EnumSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            foreach (var name in Enum.GetNames(context.Type))
            {
                schema.Enum.Add(new OpenApiString(name));
            }

            schema.Description += $" Possible values: {string.Join(", ", Enum.GetNames(context.Type))}";
        }
    }
}

/// <summary>
/// Database seed data
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        // Check if data already exists
        if (context.Users.Any()) return;

        // Seed Users
        var adminUser = new ProductAPI.Business.DTOs.User.CreateUserDto
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@myproject.com",
            UserRole = ProductAPI.Domain.Enums.UserRole.SuperAdmin
        };

        var testUser = new ProductAPI.Business.DTOs.User.CreateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@myproject.com",
            UserRole = ProductAPI.Domain.Enums.UserRole.User
        };

        await userService.CreateUserAsync(adminUser);
        await userService.CreateUserAsync(testUser);

        // Seed Products
        var products = new[]
        {
            new ProductAPI.Business.DTOs.Product.CreateProductDto
            {
                Name = "Sample Product 1",
                Description = "This is a sample product for testing",
                SKU = "SP001",
                Price = 99.99m,
                StockQuantity = 100
            },
            new ProductAPI.Business.DTOs.Product.CreateProductDto
            {
                Name = "Sample Product 2",
                Description = "Another sample product for testing",
                SKU = "SP002",
                Price = 149.99m,
                StockQuantity = 50
            }
        };

        foreach (var product in products)
        {
            await productService.CreateProductAsync(product);
        }

        Log.Information("Seed data created successfully");
    }
}