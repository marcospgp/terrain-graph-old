#if UNITY_EDITOR

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace MarcosPereira.Terrain.Graph {
    public class TerrainGraphState : GraphToolState {
        public TerrainGraphState(
            Hash128 graphViewEditorWindowGUID,
            Preferences preferences
        )
        : base(graphViewEditorWindowGUID, preferences) {
            this.SetInitialSearcherSize(
                SearcherService.Usage.k_CreateNode,
                new Vector2(425, 400),
                2.0f
            );
        }

        public override void RegisterCommandHandlers(Dispatcher dispatcher) {
            base.RegisterCommandHandlers(dispatcher);

            if (!(dispatcher is CommandDispatcher commandDispatcher)) {
                return;
            }

            // Generic Command can be reused for any input field.
            // Only has to be registered once per field type.

            commandDispatcher
                .RegisterCommandHandler<GenericCommand<string>>(
                    GenericCommand<string>.DefaultHandler
                );

            commandDispatcher
                .RegisterCommandHandler<GenericCommand<float>>(
                    GenericCommand<float>.DefaultHandler
                );

            commandDispatcher
                .RegisterCommandHandler<GenericCommand<int>>(
                    GenericCommand<int>.DefaultHandler
                );
        }
    }
}

#endif
