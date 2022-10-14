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
        "Math/Natural Log"
    )]

#endif

    [Serializable]
    public class NaturalLogNode : BaseNode {
        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var results = new List<float>(inputs[0].Count);

            for (int i = 0; i < inputs[0].Count; i++) {
                results.Add(Mathf.Log(inputs[0][i], MathF.E));
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

    public class NaturalLogNodeUI : BaseNodeUI {
        protected override string Description =>
            "Apply the natural logarithm (base e)\n" +
            "function to the input.";
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class NaturalLogNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            NaturalLogNode model
        ) {
            IModelUI ui = new NaturalLogNodeUI();

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
