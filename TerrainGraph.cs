using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [SuppressMessage(
        "",
        "CA1001",
        Justification = "MonoBehaviours do not need to be disposed. (Warning: \"Type 'TerrainGraph' owns disposable field(s) 'cancelTokenSource' but is not disposable.\")"
    )]
    public class TerrainGraph : MonoBehaviour {
        private const int CHUNK_WIDTH = 16;

        // Chunks stored by coordinates in chunk space (not world space).
        // Chunk (2, 2) will be at (2, 2) * CHUNK_WIDTH.
        private readonly Dictionary<(int, int), GameObject> chunks =
            new Dictionary<(int, int), GameObject>();

        [Header("References")]

        [SerializeField]
        private TerrainGraphAsset terrainGraphAsset;

        [SerializeField]
        private Material terrainMaterial;

        [SerializeField]
        private Transform player;

        [SerializeField, LayerSelect]
        private int groundLayer;

        [Header("Settings")]

        [SerializeField]
        [Tooltip("The view distance, measured in chunks.")]
        private int viewDistance = 4;

        [Header("Environment")]

        [SerializeField]
        [Tooltip(
            "Requires all environment object meshes to be Read/Write enabled.\n" +
            "This should be kept enabled unless you are sure it does not help performance."
        )]
        private bool useStaticBatching = true;

        [SerializeField]
        private List<EnvironmentObjectGroup> environmentObjectGroups;

        private TerrainNode terrainNode;
        private (int x, int z) centerChunk = (0, 0);
        private Task buildTask;
        private CancellationTokenSource cancelTokenSource;

#if UNITY_EDITOR
        public void Awake() {
            // When terrain graph asset is saved, rebuild terrain.
            AssetSavedListener.Listen(this.terrainGraphAsset, async () => {
                if (Application.isPlaying) {
                    this.cancelTokenSource.Cancel();
                    this.cancelTokenSource.Dispose();

                    this.StopAllCoroutines();

                    try {
                        await this.buildTask;
                    } catch (OperationCanceledException) {
                    }

                    foreach (GameObject chunk in this.chunks.Values) {
                        Destroy(chunk);
                    }

                    this.chunks.Clear();
                    this.Start();
                }
            });
        }
#endif

        public async void Start() {
            if (this.groundLayer == 0) {
                throw new Exception(
                    "Terrain Graph: Ground layer must be set in inspector."
                );
            }

            // Warn user of any non read/write enabled meshes, which will
            // prevent them from being static batched
            if (this.useStaticBatching) {
                Environment.WarnUnreadableMeshes(this.environmentObjectGroups);
            }

            this.terrainNode =
                (TerrainNode) this.terrainGraphAsset.GraphModel.NodeModels.Single(
                    nodeModel => nodeModel is TerrainNode
                );

            (int, int) center = this.GetPlayerChunk(this.player);

            this.cancelTokenSource = new CancellationTokenSource();

            this.buildTask = this.BuildOnce(center, this.cancelTokenSource.Token);

            _ = this.StartCoroutine(this.MonitorPlayerPosition());

            // Listen for exceptions other than cancellation
            try {
                await this.buildTask;
            } catch (OperationCanceledException) {
            }
        }

        public async Task BuildOnce((int x, int z) center, CancellationToken token) {
            // Build chunks in a spiral
            foreach ((int x, int z) in Spiral((0, 0), this.viewDistance + 1)) {
                token.ThrowIfCancellationRequested();

                (int, int) chunkPos = (center.x + x, center.z + z);

                GameObject chunk = await this.BuildChunk(center.x + x, center.z + z);

                this.chunks.Add(chunkPos, chunk);

                if (token.IsCancellationRequested) {
                    Destroy(chunk);
                    token.ThrowIfCancellationRequested();
                }
            }
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

        private (int, int) GetPlayerChunk(Transform player) {
            if (player == null) {
                return (0, 0);
            }

            Vector3 p = this.player.position / CHUNK_WIDTH;

            return (Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
        }

        private IEnumerator MonitorPlayerPosition() {
            while (true) {
                yield return new WaitForSecondsRealtime(0.5f);

                if (this.player == null) {
                    continue;
                }

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
                    _ = this.StartCoroutine(this.UpdateCenterChunk());
                }
            }
        }

        private IEnumerator UpdateCenterChunk() {
            UnityEngine.Debug.Log("Updating chunks!");

            this.centerChunk = (
                Mathf.FloorToInt(this.player.position.x / CHUNK_WIDTH),
                Mathf.FloorToInt(this.player.position.z / CHUNK_WIDTH)
            );

            IEnumerable<(int, int)> newActiveChunks =
                Spiral(this.centerChunk, this.viewDistance + 1);

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
