#if UNITY_EDITOR

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain {
    public class TerrainGraphOnboardingProvider : OnboardingProvider {
        public override VisualElement CreateOnboardingElements(
            CommandDispatcher commandDispatcher
        ) {
            var template = new GraphTemplate<TerrainGraphStencil>(
                TerrainGraphStencil.GraphName
            );

            // Customize onboarding view (what is displayed when no usable asset
            // is selected)

            VisualElement window = OnboardingProvider
                .AddNewGraphButton<TerrainGraphAsset>(template);

            window.Q<Label>().text =
                $"No {TerrainGraphStencil.GraphName} selected";

            return window;
        }
    }
}

#endif
