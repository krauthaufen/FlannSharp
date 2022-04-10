open Flann
open Aardvark.Base

#nowarn "9"

Aardvark.Init()

let rand = RandomSystem()


let cnt = 1024
let a =
    Array.init cnt (fun _ ->
        rand.UniformV3f()
    )
    |> Array.collect (fun v -> [|v.X; v.Y; v.Z|])


let b =
    Array.init cnt (fun _ ->
        rand.UniformV3f()
    )
    |> Array.collect (fun v -> [|v.X; v.Y; v.Z|])


let ia = Index.Build(a, cnt, 3)


let res = ia.FindClosest2(b, cnt)
printfn "%A" res