using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public class ChunkManager {
        public (int x, int z) centerChunk = (0, 0);

        // Chunks stored by coordinates in chunk space (not world space).
        // Chunk (2, 2) will be at (2, 2) * CHUNK_WIDTH.
        private readonly Dictionary<(int, int), GameObject> chunks =
            new Dictionary<(int, int), GameObject>();

        private readonly TerrainGraph terrainGraph;

        private Coroutine updateCenterChunkCoroutine;

        public ChunkManager(TerrainGraph terrainGraph) {
            this.terrainGraph = terrainGraph;
        }

        public void UpdateCenterChunk((int, int) newCenterChunk) {
            if (this.updateCenterChunkCoroutine != null) {
                this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);
            }

            this.updateCenterChunkCoroutine = this.terrainGraph.StartCoroutine(
                this.UpdateCenterChunkCoroutine(newCenterChunk)
            );
        }

        // Enumerates int coordinates for a spiral.
        private static IEnumerable<(int, int)> Spiral((int x, int z) center, int radius) {
            if (radius == 0) {
                yield break;
            }

            yield return center;

            int x = center.x;
            int z = center.z;

            for (int distance = 1; distance < radius; distance++) {
                z -= 1;

                for (; x < center.x + distance; x++) {
                    yield return (x, z);
                }

                for (; z < center.z + distance; z++) {
                    yield return (x, z);
                }

                for (; x > center.x - distance; x--) {
                    yield return (x, z);
                }

                for (; z > center.z - distance; z--) {
                    yield return (x, z);
                }

                yield return (x, z);
            }
        }

        private IEnumerator UpdateCenterChunkCoroutine((int, int) newCenterChunk) {
            IEnumerable<(int, int)> newActiveChunks =
                Spiral(newCenterChunk, this.terrainGraph.viewDistance + 1);

            foreach ((int x, int z) pos in newActiveChunks) {
                if (this.chunks.TryGetValue(pos, out GameObject obj)) {
                    obj.SetActive(true);
                } else {
                    // Build newly active chunks that weren't built before

                    UnityEngine.Debug.Log($"Building chunk {pos}");

                    Task<GameObject> task = this.BuildChunk(pos.x, pos.z);

                    yield return new WaitUntil(() => task.IsCompleted);

                    if (task.Exception != null) {
                        throw task.Exception;
                    }

                    GameObject chunk = task.Result;
                    this.chunks.Add(pos, chunk);
                }
            }

            // Disable non active chunks.
            foreach (var x in this.chunks) {
                if (!newActiveChunks.Contains(x.Key)) {
                    UnityEngine.Debug.Log($"Deactivating chunk {x.Key}");
                    x.Value.SetActive(false);
                }
            }
        }

        private async Task<GameObject> BuildChunk(int x, int z) {
            GameObject chunk = await MeshBuilder.BuildChunk(
                x,
                z,
                TerrainGraph.CHUNK_WIDTH,
                this.terrainGraph.terrainNode,
                this.terrainGraph.terrainMaterial
            );

            chunk.layer = this.terrainGraph.groundLayer;

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the chunks
            // moved during gameplay, which is not the case.
            chunk.transform.SetParent(this.terrainGraph.transform);

            int worldX = x * TerrainGraph.CHUNK_WIDTH;
            int worldZ = z * TerrainGraph.CHUNK_WIDTH;

            float[,] environmentObjectDensity =
                await this.terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    worldX,
                    worldZ,
                    TerrainGraph.CHUNK_WIDTH
                );

            _ = this.terrainGraph.StartCoroutine(Environment.PlaceObjects(
                worldX,
                worldZ,
                TerrainGraph.CHUNK_WIDTH,
                chunk.transform,
                this.terrainGraph.terrainNode,
                this.terrainGraph.environmentObjectGroups,
                environmentObjectDensity,
                this.terrainGraph.groundLayer,
                this.terrainGraph.useStaticBatching
            ));

            return chunk;
        }
    }
}
