using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [System.Serializable]
    public class EnvironmentObjectGroup {
        public string label = "Unnamed group";

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

        public List<GameObject> items;
    }
}
