using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS.MeshBuilderNS {
    public readonly struct RelativeTriangle {
        public readonly Vector2Int a;
        public readonly Vector2Int b;
        public readonly Vector2Int c;

        public RelativeTriangle((int, int) a, (int, int) b, (int, int) c) {
            this.a = new Vector2Int(a.Item1, a.Item2);
            this.b = new Vector2Int(b.Item1, b.Item2);
            this.c = new Vector2Int(c.Item1, c.Item2);
        }

        public RelativeTriangle(Vector2Int a, Vector2Int b, Vector2Int c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public static RelativeTriangle operator +(
            RelativeTriangle t,
            (int x, int y) u
        ) =>
            RelativeTriangle.Add(t, u);

        public static RelativeTriangle operator +(
            (int x, int y) u,
            RelativeTriangle t
        ) =>
            RelativeTriangle.Add(t, u);

        public static RelativeTriangle operator *(
            int u,
            RelativeTriangle t
        ) =>
            RelativeTriangle.Multiply(t, u);

        public static RelativeTriangle operator *(
            RelativeTriangle t,
            int u
        ) =>
            RelativeTriangle.Multiply(t, u);

        public static RelativeTriangle operator *(
            RelativeTriangle t,
            (int x, int y) u
        ) =>
            RelativeTriangle.Multiply(t, u);

        public static RelativeTriangle operator *(
            (int x, int y) u,
            RelativeTriangle t
        ) =>
            RelativeTriangle.Multiply(t, u);

        public static RelativeTriangle operator /(
            RelativeTriangle t,
            (int x, int y) u
        ) {
            var uVec = new Vector2Int(u.x, u.y);

            static Vector2Int Divide(Vector2Int a, Vector2Int b) =>
                new Vector2Int(a.x / b.x, a.y / b.y);

            return new RelativeTriangle(
                Divide(t.a, uVec), Divide(t.b, uVec), Divide(t.c, uVec)
            );
        }

        public static RelativeTriangle[] Multiply(
            RelativeTriangle[] ts,
            (int x, int y) u
        ) {
            var newTs = new RelativeTriangle[ts.Length];

            for (int i = 0; i < ts.Length; i++) {
                newTs[i] = ts[i] * u;
            }

            return newTs;
        }

        public static RelativeTriangle[] Divide(
            RelativeTriangle[] ts,
            (int x, int y) u
        ) {
            var newTs = new RelativeTriangle[ts.Length];

            for (int i = 0; i < ts.Length; i++) {
                newTs[i] = ts[i] / u;
            }

            return newTs;
        }

        // Rotate an array of triangles 90 degrees clockwise a given number
        // of times.
        public static RelativeTriangle[] Turn90DegClockwise(
            RelativeTriangle[] ts,
            int ninetyDegreeTurns = 1
        ) {
            var newTs = new RelativeTriangle[ts.Length];

            static Vector2Int Turn(Vector2Int v) =>
                new Vector2Int(v.y, -v.x);

            for (int i = 0; i < ts.Length; i++) {
                for (int j = 0; j < ninetyDegreeTurns; j++) {
                    newTs[i] = new RelativeTriangle(
                        Turn(ts[i].a),
                        Turn(ts[i].b),
                        Turn(ts[i].c)
                    );
                }
            }

            return newTs;
        }

        public Triangle ToAbsolute((int, int) offset, float[,] heightmap) {
            RelativeTriangle t = this + offset;

            return new Triangle(
                new Vector3(t.a.x, heightmap[t.a.x, t.a.y], t.a.y),
                new Vector3(t.b.x, heightmap[t.b.x, t.b.y], t.b.y),
                new Vector3(t.c.x, heightmap[t.c.x, t.c.y], t.c.y)
            );
        }

        private static RelativeTriangle Add(
            RelativeTriangle t,
            (int x, int y) u
        ) {
            var uVector = new Vector2Int(u.x, u.y);

            return new RelativeTriangle(
                t.a + uVector, t.b + uVector, t.c + uVector
            );
        }

        private static RelativeTriangle Multiply(
            RelativeTriangle t,
            int u
        ) =>
            new RelativeTriangle(
                t.a * u, t.b * u, t.c * u
            );

        private static RelativeTriangle Multiply(
            RelativeTriangle t,
            (int x, int y) u
        ) {
            var uVec = new Vector2Int(u.x, u.y);

            return new RelativeTriangle(
                t.a * uVec, t.b * uVec, t.c * uVec
            );
        }
    }
}
