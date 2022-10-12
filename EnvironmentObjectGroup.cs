using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [System.Serializable]
    public class EnvironmentObjectGroup {
        public string label = "Unnamed group";

        [Tooltip(
            "Whether these objects will be placed on the terrain.\n" +
            "Disabled groups still have " +
            "their frequency taken into account, so that disabling a group " +
            "does not affect how many objects of other groups are placed."
        )]
        public bool enabled = true;

        [Header("Objects")]

        public List<GameObject> items;

        [Header("Settings")]

        [Tooltip(
            "How often objects in this group will appear on the terrain, " +
            "relative to other groups."
        )]
        [Range(0f, 1f)]
        public float frequency = 1f;

        [Tooltip(
            "Whether objects in this group should be aligned with the ground's slope. " +
            "Should be checked for things like grass, and unchecked for things like trees."
        )]
        public bool alignWithGround = false;

        [Tooltip(
            "Maximum distance at which objects in this group are visible. " +
            "0 means the object will always be visible."
        )]
        public float viewDistance = 500f;
    }
}
