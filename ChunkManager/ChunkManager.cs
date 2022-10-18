using System.Collections;
using System.Collections.Generic;
using MarcosPereira.Terrain.ChunkManagerNS;
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

        // Nullable to prevent misuse.
        private (int, int)? lastBuiltChunk;

        public ChunkManager(TerrainGraph terrainGraph) {
            this.terrainGraph = terrainGraph;
        }

        public static void WarnUnreadableMeshes(List<EnvironmentObjectGroup> x) =>
            Environment.WarnUnreadableMeshes(x);

        public void UpdateCenterChunk((int, int) newCenterChunk) {
            this.centerChunk = newCenterChunk;

            if (this.updateCenterChunkCoroutine != null) {
                this.terrainGraph.StopCoroutine(this.updateCenterChunkCoroutine);

                this.updateCenterChunkCoroutine = null;

                // Last chunk may be partially built since we interrupted coroutine,
                // so destroy it
                Object.Destroy(this.chunks[this.lastBuiltChunk.Value]);
                _ = this.chunks.Remove(this.lastBuiltChunk.Value);
                this.lastBuiltChunk = null;
            }

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

        // Enumerates int coordinates for a spiral that forms a disk around a
        // given center.
        private static IEnumerable<(int, int)> Spiral((int x, int z) center, int radius) {
            if (radius == 0) {
                yield break;
            }

            yield return center;

            // Used to select chunks within the radius from the enumerated plane.
            bool IsWithinRadius((int x, int z) chunk) {
                var offset = Vector2.one * (TerrainGraph.CHUNK_WIDTH / 2f);

                return Mathf.FloorToInt(
                    Vector2.Distance(
                        new Vector2(chunk.x, chunk.z) + offset,
                        new Vector2(center.x, center.z) + offset
                    )
                ) <= radius;
            }

            int x = center.x;
            int z = center.z;

            for (int distance = 1; distance < radius; distance++) {
                z -= 1;

                for (; x < center.x + distance; x++) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; z < center.z + distance; z++) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; x > center.x - distance; x--) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                for (; z > center.z - distance; z--) {
                    if (IsWithinRadius((x, z))) {
                        yield return (x, z);
                    }
                }

                if (IsWithinRadius((x, z))) {
                    yield return (x, z);
                }
            }
        }

        private IEnumerator UpdateCenterChunkCoroutine() {
            IEnumerable<(int, int)> newActiveChunks =
                Spiral(this.centerChunk, this.terrainGraph.viewDistance + 1);

            foreach ((int x, int z) pos in newActiveChunks) {
                if (this.chunks.TryGetValue(pos, out GameObject obj)) {
                    obj.SetActive(true);
                } else {
                    // Build newly active chunks that weren't built before

                    // UnityEngine.Debug.Log($"Building chunk x{pos.x}_z{pos.z}");

                    // Build gameobject right away so we can keep track of it,
                    // even if coroutine gets interrupted.
                    var chunk = new GameObject();

                    this.lastBuiltChunk = pos;

                    this.chunks.Add(pos, chunk);

                    // Avoid cluttering the hierarchy root.
                    // I believe this would only be costly in performance if the
                    // chunks moved during gameplay, which is not the case.
                    chunk.transform.SetParent(this.terrainGraph.transform);

                    yield return ChunkBuilder.BuildChunk(
                        (pos.x, pos.z),
                        chunk,
                        this.terrainGraph
                    );
                }
            }

            // Disable non active chunks.
            foreach (var x in this.chunks) {
                bool active = false;

                foreach ((int, int) pos in newActiveChunks) {
                    if (x.Key == pos) {
                        active = true;
                        break;
                    }
                }

                if (!active) {
                    x.Value.SetActive(false);
                }

                // Wait a frame to avoid slowing game down
                yield return null;
            }
        }
    }
}
