using System.Threading.Tasks;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class MeshBuilder {
        public static async Task<Mesh> BuildChunkMesh(
            (int x, int z) worldPosition,
            int chunkWidth,
            TerrainNode node,
            int resolutionLevel,
            string name = "Unnamed mesh"
        ) {
            // Width in vertices
            int w = 2 + ((chunkWidth - 1) / (1 + resolutionLevel));

            float stepSize = (float) chunkWidth / (w - 1);

            // Build chunk with 1-unit border, to get accurate normals at chunk
            // edge.
            w += 2;

            float[,] heightmap = await node.GetHeightmap(
                worldPosition.x - 1,
                worldPosition.z - 1,
                w - 1,
                stepSize
            );

            var vertices = new Vector3[w * w];
            int[] triangles = new int[(w - 1) * (w - 1) * 2 * 3];

            for (int i = 0; i < w; i++) {
                for (int j = 0; j < w; j++) {
                    var vertexPosition = new Vector3(
                        i * stepSize,
                        heightmap[i, j],
                        j * stepSize
                    );

                    // Offset vertex by border width, otherwise mesh origin will
                    // be offset itself
                    vertexPosition -= new Vector3(1f, 0f, 1f);

                    vertices[GetIndex(i, j, w)] = vertexPosition;

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

        // Get the index of a vertex in a vertex array for a node with the given
        // coordinates, belonging to a chunk of given width.
        private static int GetIndex(int x, int z, int width) => (z * width) + x;

        private static int GetTriangleIndex(int x, int z, int width) =>
            (((z - 1) * (width - 1)) + (x - 1)) * 2 * 3;
    }
}
