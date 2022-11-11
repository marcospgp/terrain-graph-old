using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [System.Serializable]
    public class EnvironmentObjectGroup {
        public string label = "Unnamed group";

        [Tooltip("Whether objects in this group will be placed on the terrain.")]
        public bool enabled = true;

        [Header("Objects")]

        public List<GameObject> items;

        [Header("Settings")]

        [Tooltip(
            "How likely objects in this group are to be spawned on each placement point in the " +
            "terrain."
        )]
        [Range(0f, 1f)]
        public float frequency = 1f;

        [Tooltip(
            "Whether objects in this group should be aligned with the ground's slope. " +
            "Should be checked for things like grass, and unchecked for things like trees."
        )]
        public bool alignWithGround = false;

        // [Tooltip(
        //     "Maximum distance at which objects in this group are visible. " +
        //     "0 means the object will always be visible."
        // )]
        // public float viewDistance = 500f;

        [Tooltip(
            "Minimum Y coordinate at which these objects will be placed. Useful for things like " +
            "preventing objects from being placed underwater."
        )]
        public float minimumHeight = 0f;
    }
}
