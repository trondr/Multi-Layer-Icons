namespace SvgToIco
open NCmdLiner.Attributes
open Serilog

module Commands =
    
    [<Commands>]
    type CommandDefinitions =
        
        [<Command(Description = "Convert svg files in folder (and subfolders if recursive==true) to multi size icon files of specified sizes.")>]
        static member ConvertAllSvgToIco(
            [<RequiredCommandParameter(Description = "Folder to search for svg files",AlternativeName = "d", ExampleValue = @"c:\temp\icons")>]
            folder:string,
            [<OptionalCommandParameter(Description = "Recurse sub folders.", AlternativeName = "r", DefaultValue = true, ExampleValue = true)>]
            recursive:bool,
            [<OptionalCommandParameter(Description = "Recreate icon even if it already exists.",AlternativeName = "rf", DefaultValue = false, ExampleValue = false)>]
            refresh:bool,
            [<OptionalCommandParameter(Description = "Create multi size icon with the specified array of sizes", AlternativeName = "s", ExampleValue = [| 16; 32; 64; 128; 256 |], DefaultValue = [| 16; 32; 64; 128; 256 |])>]
            sizes: int[],
            [<OptionalCommandParameter(Description = "Run specified number of conversions in parallel.", AlternativeName = "p", DefaultValue = 4, ExampleValue = 4)>]
            maxDegreeOfParallelism:int) : LanguageExt.Common.Result<int> =                
                    let logger = Log.Logger
                    logger.Warning "TODO: Implement ConvertAllSvgToIco command"
                    let result = SvgToIcoConversion.convertAllSvgToIco folder recursive refresh sizes maxDegreeOfParallelism
                    match result with
                    |Ok i-> LanguageExt.Common.Result(i)
                    |Error ex ->
                        Log.Logger.Error(ex.Message)
                        LanguageExt.Common.Result(1)