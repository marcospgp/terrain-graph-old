#if UNITY_EDITOR

using System.Linq;
using System.Threading.Tasks;
using MarcosPereira.Terrain.Graph.Editor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain.Graph {
    public abstract class BaseNodeUI : CollapsibleInOutNode {
        private TextureUIPart texturePart;

        // Newly added UI parts will be placed after the one named here.
        private string lastAddedPartName = CollapsibleInOutNode.titleIconContainerPartName;

        public virtual bool HasPreview =>
            ((NodeModel) this.NodeModel).GetOutputPorts().Count() == 1;

        // Allow child classes to configure default behavior by overriding
        protected virtual string Description { get; }

        // Called repeatedly to keep UI up to date, must maximize caching.
        public bool ShouldRepaint() {
            var baseNode = (BaseNode) this.NodeModel;

            // Sometimes the preview texture may be null, so we need to
            // repaint the preview even if nothing has changed otherwise.
            bool imageIsNull = this.texturePart.imageElement.image == null;

            return imageIsNull || baseNode.preview.ShouldRepaint();
        }

        public void ErasePreview() {
            this.texturePart.imageElement.image = null;
        }

        public async Task RepaintPreview() {
            var baseNode = (BaseNode) this.NodeModel;

            this.texturePart.imageElement.image =
                await baseNode.preview.RefreshPreview();
        }

        protected void AddInputField<TField>(
            string fieldName,
            string displayName = null,
            string tooltip = null
        ) {
            // Change events for input fields are handled here so that UI code
            // can be made editor-only at the assembly level. This code can't
            // be in InputFieldUIPart.cs because it relies on the BaseNode type.
            void ChangeCallback(ChangeEvent<TField> e) {
                this.CommandDispatcher.Dispatch(
                    new GenericCommand<TField>(
                        (NodeModel) this.NodeModel,
                        fieldName,
                        e.newValue,
                        $"Change {fieldName}",
                        $"Change {fieldName}"
                    )
                );

                // Repaint preview of this and downstream nodes
                if (this.NodeModel is BaseNode baseNode) {
                    baseNode.preview.MarkParameterChanged();
                }
            }

            var field = new InputFieldUIPart<TField>(
                this,
                fieldName,
                displayName,
                tooltip,
                changeCallback: ChangeCallback
            );
            this.InsertPart(field);
        }

        protected void InsertPart(IModelUIPart part) {
            this.PartList.InsertPartAfter(this.lastAddedPartName, part);
            this.lastAddedPartName = part.PartName;
        }

        protected override void BuildPartList() {
            base.BuildPartList();

            // Insert preview first so it shows up last (counterintuitive!)
            if (this.HasPreview) {
                this.InsertPreview();
            }

            // Insert description
            if (!string.IsNullOrEmpty(this.Description)) {
                this.InsertPart(
                    new LabelUIPart(
                        this,
                        "Description",
                        this.Description,
                        verticalPadding: 6
                    )
                );
            }
        }

        protected override async void PostBuildUI() {
            base.PostBuildUI();

            // Make preview show up in create node menu.
            //
            // Affect only nodes being drawn in creation menu, otherwise there
            // will be concurrent drawing of the preview between here and our
            // graph window's ongoing task.
            // (We assume that !isGraphNode means a node is in the creation
            // menu)
            bool isGraphNode = this.GraphView is TerrainGraphView;

            if (
                !isGraphNode &&
                this.HasPreview &&
                this.NodeModel is BaseNode
            ) {
                await this.RepaintPreview();
            }

            if (this.HasPreview) {
                // Minimum node width based on preview size
                this.contentContainer.style.minWidth = 258f;

                if (this.NodeModel is BaseNode baseNode) {
                    // Keep track of whether preview is expanded or collapsed.
                    this.texturePart.foldout.value = baseNode.isPreviewExpanded;

                    _ = this.texturePart.foldout.RegisterValueChangedCallback(
                        evt => baseNode.isPreviewExpanded = evt.newValue
                    );
                }
            }
        }

        // Insert preview without affecting lastAddedPartName,
        // so that UI parts added by child classes show up above the preview.
        private void InsertPreview() {
            this.texturePart = new TextureUIPart(this, "Preview");

            this.PartList.InsertPartAfter(
                CollapsibleInOutNode.titleIconContainerPartName,
                this.texturePart
            );
        }
    }
}

#endif
