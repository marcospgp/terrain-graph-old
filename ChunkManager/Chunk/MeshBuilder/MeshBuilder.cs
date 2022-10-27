using System.Collections.Generic;
using System.Threading.Tasks;
using MarcosPereira.Terrain.ChunkManagerNS.ChunkNS.MeshBuilderNS;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class MeshBuilder {
        // Each step, a vertex (V) builds the triangles around it.
        // For vertex normal purposes, these are "even" vertices (the sum of
        // their relative coordinates is even). Other vertices are "odd".
        //
        // *-----------*-----------*
        // |         / | \         |
        // |   0   /   |   \   3   |
        // |     /     |     \     |
        // |   /   1   |   2   \   |
        // | /         |         \ |
        // *---------- V ----------*
        // | \         |         / |
        // |   \   7   |   4   /   |
        // |     \     |     /     |
        // |   6   \   |   /   5   |
        // |         \ | /         |
        // *-----------*-----------*
        //
        // Dealing with higher detail neighbors:
        //
        // Top:
        //
        // *-----*-----*-----*-----*
        // |    / \    |    / \    |
        // |   /   \   |   /   \   |
        // |  /     \  |  /     \  |
        // | /       \ | /       \ |
        // |/         \|/         \|
        // *---------- V ----------*
        // | \         |         / |
        // |   \       |       /   |
        // |     \     |     /     |
        // |       \   |   /       |
        // |         \ | /         |
        // *-----------*-----------*
        //
        // Top + right:
        //
        // *-----*-----*-----*-----*
        // |    / \    |    /  \   |
        // |   /   \   |   /     \ |
        // |  /     \  |  /      _ *
        // | /       \ | /   _ -   |
        // |/         \|/ _-       |
        // *---------- V ----------*
        // | \         | - _       |
        // |   \       |     - _   |
        // |     \     |         - *
        // |       \   |     _ -   |
        // |         \ | _ -       |
        // *-----------*-----------*
        //
        // Other cases are symmetries of the above.
        //

        private static readonly RelativeTriangle t0 = new RelativeTriangle(
            (-1, 0), (-1, 1), (0, 1)
        );

        private static readonly RelativeTriangle t1 = new RelativeTriangle(
            (0, 0), (-1, 0), (0, 1)
        );

        private static readonly RelativeTriangle t2 = new RelativeTriangle(
            (0, 0), (0, 1), (1, 0)
        );

        private static readonly RelativeTriangle t3 = new RelativeTriangle(
            (0, 1), (1, 1), (1, 0)
        );

        // Baseline triangles - those built by a vertex with no higher detail
        // neighbor.
        private static readonly RelativeTriangle[] ts = new RelativeTriangle[] {
                t0, t1, t2, t3, t0 + (1, -1), t1 + (1, -1), t2 + (-1, -1),
                t3 + (-1, -1)
        };

        // Triangles built by vertex with higher detail upper neighbor.
        //
        // Has a step size of 2 to allow representing smaller triangles when
        // bordering higher detail neighbors.
        private static readonly RelativeTriangle[] topHD = new RelativeTriangle[] {
            new RelativeTriangle((-2, 0), (-2, 2), (-1, 2)),
            new RelativeTriangle((-2, 0), (-1, 2), (0, 0)),
            new RelativeTriangle((0, 0), (-1, 2), (0, 2)),
            new RelativeTriangle((0, 0), (0, 2), (1, 2)),
            new RelativeTriangle((0, 0), (1, 2), (2, 0)),
            new RelativeTriangle((1, 2), (2, 2), (2, 0)),
            ts[4] * 2, ts[5] * 2, ts[6] * 2, ts[7] * 2
        };

        // Also has step size 2.
        private static readonly RelativeTriangle[] topRightHD = new RelativeTriangle[] {
            topHD[0], topHD[1], topHD[2], topHD[3],
            new RelativeTriangle((0, 0), (1, 2), (2, 1)),
            new RelativeTriangle((1, 2), (2, 2), (2, 1)),
            new RelativeTriangle((0, 0), (2, 1), (2, 0)),
            new RelativeTriangle((0, 0), (2, 0), (2, -1)),
            new RelativeTriangle((0, 0), (2, -1), (0, -2)),
            new RelativeTriangle((0, -2), (2, -1), (2, -2)),
            new RelativeTriangle((0, 0), (0, -2), (-2, 0)),
            new RelativeTriangle((0, -2), (-2, -2), (-2, 0))
        };

        // Triangles built by each "even" vertex, taking into account possible
        // higher detail neighbor chunks.
        //
        // Note that all entries have a step size of 2, since one has to
        // accommodate for smaller triangles when bordering a higher detail
        // neighboring chunk.
        private static readonly RelativeTriangle[][] vertexTrianglesStepSize2 =
            new RelativeTriangle[][] {
                // No higher detail neighbors
                RelativeTriangle.Multiply(ts, 2),
                // Higher detail neighbor on top
                topHD,
                // Higher detail neighbor on top + right
                topRightHD,
                // Higher detail neighbor on right
                RelativeTriangle.Turn90DegClockwise(topHD),
                // Higher detail neighbor on right + bottom
                RelativeTriangle.Turn90DegClockwise(topRightHD),
                // Higher detail neighbor on bottom
                RelativeTriangle.Turn90DegClockwise(topHD, 2),
                // Higher detail neighbor on bottom + left
                RelativeTriangle.Turn90DegClockwise(topRightHD, 2),
                // Higher detail neighbor on left
                RelativeTriangle.Turn90DegClockwise(topHD, 3),
                // Higher detail neighbor on left + top
                RelativeTriangle.Turn90DegClockwise(topRightHD, 2)
            };

        // Adjacent triangles, used to calculate vertex normals.
        // For this, we assume highest detail level.
        private static readonly RelativeTriangle[][] adjacentTriangles =
            new RelativeTriangle[][] {
                // Even vertices
                new RelativeTriangle[] {
                    ts[1],
                    ts[2],
                    ts[7],
                    ts[4]
                },

                // Odd vertices
                new RelativeTriangle[] {
                    ts[0] + (1, 0),
                    ts[1] + (1, 0),
                    ts[6] + (1, 0),
                    ts[7] + (1, 0),

                    ts[2] + (-1, 0),
                    ts[3] + (-1, 0),
                    ts[4] + (-1, 0),
                    ts[5] + (-1, 0)
                }
            };

        public static async Task<Mesh> BuildChunkMesh(
            Chunk chunk,
            int reductionLevel,
            Side higherDetailNeighbor,
            string name = "Unnamed mesh"
        ) {
            float[,] bhm = chunk.borderedHeightmap;

            // Width
            int w = chunk.borderedHeightmap.GetLength(0) - 2;

            // Step is 2^reductionLevel.
            // Each reduction level cuts the number of vertices in half.
            int step = 1 << reductionLevel;

            var triangles = new List<int>();

            // Build triangles first so we know which vertices we'll need
            void BuildTriangles((int i, int j) pos) {
                RelativeTriangle[] ts =
                    GetVertexTrianglesStepSize2(pos, w, step, higherDetailNeighbor);

                foreach (RelativeTriangle offset in ts) {
                    var triangle = pos + ((offset * step) / 2);

                    triangles.Add(GetIndex(triangle.a, w));
                    triangles.Add(GetIndex(triangle.b, w));
                    triangles.Add(GetIndex(triangle.c, w));
                }
            }

            for (int i = step; i < w - step; i += step * 2) {
                for (int j = step; j < w - step; j += step * 2) {
                    BuildTriangles((i, j));
                }
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            int[] map = new int[w * w];

            for (int i = 0; i < map.Length; i++) {
                map[i] = -1;
            }

            for (int t = 0; t < triangles.Count; t++) {
                int index = triangles[t];

                // If this vertex index hasn't been seen before, create an
                // entry for it in the map, and create its corresponding
                // vertex.
                if (map[index] == -1) {
                    map[index] = vertices.Count;

                    Vector2Int pos = GetIndexPosition(index, w);
                    vertices.Add(
                        new Vector3(pos.x, bhm[pos.x + 1, pos.y + 1], pos.y)
                    );

                    normals.Add(chunk.vertexNormals[pos.x, pos.y]);
                }

                triangles[t] = map[index];
            }

            var mesh = new Mesh() {
                name = name,
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                triangles = triangles.ToArray()
            };

            // Do not optimize as meshes are created at run time.
            // mesh.Optimize();

            return mesh;
        }

        public static async Task<Vector3[,]> CalculateVertexNormals(Chunk chunk) {
            // Bordered width
            int bw = chunk.borderedHeightmap.GetLength(0);

            var normals = new Vector3[bw - 2, bw - 2];

            for (int i = 1; i < bw - 1; i++) {
                for (int j = 1; j < bw - 1; j++) {
                    normals[i - 1, j - 1] = GetVertexNormal(chunk, (i, j));
                }
            }

            return normals;
        }

        private static Vector3 GetVertexNormal(Chunk chunk, (int x, int z) pos) {
            bool isEven = (pos.x + pos.z) % 2 == 0;

            RelativeTriangle[] triangles;

            if (isEven) {
                triangles = adjacentTriangles[0];
            } else {
                triangles = adjacentTriangles[1];
            }

            Vector3 normal = Vector3.zero;

            float[,] bhm = chunk.borderedHeightmap;

            foreach (RelativeTriangle relative in triangles) {
                Triangle t = relative.ToAbsolute(pos, bhm);

                normal += t.NonNormalizedNormal;
            }

            return normal.normalized;
        }

        // Get the index of the vertex at (X, Z) in a chunk of given width.
        private static int GetIndex(Vector2Int pos, int width) =>
            (pos.y * width) + pos.x;

        // Get top-down position of vertex with given index in a chunk of given
        // width.
        private static Vector2Int GetIndexPosition(int i, int width) =>
            new Vector2Int(i % width, i / width);

        private static RelativeTriangle[] GetVertexTrianglesStepSize2(
            (int x, int z) pos,
            int width,
            int step,
            Side higherDetailNeighbor
        ) {
            Side hd = higherDetailNeighbor;
            var ts = MeshBuilder.vertexTrianglesStepSize2;

            Side b = GetBorder(pos, width, step);

            if (hd.HasFlag(Side.Up) && b.HasFlag(Side.Up)) {
                if (hd.HasFlag(Side.Right) && b.HasFlag(Side.Right)) {
                    return ts[2];
                }

                return ts[1];
            } else if (hd.HasFlag(Side.Right) && b.HasFlag(Side.Right)) {
                if (hd.HasFlag(Side.Down) && b.HasFlag(Side.Down)) {
                    return ts[4];
                }

                return ts[3];
            } else if (hd.HasFlag(Side.Down) && b.HasFlag(Side.Down)) {
                if (hd.HasFlag(Side.Left) && b.HasFlag(Side.Left)) {
                    return ts[6];
                }

                return ts[5];
            } else if (hd.HasFlag(Side.Left) && b.HasFlag(Side.Left)) {
                if (hd.HasFlag(Side.Up) && b.HasFlag(Side.Up)) {
                    return ts[8];
                }

                return ts[7];
            }

            return ts[0];
        }

        private static Side GetBorder(
            (int x, int z) vertex,
            int width,
            int step
        ) {
            int first = step;
            int last = width - step - 1;

            if (vertex.z == last) {
                if (vertex.x == last) {
                    return Side.Up | Side.Right;
                }

                return Side.Up;
            } else if (vertex.x == last) {
                if (vertex.z == first) {
                    return Side.Right | Side.Down;
                }

                return Side.Right;
            } else if (vertex.z == first) {
                if (vertex.x == first) {
                    return Side.Down | Side.Left;
                }

                return Side.Down;
            } else if (vertex.x == first) {
                if (vertex.z == last) {
                    return Side.Left | Side.Up;
                }

                return Side.Left;
            }

            return Side.None;
        }
    }
}
