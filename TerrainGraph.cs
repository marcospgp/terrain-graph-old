using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [SuppressMessage(
        "",
        "SA1202",
        Justification = "Choosing order fields are displayed in inspector."
    )]
    public class TerrainGraph : MonoBehaviour {
        public const int CHUNK_WIDTH = 16;

        [Header("References")]

        [SerializeField]
        private TerrainGraphAsset terrainGraphAsset;

        [SerializeField]
        private Transform player;

        public Material terrainMaterial;

        [LayerSelect]
        public int groundLayer;

        [Header("Settings")]

        [Tooltip("The view distance, measured in chunks.")]
        public int viewDistance = 4;

        [Header("Environment")]

        [Tooltip(
            "Requires all environment object meshes to be Read/Write enabled.\n" +
            "This should be kept enabled unless you are sure it does not help performance."
        )]
        public bool useStaticBatching = true;

        public List<EnvironmentObjectGroup> environmentObjectGroups;

        [HideInInspector]
        public TerrainNode terrainNode;

        private ChunkManager chunkManager;

#if UNITY_EDITOR
        public void Awake() {
            // TODO: revisit
            // When terrain graph asset is saved, rebuild terrain.
            // AssetSavedListener.Listen(this.terrainGraphAsset, async () => {
            //     if (Application.isPlaying) {
            //         this.StopAllCoroutines();

            //         try {
            //             await this.buildTask;
            //         } catch (OperationCanceledException) {
            //         }

            //         foreach (GameObject chunk in this.chunkManager.chunks.Values) {
            //             Destroy(chunk);
            //         }

            //         this.chunkManager.chunks.Clear();
            //         this.Start();
            //     }
            // });
        }
#endif

        public void Start() {
            this.chunkManager = new ChunkManager(this);

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

            (int, int) playerChunk = (
                Mathf.FloorToInt(this.player.position.x / CHUNK_WIDTH),
                Mathf.FloorToInt(this.player.position.x / CHUNK_WIDTH)
            );

            this.chunkManager.UpdateCenterChunk(playerChunk);

            _ = this.StartCoroutine(this.MonitorPlayerPosition());
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
                    this.chunkManager.centerChunk.x + (CHUNK_WIDTH / 2f),
                    this.chunkManager.centerChunk.z + (CHUNK_WIDTH / 2f)
                );

                float distance = Mathf.Sqrt(
                    Mathf.Pow(center.x - this.player.position.x, 2f) +
                    Mathf.Pow(center.z - this.player.position.z, 2f)
                );

                if (distance > maxDistance) {
                    (int, int) newCenterChunk = (
                        Mathf.FloorToInt(this.player.position.x / CHUNK_WIDTH),
                        Mathf.FloorToInt(this.player.position.z / CHUNK_WIDTH)
                    );

                    this.chunkManager.UpdateCenterChunk(newCenterChunk);
                }
            }
        }
    }
}
