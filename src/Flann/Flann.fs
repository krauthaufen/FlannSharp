namespace Flann

open System
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open Microsoft.FSharp.NativeInterop

#nowarn "9"

type IndexAlgorithm =
    | Linear = 0
    | KdTree = 1
    | KMeans = 2
    | Composite = 3
    | KdTreeSingle = 4
    | Hierarchical = 5
    | LSH = 6
    | KdTreeCuda = 7
    | Saved = 254
    | Autotuned = 255

type CentersInit =
    | Random = 0
    | Gonzales = 1
    | KMeansPP = 2
    | Groupwise = 3
    
type LogLevel =
    | None = 0
    | Fatal = 1
    | Error = 2
    | Warning = 3
    | Info = 4
    | Debug = 5


[<Struct; StructLayout(LayoutKind.Sequential)>]
type Parameters =
    {
        Algorithm : IndexAlgorithm

        /// how many leafs (features) to check in one search
        Checks : int

        /// eps parameter for eps-knn search
        Epsilon : float32 

        /// indicates if results returned by radius search should be sorted or not 
        Sorted : int

        /// limits the maximum number of neighbors should be returned by radius search
        MaxNeighbours : int

        /// number of paralel cores to use for searching
        Cores : int

        /// number of randomized trees to use (for kdtree)
        Trees : int 
        LeafMaxSize : int

        /// branching factor (for kmeans tree)
        Branching : int

        /// max iterations to perform in one kmeans cluetering (kmeans tree)
        Iterations : int

        /// algorithm used for picking the initial cluster centers for kmeans tree
        CentersInit : CentersInit

        /// cluster boundary index. Used when searching the kmeans tree
        ClusterBoundaryIndex : float32

        /// precision desired (used for autotuning, -1 otherwise)
        TargetPrecision : float32

        /// build tree time weighting factor
        BuildWeight : float32 

        /// index memory weigthing factor
        MemoryWeight : float32

        /// what fraction of the dataset to use for autotuning
        SampleFraction : float32

        /// The number of hash tables to use
        TableNumber : int

        /// The length of the key in the hash tables
        KeySize : int

        /// Number of levels to use in multi-probe LSH, 0 for standard LSH
        MultiProbeLevel : int

        /// determines the verbosity of each flann function
        LogLevel : LogLevel

        /// random seed to use
        RandomSeed : uint32
    }

    static member Default =
        {
            Algorithm = IndexAlgorithm.KdTree
            Checks = 32
            Epsilon = 0.0f
            Sorted = 1
            MaxNeighbours = -1
            Cores = 0
            Trees = 4
            LeafMaxSize = 4

            Branching = 32
            Iterations = 11
            CentersInit = CentersInit.Random
            ClusterBoundaryIndex = 0.2f
            TargetPrecision = 0.9f
            BuildWeight = 0.01f
            MemoryWeight = 0.0f
            SampleFraction = 0.1f

            TableNumber = 0
            KeySize = 0
            MultiProbeLevel = 0

            LogLevel = LogLevel.None
            RandomSeed = 0u
        }

module FlannRaw = 
    [<Literal>]
    let lib = "FlannNative"

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type FlannIndexHandle private(handle : nativeint) =
        member x.IsNull = handle = 0n
        static member Null = FlannIndexHandle(0n)



    [<DllImport(lib)>]
    extern FlannIndexHandle flBuildIndex(float32* data, int rows, int cols, Parameters& parameters)
    
    [<DllImport(lib)>]
    extern void flDeleteIndex(FlannIndexHandle index)

    [<DllImport(lib)>]
    extern int flFindNearest(FlannIndexHandle index, float32* data, int rows, int cols, int* indices, float32* dists, int nn)


type Index private(handle : FlannRaw.FlannIndexHandle, rows : int, cols : int, data : nativeptr<float32>) =
    let mutable handle = handle
    let mutable data = data

    static member Build(mem : nativeptr<float32>, rows : int, cols : int, ?pars : Parameters) =
        let count = rows * cols 
        let size = nativeint count * nativeint sizeof<float32>
        let copy = Marshal.AllocHGlobal size |> NativePtr.ofNativeInt<float32>

        let src = Span<float32>(NativePtr.toVoidPtr mem, count)
        let dst = Span<float32>(NativePtr.toVoidPtr copy, count)
        src.CopyTo dst

        let mutable pars = defaultArg pars Parameters.Default
        let handle = FlannRaw.flBuildIndex(copy, rows, cols, &pars)
        if handle.IsNull then failwith "[Flann] failed to build index"
        new Index(handle, rows, cols, copy)

    static member Build(mem : Memory<float32>, rows : int, cols : int, ?pars : Parameters) =
        use ptr = mem.Pin()
        Index.Build(NativePtr.ofVoidPtr ptr.Pointer, rows, cols, ?pars = pars)

    static member Build(data : float32[], rows : int, cols : int, ?pars : Parameters) =
        Index.Build(Memory data, rows, cols, ?pars = pars)

    member x.FindClosest(query : nativeptr<float32>, queryRows : int) =
        if queryRows <= 0 || rows <= 0 then
            [||]
        else
            let indices = Array.zeroCreate<int> (queryRows)
            let dists = Array.zeroCreate<float32> (queryRows)
            use pIndices = fixed indices
            use pDists = fixed dists
            let ret = FlannRaw.flFindNearest(handle, query, queryRows, cols, pIndices, pDists, 2)
            if ret <> 0 then failwith "[Flann] failed to find closest"

            let result = Array.zeroCreate queryRows
            let mutable ri = 0
            for qi in 0 .. queryRows - 1 do

                let i0 = indices.[ri]
                let d0 = dists.[ri]
                ri <- ri + 1

                result.[qi] <- struct(i0, d0)
            result
        
    member x.FindClosest2(query : nativeptr<float32>, queryRows : int) =
        if queryRows <= 0 || rows < 2 then
            [||]
        else
            let indices = Array.zeroCreate<int> (2 * queryRows)
            let dists = Array.zeroCreate<float32> (2 * queryRows)
            use pIndices = fixed indices
            use pDists = fixed dists
            let ret = FlannRaw.flFindNearest(handle, query, queryRows, cols, pIndices, pDists, 2)
            if ret <> 0 then failwith "[Flann] failed to find closest"

            let result = Array.zeroCreate queryRows
            let mutable ri = 0
            for qi in 0 .. queryRows - 1 do

                let i0 = indices.[ri]
                let d0 = dists.[ri]
                ri <- ri + 1
                let i1 = indices.[ri]
                let d1 = dists.[ri]
                ri <- ri + 1

                result.[qi] <- struct(i0, d0, i1, d1)
            result
        
    member x.FindClosestK(query : nativeptr<float32>, queryRows : int, k : int) =
        if queryRows <= 0 || rows <= 1 then
            [||]
        else
            let k = min k rows
            let indices = Array.zeroCreate<int> (k * queryRows)
            let dists = Array.zeroCreate<float32> (k * queryRows)
            use pIndices = fixed indices
            use pDists = fixed dists
            let ret = FlannRaw.flFindNearest(handle, query, queryRows, cols, pIndices, pDists, k)
            if ret <> 0 then failwith "[Flann] failed to find closest"

            let result = Array.zeroCreate queryRows
            let mutable ri = 0
            for qi in 0 .. queryRows - 1 do
                let part = Array.zeroCreate k
                for ki in 0 .. k - 1 do
                    let i = indices.[ri + ki]
                    let d = dists.[ri + ki]
                    part.[ki] <- struct(i, d)
                    ri <- ri + 1
                result.[qi] <- part
            result
   

    member x.FindClosest(query : Memory<float32>, rows : int) =
        use h = query.Pin()
        x.FindClosest(NativePtr.ofVoidPtr h.Pointer, rows)
        
    member x.FindClosest2(query : Memory<float32>, rows : int) =
        use h = query.Pin()
        x.FindClosest2(NativePtr.ofVoidPtr h.Pointer, rows)

    member x.FindClosestK(query : Memory<float32>, rows : int, k : int) =
        use h = query.Pin()
        x.FindClosestK(NativePtr.ofVoidPtr h.Pointer, rows, k)


    member x.FindClosest(query : float32[], rows : int) =
        use h = fixed query
        x.FindClosest(h, rows)

    member x.FindClosest2(query : float32[], rows : int) =
        use h = fixed query
        x.FindClosest2(h, rows)

    member x.FindClosestK(query : float32[], rows : int, k : int) =
        use h = fixed query
        x.FindClosestK(h, rows, k)


    member x.Data = data
    member x.Handle = handle

    member x.Dispose(disposing : bool) =
        if not handle.IsNull then
            if disposing then GC.SuppressFinalize x
            FlannRaw.flDeleteIndex handle
            handle <- FlannRaw.FlannIndexHandle.Null
            Marshal.FreeHGlobal (NativePtr.toNativeInt data)
            data <- NativePtr.ofNativeInt 0n

    member x.Dispose() = x.Dispose true

    override x.Finalize() = x.Dispose false

    interface IDisposable with
        member x.Dispose() = x.Dispose()