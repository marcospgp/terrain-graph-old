using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
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
            // Wait for previous coroutine to end instead of forcing it to stop.
            // Chunks in the midst of being built would finish being built,
            // leading to duplicate chunks.
            yield return this.updateCenterChunkCoroutine;

            IEnumerable<(int, int)> newActiveChunks =
                Spiral(newCenterChunk, this.terrainGraph.viewDistance + 1);

            foreach ((int x, int z) pos in newActiveChunks) {
                if (this.chunks.TryGetValue(pos, out GameObject obj)) {
                    obj.SetActive(true);
                } else {
                    // Build newly active chunks that weren't built before

                    UnityEngine.Debug.Log($"Building chunk x{pos.x}_z{pos.z}");

                    var c = new CoroutineWithResult<GameObject>(this.BuildChunk(pos.x, pos.z));

                    yield return c;

                    GameObject chunk = c.result;

                    this.chunks.Add(pos, chunk);
                }
            }

            // Disable non active chunks.
            foreach (var x in this.chunks) {
                if (!newActiveChunks.Contains(x.Key)) {
                    x.Value.SetActive(false);
                }
            }
        }

        private IEnumerator BuildChunk(int x, int z) {
            Task<GameObject> t = MeshBuilder.BuildChunk(
                x,
                z,
                TerrainGraph.CHUNK_WIDTH,
                this.terrainGraph.terrainNode,
                this.terrainGraph.terrainMaterial
            );

            yield return t.AsCoroutine();

            GameObject chunk = t.Result;

            chunk.layer = this.terrainGraph.groundLayer;

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the chunks
            // moved during gameplay, which is not the case.
            chunk.transform.SetParent(this.terrainGraph.transform);

            (int, int) worldPos = (
                x * TerrainGraph.CHUNK_WIDTH,
                z * TerrainGraph.CHUNK_WIDTH
            );

            Task<float[,]> t2 =
                this.terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    worldPos,
                    TerrainGraph.CHUNK_WIDTH
                );

            yield return t2.AsCoroutine();

            float[,] environmentObjectDensity = t2.Result;

            yield return Environment.PlaceObjects(
                worldPos,
                chunk.transform,
                environmentObjectDensity,
                this.terrainGraph
            );

            // Last value can be accessed as the result, by using custom
            // Coroutine<T>.
            yield return chunk;
        }
    }
}
