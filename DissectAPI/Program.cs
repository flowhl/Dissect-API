using DissectAPI;
using DissectAPI.LogHandler;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using Newtonsoft.Json;
using DissectAPI.Dissect;

var logPath = "/app/logs"; // Log directory within the container
var debugLogPath = Path.Combine(logPath, "debug.txt");
var errorLogPath = Path.Combine(logPath, "error.txt");
Directory.CreateDirectory(logPath); // Ensure the directory exists

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // Capture all logs at Debug level or higher
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(x => x.Level == LogEventLevel.Debug) // Include only Debug level messages
                .WriteTo.File(debugLogPath,
                    rollingInterval: RollingInterval.Infinite, // No rolling based on time
                    rollOnFileSizeLimit: true, // Enable rolling based on file size
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                    retainedFileCountLimit: 1, // Keep only the current file
                    shared: true, // Allows multiple processes to log to the same file
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console()) // Also write debug logs to console
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(x => x.Level >= LogEventLevel.Error) // Include only Error level messages
                .WriteTo.File(errorLogPath,
                    rollingInterval: RollingInterval.Day, // Roll over weekly
                    rollOnFileSizeLimit: true, // Also roll over at 100MB
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                    retainedFileCountLimit: null, // Keep all files
                    shared: true, // Allows multiple processes to log to the same file
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console()) // Also write error logs to console
            .CreateLogger();

//Cleanup Logs
LogHandler.CleanupOldLogFiles(logPath, TimeSpan.FromDays(14));

builder.Host.UseSerilog(); // Use Serilog for logging

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
    Log.Error((Exception)eventArgs.ExceptionObject, "Unhandled domain exception caught.");
};

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
    Log.Error(eventArgs.Exception, "Unobserved task exception caught.");
    eventArgs.SetObserved(); // This prevents the process from being terminated.
};

Log.Debug($"Current available threads: {Environment.ProcessorCount}");

Log.Debug("Cleaning up old files");
DissectHelper.CleanUpFolders();

app.Run();
