using System.Threading.Tasks;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class MeshBuilder {
        public static async Task<GameObject> BuildChunk(
            int x,
            int z,
            int chunkWidth,
            TerrainNode node,
            Material mat
        ) {
            int worldX = x * chunkWidth;
            int worldZ = z * chunkWidth;

            string name = $"chunk_x{x}_z{z}";

            Mesh mesh = await MeshBuilder.BuildMesh(worldX, worldZ, chunkWidth, node, name);

            var chunk = new GameObject(name);
            chunk.transform.position = new Vector3(worldX, 0f, worldZ);

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.material = mat;

            _ = chunk.AddComponent<MeshCollider>();

            return chunk;
        }

        private static async Task<Mesh> BuildMesh(
            int worldX,
            int worldZ,
            int chunkWidth,
            TerrainNode node,
            string name = "Unnamed mesh"
        ) {
            // Width in vertices
            int width = chunkWidth + 1;

            // Build chunk with 1-unit border, to get accurate normals at chunk edge.
            width += 2;

            float[,] heightmap = await node.GetHeightmap(worldX - 1, worldZ - 1, width);

            var vertices = new Vector3[width * width];
            int[] triangles = new int[(width - 1) * (width - 1) * 2 * 3];

            for (int i = 0; i < width; i++) {
                for (int j = 0; j < width; j++) {
                    var vertexPosition = new Vector3(i, heightmap[i, j], j);

                    // Offset vertex by border width, otherwise mesh origin will
                    // be offset itself
                    vertexPosition -= new Vector3(1f, 0f, 1f);

                    vertices[GetIndex(i, j, width)] = vertexPosition;

                    if (i > 0 && j > 0) {
                        int ti = GetTriangleIndex(i, j, width);

                        triangles[ti] = GetIndex(i, j, width);
                        triangles[ti + 1] = GetIndex(i - 1, j - 1, width);
                        triangles[ti + 2] = GetIndex(i - 1, j, width);

                        triangles[ti + 3] = GetIndex(i, j, width);
                        triangles[ti + 4] = GetIndex(i, j - 1, width);
                        triangles[ti + 5] = GetIndex(i - 1, j - 1, width);
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

            int width2 = width - 2;
            var vertices2 = new Vector3[width2 * width2];
            var normals2 = new Vector3[width2 * width2];
            int[] triangles2 = new int[(width2 - 1) * (width2 - 1) * 2 * 3];

            for (int i = 0; i < width2; i++) {
                for (int j = 0; j < width2; j++) {
                    vertices2[GetIndex(i, j, width2)] = vertices[GetIndex(i + 1, j + 1, width)];
                    normals2[GetIndex(i, j, width2)] = normals[GetIndex(i + 1, j + 1, width)];

                    if (i > 0 && j > 0) {
                        int ti = GetTriangleIndex(i, j, width2);

                        triangles2[ti] = GetIndex(i, j, width2);
                        triangles2[ti + 1] = GetIndex(i - 1, j - 1, width2);
                        triangles2[ti + 2] = GetIndex(i - 1, j, width2);

                        triangles2[ti + 3] = GetIndex(i, j, width2);
                        triangles2[ti + 4] = GetIndex(i, j - 1, width2);
                        triangles2[ti + 5] = GetIndex(i - 1, j - 1, width2);
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
