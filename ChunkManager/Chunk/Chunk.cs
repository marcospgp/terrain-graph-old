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

        public Vector3[,] vertexNormals;

        private readonly TerrainGraph terrainGraph;

        private readonly MeshFilter meshFilter;

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
        }

        public void Destroy() {
            UnityEngine.Object.Destroy(this.gameObject);
        }

        public IEnumerator SetQuality(
            int reductionLevel,
            Side higherDetailNeighbor
        ) {
            UnityEngine.Debug.Log(
                $"Setting quality: reduction level {reductionLevel}, hq sides {higherDetailNeighbor}"
            );
            if (this.borderedHeightmap == null) {
                yield return this.GetHeightmap();
            }

            if (this.vertexNormals == null) {
                Task<Vector3[,]> t = MeshBuilder.CalculateVertexNormals(this);

                yield return t.AsCoroutine();

                this.vertexNormals = t.Result;
            }

            Task<Mesh> t2 = MeshBuilder.BuildChunkMesh(
                this,
                reductionLevel,
                higherDetailNeighbor,
                name: this.gameObject.name
            );

            yield return t2.AsCoroutine();

            Mesh mesh = t2.Result;

            this.meshFilter.mesh = mesh;

            if (reductionLevel == 0) {
                _ = this.gameObject.AddComponent<MeshCollider>();
            }

            if (this.terrainGraph.placeEnvironmentObjects) {
                yield return Environment.PlaceObjects(
                    this,
                    this.terrainGraph
                );
            }
        }

        private IEnumerator GetHeightmap() {
            Task<float[,]> t = this.terrainGraph.terrainNode.GetHeightmap(
                this.pos.x - 1,
                this.pos.z - 1,
                TerrainGraph.CHUNK_WIDTH + 3
            );

            yield return t.AsCoroutine();

            this.borderedHeightmap = t.Result;
        }
    }
}
