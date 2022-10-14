#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain {
    [SuppressMessage("", "CA1001", Justification = "No need for Dispose().")]
    public class TerrainGraphWindow : GraphViewEditorWindow {
        private Task repaintTask;
        private CancellationTokenSource repaintTokenSource;

        [MenuItem("Window/Terrain Graph")]
        public static void ShowWindow() => EditorWindow.GetWindow<TerrainGraphWindow>();

        protected override void OnDestroy() {
            base.OnDestroy();

            // Null check necessary, exception thrown in the past.
            if (this.repaintTokenSource != null) {
                this.repaintTokenSource.Cancel();
                this.repaintTokenSource.Dispose();
            }
        }

        protected override async void OnEnable() {
            base.OnEnable();

            this.EditorToolName = TerrainGraphStencil.GraphName;

            // Hide blackboard button, we don't use it
            this.MainToolbar.Q("showBlackboardButton").RemoveFromHierarchy();

            // Hide options button, options menu is empty
            this.MainToolbar.Q("optionsButton").RemoveFromHierarchy();

            // Hide error toolbar, not sure what it's for
            this.m_ErrorToolbar.style.display = DisplayStyle.None;

            // Wait while graph view or model are null, not sure why it happens.
            while (
                this.GraphView == null ||
                this.GraphView.GraphModel == null
            ) {
                await Task.Delay(100, CancellationToken.None);
            }

            // Mark all nodes as dirty. On script reload, nodes were being
            // marked as not dirty while their preview value cache was empty,
            // causing a null exception when accessing the cache.
            foreach (INodeModel node in this.GraphView.GraphModel.NodeModels) {
                if (node is BaseNode baseNode) {
                    baseNode.preview.MarkParameterChanged(propagate: false);
                }
            }

            // Ensure repaint task is ongoing.
            if (this.repaintTask == null) {
                this.repaintTokenSource = new CancellationTokenSource();

                CancellationToken token = this.repaintTokenSource.Token;

                this.repaintTask = this.RepaintContinuously(token);

                // Await task to ensure exceptions are logged.
                try {
                    await this.repaintTask;
                } catch (OperationCanceledException) {
                }
            }
        }

        protected override ModelInspectorView CreateModelInspectorView() {
            // Remove node inspector, we don't use it
            return null;
        }

        protected override GraphToolState CreateInitialState() {
            var preferences = Preferences.CreatePreferences(this.EditorToolName);
            return new TerrainGraphState(this.GUID, preferences);
        }

        // We use a custom graph view in order to hide things from contextual
        // (right click) menu.
        protected override GraphView CreateGraphView() =>
            new TerrainGraphView(this, this.CommandDispatcher, this.EditorToolName);

        protected override BlankPage CreateBlankPage() {
            var onboardingProviders = new List<OnboardingProvider>() {
                new TerrainGraphOnboardingProvider()
            };

            return new BlankPage(this.CommandDispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset) =>
            asset is TerrainGraphAsset;

        [InitializeOnLoadMethod]
        private static void RegisterTool() =>
            ShortcutHelper.RegisterDefaultShortcuts<TerrainGraphWindow>(
                TerrainGraphStencil.GraphName
            );

        // Texture previews in nodes are checked and updated repeatedly here
        // because OnConnection/OnDisconnection callbacks don't see latest
        // connections.
        private async Task RepaintContinuously(CancellationToken token) {
            // Wait while graph view or model are null, not sure why it happens.
            while (
                this.GraphView == null ||
                this.GraphView.GraphModel == null
            ) {
                await Task.Delay(100, token);
            }

            var nodesToRepaint = new List<BaseNodeUI>();

            // Now repaint forever, only stopping if task is canceled.
            while (true) {
                token.ThrowIfCancellationRequested();

                // The cast to object then enumerable is to avoid an exception
                // when starting up Unity. This exception goes away when
                // reloading scripts or opening a graph asset, don't know why.
                var nodes = (IEnumerable<Node>) (object) this.GraphView.Nodes;

                nodesToRepaint.Clear();

                foreach (Node node in nodes) {
                    if (
                        node is BaseNodeUI nodeUI &&
                        nodeUI.HasPreview &&
                        nodeUI.ShouldRepaint()
                    ) {
                        token.ThrowIfCancellationRequested();

                        nodesToRepaint.Add(nodeUI);
                    }
                }

                // Ensure repaint does not restart immediately even when it has
                // nothing to do.
                await Task.Delay(100, token);

                // Make previews of all dirty nodes blank while repainting.
                foreach (BaseNodeUI nodeUI in nodesToRepaint) {
                    token.ThrowIfCancellationRequested();

                    nodeUI.ErasePreview();
                }

                // Repaint in sequence to avoid race conditions and double work
                // (repainting a node involves calculating values for all preceding nodes).
                foreach (BaseNodeUI nodeUI in nodesToRepaint) {
                    token.ThrowIfCancellationRequested();

                    await nodeUI.RepaintPreview();
                }
            }
        }
    }
}

#endif
