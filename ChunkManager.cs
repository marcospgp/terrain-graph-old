using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public class ChunkManager {
        // Chunks stored by coordinates in chunk space (not world space).
        // Chunk (2, 2) will be at (2, 2) * CHUNK_WIDTH.
        private readonly Dictionary<(int, int), GameObject> chunks =
            new Dictionary<(int, int), GameObject>();

        private readonly int chunkWidth;
        private readonly int viewDistance;

        private (int x, int z) centerChunk = (0, 0);

        private Coroutine updateChunksCoroutine;

        public ChunkManager(int chunkWidth) {
            this.chunkWidth = chunkWidth;
        }

        public IEnumerator UpdateChunks(
            Vector3 playerPosition,
            int viewDistance
        ) {
            UnityEngine.Debug.Log("Updating chunks!");

            // Update visible chunks when player is too far from current
                // "center" chunk.
                // One must be careful when setting the distance at which this
                // is done, to avoid a hard boundary that can cause chunk
                // regeneration by repeatedly being crossed.
                // Here, we set it to the length of a chunk's diagonal.

                float maxDistance = Mathf.Sqrt(CHUNK_WIDTH * CHUNK_WIDTH * 2f);

                (float x, float z) center = (
                    this.centerChunk.x + (CHUNK_WIDTH / 2f),
                    this.centerChunk.z + (CHUNK_WIDTH / 2f)
                );

                float distance = Mathf.Sqrt(
                    Mathf.Pow(center.x - this.player.position.x, 2f) +
                    Mathf.Pow(center.z - this.player.position.z, 2f)
                );

                UnityEngine.Debug.Log(
                    $"Distance from center chunk: {distance:0.00} max: {maxDistance:0.00}"
                );

                if (distance > maxDistance) {
                    this.centerChunk = (
                        Mathf.FloorToInt(player.position.x / chunkWidth),
                        Mathf.FloorToInt(player.position.z / chunkWidth)
                    );

                    _ = this.StartCoroutine(
                        Builder.UpdateChunks(newCenterChunk: this.centerChunk)
                    );
                }

            // ------------------------------

            IEnumerable<(int, int)> newActiveChunks =
                Spiral(newCenterChunk, viewDistance + 1);

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

        public (int, int) GetPlayerChunk(Transform player, int chunkWidth) {
            if (player == null) {
                return (0, 0);
            }

            Vector3 p = player.position / chunkWidth;

            return (Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
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

        private async Task<GameObject> BuildChunk(int x, int z) {
            GameObject chunk = await MeshBuilder.BuildChunk(
                x,
                z,
                CHUNK_WIDTH,
                this.terrainNode,
                this.terrainMaterial
            );

            chunk.layer = this.groundLayer;

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the chunks
            // moved during gameplay, which is not the case.
            chunk.transform.SetParent(this.transform);

            int worldX = x * CHUNK_WIDTH;
            int worldZ = z * CHUNK_WIDTH;

            float[,] environmentObjectDensity =
                await this.terrainNode.GetEnvironmentObjectDensity(worldX, worldZ, CHUNK_WIDTH);

            _ = this.StartCoroutine(Environment.PlaceObjects(
                worldX,
                worldZ,
                CHUNK_WIDTH,
                chunk.transform,
                this.terrainNode,
                this.environmentObjectGroups,
                environmentObjectDensity,
                this.groundLayer,
                this.useStaticBatching
            ));

            return chunk;
        }
    }
}
