namespace MarcosPereira.Terrain {
    // Chunk width must be power of two to simplify building lower detail
    // meshes.
    public enum ChunkWidth : int {
        _8 = 8,
        _16 = 16,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256
    }
}
