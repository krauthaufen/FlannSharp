open Flann
open Aardvark.Base
open System.IO
#nowarn "9"

[<EntryPoint>]
let main argv =
    Aardvark.Init()

    let a = File.ReadAllBytes "/Users/Schorsch/Desktop/mondo/bytes0" |> Array.map float32
    let b = File.ReadAllBytes "/Users/Schorsch/Desktop/mondo/bytes1" |> Array.map float32

    let dim = 61
    let ca = a.Length / dim
    let cb = b.Length / dim


    let cfg = { Parameters.Default with Cores = 8 }

    printf "build %d" ca
    let sw = System.Diagnostics.Stopwatch.StartNew()
    use ia = Index.Build(a, ca, dim, cfg)
    printfn " took: %.3fs" sw.Elapsed.TotalSeconds

    printf "build %d" cb
    sw.Restart()
    use ib = Index.Build(b, cb, dim, cfg)
    printfn " took: %.3fs" sw.Elapsed.TotalSeconds

    printf "lookup %d" ib.Rows
    sw.Restart()
    let res = ia.FindClosest2(ib.Data, ib.Rows)
    printfn " took: %.3fs" sw.Elapsed.TotalSeconds


    printf "lookup %d" ia.Rows
    sw.Restart()
    let res = ib.FindClosest2(ia.Data, ia.Rows)
    printfn " took: %.3fs" sw.Elapsed.TotalSeconds

    0