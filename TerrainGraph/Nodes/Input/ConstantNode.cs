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
        "Input/Constant"
    )]

#endif

    [Serializable]
    public class ConstantNode : BaseNode {
        // Fields must be public to be accessible by UI parts
        public float value;

        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var values = new List<float>(points.Count);

            for (int i = 0; i < points.Count; i++) {
                values.Add(this.value);
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

    public class ConstantNodeUI : BaseNodeUI {
        protected override string Description => "A constant value.";

        protected override void BuildPartList() {
            base.BuildPartList();
            this.AddInputField<float>("value");
        }
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class ConstantNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            ConstantNode model
        ) {
            IModelUI ui = new ConstantNodeUI();

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
