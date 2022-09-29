using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class Environment {
        /// <summary>
        /// Place environment objects on a given chunk. The chunk object must
        /// already exist in the scene.
        /// </summary>
        public static async Task PlaceObjects(
            int worldX,
            int worldZ,
            int chunkWidth,
            Transform chunk,
            TerrainNode terrainNode,
            List<EnvironmentObjectGroup> environmentObjectGroups
        ) {
            float[,] environmentObjectDensity =
                await terrainNode.GetEnvironmentObjectDensity(worldX, worldZ, chunkWidth);

            // Minimum distance between objects
            const float minSpacing = 0.1f;

            // Space between objects must be multiple of chunk width to ensure
            // uniform distribution.
            float spacing = (float) chunkWidth / Mathf.Floor(chunkWidth / minSpacing);

            void PlaceObject(float i, float j) =>
                TryPlace(
                    environmentObjectGroups,
                    worldX,
                    worldZ,
                    i,
                    j,
                    terrainNode,
                    chunk,
                    environmentObjectDensity,
                    spacing
                );

            // Place objects.
            // Offsetting by half of spacing ensures object is properly
            // spaced along chunk borders.
            for (float i = spacing / 2; i < chunkWidth; i += spacing) {
                for (float j = spacing / 2; j < chunkWidth; j += spacing) {
                    PlaceObject(i, j);
                }
            }

            // Combine chunk and vegetation into a static batch.
            // This was experimentally found to perform better (for grass) than
            // either GPU instancing or the default scriptable render pipeline
            // batching.
            StaticBatchingUtility.Combine(chunk.gameObject);
        }

        /// <summary>
        /// Try to place a single instance of a prefab on a chunk.
        /// </summary>
        private static void TryPlace(
            List<EnvironmentObjectGroup> environmentObjectGroups,
            int x,
            int z,
            float i,
            float j,
            TerrainNode terrainNode,
            Transform chunk,
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
                return;
            }

            float avgFrequency = 0f;

            foreach (EnvironmentObjectGroup group in environmentObjectGroups) {
                avgFrequency += group.frequency;
            }

            avgFrequency /= environmentObjectGroups.Count;

            // Determine which object to place
            float random2 = Hash.Get01(placeX, placeZ, "which environment object group");

            List<GameObject> prefabList = null;
            bool alignWithGround = false;

            float accumulator = 0f;

            foreach (EnvironmentObjectGroup group in environmentObjectGroups) {
                accumulator += group.frequency / avgFrequency;

                if (random2 <= accumulator) {
                    prefabList = group.items;
                    alignWithGround = group.alignWithGround;

                    break;
                }
            }

            if (prefabList.Count == 0) {
                return;
            }

            // Determine which prefab to place from given list.
            float random3 = Hash.Get01Exclusive(placeX, placeZ, "which environment object");
            int index = Mathf.CeilToInt(prefabList.Count * random3) - 1;
            GameObject prefab = prefabList[index];

            if (Physics.Raycast(
                new Vector3(placeX, terrainNode.maxHeight + 1f, placeZ),
                Vector3.down,
                out RaycastHit hit,
                terrainNode.maxHeight + 2f
            )) {
                // Check parent transform due to LODs
                if (hit.transform != chunk && hit.transform.parent != chunk) {
                    UnityEngine.Debug.LogWarning(
                        "Terrain Graph: Hit something unexpected while " +
                        "placing environment objects on terrain."
                    );
                    return;
                }

                // Respect maximum slope
                float slope = Vector3.Angle(Vector3.up, hit.normal);
                if (slope > terrainNode.vegetationMaxSlope) {
                    return;
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
            }
        }
    }
}
