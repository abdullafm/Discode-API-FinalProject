using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Discode_API_FinalProject.Helpers
{
    public static class LoggingSetup
    {
        public const string AuditSource = "Audit";

        private const string ConsoleTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        public static void AddDiscodeLogging(this WebApplicationBuilder builder)
        {
            var testing = builder.Environment.IsEnvironment("Testing");
            builder.Host.UseSerilog((context, services, logger) =>
            {
                logger
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: ConsoleTemplate);

                if (testing)
                {
                    return;
                }

                var folder = context.Configuration["Logs:Folder"];
                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs");
                }

                logger.WriteTo.File(Path.Combine(folder, "api-.log"),
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, shared: true, outputTemplate: FileTemplate);

                logger.WriteTo.Logger(errors => errors
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Warning)
                    .WriteTo.File(Path.Combine(folder, "errors-.log"),
                        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 60, shared: true, outputTemplate: FileTemplate));

                logger.WriteTo.Logger(audit => audit
                    .Filter.ByIncludingOnly(Matching.FromSource(AuditSource))
                    .WriteTo.File(Path.Combine(folder, "audit-.log"),
                        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180, shared: true, outputTemplate: FileTemplate));
            });
        }
    }
}
