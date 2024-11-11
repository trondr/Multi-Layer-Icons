namespace SvgToIco

open System
open System.IO
open System.Configuration
open ImageMagick

module SvgToIcoConversion =
    open System
    open System.IO
    open Serilog
    
   
    let rec getAllSvgFiles (folderPath: string) =
        seq {
            for file in Directory.GetFiles(folderPath, "*.svg") do
                yield file
            for subDir in Directory.GetDirectories(folderPath) do
                yield! getAllSvgFiles subDir
        }
        
    let getInkscapeExe () =
        let exe = (Configuration.getConfiguration()).InkscapeExe
        if String.IsNullOrEmpty(exe) then
            raise (Exception("'InkscapeExe' not specified in AppSettings in App.config"))
        if not (File.Exists(exe)) then
            raise (FileNotFoundException("Inkscape.exe was not found.", exe))
        exe
    
    let fileExist filePath =
        File.Exists(filePath)
        
    let getFullPath filePath =
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(filePath))
    
    type IconFileInfo = { Name:string; FullName:string; ModifiedTime:DateTime}
    let createIconFileInfo (svgFileName:string) =
        {
            Name= Path.GetFileName(svgFileName)
            FullName = svgFileName
            ModifiedTime = File.GetLastWriteTime(svgFileName)    
         }
    type PngFileInfo = { Name:string; FullName:string; ModifiedTime:DateTime; Size:int }
    let createPngFileInfo (pngFileName:string) size =
        {
            Name= Path.GetFileName(pngFileName)
            FullName = pngFileName
            ModifiedTime = File.GetLastWriteTime(pngFileName)
            Size = size
         }    
    type IconInfo = { SvgFile:IconFileInfo; IconFile:IconFileInfo;LargestPngFile:PngFileInfo; Sizes: int array; PngFiles: PngFileInfo array}
    let createIconInfo svgFileName sizes =
        let getPngFiles baseFileName sizes =
            sizes
            |> Array.sort
            |> Array.map(fun s ->
                    let pngFileName = $"%s{baseFileName}-%i{s}.png"
                    createPngFileInfo pngFileName s                
                )
            
        let getBaseFileName (fileName: string) =
            let file = FileInfo(fileName)
            let baseName = 
                if file.Directory <> null then 
                    Path.Combine(file.Directory.FullName, Path.GetFileNameWithoutExtension(fileName))
                else 
                    Path.GetFileNameWithoutExtension(fileName)
            baseName
        
        let baseFileName = getBaseFileName svgFileName
        let pngFiles = getPngFiles baseFileName sizes
        let largestPngFile = (pngFiles |> Array.last)
        {
          SvgFile = createIconFileInfo svgFileName
          IconFile = createIconFileInfo ($"%s{baseFileName}.ico")
          LargestPngFile = largestPngFile
          Sizes = sizes
          PngFiles = pngFiles
        }

    let pngNeedUpdate (svgFile:IconFileInfo) (pngFile:PngFileInfo) =
        match (fileExist pngFile.FullName) with
        | false -> true
        | true -> (pngFile.ModifiedTime < svgFile.ModifiedTime)
        
    let icoNeedUpdate (iconInfo:IconInfo) =
        match (fileExist iconInfo.IconFile.FullName) with
        | false -> true
        | true ->
            match (iconInfo.IconFile.ModifiedTime < iconInfo.SvgFile.ModifiedTime) with
            |true -> true
            |false -> iconInfo.PngFiles |> Array.exists (fun p -> pngNeedUpdate iconInfo.SvgFile p)
        
    let exportSvgToPng (svgFile: IconFileInfo) (pngFile: PngFileInfo) =
        Log.Logger.Information $"Exporting: %s{svgFile.Name} -> %s{pngFile.Name}"
        let inkscapeExe = getInkscapeExe()
        let arguments = $"-o \"%s{pngFile.FullName}\" -w %d{pngFile.Size} -h %d{pngFile.Size} \"%s{svgFile.FullName}\""
        let workingDirectory = String.Empty
        let exitCodeResult = ProcessOperations.startConsoleProcess inkscapeExe arguments workingDirectory -1 null null false
        match exitCodeResult with
        |Ok exitCode ->
            match (fileExist pngFile.FullName) with
            | false -> Result.Error(new Exception($"Failed to export svg to png. Png file has not been created: %s{pngFile.FullName}"))
            | true -> Result.Ok {pngFile with ModifiedTime = svgFile.ModifiedTime }
        |Error ex -> Result.Error(new Exception($"Failed to export svg to png file. %s{ex.Message}"))
       
    let createIconFromPngFilesFromSvg (iconInfo: IconInfo) =        
        use imageCollection = new MagickImageCollection()
        iconInfo.PngFiles
        |> Array.iter (fun iconInfoPngFile ->
            let image = new MagickImage(iconInfoPngFile.FullName)
            imageCollection.Add(image)
        )
        imageCollection.Write(iconInfo.IconFile.FullName)
       
    let convertAllSvgToIco folderPath recursive refresh sizes maxDegreeOfParallelism : Result<int,Exception> =
        let iconInfos =
            getAllSvgFiles folderPath
            |> Seq.map(fun f -> createIconInfo f sizes)
        let icons =
            iconInfos
            |> Seq.filter(fun i -> icoNeedUpdate i)
            |> Seq.map(fun i ->
                       let result =
                           i.PngFiles
                           |> Array.Parallel.map(fun p -> exportSvgToPng i.SvgFile p)
                           |> Array.map (fun r -> (F.resultToOption Log.Logger r))
                       let allGood = result |> Array.exists (fun o -> not (match o with|Some s->false|None->true))
                       if allGood then Some i else None
                       )
            |>Seq.choose id
            |>Seq.toArray
            |>Array.Parallel.map(fun i ->
                    createIconFromPngFilesFromSvg i
                )
        Result.Ok 0