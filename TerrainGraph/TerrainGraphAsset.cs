using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MarcosPereira.Terrain.Graph;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;

#endif

// This class is in a more accessible namespace because it is commonly used.
namespace MarcosPereira.Terrain {
    [Serializable]
    public class TerrainGraphAsset : GraphAssetModel {
        protected override Type GraphModelType => typeof(TerrainGraphModel);

#if UNITY_EDITOR

        [MenuItem("Assets/Create/Terrain Graph")]
        public static void CreateGraph(/* MenuCommand menuCommand */) {
            var template = new GraphTemplate<TerrainGraphStencil>(
                TerrainGraphStencil.GraphName
            );

            CommandDispatcher commandDispatcher = null;

            if (EditorWindow.HasOpenInstances<TerrainGraphWindow>()) {
                TerrainGraphWindow window =
                    EditorWindow.GetWindow<TerrainGraphWindow>();

                if (window != null) {
                    commandDispatcher = window.CommandDispatcher;
                }
            }

            // Create asset in active directory.
            // From https://forum.unity.com/threads/how-to-get-currently-selected-folder-for-putting-new-asset-into.81359/#post-6752458
            MethodInfo getActiveFolderPath = typeof(ProjectWindowUtil).GetMethod(
                "GetActiveFolderPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            string folderPath = (string) getActiveFolderPath.Invoke(null, null);

            GraphAssetCreationHelpers<TerrainGraphAsset>.CreateInProjectWindow(
                template,
                commandDispatcher,
                folderPath
            );
        }

        [OnOpenAsset(1)]
        [SuppressMessage("", "RCS0056", Justification = "Allow long line.")]
        [SuppressMessage(
            "",
            "IDE0060",
            Justification = "Allow unused parameter (Unity enforces method signature for OnOpenAsset)"
        )]
        [SuppressMessage("", "RCS1163", Justification = "Same as above.")]
        public static bool OpenGraphAsset(int instanceId, int line) {
            UnityEngine.Object obj =
                EditorUtility.InstanceIDToObject(instanceId);

            if (obj is TerrainGraphAsset graphAssetModel) {
                // Close any existing terrain graph editor window before
                // switching graphs, otherwise the async task that repaints
                // node previews will throw an exception.

                TerrainGraphWindow[] existingWindow =
                    Resources.FindObjectsOfTypeAll<TerrainGraphWindow>();

                if (existingWindow.Length > 0) {
                    try {
                        existingWindow[0].Close();
                    } catch (NullReferenceException) {
                        UnityEngine.Debug.LogError(
                            "Terrain Graph: Couldn't close open editor. " +
                            "Please try resetting your window layout before " +
                            "opening a new graph."
                        );

                        throw;
                    }
                }

                // Ensure graph editor is docked next to scene view.
                // If no scene view exists, it opens as a standalone window.
                TerrainGraphWindow window = EditorWindow.GetWindow<TerrainGraphWindow>(
                    new Type[] { typeof(SceneView) }
                );

                window.SetCurrentSelection(
                    graphAssetModel,
                    GraphViewEditorWindow.OpenMode.OpenAndFocus
                );

                return window != null; // If true, we handled the asset open
            }

            return false; // We did not handle the asset open
        }

#endif

    }
}
