using AnilistConEnie.Infrastructure.Services.Interface;
using Discord;
using Serilog.Events;
using Serilog;
using Serilog.Core;
using System.Globalization;

namespace AnilistConEnie.Infrastructure.Services.Impl
{
    public class LogService : ILogService
    {
        public void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{ProcessId}] [{Level:u4}]: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: $"logs/{DateTime.Now.ToString("dd'-'MM'-'yyyy' 'HH'_'mm'_'ss", CultureInfo.InvariantCulture)}.log",
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Information),
                outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{Level:u4}]: {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 8_388_608, /* 8 megabytes */
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 50)
            .CreateLogger();
        }

        public async Task LogAsync(LogMessage message)
        {
            var severity = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };

            Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

            await Task.CompletedTask;
        }
    }
}
