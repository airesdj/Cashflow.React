using CashFlow.DataAccess;
using CashFlow.DataAccess.Classes;
using CashFlow.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor(); // Register IHttpContextAccessor

var logPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "Logs",
    "cashflow.react-.log"
);

builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .MinimumLevel.Override(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogEventLevel.Error)
        .WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Month,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                "{Message:lj}{NewLine}{Exception}"
        );
});

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CashFlow.Antiforgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "CashFlow.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

var settings = builder.Configuration
    .GetSection("AppSettings")
    .Get<AppSettings>();
AppConfig.Init(settings!);

builder.Services.AddDbContext<Context>(options =>
{
    options.UseMySql(
        AppConfig.DbConnectionString_Cashflow,
        new MySqlServerVersion(new Version(8, 4, 0)),
        mysql => mysql.CommandTimeout(30)
    );

    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
});


var repositoryAssembly = typeof(CashFlowRepository).Assembly;

foreach (var type in repositoryAssembly.GetTypes()
                                       .Where(t => t.IsClass &&
                                                   !t.IsAbstract &&
                                                   t.Name.EndsWith("Repository")))
{
    builder.Services.AddScoped(type);
}


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, elapsed, exception) =>
        exception != null || context.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : context.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Debug;
});

app.MapControllers();

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
