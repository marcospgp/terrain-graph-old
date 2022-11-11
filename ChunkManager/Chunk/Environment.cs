using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS.ChunkNS {
    public static class Environment {
        // Minimum distance between objects.
        // Changing this value will affect how much vegetation is spawned,
        // even if all other configuration remains the same.
        private const float MIN_SPACING = 0.5f;

        public static void WarnUnreadableMeshes(List<EnvironmentObjectGroup> envObjGroups) {
            static void LogReadWriteWarning(string meshName, string objName) =>
                UnityEngine.Debug.LogWarning(
                    $"Terrain Graph: Mesh \"{meshName}\" in " +
                    $" environment object \"{objName}\" is not Read/Write enabled.\n" +
                    "This will prevent it from being optimized by static batching.\n" +
                    "Read/Write can be enabled in the mesh's import settings."
                );

            foreach (EnvironmentObjectGroup group in envObjGroups) {
                foreach (GameObject go in group.items) {
                    foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>()) {
                        if (filter.sharedMesh != null && !filter.sharedMesh.isReadable) {
                            LogReadWriteWarning(filter.sharedMesh.name, go.name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Place environment objects on a given chunk. The chunk object must
        /// already exist in the scene.
        /// </summary>
        public static IEnumerator PlaceObjects(
            Chunk chunk,
            TerrainGraph terrainGraph
        ) {
            Task<float[,]> t =
                terrainGraph.terrainNode.GetEnvironmentObjectDensity(
                    chunk.pos,
                    TerrainGraph.CHUNK_WIDTH
                );

            yield return t.AsCoroutine();

            float[,] environmentObjectDensity = t.Result;

            const int chunkWidth = TerrainGraph.CHUNK_WIDTH;

            // Space between objects must be multiple of chunk width to ensure
            // uniform distribution.
            float spacing = (float) chunkWidth / Mathf.Floor(chunkWidth / MIN_SPACING);

            List<EnvironmentObjectGroup> groups = terrainGraph.environmentObjectGroups;

            // Linked list representing the order of priority of each object group waiting to be
            // placed.
            // We try to place objects following the order stored in this line.
            // When an object is placed, its group is moved to the end of the line.
            var placementLine = new LinkedList<int>();

            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].enabled && groups[i].frequency > 0f) {
                    _ = placementLine.AddLast(i);
                }
            }

            // If no groups are going to be placed, stop here.
            if (placementLine.Count == 0) {
                yield break;
            }

            // Limit objects placed per frame to avoid slowing game down too much.
            const int objectsPerFrame = 64;
            int counter = 0;

            // Place objects.
            // Offsetting by half of spacing ensures object is properly
            // spaced along chunk borders.
            for (float i = spacing / 2; i < chunkWidth; i += spacing) {
                for (float j = spacing / 2; j < chunkWidth; j += spacing) {
                    bool placed = TryPlace(
                        new Vector2(chunk.pos.x + i, chunk.pos.z + j),
                        (i, j),
                        environmentObjectDensity,
                        chunk.gameObject.transform,
                        terrainGraph,
                        placementLine
                    );

                    if (placed) {
                        counter++;

                        if (counter == objectsPerFrame) {
                            counter = 0;
                            yield return null;
                        }
                    }
                }
            }

            if (terrainGraph.useStaticBatching) {
                // Combine chunk and vegetation into a static batch.
                // This seemed to increase FPS a little compared to SRP batching and GPU instancing,
                // but at the cost of too much memory.
                StaticBatchingUtility.Combine(chunk.gameObject);
            }
        }

        /// <summary>
        /// Try to place a single instance of a prefab on a chunk.
        /// </summary>
        private static bool TryPlace(
            Vector2 worldPos,
            (float x, float z) offset,
            float[,] environmentObjectDensity,
            Transform chunk,
            TerrainGraph terrainGraph,
            LinkedList<int> placementLine,
            float densityMultiplier = 1f,
            float scaleVariation = 0f
        ) {
            float density = environmentObjectDensity[
                Mathf.FloorToInt(offset.x),
                Mathf.FloorToInt(offset.z)
            ] * densityMultiplier;

            // Roll deterministic dice to see if an object should be placed
            float random = Hash.Get01(worldPos.x, worldPos.y, "environment objects");

            if (random >= density) {
                // No placing this time
                return false;
            }

            float random2 = Hash.Get01(worldPos.x, worldPos.y, "which environment object group");

            var groups = terrainGraph.environmentObjectGroups;
            EnvironmentObjectGroup groupToPlace = null;

            var node = placementLine.First;

            while (node != null) {
                if (groups[node.Value].frequency >= random2) {
                    groupToPlace = terrainGraph.environmentObjectGroups[node.Value];
                    placementLine.Remove(node);
                    placementLine.AddLast(node);
                    break;
                }

                node = node.Next;
            }

            if (groupToPlace == null) {
                return false;
            }

            // Determine which object to place from given group.
            float random3 = Hash.Get01Exclusive(worldPos.x, worldPos.y, "which environment object");
            // Ceil because 1 is inclusive.
            int index = Mathf.CeilToInt(groupToPlace.items.Count * random3) - 1;

            GameObject prefab = groupToPlace.items[index];

            int maxHeight = terrainGraph.terrainNode.maxHeight;

            if (Physics.Raycast(
                origin: new Vector3(worldPos.x, maxHeight * 2f, worldPos.y),
                direction: Vector3.down,
                hitInfo: out RaycastHit hit,
                maxDistance: Mathf.Infinity,
                layerMask: 1 << terrainGraph.groundLayer
            )) {
                // Check parent transform due to possible LODs
                if (
                    hit.transform != chunk &&
                    hit.transform.parent != chunk
                ) {
                    UnityEngine.Debug.LogWarning(
                        "Terrain Graph: Hit unexpected gameobject " +
                        $"\"{hit.transform.name}\" while placing environment " +
                        $"objects on \"{chunk.name}\"."
                    );

                    return false;
                }

                // Respect maximum slope
                float slope = Vector3.Angle(Vector3.up, hit.normal);
                if (slope > terrainGraph.terrainNode.vegetationMaxSlope) {
                    return false;
                }

                // Respect minimum height
                if (hit.point.y < groupToPlace.minimumHeight) {
                    return false;
                }

                Environment.Instantiate(
                    prefab,
                    hit,
                    parent: chunk.gameObject.transform,
                    groupToPlace.alignWithGround,
                    scaleVariation,
                    terrainGraph.disableSRPBatching
                );

                return true;
            }

            UnityEngine.Debug.LogWarning(
                "Terrain Graph: Raycast miss while placing environment " +
                $"object on \"{chunk.name}\"."
            );

            return false;
        }

        private static void Instantiate(
            GameObject prefab,
            RaycastHit hit,
            Transform parent,
            bool alignWithGround,
            float scaleVariation,
            bool disableSRPBatching
        ) {
            // Align with ground
            Quaternion rotation = Quaternion.identity;
            if (alignWithGround) {
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }

            GameObject obj = Object.Instantiate(
                prefab,
                hit.point,
                rotation,
                parent
            );

            // Random rotation around y axis
            float yAngle = Hash.Get01(hit.point.x, hit.point.z, "yAngle") * 360f;
            obj.transform.Rotate(Vector3.up, yAngle, Space.Self);

            // Scale variation

            float maxScale = 1f + Mathf.Max(0f, scaleVariation);
            float minScale = 1f / Mathf.Max(0f, maxScale);

            float scale01 = Hash.Get01(hit.point.x, hit.point.z, "scale variation");

            obj.transform.localScale *= Mathf.Lerp(minScale, maxScale, scale01);

            if (disableSRPBatching) {
                var block = new MaterialPropertyBlock();
                block.SetInteger("Manually remove SRP Batcher compatibility.", 123);
                foreach (var r in obj.GetComponentsInChildren<Renderer>(includeInactive: true)) {
                    r.SetPropertyBlock(block);
                }
            }
        }
    }
}
