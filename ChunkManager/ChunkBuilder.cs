using System.Collections;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class ChunkBuilder {
        /// <param name="chunk">
        /// An empty gameobject to work with. An out parameter would be cleaner,
        /// but that is not possible when using coroutines.
        /// </param>
        public static IEnumerator BuildChunk(
            (int x, int z) pos,
            GameObject chunk,
            TerrainGraph terrainGraph
        ) {
            string name = $"chunk_x{pos.x}_z{pos.z}";

            int worldX = pos.x * TerrainGraph.CHUNK_WIDTH;
            int worldZ = pos.z * TerrainGraph.CHUNK_WIDTH;

            chunk.name = name;
            chunk.transform.position = new Vector3(worldX, 0f, worldZ);
            chunk.layer = terrainGraph.groundLayer;

            Task<Mesh> t = MeshBuilder.BuildChunkMesh(
                worldX,
                worldZ,
                TerrainGraph.CHUNK_WIDTH,
                terrainGraph.terrainNode,
                name
            );

            yield return t.AsCoroutine();

            Mesh mesh = t.Result;

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.material = terrainGraph.terrainMaterial;

            _ = chunk.AddComponent<MeshCollider>();

            if (terrainGraph.placeEnvironmentObjects) {
                yield return ChunkBuilder.PlaceEnvironmentObjects(
                    pos,
                    chunk.transform,
                    terrainGraph
                );
            }
        }

        private static IEnumerator PlaceEnvironmentObjects(
            (int x, int z) chunkPos,
            Transform chunk,
            TerrainGraph terrainGraph
        ) {
            (int, int) worldPos = (
                chunkPos.x * TerrainGraph.CHUNK_WIDTH,
                chunkPos.z * TerrainGraph.CHUNK_WIDTH
            );

            Task<float[,]> t2 =
                terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    worldPos,
                    TerrainGraph.CHUNK_WIDTH
                );

            yield return t2.AsCoroutine();

            float[,] environmentObjectDensity = t2.Result;

            yield return Environment.PlaceObjects(
                worldPos,
                chunk,
                environmentObjectDensity,
                terrainGraph
            );
        }
    }
}
