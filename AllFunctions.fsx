open System
module Functions = 
    let rand = Random()
    let clone i (arr:'T[,]) = arr.[i..i, *]|> Seq.cast<'T> |> Seq.toArray
    let multiply text times = String.replicate times text