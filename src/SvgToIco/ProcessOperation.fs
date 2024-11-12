namespace SvgToIco

open Serilog

module ProcessOperations =
    open F
    let logger = Log.Logger
    
    type ProcessOperations = class end
    open System.Diagnostics
    open System.Text    
    open System    
        
    type ProcessExitData = {FileName:string;Arguments:string;ExitCode:int;StdOutput:string;StdError:string}

    let writeProcessExitDataToLog processExitData logFileName appendToLogFile =
        let writeToFileBase logFileName appendToLogFile (text:string) =
            use sw =
                match appendToLogFile with
                |true -> System.IO.File.AppendText(logFileName)
                |false -> System.IO.File.CreateText(logFileName)
            sw.Write(text)
                        
        let writeToFile text =
            writeToFileBase logFileName appendToLogFile text
        
        let logToFile = 
            not (String.IsNullOrWhiteSpace(logFileName))
        
        let writeToLog logFunction (line:string) =
            seq{ yield line}
            |>Seq.map(fun s -> 
                                logFunction(s)
                                s
                        )
            |>Seq.filter(fun _ -> logToFile)
            |>Seq.map(fun s -> (writeToFile s))
            |>Seq.toArray
            |>ignore

        logger.Debug("writeToLog command line")
        writeToLog logger.Information $"STOP: '%s{processExitData.FileName}' %s{processExitData.Arguments} (Exit Code: %d{processExitData.ExitCode})"        
        
        logger.Debug("writeToLog StdOutput")
        if (not (String.IsNullOrWhiteSpace(processExitData.StdOutput))) then
            writeToLog logger.Information processExitData.StdOutput
         
        logger.Debug("writeToLog StdError")
        if (not (String.IsNullOrWhiteSpace(processExitData.StdError))) then
            writeToLog logger.Error processExitData.StdError

    let startConsoleProcessUnsafe fileName arguments workingDirectory (timeout:int) inputData logFileName appendToLogFile =
        logger.Information $"START: '%s{fileName}' %s{arguments}"
        let startInfo = new ProcessStartInfo()
        startInfo.CreateNoWindow <- true
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.FileName <- fileName
        if(not (System.String.IsNullOrWhiteSpace(arguments))) then
            startInfo.Arguments <- arguments
        if(not (System.String.IsNullOrWhiteSpace(workingDirectory))) then
            startInfo.WorkingDirectory <- workingDirectory        
        use consoleProcess = new Process()
        consoleProcess.StartInfo <- startInfo
        if(not (System.String.IsNullOrWhiteSpace(inputData))) then
            consoleProcess.StandardInput.Write(inputData)
            consoleProcess.StandardInput.Close()

        let stdOutBuilder = new StringBuilder()
        let stdErrorBuilder = new StringBuilder()

        let outputDataReceivedHandler =
           new DataReceivedEventHandler(fun s e ->                                             
                                            stdOutBuilder.AppendLine(e.Data) |> ignore
                                       )
        let errorDataReceivedHandler =           
           new DataReceivedEventHandler(fun s e ->                                             
                                            stdErrorBuilder.AppendLine(e.Data) |> ignore                                            
                                            )

        consoleProcess.OutputDataReceived.AddHandler(outputDataReceivedHandler)
        consoleProcess.ErrorDataReceived.AddHandler(errorDataReceivedHandler)

        consoleProcess.EnableRaisingEvents <- true
        consoleProcess.Start() |> ignore
        consoleProcess.BeginOutputReadLine()
        consoleProcess.BeginErrorReadLine()
        match consoleProcess.WaitForExit(timeout) with
        |true -> ignore
        |false ->            
            consoleProcess.Close()
            raise (new Exception $"Process execution timed out: \"%A{fileName}\" %s{arguments}")
        |>ignore
            
        consoleProcess.CancelErrorRead()
        consoleProcess.CancelOutputRead()
        consoleProcess.ErrorDataReceived.RemoveHandler(errorDataReceivedHandler)
        consoleProcess.OutputDataReceived.RemoveHandler(outputDataReceivedHandler)
        let processExitData =
            {
                FileName = fileName
                Arguments = arguments
                ExitCode = consoleProcess.ExitCode
                StdOutput = stdOutBuilder.ToString().Trim()
                StdError = stdErrorBuilder.ToString().Trim()
            }
        
        writeProcessExitDataToLog processExitData logFileName appendToLogFile            
        logger.Information($"%s{processExitData.StdOutput}")
        logger.Information($"%s{processExitData.StdError}")
        logger.Information($"Exit Code: %i{processExitData.ExitCode}")
        processExitData.ExitCode
    
    let startConsoleProcess filePath arguments workingDirectory ( timeout:int) inputData logFileName appendToLogFile =
        let message = (Some $"Start of console process ('%s{filePath}' %s{arguments}) failed.")
        tryCatch7 message startConsoleProcessUnsafe filePath arguments workingDirectory timeout inputData logFileName appendToLogFile
    
    let startConsoleProcess' filePath arguments workingDirectory (timeout:int) inputData logFileName appendToLogFile successExitCodes =
        result{
            let! exitCode = startConsoleProcess filePath arguments workingDirectory timeout inputData logFileName appendToLogFile 
            let optionalSuccess = successExitCodes|>Array.tryFind(fun ec -> ec = exitCode)
            let! exitCodeResult = 
                match optionalSuccess with
                |Some ec -> Result.Ok ec
                |None -> Result.Error (toException $"ERROR: '%s{filePath}' %s{arguments} (Exit Code: %d{exitCode})" None)
            return exitCodeResult
        }

    let onError f i r =
        match r with
        |Result.Ok _ -> r
        |Result.Error _ ->
            f i

    let startProcess fileName arguments (workingDirectory: string option) waitForExit =
        try
            let startInfo = new ProcessStartInfo()            
            startInfo.UseShellExecute <- true
            startInfo.FileName <- fileName
            startInfo.Arguments <- arguments
            match workingDirectory with
            |Some wd -> startInfo.WorkingDirectory <- wd
            |None -> startInfo.WorkingDirectory <- null
            let proc = (System.Diagnostics.Process.Start(startInfo))
            match waitForExit with
            |true -> proc.WaitForExit()
            |false -> ()
            Result.Ok proc
        with
        |ex -> Result.Error ex

    let getProcessName (exePath:string) =
        System.IO.Path.GetFileNameWithoutExtension(exePath)

    let getProcessesByName processName =
        System.Diagnostics.Process.GetProcessesByName(processName)

    let terminateProcesses processName =        
        (getProcessesByName (processName)) 
        |> Array.map(fun p ->             
            logger.Information $"Terminating existing process '%s{p.ProcessName}' (%d{p.Id})"
            p.Kill()
        ) |> ignore
    
    let processIsRunning processName =
        let proc = getProcessesByName processName
        match proc with
        | [||] -> false
        | _ -> true

    let getCurrentProcess() =
        System.Diagnostics.Process.GetCurrentProcess()

    //Terminate existing processes except current instance
    let terminateProcessesExceptCurrent processName =
        let currentProcess = getCurrentProcess()
        (getProcessesByName (processName))
        |> Array.filter (fun p -> p.Id <> currentProcess.Id)
        |> Array.map(fun p ->             
            logger.Information $"Terminating existing process '%s{p.ProcessName}' (%d{p.Id})"
            p.Kill()
        ) |> ignore

