using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace MarcosPereira.Terrain.Graph.Editor {
    public abstract class BaseUIPart : BaseModelUIPart {
        protected readonly NodeModel nodeModel;

        protected BaseUIPart(
            Node nodeUI,
            string name
        ) : base(name, nodeUI.Model, nodeUI, Node.ussClassName) {
            this.nodeModel = (NodeModel) nodeUI.NodeModel;
        }
    }
}
