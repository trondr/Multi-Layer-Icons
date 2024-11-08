open System
open System.Diagnostics
open System.Reflection
open Serilog
open SvgToIco.Commands
open SvgToIco.Logging

let getAppName () =
    let assembly = Assembly.GetEntryAssembly()
    let exePath = assembly.Location
    let exeName = System.IO.Path.GetFileNameWithoutExtension(exePath)
    exeName

let getAppVersion () =
    let assembly = Assembly.GetEntryAssembly()
    let version = assembly.GetName().Version
    version.ToString()
    
let unhandledExceptionHandler (sender: obj) (args: UnhandledExceptionEventArgs) =
    let ex = args.ExceptionObject :?> Exception    
    printf $"Unhandled exception: %s{ex.ToString()}"

let setup() =
    AppDomain.CurrentDomain.UnhandledException.AddHandler(unhandledExceptionHandler)
    Log.Logger <- configureLogging()
    ()   
let teardown () =
    AppDomain.CurrentDomain.UnhandledException.RemoveHandler(unhandledExceptionHandler)
    ()
    
[<EntryPoint>]
[< STAThread >]
let main argv =
    setup()
    let logger = Log.Logger
    let stopWatch = Stopwatch.StartNew()
    logger.Information $"START: %s{getAppName()} %s{getAppVersion()}"
    logger.Warning "TODO: Implement SvgToIco"
    let result = NCmdLiner.CmdLinery.Run(typeof<CommandDefinitions>,argv) |> Async.AwaitTask |> Async.RunSynchronously
    let exitCode = result.Match((fun i ->i),(fun ex -> logger.Error(ex.Message);1))
    stopWatch.Stop()
    logger.Information $"END: %s{getAppName()} %s{getAppVersion()} Elapsed time: %s{stopWatch.Elapsed.ToString()}. Exit code: {exitCode}."
    teardown()
    exitCode // return an integer exit code
