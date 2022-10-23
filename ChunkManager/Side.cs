namespace MarcosPereira.Terrain.ChunkManagerNS {
    public enum Side : byte {
        None = 0,
        Up = 1, // Z positive
        Right = 2, // X positive
        Down = 4, // Z negative
        Left = 8 // X negative
    }
}
