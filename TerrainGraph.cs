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
        "CA1001",
        Justification = "MonoBehaviours do not need to be disposed. (Warning: \"Type 'TerrainGraph' owns disposable field(s) 'cancelTokenSource' but is not disposable.\")"
    )]
    public class TerrainGraph : MonoBehaviour {
        private const int CHUNK_WIDTH = 16;

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

        private readonly ChunkManager chunkManager = new ChunkManager(CHUNK_WIDTH);
        private TerrainNode terrainNode;

#if UNITY_EDITOR
        public void Awake() {
            // When terrain graph asset is saved, rebuild terrain.
            AssetSavedListener.Listen(this.terrainGraphAsset, async () => {
                // if (Application.isPlaying) {
                //     this.StopAllCoroutines();

                //     try {
                //         await this.buildTask;
                //     } catch (OperationCanceledException) {
                //     }

                //     foreach (GameObject chunk in this.chunkManager.chunks.Values) {
                //         Destroy(chunk);
                //     }

                //     this.chunkManager.chunks.Clear();
                //     this.Start();
                // }
            });
        }
#endif

        public void Start() {
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

            _ = this.StartCoroutine(this.MonitorPlayerPosition());
        }

        private IEnumerator MonitorPlayerPosition() {
            while (true) {
                if (this.player == null) {
                    yield return new WaitForSecondsRealtime(0.5f);
                    continue;
                }

                // Update chunks right away because when starting there will be
                // none.
                yield return this.chunkManager.UpdateChunks();

                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
    }
}
