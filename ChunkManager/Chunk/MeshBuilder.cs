using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class MeshBuilder {
        // Each vertex (V) builds the triangles to its bottom left.
        //
        //  ------ V
        // |     / |
        // | 1  /  |
        // |   /   |
        // |  /  2 |
        // | /_____|
        //
        private static readonly (int, int)[][] vertexTriangles = new (int, int)[][] {
            new (int, int)[] { (0, 0), (-1, -1), (-1, 0) },
            new (int, int)[] { (0, 0), (0, -1), (-1, -1) }
        };

        // Triangles that include each vertex, used to calculate vertex normals.
        private static readonly (int, int)[][] vertexNormalTriangles = new (int, int)[][] {
            // Bottom left
            vertexTriangles[0],
            vertexTriangles[1],

            // Bottom right
            new (int, int)[] { (0, 0), (1, 0), (0, -1) },

            // Upper left
            new (int, int)[] { (0, 0), (-1, 0), (0, 1) },

            // Upper right
            new (int, int)[] { (0, 0), (0, 1), (1, 1) },
            new (int, int)[] { (0, 0), (1, 1), (0, 1) }
        };

        public static async Task<Mesh> BuildChunkMesh(
            Chunk chunk,
            int reductionLevel,
            Side higherDetailNeighbors,
            string name = "Unnamed mesh"
        ) {
            // Width in vertices.
            int w = chunk.borderedHeightmap.GetLength(0) - 2;

            // Step is 2^reductionLevel.
            // Each reduction level cuts the number of vertices in half.
            int step = 1 << reductionLevel;

            var vertices = new Vector3[w * w];
            var normals = new Vector3[w * w];
            int[] triangles = new int[(w - 1) * (w - 1) * 2 * 3];

            await SafeTask.Run(() => {
                for (int i = 0; i < w; i++) {
                    for (int j = 0; j < w; j++) {
                        var vertex = new Vector3(
                            i,
                            chunk.borderedHeightmap[i + 1, j + 1],
                            j
                        );

                        int vi = GetIndex((i, j), w);

                        vertices[vi] = vertex;
                        normals[vi] = chunk.vertexNormals[i, j];

                        // Build triangles
                        if (i > 0 && j > 0) {
                            int ti = GetTriangleIndex((i, j), w);

                            foreach ((int, int)[] triangle in vertexTriangles) {
                                for (int k = 0; k < 3; k++) {
                                    triangles[ti + k] = GetIndex((i, j).Add(triangle[k]), w);
                                }

                                ti += 3;
                            }
                        }
                    }
                }
            });

            var mesh = new Mesh() {
                name = name,
                vertices = vertices,
                normals = normals,
                triangles = triangles
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

            static Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c) =>
                Vector3.Cross(b - a, c - a);

            await SafeTask.Run(() => {
                for (int i = 1; i < bw - 1; i++) {
                    for (int j = 1; j < bw - 1; j++) {
                        // TODO
                        normals[i - 1, j - 1] = Vector3.up;
                    }
                }
            });

            return normals;
        }

        // Get the index of the vertex at (X, Z) in a chunk of given width.
        private static int GetIndex((int x, int z) pos, int width) =>
            (pos.z * width) + pos.x;

        private static int GetTriangleIndex((int x, int z) pos, int width) =>
            (((pos.z - 1) * (width - 1)) + (pos.x - 1)) * 2 * 3;
    }
}
