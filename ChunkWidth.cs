// Chunk width must be a power of two due to how their lower detail meshes are
// generated.
namespace MarcosPereira.Terrain {
    public enum ChunkWidth : int {
        _8,
        _16,
        _32,
        _64,
        _128,
        _256
    }
}
