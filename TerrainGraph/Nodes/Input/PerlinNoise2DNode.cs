#pragma warning disable SA1402 // SA1402: File may only contain a single type
#pragma warning disable SA1204 // SA1204: Static members should appear before non-static members

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarcosPereira.UnityUtilities;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace MarcosPereira.Terrain.Graph {
#if UNITY_EDITOR

    [SearcherItem(
        typeof(TerrainGraphStencil),
        SearcherContext.Graph,
        "Input/Perlin Noise"
    )]

#endif

    [Serializable]
    public class PerlinNoise2DNode : BaseNode {
        // Fields must be public to be accessible by UI parts
        public string seed = string.Empty;
        public float baseFrequency = 0.01f;
        public int numberOfOctaves = 1;
        public float lacunarity = 2f;
        public float persistence = 0.5f;

        public override Task<List<float>> Execute(
            List<Vector3Int> points,
            List<float>[] inputs,
            string outputPortName
        ) =>
            SafeTask.Run(() => // Run perlin noise on another thread
                points.ConvertAll(point =>
                    PerlinNoise.Get(
                        point.x,
                        point.z,
                        this.seed,
                        this.baseFrequency,
                        this.numberOfOctaves,
                        this.lacunarity,
                        this.persistence
                    )
                )
            );

        protected override void OnDefineNode() {
            base.OnDefineNode();

            this.AddOutputPort(
                "Output",
                TypeHandle.Float
            );
        }
    }

#if UNITY_EDITOR

    public class PerlinNoise2DNodeUI : BaseNodeUI {
        protected override string Description =>
            "Generate 2D perlin noise.\n" +
            "Output values are in the range [0, 1].";

        protected override void BuildPartList() {
            base.BuildPartList();

            this.AddInputField<string>("seed");
            this.AddInputField<float>("baseFrequency");
            this.AddInputField<int>("numberOfOctaves");
            this.AddInputField<float>("lacunarity");
            this.AddInputField<float>("persistence");
        }
    }

    // UI factory method
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class PerlinNoise2DNodeExtensions {
        public static IModelUI CreateNode(
            this ElementBuilder elementBuilder,
            CommandDispatcher dispatcher,
            PerlinNoise2DNode model
        ) {
            IModelUI ui = new PerlinNoise2DNodeUI();

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
