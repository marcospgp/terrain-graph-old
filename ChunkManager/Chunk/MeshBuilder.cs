using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class MeshBuilder {
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

                        int vi = GetIndex(i, j, w);

                        vertices[vi] = vertex;
                        normals[vi] = chunk.vertexNormals[i, j];

                        if (i > 0 && j > 0) {
                            int ti = GetTriangleIndex(i, j, w);

                            triangles[ti] = vi;
                            triangles[ti + 1] = GetIndex(i - 1, j - 1, w);
                            triangles[ti + 2] = GetIndex(i - 1, j, w);

                            triangles[ti + 3] = vi;
                            triangles[ti + 4] = GetIndex(i, j - 1, w);
                            triangles[ti + 5] = GetIndex(i - 1, j - 1, w);
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
            int w = chunk.borderedHeightmap.GetLength(0) - 2;
            var normals = new Vector3[w, w];

            await SafeTask.Run(() => {
                for (int i = 0; i < w; i++) {
                    for (int j = 0; j < w; j++) {
                        // TODO
                        normals[i, j] = Vector3.up;
                    }
                }
            });

            return normals;
        }

        // Get the index of the vertex at (X, Z) in a chunk of given width.
        private static int GetIndex(int x, int z, int width) => (z * width) + x;

        private static int GetTriangleIndex(int x, int z, int width) =>
            (((z - 1) * (width - 1)) + (x - 1)) * 2 * 3;
    }
}
