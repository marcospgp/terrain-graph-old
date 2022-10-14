using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain.Editor {
    public class LabelUIPart : BaseUIPart {
        private readonly string content;
        private readonly string tooltip;
        private readonly int verticalPadding;
        private readonly FontStyle fontStyle;
        private Label label;

        public LabelUIPart(
            Node nodeUI,
            string name,
            string content = null,
            string tooltip = null,
            int verticalPadding = 3,
            FontStyle fontStyle = FontStyle.Normal
        ) : base(nodeUI, name) {
            this.content = content;
            this.tooltip = tooltip;
            this.verticalPadding = verticalPadding;
            this.fontStyle = fontStyle;
        }

        public override VisualElement Root => this.label;

        protected override void BuildPartUI(VisualElement parent) {
            string content = string.IsNullOrEmpty(this.content) ?
                this.PartName : this.content;

            this.label = new Label(content) {
                name = this.PartName
            };

            if (!string.IsNullOrEmpty(this.tooltip)) {
                this.label.tooltip = this.tooltip;
            }

            this.label.style.unityFontStyleAndWeight = this.fontStyle;

            this.label.style.paddingTop = this.verticalPadding;
            this.label.style.paddingRight = 3;
            this.label.style.paddingBottom = this.verticalPadding;
            this.label.style.paddingLeft = 3;

            parent.Add(this.label);
        }

        protected override void UpdatePartFromModel() {
            // Do nothing
        }
    }
}
