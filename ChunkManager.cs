using System.Collections;
using System.Collections.Generic;
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
            this.centerChunk = newCenterChunk;

            this.updateCenterChunkCoroutine = this.terrainGraph.StartCoroutine(
                this.UpdateCenterChunkCoroutine()
            );
        }

        public void Reset() {
            // Stop building chunks if any are being built.
            // We don't mind partially built chunks because we are destroying
            // all of them.
            this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);

            foreach (GameObject chunk in this.chunks.Values) {
                UnityEngine.Object.Destroy(chunk);
            }

            this.chunks.Clear();

            this.UpdateCenterChunk(this.centerChunk);
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

        private IEnumerator UpdateCenterChunkCoroutine() {
            // Wait for previous coroutine to end if it is underway, so that
            // we do not end up with partially-built chunks.
            if (this.updateCenterChunkCoroutine != null) {
                yield return this.updateCenterChunkCoroutine;
            }

            IEnumerable<(int, int)> newActiveChunks =
                Spiral(this.centerChunk, this.terrainGraph.viewDistance + 1);

            foreach ((int x, int z) pos in newActiveChunks) {
                if (this.chunks.TryGetValue(pos, out GameObject obj)) {
                    obj.SetActive(true);
                } else {
                    // Build newly active chunks that weren't built before

                    // UnityEngine.Debug.Log($"Building chunk x{pos.x}_z{pos.z}");

                    yield return this.BuildChunk((pos.x, pos.z));

                    if (this.terrainGraph.placeEnvironmentObjects) {
                        yield return this.PlaceEnvironmentObjects((pos.x, pos.z));
                    }
                }
            }

            // Disable non active chunks.
            foreach (var x in this.chunks) {
                bool contains = false;

                foreach ((int, int) pos in newActiveChunks) {
                    if (x.Key == pos) {
                        contains = true;
                        break;
                    }
                }

                if (!contains) {
                    x.Value.SetActive(false);
                }

                // Wait a frame to avoid slowing game down
                yield return null;
            }
        }

        private IEnumerator BuildChunk((int x, int z) pos) {
            var chunk = new GameObject();

            // Chunk must be added to chunks dictionary before the next yield,
            // so that it is not unaccounted for in case this coroutine gets
            // interrupted.
            this.chunks.Add(pos, chunk);

            string name = $"chunk_x{pos.x}_z{pos.z}";

            int worldX = pos.x * TerrainGraph.CHUNK_WIDTH;
            int worldZ = pos.z * TerrainGraph.CHUNK_WIDTH;

            chunk.name = name;
            chunk.transform.position = new Vector3(worldX, 0f, worldZ);
            chunk.layer = this.terrainGraph.groundLayer;

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the chunks
            // moved during gameplay, which is not the case.
            chunk.transform.SetParent(this.terrainGraph.transform);

            Task<Mesh> t = MeshBuilder.BuildChunkMesh(
                worldX,
                worldZ,
                TerrainGraph.CHUNK_WIDTH,
                this.terrainGraph.terrainNode,
                name
            );

            yield return t.AsCoroutine();

            Mesh mesh = t.Result;

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.material = this.terrainGraph.terrainMaterial;

            _ = chunk.AddComponent<MeshCollider>();
        }

        private IEnumerator PlaceEnvironmentObjects((int x, int z) chunkPos) {
            (int, int) worldPos = (
                chunkPos.x * TerrainGraph.CHUNK_WIDTH,
                chunkPos.z * TerrainGraph.CHUNK_WIDTH
            );

            Task<float[,]> t2 =
                this.terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    worldPos,
                    TerrainGraph.CHUNK_WIDTH
                );

            yield return t2.AsCoroutine();

            float[,] environmentObjectDensity = t2.Result;

            Transform chunk = this.chunks[chunkPos].transform;

            yield return Environment.PlaceObjects(
                worldPos,
                chunk,
                environmentObjectDensity,
                this.terrainGraph
            );
        }
    }
}
