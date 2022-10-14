#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace MarcosPereira.Terrain.Graph {
    public class TerrainGraphStencil : Stencil {
        public static string GraphName => "Terrain Graph";

        public override string ToolName => TerrainGraphStencil.GraphName;

        public override void PreProcessGraph(IGraphModel graphModel) {
            base.PreProcessGraph(graphModel);

            // Add default output node if it doesn't exist yet.

            bool outputNodeExists = this.GraphModel.NodeModels.Any(
                node => node is TerrainNode
            );

            if (!outputNodeExists) {
                _ = this.GraphModel.CreateNode<TerrainNode>(
                    "Terrain",
                    UnityEngine.Vector2.zero
                );
            }
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle) =>
            TypeToConstantMapper.GetConstantNodeType(typeHandle);

        public override IBlackboardGraphModel CreateBlackboardGraphModel(
            IGraphAssetModel graphAssetModel
        ) =>
            new BlackboardGraphModel(graphAssetModel);
    }
}

#endif
