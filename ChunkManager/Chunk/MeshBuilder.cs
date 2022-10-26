using System.Collections.Generic;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class MeshBuilder {
        // Each step, a vertex (V) builds the triangles around it.
        // For vertex normal purposes, these are "even" vertices (the sum of
        // their relative coordinates is even). Other vertices are "odd".
        //
        //  _______________
        // |     / | \     |
        // | 0  /  |  \  3 |
        // |   /   |   \   |
        // |  / 1  | 2  \  |
        // | /     |     \ |
        //  ------ V ------
        // | \     |     / |
        // |  \  7 | 4  /  |
        // |   \   |   /   |
        // | 6  \  |  /  5 |
        // |_____\ | /_____|
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

        // Even vertices must build these triangles around themselves.
        private static readonly RelativeTriangle[] vertexTriangles = new RelativeTriangle[] {
            t0, t1, t2, t3, t0 + (1, -1), t1 + (1, -1), t2 + (-1, -1), t3 + (-1, -1)
        };

        // Adjacent triangles, used to calculate vertex normals.
        private static readonly RelativeTriangle[][] adjacentTriangles =
            new RelativeTriangle[][] {
                // Even vertices
                new RelativeTriangle[] {
                    vertexTriangles[1],
                    vertexTriangles[2],
                    vertexTriangles[7],
                    vertexTriangles[4]
                },

                // Odd vertices
                new RelativeTriangle[] {
                    vertexTriangles[0] + (1, 0),
                    vertexTriangles[1] + (1, 0),
                    vertexTriangles[6] + (1, 0),
                    vertexTriangles[7] + (1, 0),

                    vertexTriangles[2] + (-1, 0),
                    vertexTriangles[3] + (-1, 0),
                    vertexTriangles[4] + (-1, 0),
                    vertexTriangles[5] + (-1, 0)
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
            await SafeTask.Run(() => {
                void BuildTriangles((int i, int j) pos) {
                    foreach (RelativeTriangle offset in vertexTriangles) {
                        var triangle = pos + (offset * step);

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
            });

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            int[] map = new int[w * w];

            // Build vertices
            await SafeTask.Run(() => {
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

                map = null;

                // for (int i = 0; i < w; i += step) {
                //     for (int j = 0; j < w; j += step) {
                //         if (i == 0 && higherDetailNeighbor.HasFlag(Side.Down)) {

                //         } else if (i == w - 1 && higherDetailNeighbor.HasFlag(Side.Up)) {

                //         } else if (j == 0 && higherDetailNeighbor.HasFlag(Side.Left)) {

                //         } else if (j == w - 1 && higherDetailNeighbor.HasFlag(Side.Right)) {

                //         }
                //     }
                // }
            });

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
            float[,] bhm = chunk.borderedHeightmap;

            // Bordered width
            int bw = chunk.borderedHeightmap.GetLength(0);

            var normals = new Vector3[bw - 2, bw - 2];

            await SafeTask.Run(() => {
                for (int i = 1; i < bw - 1; i++) {
                    for (int j = 1; j < bw - 1; j++) {
                        normals[i - 1, j - 1] = GetVertexNormal(chunk, (i, j));
                    }
                }
            });

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

        private static int GetTriangleIndex((int x, int z) pos, int width) =>
            (((pos.z - 1) * (width - 1)) + (pos.x - 1)) * 2 * 3;

        private readonly struct RelativeTriangle {
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
        }

        private readonly struct Triangle {
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
}
