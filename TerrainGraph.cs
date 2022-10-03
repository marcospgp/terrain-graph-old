using System;
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
        private const int CHUNK_WIDTH = 32;

        private readonly Dictionary<Vector2Int, GameObject> chunks =
            new Dictionary<Vector2Int, GameObject>();

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
        // private Vector2Int centerChunk = Vector2Int.zero;
        private Task buildTask;
        private CancellationTokenSource cancelTokenSource;

#if UNITY_EDITOR
        public void Awake() {
            // When terrain graph asset is saved, rebuild terrain.
            AssetSavedListener.Listen(this.terrainGraphAsset, async () => {
                if (Application.isPlaying) {
                    this.cancelTokenSource.Cancel();
                    this.cancelTokenSource.Dispose();

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

            Vector2Int center = this.GetPlayerChunk(this.player);

            this.cancelTokenSource = new CancellationTokenSource();

            this.buildTask = this.BuildOnce(center, this.cancelTokenSource.Token);

            // TODO: monitor player position
            // Task monitor = this.MonitorPlayerPosition(token);

            // Listen for exceptions other than cancellation
            try {
                // await Task.WhenAll(new Task[] { this.buildTask, monitor });

                await this.buildTask;
            } catch (OperationCanceledException) {
            }
        }

        public async Task BuildOnce(Vector2Int center, CancellationToken token) {
            int x = 0;
            int z = 0;

            await Build(x, z);

            // Build remaining chunks in a spiral
            for (int distance = 1; distance <= this.viewDistance; distance++) {
                token.ThrowIfCancellationRequested();

                z -= 1;

                for (; x < distance; x++) {
                    await Build(x, z);
                }

                for (; z < distance; z++) {
                    await Build(x, z);
                }

                for (; x > -1 * distance; x--) {
                    await Build(x, z);
                }

                for (; z > -1 * distance; z--) {
                    await Build(x, z);
                }

                await Build(x, z); // Build last corner
            }

            async Task Build(int x, int z) {
                token.ThrowIfCancellationRequested();

                Vector2Int key = center + new Vector2Int(x, z);

                GameObject chunk = await this.BuildChunk(key.x, key.y);

                if (token.IsCancellationRequested) {
                    Destroy(chunk);
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        // private async Task MonitorPlayerPosition(CancellationToken token) {
        //     while (true) {
        //         await Task.Delay(1000, token);

        //         token.ThrowIfCancellationRequested();

        //         if (this.player == null) {
        //             continue;
        //         }

        //         // Determine which chunk player is in initially; Call it "center chunk".
        //         // If they move 2 chunks away from the center chunk, update chunks around them and
        //         // reassign the center chunk.
        //         // The 2x2 chunk area check ensures that there's no hard boundary that can cause
        //         // chunk regeneration by repeatedly being crossed.

        //         Vector2Int playerChunk = this.GetPlayerChunk(this.player);

        //         float distance = Vector2Int.Distance(this.centerChunk, playerChunk);

        //         UnityEngine.Debug.Log($"Distance from center chunk: {distance} max: 2");

        //         if (distance >= 2f) {
        //             await this.UpdateCenterChunk(playerChunk);
        //         }
        //     }
        // }

        private Vector2Int GetPlayerChunk(Transform player) {
            if (player == null) {
                return Vector2Int.zero;
            }

            Vector3 p = this.player.position / CHUNK_WIDTH;

            return new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
        }

        // private async Task UpdateCenterChunk(Vector2Int newCenterChunk) {
        //     this.centerChunk = newCenterChunk;

        //     foreach (GameObject chunk in this.chunks.Values) {
        //         chunk.SetActive(false);
        //     }

        //     var activeChunkList = new List<Vector2Int> {
        //         CHUNK_WIDTH * this.centerChunk
        //     };

        //     int start = -1 * (this.viewDistance / 2);
        //     int end = this.viewDistance / 2;

        //     for (int i = start; i <= end; i++) {
        //         for (int j = start; j <= end; j++) {
        //             activeChunkList.Add(
        //                 (this.centerChunk + new Vector2Int(i, j)) * CHUNK_WIDTH
        //             );
        //         }
        //     }

        //     foreach (Vector2Int pos in activeChunkList) {
        //         if (this.chunks.TryGetValue(pos, out GameObject obj)) {
        //             obj.SetActive(true);
        //         } else {
        //             GameObject chunk = await this.BuildChunk(pos.x, pos.y);
        //             this.chunks.Add(pos, chunk);
        //         }
        //     }
        // }

        private async Task<GameObject> BuildChunk(int x, int z) {
            GameObject chunk = await Builder.BuildChunk(
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
