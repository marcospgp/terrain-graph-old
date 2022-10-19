using System.Collections;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS {
    public class Chunk {
        /// <summary>
        /// Position in world space of the chunk's southwest corner (lowest XZ
        /// coordinate).
        /// </summary>
        public readonly (int x, int z) pos;

        public readonly GameObject gameObject;

        // Resolution level of 0 = 1 unit per vertex.
        // Each additional level divides resolution by 2, used for
        // distant chunks.
        public readonly int resolutionLevel;

        // Expose build coroutine so it can be awaited by caller.
        public readonly Coroutine buildCoroutine;

        // Used to manage coroutines
        private readonly TerrainGraph terrainGraph;

        public Chunk(
            (int x, int z) worldPosition,
            int resolutionLevel,
            TerrainGraph terrainGraph
        ) {
            this.pos = worldPosition;
            this.resolutionLevel = resolutionLevel;
            this.terrainGraph = terrainGraph;

            this.gameObject = new GameObject {
                name = $"chunk_x{worldPosition.x}_z{worldPosition.z}",
                layer = terrainGraph.groundLayer
            };

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the
            // chunks moved during gameplay, which is not the case.
            this.gameObject.transform.SetParent(this.terrainGraph.transform);

            this.gameObject.transform.position =
                new Vector3(worldPosition.x, 0f, worldPosition.z);

            MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = terrainGraph.terrainMaterial;

            MeshFilter meshFilter = this.gameObject.AddComponent<MeshFilter>();

            this.buildCoroutine = this.terrainGraph.StartCoroutine(
                this.Build(meshFilter)
            );
        }

        public void Destroy() {
            if (this.buildCoroutine != null) {
                this.terrainGraph.StopCoroutine(this.buildCoroutine);
            }

            UnityEngine.Object.Destroy(this.gameObject);
        }

        private IEnumerator Build(MeshFilter meshFilter) {
            Task<Mesh> t = MeshBuilder.BuildChunkMesh(
                this.pos,
                TerrainGraph.CHUNK_WIDTH,
                this.terrainGraph.terrainNode,
                this.gameObject.name
            );

            yield return t.AsCoroutine();

            Mesh mesh = t.Result;

            meshFilter.mesh = mesh;

            _ = this.gameObject.AddComponent<MeshCollider>();

            if (this.terrainGraph.placeEnvironmentObjects && this.resolutionLevel == 0) {
                yield return this.PlaceEnvironmentObjects();
            }
        }

        private IEnumerator PlaceEnvironmentObjects() {
            Task<float[,]> t2 =
                this.terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    this.pos,
                    TerrainGraph.CHUNK_WIDTH
                );

            yield return t2.AsCoroutine();

            float[,] environmentObjectDensity = t2.Result;

            yield return Environment.PlaceObjects(
                this.pos,
                this.gameObject.transform,
                environmentObjectDensity,
                this.terrainGraph
            );
        }
    }
}
