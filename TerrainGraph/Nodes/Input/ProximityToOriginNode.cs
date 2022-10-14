#pragma warning disable SA1402 // SA1402: File may only contain a single type
#pragma warning disable SA1204 // SA1204: Static members should appear before non-static members

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace MarcosPereira.Terrain.Graph {
#if UNITY_EDITOR

    [SearcherItem(
        typeof(TerrainGraphStencil),
        SearcherContext.Graph,
        "Input/Proximity To Origin"
    )]

#endif

    [Serializable]
    public class ProximityToOriginNode : BaseNode {
        // Fields must be public to be accessible by UI parts
        public float steepness = 2f;
        public float scale = 500f;

        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var values = new List<float>(points.Count);

            for (int i = 0; i < points.Count; i++) {
                float distance = Vector3Int.Distance(Vector3Int.zero, points[i]);

                float proximity = 1f / (1f + distance);

                proximity *= this.scale;

                proximity = Mathf.Pow(proximity, this.steepness);

                values.Add(Mathf.Clamp01(proximity));
                // values.Add(Mathf.Clamp01(PerlinNoise.SmootherStep(proximity)));
            }

            return Task.FromResult(values);
        }

        protected override void OnDefineNode() {
            base.OnDefineNode();

            this.AddOutputPort(
                "Output",
                TypeHandle.Float
            );
        }
    }

#if UNITY_EDITOR

    public class ProximityToOriginNodeUI : BaseNodeUI {
        protected override string Description =>
            "Proximity to origin.\nOutput is clamped to [0, 1].";

        protected override void BuildPartList() {
            base.BuildPartList();

            this.AddInputField<float>("steepness");
            this.AddInputField<float>("scale");
        }
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class ProximityToOriginNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            ProximityToOriginNode model
        ) {
            IModelUI ui = new ProximityToOriginNodeUI();

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
