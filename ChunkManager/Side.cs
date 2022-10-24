using System;

namespace MarcosPereira.Terrain.ChunkManagerNS {
    [Flags]
    public enum Side : byte {
        None = 0,
        Up = 1 << 0, // Z+
        Right = 1 << 1, // X+
        Down = 1 << 2, // Z-
        Left = 1 << 3 // X-
    }
}
