namespace SvgToIco

module SvgToIcoConversion =
    open System
    open System.IO
   
    let rec getAllSvgFiles (folderPath: string) =
        seq {
            for file in Directory.GetFiles(folderPath, "*.svg") do
                yield file
            for subDir in Directory.GetDirectories(folderPath) do
                yield! getAllSvgFiles subDir
        }
       
    let convertAllSvgToIco folder recursive refresh sizes maxDegreeOfParallelism : Result<int,Exception> =
        Result.Error(new NotImplementedException())


    

