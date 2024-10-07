using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using Serilog.Templates;

var prefixApp = "TestElasticoConsole";

Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .Enrich.WithProperty("UtcTimestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")) // UTC0
               .Enrich.WithProperty("AppTimestamp", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")) // App
               .Enrich.WithProperty("AppSource", prefixApp) // Which App?
               .Enrich.WithProcessId()
               .Enrich.WithMachineName()
               .Enrich.WithThreadId()
               .WriteTo.Console(new ExpressionTemplate(
                                                         "[{@t:yyyy-MM-dd HH:mm:ss.fff zz} {@l:u3}" +
                                                         "{#if SourceContext is not null} ({SourceContext}){#end}] {@m,3} {@p} \n{@x}"))
               .WriteTo.Async(w => w.File(formatter: new ExpressionTemplate("{@t:yyyy-MM-dd HH:mm:ss.fff} [{@l:u3} {Coalesce(SourceContext, '<none>')}] {@m} {@p}\n{@x}"),
                             path: "logs/log_.txt",
                             rollingInterval: RollingInterval.Day,
                             shared: true)) // To enable multi-process shared log files, set shared to true)
               .WriteTo.Async(w => w.File(formatter: new CompactJsonFormatter(),
                             path: "logs/log_.json",
                             rollingInterval: RollingInterval.Day,
                             shared: true)) // To enable multi-process shared log files, set shared to true)
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200")) // network-based sinks  such as Elasticsearch already perform asynchronous batching natively.
               {
                   IndexFormat = prefixApp + "{0:yyyy.MM.dd}",
                   AutoRegisterTemplate = true, // create automatic index
                   ModifyConnectionSettings = x => x.BasicAuthentication("elastic", "YourStrongPass"),
                   NumberOfShards = 2,
                   NumberOfReplicas = 1,
                   OverwriteTemplate = true,
                   AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
                   EmitEventFailure =
                           EmitEventFailureHandling.WriteToSelfLog |
                           EmitEventFailureHandling.RaiseCallback |
                           EmitEventFailureHandling.ThrowException,
                   CustomFormatter = new ElasticsearchJsonFormatter()
               })
               .MinimumLevel.Override("System", LogEventLevel.Warning)
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
               .MinimumLevel.Verbose()
               .CreateLogger();

// sink gets error
if (!Directory.Exists("logs"))
{
    Directory.CreateDirectory("logs");
}
var file = File.CreateText("logs/serilog_sink_error.txt");
Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(file));


Log.Information("Hello, Berk!");
Console.ReadLine();