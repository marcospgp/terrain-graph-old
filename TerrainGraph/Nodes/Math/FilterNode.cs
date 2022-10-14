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
        "Math/Filter"
    )]

#endif

    [Serializable]
    public class FilterNode : BaseNode {
        // Fields must be public to be accessible by UI parts
        public float min = 0f;
        public float max = 1f;

        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var results = new List<float>(inputs[0].Count);

            for (int i = 0; i < inputs[0].Count; i++) {
                if (inputs[0][i] < this.min || inputs[0][i] > this.max) {
                    results.Add(0f);
                } else {
                    results.Add(inputs[0][i]);
                }
            }

            return Task.FromResult(results);
        }

        protected override void OnDefineNode() {
            base.OnDefineNode();

            this.AddInputPort(
                "Input",
                TypeHandle.Float
            );

            this.AddOutputPort(
                "Output",
                TypeHandle.Float
            );
        }
    }

#if UNITY_EDITOR

    public class FilterNodeUI : BaseNodeUI {
        protected override string Description =>
            "Values outside given range become 0.";

        protected override void BuildPartList() {
            base.BuildPartList();

            this.AddInputField<float>("min");
            this.AddInputField<float>("max");
        }
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class FilterNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            FilterNode model
        ) {
            IModelUI ui = new FilterNodeUI();

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
