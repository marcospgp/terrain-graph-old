using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    [System.Serializable]
    public class EnvironmentObjectGroup {
        public string label = "Unnamed group";

        [Tooltip(
            "How often objects in this group will appear on the terrain, " +
            "relative to objects in other groups."
        )]
        [Range(0f, 1f)]
        public float frequency = 1f;

        [Tooltip(
            "Whether objects in this group should be aligned with the ground. " +
            "Should be checked for things like grass, and unchecked for things like trees."
        )]
        public bool alignWithGround = false;

        public List<GameObject> items;
    }
}
