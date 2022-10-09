using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
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
            int worldX,
            int worldZ,
            int chunkWidth,
            Transform chunk,
            TerrainNode terrainNode,
            List<EnvironmentObjectGroup> environmentObjectGroups,
            float[,] environmentObjectDensity,
            int groundLayer,
            bool useStaticBatching
        ) {
            // Minimum distance between objects.
            // Changing this value will affect the behavior of the vegetation
            // density port in terrain graph.
            const float minSpacing = 0.1f;

            // Space between objects must be multiple of chunk width to ensure
            // uniform distribution.
            float spacing = (float) chunkWidth / Mathf.Floor(chunkWidth / minSpacing);

            // Calculate total frequency
            float totalFrequency = 0f;
            foreach (EnvironmentObjectGroup group in environmentObjectGroups) {
                totalFrequency += group.frequency;
            }

            bool PlaceObject(float i, float j) =>
                TryPlace(
                    worldX,
                    worldZ,
                    i,
                    j,
                    environmentObjectGroups,
                    totalFrequency,
                    terrainNode,
                    chunk,
                    1 << groundLayer,
                    environmentObjectDensity,
                    spacing
                );

            // Place objects.
            // Offsetting by half of spacing ensures object is properly
            // spaced along chunk borders.
            for (float i = spacing / 2; i < chunkWidth; i += spacing) {
                for (float j = spacing / 2; j < chunkWidth; j += spacing) {
                    if (PlaceObject(i, j)) {
                        // Place one object per frame to avoid slowing game down.
                        yield return null;
                    }
                }
            }

            if (useStaticBatching) {
                // Combine chunk and vegetation into a static batch.
                // This was experimentally found to perform better than
                // SRP batching or GPU instancing.
                StaticBatchingUtility.Combine(chunk.gameObject);
            }
        }

        /// <summary>
        /// Try to place a single instance of a prefab on a chunk.
        /// </summary>
        private static bool TryPlace(
            int x,
            int z,
            float i,
            float j,
            List<EnvironmentObjectGroup> environmentObjectGroups,
            float totalFrequency,
            TerrainNode terrainNode,
            Transform chunk,
            int groundLayerMask,
            float[,] environmentObjectDensity,
            float densityMultiplier = 1f,
            float scaleVariation = 0f
        ) {
            float density = environmentObjectDensity[
                Mathf.FloorToInt(i),
                Mathf.FloorToInt(j)
            ] * densityMultiplier;

            // Get placement coordinates in world space.
            float placeX = x + i;
            float placeZ = z + j;

            // Roll deterministic dice to see if an object should be placed
            float random = Hash.Get01(placeX, placeZ, "environment objects");

            if (random >= density) {
                // No placing this time
                return false;
            }

            // Determine which object to place
            float random2 = Hash.Get01(placeX, placeZ, "which environment object group");

            List<GameObject> prefabList = null;
            bool alignWithGround = false;

            float accumulator = 0f;

            foreach (EnvironmentObjectGroup group in environmentObjectGroups) {
                accumulator += group.frequency / totalFrequency;

                if (accumulator >= random2) {
                    prefabList = group.items;
                    alignWithGround = group.alignWithGround;
                    break;
                }
            }

            if (prefabList.Count == 0) {
                return false;
            }

            // Determine which prefab to place from given list.
            float random3 = Hash.Get01Exclusive(placeX, placeZ, "which environment object");
            int index = Mathf.CeilToInt(prefabList.Count * random3) - 1;
            GameObject prefab = prefabList[index];

            if (Physics.Raycast(
                origin: new Vector3(placeX, terrainNode.maxHeight * 2f, placeZ),
                direction: Vector3.down,
                hitInfo: out RaycastHit hit,
                maxDistance: Mathf.Infinity,
                layerMask: groundLayerMask
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
                if (slope > terrainNode.vegetationMaxSlope) {
                    return false;
                }

                // Align prefab with ground
                Quaternion rotation = Quaternion.identity;
                if (alignWithGround) {
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }

                GameObject obj = Object.Instantiate(
                    prefab,
                    hit.point,
                    rotation,
                    parent: chunk
                );

                // Random rotation around y axis
                float yAngle = Hash.Get01(placeX, placeZ, "yAngle") * 360f;
                obj.transform.Rotate(Vector3.up, yAngle, Space.Self);

                // Scale variation

                float maxScale = 1f + Mathf.Max(0f, scaleVariation);
                float minScale = 1f / Mathf.Max(0f, maxScale);

                float scale01 = Hash.Get01(placeX, placeZ, "scale variation");

                obj.transform.localScale *= Mathf.Lerp(minScale, maxScale, scale01);

                return true;
            }

            return false;
        }
    }
}
