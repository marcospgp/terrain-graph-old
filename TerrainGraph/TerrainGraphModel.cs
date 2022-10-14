// Allow overridden parameter names to be different from base definition.
// Graph Tools Foundation naming is bad.
#pragma warning disable RCS1168, CA1725

using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace MarcosPereira.Terrain {
    [Serializable]
    public class TerrainGraphModel : GraphModel {
        public TerrainGraphModel() {
            // This is done in the samples, no idea why. Doing it too.
            this.StencilType = null;
        }

#if UNITY_EDITOR

        public override Type DefaultStencilType => typeof(TerrainGraphStencil);

#pragma warning disable IDE0055 // Formatting
        protected override bool IsCompatiblePort(
            IPortModel a,
            IPortModel b
        ) =>
            // Nodes can't be connected to ports of the same node
            a.NodeModel != b.NodeModel &&
            // Outputs must connect to inputs.
            // No input -> input or output -> output.
            a.Direction != b.Direction &&
            // Only compatible data types can be linked
            a.DataTypeHandle == b.DataTypeHandle;
#pragma warning restore IDE0055

#endif

    }
}
