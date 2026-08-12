using System.Text;
using ElsInt.Application;
using ElsInt.Infrastructure;
using ElsInt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine("logs", "elsint-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("=== ElsInt.Api starting ===");
    Log.Information("ContentRoot={ContentRoot}", Directory.GetCurrentDirectory());

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    Log.Information("Environment={Environment}", builder.Environment.EnvironmentName);
    Log.Information("ConnectionString={ConnectionString}",
        MaskConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));
    Log.Information("Cors={Cors}",
        string.Join(", ", builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? []));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Storefront", policy =>
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                    ?? ["http://localhost:5173", "http://localhost:3000"])
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    var app = builder.Build();
    Log.Information("WebApplication built");

    app.UseSerilogRequestLogging();

    // Turn domain/validation failures into readable JSON so admin forms can show the reason.
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (FluentValidation.ValidationException ex)
        {
            Log.Warning(ex, "Validation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                message = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage).Distinct())
            });
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Invalid operation for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "Not found for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
    });
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Storefront");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    string? seedError = null;
    var seedOk = false;

    app.MapGet("/", () => Results.Ok(new
    {
        service = "ElsInt.Api",
        status = "ok",
        seedOk,
        seedError,
        timeUtc = DateTime.UtcNow,
        diag = "/diag",
        logs = "logs/elsint-*.log"
    }));

    app.MapGet("/diag", async (IConfiguration config, AppDbContext db, IWebHostEnvironment env) =>
    {
        var logDir = Path.Combine(env.ContentRootPath, "logs");
        var latestLog = Directory.Exists(logDir)
            ? Directory.GetFiles(logDir, "elsint-*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        var info = new Dictionary<string, object?>
        {
            ["timeUtc"] = DateTime.UtcNow,
            ["environment"] = env.EnvironmentName,
            ["contentRoot"] = env.ContentRootPath,
            ["connectionString"] = MaskConnectionString(config.GetConnectionString("DefaultConnection")),
            ["cors"] = config.GetSection("Cors:Origins").Get<string[]>() ?? [],
            ["seedOk"] = seedOk,
            ["seedError"] = seedError,
            ["latestLogFile"] = latestLog,
            ["latestLogTail"] = ReadTail(latestLog, 60)
        };

        try
        {
            info["dbCanConnect"] = await db.Database.CanConnectAsync();
            info["pendingMigrations"] = await db.Database.GetPendingMigrationsAsync();
            info["appliedMigrations"] = await db.Database.GetAppliedMigrationsAsync();
            info["productCount"] = await db.Products.CountAsync();
            info["brandCount"] = await db.Brands.CountAsync();
            info["dbOk"] = true;
        }
        catch (Exception ex)
        {
            info["dbOk"] = false;
            info["dbError"] = ex.Message;
            info["dbErrorDetail"] = ex.ToString();
            Log.Error(ex, "Diag DB check failed");
        }

        return Results.Ok(info);
    });

    app.MapControllers();
    Log.Information("Endpoints mapped");

    try
    {
        Log.Information("Migrate/seed starting");
        await ElsInt.Infrastructure.DependencyInjection.SeedAsync(app.Services);
        seedOk = true;
        Log.Information("Migrate/seed completed OK");
    }
    catch (Exception ex)
    {
        seedOk = false;
        seedError = ex.GetBaseException().Message;
        Log.Error(ex, "Migrate/seed FAILED");
    }

    Log.Information("Calling app.Run()");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FATAL startup failure");
    try
    {
        File.AppendAllText(
            Path.Combine("logs", "fatal.log"),
            $"[{DateTime.UtcNow:O}] FATAL{Environment.NewLine}{ex}{Environment.NewLine}",
            Encoding.UTF8);
    }
    catch { /* ignore */ }
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static string MaskConnectionString(string? cs)
{
    if (string.IsNullOrWhiteSpace(cs)) return "(null)";
    return System.Text.RegularExpressions.Regex.Replace(
        cs,
        "(Password|Pwd)\\s*=\\s*[^;]*",
        "$1=***",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

static string ReadTail(string? path, int lines)
{
    try
    {
        if (path is null || !File.Exists(path)) return "(no serilog file yet)";
        var all = File.ReadAllLines(path);
        return string.Join(Environment.NewLine, all.TakeLast(Math.Max(1, lines)));
    }
    catch (Exception ex)
    {
        return $"read error: {ex.Message}";
    }
}

public partial class Program;
