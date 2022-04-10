#include <flann/flann.h>

#include <stdio.h>
#include <stdlib.h>


#ifdef __APPLE__
#define DllExport(t) extern "C" __attribute__((visibility("default"))) t
#elif __GNUC__
#define DllExport(t) extern "C" __attribute__((visibility("default"))) t
#else
#define DllExport(t) extern "C"  __declspec( dllexport ) t __cdecl
#endif

typedef struct {
    struct FLANNParameters Parameters;
    flann_index_t Index;
} FlIndex;


DllExport(FlIndex*) flBuildIndex(float* data, int rows, int cols, struct FLANNParameters* pp);
DllExport(int) flFindNearest(FlIndex* index, float* data, int rows, int cols, int* indices, float* dists, int nn);
DllExport(void) flDeleteIndex(FlIndex* index);