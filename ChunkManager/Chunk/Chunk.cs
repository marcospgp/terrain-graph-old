using System.Collections;
using System.Threading.Tasks;
using MarcosPereira.Terrain.ChunkManagerNS.ChunkNS;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS {
    public sealed class Chunk {
        /// <summary>
        /// Position in world space of the chunk's southwest corner (lowest XZ
        /// coordinate).
        /// </summary>
        public readonly (int x, int z) pos;

        public readonly GameObject gameObject;

        public float[,] borderedHeightmap;

        private readonly TerrainGraph terrainGraph;

        private readonly MeshFilter meshFilter;

        private readonly MeshCollider meshCollider;

        /// <summary>
        /// Creates a new empty chunk.
        /// This is used instead of an asynchronous factory method so that a
        /// reference to the chunk is immediately available.
        /// </summary>
        public Chunk(
            (int x, int z) worldPosition,
            TerrainGraph terrainGraph
        ) {
            this.pos = worldPosition;
            this.terrainGraph = terrainGraph;

            this.gameObject = new GameObject {
                name = $"chunk_x{this.pos.x}_z{this.pos.z}",
                layer = terrainGraph.groundLayer
            };

            // Avoid cluttering the hierarchy root.
            // I believe this would only be costly in performance if the
            // chunks moved during gameplay, which is not the case.
            this.gameObject.transform.SetParent(terrainGraph.transform);

            this.gameObject.transform.position =
                new Vector3(this.pos.x, 0f, this.pos.z);

            MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = terrainGraph.terrainMaterial;

            this.meshFilter = this.gameObject.AddComponent<MeshFilter>();

            this.meshCollider = this.gameObject.AddComponent<MeshCollider>();
        }

        public void Destroy() {
            UnityEngine.Object.Destroy(this.gameObject);
        }

        public IEnumerator Build() {
            Task<float[,]> t = this.terrainGraph.terrainNode.GetHeightmap(
                this.pos.x - 1,
                this.pos.z - 1,
                TerrainGraph.CHUNK_WIDTH + 3
            );

            yield return t.AsCoroutine();

            this.borderedHeightmap = t.Result;

            Task<Mesh> t2 = MeshBuilder.BuildChunkMesh(
                this,
                name: this.gameObject.name
            );

            yield return t2.AsCoroutine();

            Mesh mesh = t2.Result;

            this.meshFilter.mesh = mesh;

            // Refresh mesh collider
            // Source: https://forum.unity.com/threads/how-to-update-a-mesh-collider.32467/
            this.meshCollider.sharedMesh = null;
            this.meshCollider.sharedMesh = this.meshFilter.mesh;

            if (this.terrainGraph.placeEnvironmentObjects) {
                yield return Environment.PlaceObjects(
                    this,
                    this.terrainGraph
                );
            }
        }
    }
}
