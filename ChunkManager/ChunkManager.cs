using System.Collections;
using System.Collections.Generic;
using MarcosPereira.Terrain.ChunkManagerNS;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public class ChunkManager {
        public (int x, int z) centerChunk;

        private readonly Dictionary<(int, int), Chunk> chunks =
            new Dictionary<(int, int), Chunk>();

        private readonly TerrainGraph terrainGraph;

        private Coroutine updateCenterChunkCoroutine;

        private Chunk pendingChunk;

        public ChunkManager(TerrainGraph terrainGraph) {
            this.terrainGraph = terrainGraph;
        }

        public void UpdateCenterChunk(Vector3 playerPosition) {
            const int cw = TerrainGraph.CHUNK_WIDTH;

            (int, int) newCenterChunk = (
                cw * Mathf.FloorToInt(playerPosition.x / cw),
                cw * Mathf.FloorToInt(playerPosition.z / cw)
            );

            this.updateCenterChunkCoroutine = this.terrainGraph.StartCoroutine(
                this.UpdateCenterChunk(newCenterChunk)
            );
        }

        public void Reset() {
            // Stop building chunks if any are being built.
            // We don't mind partially built chunks because we are destroying
            // all of them.
            this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);

            foreach (Chunk chunk in this.chunks.Values) {
                chunk.Destroy();
            }

            this.chunks.Clear();

            this.updateCenterChunkCoroutine = this.terrainGraph.StartCoroutine(
                this.UpdateCenterChunk(this.centerChunk)
            );
        }

        // Enumerates coordinates for a spiral.
        private static IEnumerable<(int, int)> Spiral(int radius) {
            if (radius == 0) {
                yield break;
            }

            int x = 0;
            int z = 0;

            yield return (x, z);

            // Used to select coordinates within the radius, as we loop defining
            // a plane, not a disk.
            bool IsWithinRadius((int x, int z) pos) {
                int norm = Mathf.FloorToInt(
                    Mathf.Sqrt(Mathf.Pow(pos.x, 2f) + Mathf.Pow(pos.z, 2f))
                );

                return norm <= radius;
            }

            for (int i = 1; i < radius; i++) {
                z -= 1;

                for (; x < +i; x++) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; z < +i; z++) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; x > -i; x--) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; z > -i; z--) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                if (IsWithinRadius((x, z))) {
                    yield return (x, z);
                }
            }
        }

        private IEnumerator UpdateCenterChunk((int, int) newCenterChunk) {
            this.centerChunk = newCenterChunk;

            if (this.updateCenterChunkCoroutine != null) {
                // Interrupt possible running coroutine.
                this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);
                this.updateCenterChunkCoroutine = null;

                // Destroy pending chunk which was likely left unfinished.
                _ = this.chunks.Remove(this.pendingChunk.pos);
                this.pendingChunk.Destroy();
            }

            int radius = 1 + this.terrainGraph.viewDistance;

            IEnumerable<(int, int)> spiral = Spiral(radius);

            var activeChunks = new List<(int, int)>();

            foreach ((int x, int z) offset in spiral) {
                (int x, int z) chunkPos = (
                    this.centerChunk.x + (TerrainGraph.CHUNK_WIDTH * offset.x),
                    this.centerChunk.z + (TerrainGraph.CHUNK_WIDTH * offset.z)
                );

                activeChunks.Add(chunkPos);

                if (!this.chunks.ContainsKey(chunkPos)) {
                    var chunk = new Chunk(
                        chunkPos,
                        this.terrainGraph
                    );

                    this.chunks.Add(chunkPos, chunk);
                    this.pendingChunk = chunk;

                    yield return chunk.Build();
                }
            }

            // Destroy non active chunks.
            var toRemove = new List<(int, int)>();
            foreach (var x in this.chunks) {
                bool active = false;

                foreach ((int, int) pos in activeChunks) {
                    if (x.Key == pos) {
                        active = true;
                        break;
                    }
                }

                if (!active) {
                    x.Value.Destroy();
                    toRemove.Add(x.Key);
                }

                // Wait a frame to avoid slowing game down
                yield return null;
            }

            foreach (var key in toRemove) {
                _ = this.chunks.Remove(key);
            }
        }
    }
}
