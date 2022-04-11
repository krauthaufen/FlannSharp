open Flann
open Aardvark.Base
open System.IO
#nowarn "9"

Aardvark.Init()

let a = File.ReadAllBytes "/Users/Schorsch/Desktop/mondo/bytes0" |> Array.map float32
let b = File.ReadAllBytes "/Users/Schorsch/Desktop/mondo/bytes1" |> Array.map float32

let dim = 61
let ca = a.Length / dim
let cb = b.Length / dim


let ia = Index.Build(a, ca, dim)


let sw = System.Diagnostics.Stopwatch.StartNew()
let res = ia.FindClosest2(b, cb)
sw.Stop()



printfn "%A (%.3fs)" res sw.Elapsed.TotalSeconds