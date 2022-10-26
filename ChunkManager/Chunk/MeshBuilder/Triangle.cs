using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS.MeshBuilderNS {
    public readonly struct Triangle {
        public readonly Vector3 a;
        public readonly Vector3 b;
        public readonly Vector3 c;

        public Triangle(Vector3 a, Vector3 b, Vector3 c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public Vector3 NonNormalizedNormal {
            get => Vector3.Cross(this.b - this.a, this.c - this.a);
        }
    }
}
