namespace SvgToIco

open System
open Serilog

module F =
    
    let arrayToSeq (array:Array) =
        seq{
            for item in array do
                yield item
        }
    
    let getEnumValuesToString (enumType) =
        let enumValues = 
            Enum.GetValues(enumType)
            |>arrayToSeq
            |>Seq.map(fun v -> v.ToString())
            |>Seq.toArray
        "[" + String.Join("|",enumValues) + "]"

    //Source: http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    let ofOption error = function Some s -> Result.Ok s | None -> Result.Error error
    
    //Source: http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    type ResultBuilder() =
        member __.Return(x) = Ok x

        member __.ReturnFrom(m: Result<_, _>) = m

        member __.Bind(m, f) = Result.bind f m
        member __.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

        member __.Zero() = None

        member __.Combine(m, f) = Result.bind f m

        member __.Delay(f: unit -> _) = f

        member __.Run(f) = f()

        member __.TryWith(m, h) =
            try __.ReturnFrom(m)
            with e -> h e

        member __.TryFinally(m, compensation) =
            try __.ReturnFrom(m)
            finally compensation()

        member __.Using(res:#IDisposable, body) =
            __.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

        member __.While(guard, f) =
            if not (guard()) then Ok () else
            do f() |> ignore
            __.While(guard, f)

        member __.For(sequence:seq<_>, body) =
            __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))

    let result = new ResultBuilder()
    
    let sourceException ex = 
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).SourceException

    let toException message (innerException: System.Exception option) =
        match innerException with
        |Some iex ->
            (new System.Exception(message, sourceException iex))            
        |None ->
            (new System.Exception(message))
    
    let toErrorResult ex message =
       match message with
       |Some m -> Result.Error (toException m (Some ex))
       |None -> Result.Error (sourceException ex)
    
    let tryCatch7<'T1,'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) (t5:'T5) (t6:'T6) (t7:'T7) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4 t5 t6 t7)
        with
            | ex -> toErrorResult ex message
            
    let resultToOption (logger:ILogger) (result : Result<_,Exception>) =
        match result with
        |Result.Ok s -> Some s
        |Result.Error ex -> 
            logger.Error(ex.Message)
            None
