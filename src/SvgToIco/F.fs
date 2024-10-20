namespace SvgToIco

open System

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

