using System.Collections;
using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.Terrain.ChunkManagerNS {
    public static class Environment {
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
            (int x, int z) worldPos,
            Transform chunk,
            float[,] environmentObjectDensity,
            TerrainGraph terrainGraph
        ) {
            // Minimum distance between objects.
            // Changing this value will affect the behavior of the vegetation
            // density port in terrain graph.
            const float minSpacing = 0.1f;

            const int chunkWidth = TerrainGraph.CHUNK_WIDTH;

            // Space between objects must be multiple of chunk width to ensure
            // uniform distribution.
            float spacing = (float) chunkWidth / Mathf.Floor(chunkWidth / minSpacing);

            // Calculate total frequency
            float envObjTotalFrequency = 0f;
            foreach (EnvironmentObjectGroup group in terrainGraph.environmentObjectGroups) {
                // We ignore group.enabled on purpose. Disabled groups still have their
                // frequency taken into account, so that disabling a group does not affect
                // how many objects of other groups are placed on the terrain.
                envObjTotalFrequency += group.frequency;
            }

            // No groups enabled or all frequencies set to 0
            if (envObjTotalFrequency == 0f) {
                yield break;
            }

            bool PlaceObject((float, float) offset) =>
                TryPlace(
                    worldPos,
                    offset,
                    chunk,
                    environmentObjectDensity,
                    terrainGraph,
                    envObjTotalFrequency
                );

            // Limit objects placed per frame to avoid slowing game down
            const int objectsPerFrame = int.MaxValue;
            int counter = 0;

            // Place objects.
            // Offsetting by half of spacing ensures object is properly
            // spaced along chunk borders.
            for (float i = spacing / 2; i < chunkWidth; i += spacing) {
                for (float j = spacing / 2; j < chunkWidth; j += spacing) {
                    if (PlaceObject((i, j))) {
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
                // This seemed to increase FPS a little compared to SRP batching
                // and GPU instancing, but at the cost of too much memory.
                StaticBatchingUtility.Combine(chunk.gameObject);
            }
        }

        /// <summary>
        /// Try to place a single instance of a prefab on a chunk.
        /// </summary>
        private static bool TryPlace(
            (int x, int z) worldPos,
            (float x, float z) offset,
            Transform chunk,
            float[,] environmentObjectDensity,
            TerrainGraph terrainGraph,
            float envObjTotalFrequency,
            float densityMultiplier = 1f,
            float scaleVariation = 0f
        ) {
            float density = environmentObjectDensity[
                Mathf.FloorToInt(offset.x),
                Mathf.FloorToInt(offset.z)
            ] * densityMultiplier;

            // Get placement coordinates in world space.
            float placeX = worldPos.x + offset.x;
            float placeZ = worldPos.z + offset.z;

            // Roll deterministic dice to see if an object should be placed
            float random = Hash.Get01(placeX, placeZ, "environment objects");

            if (random >= density) {
                // No placing this time
                return false;
            }

            float random2 = Hash.Get01(placeX, placeZ, "which environment object group");

            EnvironmentObjectGroup group = null;
            float accumulator = 0f;
            int groupCount = terrainGraph.environmentObjectGroups.Count;

            for (int i = 0; i < groupCount; i++) {
                accumulator +=
                    terrainGraph.environmentObjectGroups[i].frequency / envObjTotalFrequency;

                // Avoid floating point inaccuracies by always picking last
                // group if it gets to it
                bool isLastGroup = i == groupCount - 1;

                if (accumulator >= random2 || isLastGroup) {
                    group = terrainGraph.environmentObjectGroups[i];
                    break;
                }
            }

            if (!group.enabled || group.items.Count == 0) {
                return false;
            }

            // Determine which prefab to place from given list.
            float random3 = Hash.Get01Exclusive(placeX, placeZ, "which environment object");
            int index = Mathf.CeilToInt(group.items.Count * random3) - 1;
            GameObject prefab = group.items[index];

            int maxHeight = terrainGraph.terrainNode.maxHeight;

            if (Physics.Raycast(
                origin: new Vector3(placeX, maxHeight * 2f, placeZ),
                direction: Vector3.down,
                hitInfo: out RaycastHit hit,
                maxDistance: Mathf.Infinity,
                layerMask: 1 << terrainGraph.groundLayer
            )) {
                // Check parent transform due to possible LODs
                if (hit.transform != chunk && hit.transform.parent != chunk) {
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
                if (hit.point.y < group.minimumHeight) {
                    return false;
                }

                Environment.Instantiate(
                    prefab,
                    hit,
                    parent: chunk,
                    group.alignWithGround,
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
