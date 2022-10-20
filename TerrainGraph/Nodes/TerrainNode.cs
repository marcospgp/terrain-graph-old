#pragma warning disable SA1402 // SA1402: File may only contain a single type
#pragma warning disable SA1204 // SA1204: Static members should appear before non-static members

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarcosPereira.Terrain.Graph;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using MarcosPereira.Terrain.Graph.Editor;
#endif

// This class is in a more accessible namespace because it is commonly used.
namespace MarcosPereira.Terrain {
    [Serializable]
    public class TerrainNode : NodeModel {
        // Fields must be public to be accessible by UI
        public int smoothingIterations = 16;
        public int maxHeight = 256;
        public float vegetationMaxSlope = 45f;
        // public int maxCaveHeight = 32;
        // public int caveIterations = 1;

        private IPortModel heightInputPort;
        private IPortModel vegetationDensityInputPort;
        // private IPortModel caveHeightInputPort;
        // private IPortModel caveDepthInputPort;

        public TerrainNode() {
            this.SetCapability(
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Deletable,
                false
            );

            this.SetCapability(
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Renamable,
                false
            );

            this.SetCapability(
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Copiable,
                false
            );
        }

        /// <summary>
        /// X and Z are the world space coordinates of the heightmap's southwest
        /// corner.
        /// </summary>
        public async Task<float[,]> GetHeightmap(
            int x,
            int z,
            int widthInSteps,
            float stepSize = 1f
        ) {
            List<Vector2> points =
                TerrainNode.GetPoints(x, z, widthInSteps, stepSize);

            // Get heights in range [0, TerrainNode.maxHeight].
            List<float> heights = await TerrainNode.GetUpstreamValues(
                this.heightInputPort,
                points
            );

            heights = heights.ConvertAll(
                x => float.IsNaN(x) ? 0 : Mathf.Clamp01(x) * this.maxHeight
            );

            return TerrainNode.ListToMap(heights, widthInSteps);
        }

        /// <summary>
        /// Get density map for environment objects (grass, trees, etc.).
        /// Density is in range [0, 1].
        /// </summary>
        public async Task<float[,]> GetEnvironmentObjectDensity(
            (int x, int z) worldPos,
            int width
        ) {
            List<Vector2> points = TerrainNode.GetPoints(
                worldPos.x,
                worldPos.z,
                width
            );

            List<float> values = await TerrainNode.GetUpstreamValues(
                this.vegetationDensityInputPort,
                points
            );

            return TerrainNode.ListToMap(values, width);
        }

        /*
        Caves were removed for now.

        /// <summary>
        /// Get two 2D maps describing caves: the first is how tall they are,
        /// the second is how deep.
        /// </summary>
        public async Task<float[][,]> GetCaveMaps(int x, int z, int chunkWidth) {
            List<Vector2> points = TerrainNode.GetPoints(x, z, chunkWidth);

            List<float> heightValues = await TerrainNode.GetUpstreamValues(
                this.caveHeightInputPort,
                points
            );

            List<float> depthValues = await TerrainNode.GetUpstreamValues(
                this.caveDepthInputPort,
                points
            );

            return new float[][,] {
                TerrainNode.ListToMap(heightValues, chunkWidth),
                TerrainNode.ListToMap(depthValues, chunkWidth)
            };
        }
        */

        protected override void OnDefineNode() {
            base.OnDefineNode();

            // Input ports must be of type PortType.Data to disallow multiple
            // input connections, not sure why.

            this.heightInputPort = this.AddInputPort(
                "Height",
                PortType.Data,
                TypeHandle.Float,
                options: PortModelOptions.NoEmbeddedConstant
            );

            this.heightInputPort.ToolTip =
                "The ground's height at a given coordinate. " +
                "Values are clamped to [0, 1], where 1 represents the " +
                "maximum height.";

            // Removed caves for now.

            // this.caveDepthInputPort = this.AddInputPort(
            //     "Cave Depth",
            //     PortType.Data,
            //     TypeHandle.Float,
            //     options: PortModelOptions.NoEmbeddedConstant
            // );

            // this.caveDepthInputPort.ToolTip =
            //     "How deep a cave will be if it exists at a given coordinate.";

            // this.caveHeightInputPort = this.AddInputPort(
            //     "Cave Height",
            //     PortType.Data,
            //     TypeHandle.Float,
            //     options: PortModelOptions.NoEmbeddedConstant
            // );

            // this.caveHeightInputPort.ToolTip =
            //     "The cave height in units at a given coordinate.";

            this.vegetationDensityInputPort = this.AddInputPort(
                "Vegetation Density",
                PortType.Data,
                TypeHandle.Float,
                options: PortModelOptions.NoEmbeddedConstant
            );
        }

        private static T[,] ListToMap<T>(List<T> list, int width) {
            var map = new T[width, width];

            for (int i = 0; i < width; i++) {
                for (int j = 0; j < width; j++) {
                    map[i, j] = list[(i * width) + j];
                }
            }

            return map;
        }

        /// <summary>X and Z are in world space (not chunk space).</summary>
        private static List<Vector2> GetPoints(
            int x,
            int z,
            int widthInSteps,
            float stepSize = 1f
        ) {
            var points = new List<Vector2>(widthInSteps * widthInSteps);

            for (int i = 0; i < widthInSteps; i++) {
                for (int j = 0; j < widthInSteps; j++) {
                    points.Add(
                        new Vector2(
                            x + (i * stepSize),
                            z + (j * stepSize)
                        )
                    );
                }
            }

            return points;
        }

        private static Task<List<float>> GetUpstreamValues(
            IPortModel inputPort,
            List<Vector2> points
        ) {
            IEnumerable<IEdgeModel> edges = inputPort.GetConnectedEdges();

            if (!edges.Any()) {
                return Task.FromResult(new List<float>(new float[points.Count]));
            }

            var baseNode = (BaseNode) edges.First().FromPort.NodeModel;
            string portName = ((PortModel) edges.First().FromPort).Title;

            return baseNode.GetValues(points, portName);
        }
    }

#if UNITY_EDITOR

    public class TerrainNodeUI : BaseNodeUI {
        protected override void BuildPartList() {
            base.BuildPartList();

            this.InsertPart(
                new LabelUIPart(
                    this,
                    "Settings",
                    fontStyle: FontStyle.Bold
                )
            );

            this.AddInputField<int>(
                "maxHeight",
                tooltip: "The height in units reached by a height value of 1."
            );

            // Hiding smoothing level from user as it doesn't seem to change
            // anything qualitatively. Always was setting it to 1 to reduce
            // number of smoothing iterations.
            //
            // const string t1 =
            //     "How close each vertex is brought towards the average of its " +
            //     "neighbors on each smoothing iteration. 0 is not moved at " +
            //     "all, 1 is moved completely.";

            // this.AddInputField<float>(
            //     "smoothingLevel",
            //     displayName: "Intensity",
            //     tooltip: t1
            // );

            const string t2 =
                "The number of times each vertex is moved towards the " +
                "average of its neighbors.";

            this.AddInputField<int>(
                "smoothingIterations",
                displayName: "Smoothing Iterations",
                tooltip: t2
            );

            // Caves are disabled for now.

            // this.InsertPart(
            //     new LabelUIPart(
            //         this,
            //         "Caves",
            //         fontStyle: FontStyle.Bold
            //     )
            // );

            // this.AddInputField<int>(
            //     "maxCaveHeight",
            //     displayName: "Max Cave Height",
            //     tooltip: "The cave height in units reached by a value of 1."
            // );

            this.InsertPart(
                new LabelUIPart(
                    this,
                    "Vegetation",
                    fontStyle: FontStyle.Bold
                )
            );

            const string t3 =
                "Maximum ground angle at which vegetation will be placed. " +
                "Should be in the range [0, 90].";

            this.AddInputField<float>(
                "vegetationMaxSlope",
                displayName: "Max Vegetation Slope",
                tooltip: t3
            );
        }

        protected override void PostBuildUI() {
            base.PostBuildUI();

            this.contentContainer.style.minWidth = 256f;

            // Highlight border to indicate this is a special node

            var gray = new Color(0.8f, 0.8f, 0.8f, 1f);

            this.contentContainer.style.borderTopColor = gray;
            this.contentContainer.style.borderRightColor = gray;
            this.contentContainer.style.borderBottomColor = gray;
            this.contentContainer.style.borderLeftColor = gray;

            this.contentContainer.style.borderTopWidth = 3f;
            this.contentContainer.style.borderRightWidth = 3f;
            this.contentContainer.style.borderBottomWidth = 3f;
            this.contentContainer.style.borderLeftWidth = 3f;

            this.contentContainer.style.borderTopLeftRadius = 6f;
            this.contentContainer.style.borderTopRightRadius = 6f;
            this.contentContainer.style.borderBottomRightRadius = 6f;
            this.contentContainer.style.borderBottomLeftRadius = 6f;

            VisualElement icon =
                this.contentContainer.MandatoryQ(className: "ge-node__icon");

            icon.style.backgroundImage = Resources.Load<Texture2D>("mountain-icon");
        }
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class TerrainNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            TerrainNode model
        ) {
            IModelUI ui = new TerrainNodeUI();

            ui.SetupBuildAndUpdate(
                model,
                dispatcher,
                elementBuilder.View,
                elementBuilder.Context
            );

            return ui;
        }
    }

#endif

}
