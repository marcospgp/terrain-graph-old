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
                this.UpdateChunks(newCenterChunk)
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
                this.UpdateChunks(this.centerChunk)
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
                float norm =
                    Mathf.Sqrt(Mathf.Pow(pos.x, 2f) + Mathf.Pow(pos.z, 2f));

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

        private IEnumerator UpdateChunks((int, int) newCenterChunk) {
            this.centerChunk = newCenterChunk;

            if (this.updateCenterChunkCoroutine != null) {
                // Interrupt possible running coroutine.
                this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);
                this.updateCenterChunkCoroutine = null;

                // Destroy pending chunk which was likely left unfinished.
                _ = this.chunks.Remove(this.pendingChunk.pos);
                this.pendingChunk.Destroy();
            }

            // Add 1 because center chunk doesn't count for view distance.
            // Multiply view distance in order to generate lower detail chunks
            // for the horizon.
            int radius = 1 + (this.terrainGraph.viewDistance * 4);

            IEnumerable<(int, int)> spiral = Spiral(radius);

            var activeChunks = new List<(int, int)>();

            foreach ((int x, int z) offset in spiral) {
                (int x, int z) chunkPos = (
                    this.centerChunk.x + (TerrainGraph.CHUNK_WIDTH * offset.x),
                    this.centerChunk.z + (TerrainGraph.CHUNK_WIDTH * offset.z)
                );

                activeChunks.Add(chunkPos);

                if (!this.chunks.TryGetValue(chunkPos, out Chunk chunk)) {
                    chunk = new Chunk(
                        chunkPos,
                        this.terrainGraph
                    );

                    this.chunks.Add(chunkPos, chunk);
                }

                this.pendingChunk = chunk;

                yield return chunk.SetQuality(
                    this.GetReductionLevel(chunkPos),
                    this.GetHigherDetailNeighbors(chunkPos)
                );
            }

            // Destroy non active chunks.
            var toRemove = new List<KeyValuePair<(int, int), Chunk>>();
            foreach (var x in this.chunks) {
                foreach ((int, int) pos in activeChunks) {
                    if (x.Key == pos) {
                        // Chunk is active, skip
                        break;
                    }
                }

                // Chunk is not active, mark for removal
                // (can't remove now because we're inside the foreach)
                toRemove.Add(x);

                // Wait a frame to avoid slowing game down
                yield return null;
            }

            foreach (var x in toRemove) {
                // Must not yield between destroying chunk and removing its key from chunks
                // dictionary, at the risk of this coroutine being interrupted inbetween.
                x.Value.Destroy();
                _ = this.chunks.Remove(x.Key);

                yield return null;
            }
        }

        private int GetReductionLevel((int x, int z) chunkPos) {
            var center = new Vector2(
                chunkPos.x + (TerrainGraph.CHUNK_WIDTH / 2f),
                chunkPos.z + (TerrainGraph.CHUNK_WIDTH / 2f)
            );

            var centerChunk = new Vector2(
                this.centerChunk.x + (TerrainGraph.CHUNK_WIDTH / 2f),
                this.centerChunk.z + (TerrainGraph.CHUNK_WIDTH / 2f)
            );

            float distance = Vector2.Distance(center, centerChunk);

            // View length = chunk width * (view distance + 1)
            // The + 1 is to compensate for the center chunk, which is not
            // included in view distance.
            // Goal is that rendering only up to 1 viewDistance will make all
            // chunks have reduction level 0.
            float viewLength =
                (this.terrainGraph.viewDistance + 1) * TerrainGraph.CHUNK_WIDTH;

            return Mathf.FloorToInt(distance / viewLength);
        }

        private Side GetHigherDetailNeighbors((int x, int z) chunkPos) {
            int reductionLevel = this.GetReductionLevel(chunkPos);

            var neighbors = new Vector2Int[] {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };

            Side higherDetailNeighbors = Side.None;

            for (int i = 0; i < neighbors.Length; i++) {
                Vector2Int offset = neighbors[i];

                Vector2Int neighborPos =
                    new Vector2Int(chunkPos.x, chunkPos.z) +
                    (offset * TerrainGraph.CHUNK_WIDTH);

                int neighborReductionLevel =
                    this.GetReductionLevel((neighborPos.x, neighborPos.y));

                if (neighborReductionLevel < reductionLevel) {
                    higherDetailNeighbors |= (Side) (1 << i);
                }
            }

            return higherDetailNeighbors;
        }
    }
}
