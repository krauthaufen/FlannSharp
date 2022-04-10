#include "FlannNative.h"

DllExport(FlIndex*) flBuildIndex(float* data, int rows, int cols, struct FLANNParameters* pp)
{
    struct FLANNParameters p = *pp;
    
    float speedup;
    flann_index_t index = flann_build_index(data, rows, cols, &speedup, &p);
    if(index == NULL) return NULL;

    FlIndex* res = new FlIndex[1];
    res->Index = index;
    res->Parameters = p;
    return res;
}

DllExport(int) flFindNearest(FlIndex* index, float* data, int rows, int cols, int* indices, float* dists, int nn)
{
    return flann_find_nearest_neighbors_index(index->Index, data, rows, indices, dists, nn, &index->Parameters);
}

DllExport(void) flDeleteIndex(FlIndex* index) {
    if(index == NULL) return;
    flann_free_index(index->Index, &index->Parameters);
    delete[] index;
}