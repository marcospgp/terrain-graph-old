using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class MeshBuilder {
        public static async Task<Mesh> BuildChunkMesh(
            Chunk chunk,
            string name = "Unnamed mesh",
            int vertexReductionFactor = 1
        ) {
            // Width in vertices.
            // Chunk is built with a border in order to get accurate normals at
            // chunk edge.
            int w = chunk.borderedHeightmap.GetLength(0);

            float step = (w - 3) / vertexReductionFactor;

            var vertices = new Vector3[w * w];
            int[] triangles = new int[(w - 1) * (w - 1) * 2 * 3];

            await SafeTask.Run(() => {
                for (int i = 0; i < w; i++) {
                    for (int j = 0; j < w; j++) {
                        var vertex = new Vector3(
                            i,
                            chunk.borderedHeightmap[i, j],
                            j
                        );

                        // Offset vertex by border width so that mesh origin
                        // ends up in the right place.
                        vertex -= new Vector3(1f, 0f, 1f);

                        vertices[GetIndex(i, j, w)] = vertex;

                        if (i > 0 && j > 0) {
                            int ti = GetTriangleIndex(i, j, w);

                            triangles[ti] = GetIndex(i, j, w);
                            triangles[ti + 1] = GetIndex(i - 1, j - 1, w);
                            triangles[ti + 2] = GetIndex(i - 1, j, w);

                            triangles[ti + 3] = GetIndex(i, j, w);
                            triangles[ti + 4] = GetIndex(i, j - 1, w);
                            triangles[ti + 5] = GetIndex(i - 1, j - 1, w);
                        }
                    }
                }
            });

            var borderedMesh = new Mesh() {
                vertices = vertices,
                triangles = triangles
            };

            borderedMesh.RecalculateNormals();

            Vector3[] normals = borderedMesh.normals;

            // Now that we have calculated correct normals, create non-bordered mesh.

            int w2 = w - 2;
            var vertices2 = new Vector3[w2 * w2];
            var normals2 = new Vector3[w2 * w2];
            int[] triangles2 = new int[(w2 - 1) * (w2 - 1) * 2 * 3];

            await SafeTask.Run(() => {
                for (int i = 0; i < w2; i++) {
                    for (int j = 0; j < w2; j++) {
                        vertices2[GetIndex(i, j, w2)] = vertices[GetIndex(i + 1, j + 1, w)];
                        normals2[GetIndex(i, j, w2)] = normals[GetIndex(i + 1, j + 1, w)];

                        if (i > 0 && j > 0) {
                            int ti = GetTriangleIndex(i, j, w2);

                            triangles2[ti] = GetIndex(i, j, w2);
                            triangles2[ti + 1] = GetIndex(i - 1, j - 1, w2);
                            triangles2[ti + 2] = GetIndex(i - 1, j, w2);

                            triangles2[ti + 3] = GetIndex(i, j, w2);
                            triangles2[ti + 4] = GetIndex(i, j - 1, w2);
                            triangles2[ti + 5] = GetIndex(i - 1, j - 1, w2);
                        }
                    }
                }
            });

            var mesh = new Mesh() {
                name = name,
                vertices = vertices2,
                normals = normals2,
                triangles = triangles2
            };

            // Do not optimize as meshes are created at run time.
            // mesh.Optimize();

            return mesh;
        }

        // Get the vertex array index of a vertex with the given coordinates,
        // belonging to a chunk of given width.
        private static int GetIndex(int x, int z, int width) => (z * width) + x;

        private static int GetTriangleIndex(int x, int z, int width) =>
            (((z - 1) * (width - 1)) + (x - 1)) * 2 * 3;

        // TODO?
        // private static bool IsBorder(int x, int z, int width) =>

    }
}
