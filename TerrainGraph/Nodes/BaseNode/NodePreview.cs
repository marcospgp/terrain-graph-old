using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace MarcosPereira.Terrain.Graph {
    public class NodePreview {
        private const int PREVIEW_WIDTH = 128;

        // Keep track of embedded values (used on disconnected input ports),
        // so we can refresh the node preview when they change.
        private readonly List<float> embeddedConstantCache = new List<float>();

        private readonly List<Vector2> previewPoints;

        private readonly BaseNode node;

        // If a parameter changed, this node's preview is outdated and should be
        // repainted.
        private bool parameterChanged;

        // We need a flag to signal wether the preview is stale separately of
        // the preview value cache, because downstream nodes may refresh this
        // node's cache while they refresh their own preview - but this node's
        // preview will still be stale.
        private bool isPreviewStale;

        // Dictionary mapping output port names to cached values.
        // Used for preview texture.
        private Dictionary<string, List<float>> previewValuesCache;

        public NodePreview(BaseNode node) {
            this.node = node;

            // Calculate and store preview points

            const int width = PREVIEW_WIDTH;

            var points = new List<Vector2>(width * width);
            const float unitsPerPixel = 1000f / (float) width;

            for (int i = -(width / 2); i < (width / 2); i++) {
                for (int j = -(width / 2); j < (width / 2); j++) {
                    points.Add(
                        new Vector2(
                            Mathf.FloorToInt(i * unitsPerPixel),
                            Mathf.FloorToInt(j * unitsPerPixel)
                        )
                    );
                }
            }

            this.previewPoints = points;
        }

        // Mark this and downstream nodes as dirty.
        public void MarkParameterChanged(bool propagate = true) {
            this.parameterChanged = true;
            this.isPreviewStale = true;

            if (!propagate) {
                return;
            }

            foreach (IPortModel port in this.node.GetOutputPorts()) {
                foreach (IPortModel target in port.GetConnectedPorts()) {
                    if (target.NodeModel is BaseNode baseNode) {
                        baseNode.preview.MarkParameterChanged();
                    }
                }
            }
        }

        public void OnInputPortAdded(PortModelOptions options) {
            if ((options & PortModelOptions.NoEmbeddedConstant) == 0) {
                this.embeddedConstantCache.Add(default);
            }
        }

        public async Task<Texture2D> RefreshPreview() {
            // Set flag to false right away, as node may be marked stale while
            // we refresh it.
            this.isPreviewStale = false;

            // If we are getting the preview for a node, we can assume it only
            // has one output port - only 2D nodes get previews.
            string outputPortName =
                ((PortModel) this.node.GetOutputPorts().First()).Title;

            List<float> previewValues = await this.GetPreviewValues(outputPortName);

            const int width = PREVIEW_WIDTH;

            var previewTexture = new Texture2D(
                width,
                width,
                TextureFormat.RGBA32,
                mipChain: false
            );

            for (int i = 0; i < width; i++) {
                for (int j = 0; j < width; j++) {
                    var color = Color.Lerp(
                        Color.black,
                        Color.white,
                        previewValues[(i * width) + j]
                    );

                    previewTexture.SetPixel(i, j, color);
                }
            }

            previewTexture.Apply();

            return previewTexture;
        }

        public bool IsCacheStale() {
            this.RefreshEmbeddedConstantCache();

            // Sometimes cache is null, not sure why.
            // Happened when drawing previews for nodes in creation menu.
            bool isCacheNull = this.previewValuesCache == null;

            return this.parameterChanged || isCacheNull;
        }

        // Note this method is different from IsCacheStale(). It only concerns
        // the preview itself, not its cached values.
        public bool ShouldRepaint() {
            this.RefreshEmbeddedConstantCache();

            return this.isPreviewStale;
        }

        private async Task<List<float>> GetPreviewValues(string outputPortName) {
            if (this.IsCacheStale()) {
                await this.RefreshCache();
            }

            return this.previewValuesCache[outputPortName];
        }

        // Refresh cached values used for preview texture.
        private async Task RefreshCache() {
            // Set flags to false right away, as node may be marked stale while
            // we refresh it.
            this.parameterChanged = false;

            List<Vector2> points = this.previewPoints;

            IPortModel[] outputPorts = this.node.GetOutputPorts().ToArray();

            this.previewValuesCache = new Dictionary<string, List<float>>(
                capacity: outputPorts.Length
            );

            List<float>[] upstreamValues = await this.GetUpstreamPreviewValues();

            // Get this node's preview values for each of its output ports
            for (int i = 0; i < outputPorts.Length; i++) {
                string outputPortName = ((PortModel) outputPorts[i]).Title;

                List<float> values = await this.node.Execute(
                    points,
                    inputs: upstreamValues,
                    outputPortName
                );

                this.previewValuesCache.Add(outputPortName, values);
            }
        }

        private async Task<List<float>[]> GetUpstreamPreviewValues() {
            const int pointCount = PREVIEW_WIDTH * PREVIEW_WIDTH;

            IPortModel[] inputPorts = this.node.GetInputPorts().ToArray();

            var values = new List<float>[inputPorts.Length];

            for (int i = 0; i < inputPorts.Length; i++) {
                IEnumerable<IEdgeModel> edges = inputPorts[i].GetConnectedEdges();

                if (edges.Any()) {
                    var baseNode = (BaseNode) edges.First().FromPort.NodeModel;
                    string portName = ((PortModel) edges.First().FromPort).Title;

                    values[i] = await baseNode.preview.GetPreviewValues(portName);
                    continue;
                }

                if (inputPorts[i].EmbeddedValue == null) {
                    // Fill values with zeroes
                    values[i] = new List<float>(new float[pointCount]);
                    continue;
                }

                // Fill values with embedded value

                var list = new List<float>(pointCount);
                float value = (float) inputPorts[i].EmbeddedValue.ObjectValue;

                for (int j = 0; j < pointCount; j++) {
                    list.Add(value);
                }

                values[i] = list;
            }

            return values;
        }

        private void RefreshEmbeddedConstantCache() {
            int i = 0;
            bool dirty = false;

            foreach (IPortModel input in this.node.GetInputPorts()) {
                if (input.EmbeddedValue == null) {
                    continue;
                }

                float value = (float) input.EmbeddedValue.ObjectValue;

                if (value != this.embeddedConstantCache[i]) {
                    dirty = true;

                    // Store new value
                    this.embeddedConstantCache[i] = value;
                }

                i++;
            }

            // Only make dirty flag true, not false - so that calling this
            // method for a second time would not signal not dirty after
            // signaling dirty.
            if (dirty) {
                this.MarkParameterChanged();
            }
        }
    }
}
