using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace MarcosPereira.Terrain {
    // Graph Tools Foundation's command classes must be registered and cannot have "open constructed"
    // types, which means GenericCommand must be declared once for each specific type of TValue
    // before it is dispatched.
    // This declaring is done in the RegisterCommandHandlers method of the Graph State class.
    public class GenericCommand<TValue> :
    ModelCommand<NodeModel, TValue> {
        public readonly string fieldName;

        public GenericCommand(
            NodeModel node,
            string fieldName, // Name of the field holding the value.
            TValue value,
            string undoStringSingular,
            string undoStringPlural
        ) : base(undoStringSingular, undoStringPlural, value, new NodeModel[] { node }) {
            this.fieldName = fieldName;
        }

        public static void DefaultHandler(
            GraphToolState state,
            GenericCommand<TValue> command
        ) {
            state.PushUndo(command);

            using GraphViewStateComponent.StateUpdater graphUpdater =
                state.GraphViewState.UpdateScope;

            foreach (NodeModel nodeModel in command.Models) {
                nodeModel.GetType().GetField(
                    command.fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase
                )
                .SetValue(nodeModel, command.Value);

                graphUpdater.MarkChanged(nodeModel);
            }
        }
    }
}
