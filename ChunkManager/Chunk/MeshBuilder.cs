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
            Vector3 normal = Vector3.zero;

            float[,] bhm = chunk.borderedHeightmap;

            foreach ((int, int)[] t in vertexNormalTriangles) {
                var v = new (int x, int z)[3];

                for (int i = 0; i < 3; i++) {
                    (int x, int z) offset = t[i];
                    v[i] = pos.Add(offset);
                }

                var triangle = new Vector3[3];

                triangle[0] = new Vector3(v[0].x, bhm[v[0].x, v[0].z], v[0].z);
                triangle[1] = new Vector3(v[1].x, bhm[v[1].x, v[1].z], v[1].z);
                triangle[2] = new Vector3(v[2].x, bhm[v[2].x, v[2].z], v[2].z);

                normal += Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]);
            }

            return normal;
        }

        // Get the index of the vertex at (X, Z) in a chunk of given width.
        private static int GetIndex((int x, int z) pos, int width) =>
            (pos.z * width) + pos.x;

        private static int GetTriangleIndex((int x, int z) pos, int width) =>
            (((pos.z - 1) * (width - 1)) + (pos.x - 1)) * 2 * 3;
    }
}
