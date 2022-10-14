using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain.Editor {
    public class SeparatorUIPart : BaseUIPart {
        private VisualElement element;

        public SeparatorUIPart(Node nodeUI, string name) : base(nodeUI, name) {
        }

        public override VisualElement Root => this.element;

        protected override void BuildPartUI(VisualElement parent) {
            this.element = new VisualElement();

            // Same border top color as used elsewhere.
            this.element.style.borderTopColor = new Color(
                35f / 255f,
                35f / 255f,
                35f / 255f,
                205f / 255f
            );
            this.element.style.borderTopWidth = 1f;
            parent.Add(this.element);
        }

        protected override void UpdatePartFromModel() {
        }
    }
}
