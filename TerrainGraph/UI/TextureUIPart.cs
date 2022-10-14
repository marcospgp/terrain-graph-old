using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain.Editor {
    public class TextureUIPart : BaseUIPart {
        public Image imageElement;
        public Foldout foldout;

        public TextureUIPart(Node nodeUI, string name) : base(nodeUI, name) {
        }

        public override VisualElement Root => this.foldout;

        protected override void BuildPartUI(VisualElement parent) {
            this.foldout = new Foldout {
                text = "Preview"
            };

            // Same border top color as used elsewhere.
            this.foldout.style.borderTopColor = new Color(
                35f / 255f,
                35f / 255f,
                35f / 255f,
                205f / 255f
            );
            this.foldout.style.borderTopWidth = 1f;

            // Same darker background color as used elsewhere.
            this.foldout.style.backgroundColor = new Color(
                46f / 255f,
                46f / 255f,
                46f / 255f,
                205f / 255f
            );

            // Remove default left margin
            this.foldout.MandatoryQ("unity-content").style.marginLeft = 0;

            parent.Add(this.foldout);

            this.imageElement = new Image {
                name = this.PartName
            };

            this.imageElement.style.width = new Length(100f, LengthUnit.Percent);
            this.imageElement.style.height = 259f;

            this.foldout.Add(this.imageElement);

            var label = new Label(
                "Preview size: 1000x1000 units.\n0 is black, 1 is white."
            );

            label.style.paddingTop = 3;
            label.style.paddingRight = 3;
            label.style.paddingBottom = 3;
            label.style.paddingLeft = 3;

            this.foldout.Add(label);
        }

        protected override void UpdatePartFromModel() {
            // Texture is not updated here because this method gets called too
            // many times (such as when dragging nodes around), which made
            // things slow.
        }
    }
}
