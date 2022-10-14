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
        "Math/Absolute"
    )]

#endif

    [Serializable]
    public class AbsoluteNode : BaseNode {
        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var results = new List<float>(inputs[0].Count);

            for (int i = 0; i < inputs[0].Count; i++) {
                results.Add(Mathf.Abs(inputs[0][i]));
            }

            return Task.FromResult(results);
        }

        protected override void OnDefineNode() {
            base.OnDefineNode();

            this.AddInputPort(
                "Input",
                TypeHandle.Float,
                showEmbeddedConstant: true
            );

            this.AddOutputPort(
                "Output",
                TypeHandle.Float
            );
        }
    }

#if UNITY_EDITOR

    public class AbsoluteNodeUI : BaseNodeUI {
        protected override string Description =>
            "Multiply negative values in the input by -1.";
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class AbsoluteNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            AbsoluteNode model
        ) {
            IModelUI ui = new AbsoluteNodeUI();

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
