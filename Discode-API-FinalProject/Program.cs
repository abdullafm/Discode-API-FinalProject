using Discode_API_FinalProject.Helpers;
using Discode_API_FinalProject.Hubs;
using Discode_API_FinalProject.Middlewares;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repository;
using Repository.Data;
using Service;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Repository.Settings;
using Service.Helpers.Settings;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Discode_API_FinalProject.Filters.AccountStatusCache>();
builder.Services.AddScoped<Discode_API_FinalProject.Hubs.SystemMessenger>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Discode_API_FinalProject.Filters.AccountStatusFilter>();
    options.Filters.Add<Discode_API_FinalProject.Filters.BannedServerFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // a bad model answers in the same { isSuccess, errors } shape as every other failure, so clients show the real message
    options.InvalidModelStateResponseFactory = context =>
    {
        var all = context.ModelState.Values.SelectMany(entry => entry.Errors).ToList();

        // a body that is not JSON carries parser internals ("'b' is an invalid start of a property name..."): keep those out of the answer.
        // The JSON parser reports under the key "$" or "$.property".
        var errors = all.Any(error => error.Exception is not null) || context.ModelState.Keys.Any(key => key.StartsWith('$'))
            ? new[] { "The request body is not valid." }
            : all.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The request is not valid." : error.ErrorMessage)
                 .Distinct()
                 .ToArray();
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            isSuccess = false,
            errors = errors.Length > 0 ? errors : new[] { "The request is not valid." },
            traceId = context.HttpContext.TraceIdentifier
        });
    };
});
builder.Services.AddSignalR(options =>
{
    options.AddFilter<Discode_API_FinalProject.Hubs.HubExceptionFilter>();
});
builder.Services.AddHostedService<VoiceGhostSweeper>();
builder.Services.AddHostedService<NitroExpiryWorker>();
builder.Services.AddHostedService<StatusClearWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<Discode_API_FinalProject.Helpers.SwaggerExamplesFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from login (without the Bearer prefix)."
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

var connectionString = ConnectionStringResolver.Resolve(
    builder.Configuration,
    LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Database"));

builder.Services.AddSingleton<Repository.Notifications.IAdminChangeNotifier, Discode_API_FinalProject.Hubs.AdminHubNotifier>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(services.GetRequiredService<Repository.Interceptors.AdminChangeInterceptor>()));

builder.Services.AddServiceLayer();
builder.Services.AddScoped<DiscoverBroadcaster>();
builder.Services.AddRepositoryLayer();

builder.Services.AddHttpClient<IEmailRepository, EmailRepository>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
            errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri?.IsLoopback == true;
    }

    return handler;
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
// the Stripe keys live in "Stripe"; "Logging:Stripe" is read too because that is where an early copy of the key was put
builder.Services.Configure<StripeSettings>(options =>
{
    var section = builder.Configuration.GetSection("Stripe");
    var early = builder.Configuration.GetSection("Logging:Stripe");
    options.SecretKey = section["SecretKey"] ?? early["SecretKey"] ?? string.Empty;
    options.WebhookSecret = section["WebhookSecret"] ?? early["WebhookSecret"] ?? string.Empty;
});

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);

var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

var appSettings = builder.Configuration.GetSection("App").Get<AppSettings>() ?? new AppSettings();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy => policy
        .WithOrigins(appSettings.ClientUrl.TrimEnd('/'))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            errors = new[] { "Too many attempts. Wait a minute and try again." },
            traceId = context.HttpContext.TraceIdentifier
        }, token);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Environment.IsDevelopment() ? 100 : 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();
var startupLog = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

// Exceptions nobody catches (fire-and-forget tasks, timers) would otherwise disappear or kill the process without a trace
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    startupLog.LogCritical(e.ExceptionObject as Exception, "Unhandled exception, terminating: {Terminating}", e.IsTerminating);
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    startupLog.LogError(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // without these the app cannot work, so a failure stops the start with a clear reason
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ICallRepository>().EndAllOpenAsync();
        await scope.ServiceProvider.GetRequiredService<IGroupCallRepository>().EndAllOpenAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedAsync(roleManager);
        await RoleSeeder.SeedSuperAdminAsync(scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(), app.Configuration["Seed:SuperAdminEmail"]);
        await SystemUserSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        startupLog.LogCritical(ex, "The database could not be prepared. Check the connection string and that SQL Server is running.");
        throw;
    }

    // emojis and translations are content, the app still works without them: log and carry on
    try
    {
        await EmojiSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        startupLog.LogError(ex, "Seeding the emojis failed. The app starts without them.");
    }

    try
    {
        await TranslationSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        startupLog.LogError(ex, "Seeding the translations failed. The app starts without them.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// 401, 403, a route that does not exist, a wrong method: they have no body, give them the same JSON shape as every other failure
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var message = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => "You need to sign in.",
        StatusCodes.Status403Forbidden => "You are not allowed to do that.",
        StatusCodes.Status404NotFound => "Not found.",
        StatusCodes.Status405MethodNotAllowed => "That method is not allowed here.",
        _ => "The request failed."
    };
    response.ContentType = "application/json; charset=utf-8";
    await response.WriteAsJsonAsync(new { isSuccess = false, errors = new[] { message }, traceId = context.HttpContext.TraceIdentifier });
});
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PresenceHub>("/hubs/presence");
app.Run();
