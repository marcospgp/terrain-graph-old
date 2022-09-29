using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace MarcosPereira.Terrain {
    [Serializable]
    public abstract class BaseNode : NodeModel {
        // Remember whether preview is expanded or collapsed.
        // Must be in BaseNode and not in BaseNodeUI, as node state gets stored
        // here.
        public bool isPreviewExpanded = true;

        public NodePreview preview;

        protected BaseNode() {
            this.preview = new NodePreview(this);
        }

#pragma warning disable CA1725, RCS1168 // Overridden parameter name differs from original

        public override void OnConnection(IPortModel self, IPortModel other) {
            base.OnConnection(self, other);

            // Refresh UI when an input is connected
            if (self.Direction == PortDirection.Input) {
                this.preview.MarkParameterChanged();
            }
        }

        public override void OnDisconnection(IPortModel self, IPortModel other) {
            base.OnDisconnection(self, other);

            // Refresh UI when an input is disconnected
            if (self.Direction == PortDirection.Input) {
                this.preview.MarkParameterChanged();
            }
        }

#pragma warning restore

        public void AddInputPort(
            string name,
            TypeHandle dataType,
            bool showEmbeddedConstant = false
        ) {
            PortModelOptions options = showEmbeddedConstant ?
                PortModelOptions.Default : PortModelOptions.NoEmbeddedConstant;

            this.preview.OnInputPortAdded(options);

            // Port must be of type PortType.Data to disallow multiple input
            // connections, not sure why.
            _ = base.AddInputPort(
                name,
                PortType.Data,
                dataType,
                options: options
            );
        }

        // Force usage of PortModelOptions.NoEmbeddedConstant
        // to hide port input box while still disallowing multiple inputs with
        // PortType.Data. Not sure why PortType.Execution allows multiple
        // inputs.
        public void AddOutputPort(string name, TypeHandle dataType) =>
            base.AddOutputPort(
                name,
                PortType.Data,
                dataType,
                options: PortModelOptions.NoEmbeddedConstant
            );

        public async Task<List<float>> GetValues(
            List<Vector3Int> points,
            string outputPortName
        ) {
            List<float>[] upstreamValues = await this.GetUpstreamValues(points);

            List<float> values = await this.Execute(
                points,
                inputs: upstreamValues,
                outputPortName
            );

            if (values.Count != points.Count) {
                throw new Exception(
                    $"Node of type \"{this.GetType()}\" returned wrong number of values."
                );
            }

            return values;
        }

        /// <summary>
        /// Each type of node should implement its logic in an overriding
        /// Execute() method.
        /// </summary>
        /// <param name="outputPortName">
        /// The name of the output port to retrieve values from.
        /// Only necessary when the node has more than one output port, as
        /// a value of null should be interpreted as the first port.
        /// </param>
        public abstract Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        );

        private Task<List<float>[]> GetUpstreamValues(List<Vector3Int> points) {
            IPortModel[] inputPorts = this.GetInputPorts().ToArray();

            var tasks = new Task<List<float>>[inputPorts.Length];

            for (int i = 0; i < inputPorts.Length; i++) {
                IEnumerable<IEdgeModel> edges = inputPorts[i].GetConnectedEdges();

                // If no upstream port is connected
                if (!edges.Any()) {
                    // If input port has no embedded value
                    if (inputPorts[i].EmbeddedValue == null) {
                        tasks[i] = Task.FromResult(
                            new List<float>(new float[points.Count])
                        );
                        continue;
                    }

                    var list = new List<float>(points.Count);
                    float value = (float) inputPorts[i].EmbeddedValue.ObjectValue;

                    for (int j = 0; j < points.Count; j++) {
                        list.Add(value);
                    }

                    tasks[i] = Task.FromResult(list);
                    continue;
                }

                var baseNode = (BaseNode) edges.First().FromPort.NodeModel;
                string portName = ((PortModel) edges.First().FromPort).Title;

                tasks[i] = baseNode.GetValues(points, portName);
            }

            return Task.WhenAll(tasks);
        }
    }
}
