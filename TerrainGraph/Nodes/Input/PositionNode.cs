#pragma warning disable SA1402 // SA1402: File may only contain a single type
#pragma warning disable SA1204 // SA1204: Static members should appear before non-static members

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace MarcosPereira.Terrain {
#if UNITY_EDITOR

    [SearcherItem(
        typeof(TerrainGraphStencil),
        SearcherContext.Graph,
        "Input/Position"
    )]

#endif

    [Serializable]
    public class PositionNode : BaseNode {
        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) {
            var values = new List<float>(points.Count);

            for (int i = 0; i < points.Count; i++) {
                if (outputPortName == "X") {
                    values.Add(points[i].x);
                } else if (outputPortName == "Y") {
                    values.Add(points[i].y);
                } else if (outputPortName == "Z") {
                    values.Add(points[i].z);
                } else {
                    throw new Exception("Unknown port name.");
                }
            }

            return Task.FromResult(values);
        }

        protected override void OnDefineNode() {
            base.OnDefineNode();

            this.AddOutputPort(
                "X",
                TypeHandle.Float
            );

            // Disable Y port for now, seems to have no use.
            // All nodes are based on a 2D map, although the generation does
            // involve 3D voxels so this might be reactivated later.
            //
            // this.AddOutputPort(
            //     "Y",
            //     TypeHandle.Float
            // );

            this.AddOutputPort(
                "Z",
                TypeHandle.Float
            );
        }
    }

#if UNITY_EDITOR

    public class PositionNodeUI : BaseNodeUI {
        protected override string Description => "World space position.";
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class PositionNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            PositionNode model
        ) {
            IModelUI ui = new PositionNodeUI();

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
