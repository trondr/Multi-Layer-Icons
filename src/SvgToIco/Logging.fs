namespace SvgToIco

    open Serilog
    open Serilog.Events
    open F

    module Logging =
        
        let toLogLevel logLevelValue =
            match logLevelValue with
            | "Debug" -> LogEventLevel.Debug
            | "Error" -> LogEventLevel.Error
            | "Fatal" -> LogEventLevel.Fatal
            | "Info" -> LogEventLevel.Information
            | "Trace" -> LogEventLevel.Verbose
            | "Warn" -> LogEventLevel.Warning
            | _ -> failwith $"Invalid loglevel '%s{logLevelValue}' specified in appsettings.json. Valid log level values are: %s{getEnumValuesToString typeof<LogEventLevel>}"
        
        let configureLogging () =
            let configuration = Configuration.getConfiguration()
            let logLevel = configuration.LogLevel |> toLogLevel
            let logger = LoggerConfiguration()
                             .MinimumLevel.Is(logLevel)
                             .Enrich.FromLogContext()
                             .WriteTo.Console()
                             .WriteTo.File(configuration.LogFile)
                             .CreateLogger()            
            logger
