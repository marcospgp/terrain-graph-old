#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public class AssetSavedListener : UnityEditor.AssetModificationProcessor {
        private static string listenPath;
        private static Action callback;

        public static void Listen(ScriptableObject obj, Action callback) {
            AssetSavedListener.listenPath = AssetDatabase.GetAssetPath(obj);
            AssetSavedListener.callback = callback;
        }

        // Unity callback inherited from AssetModificationProcessor
        public static string[] OnWillSaveAssets(string[] paths) {
            foreach (string path in paths) {
                if (path == AssetSavedListener.listenPath) {
                    AssetSavedListener.callback();
                }
            }

            // Do not interfere with which assets will be saved.
            return paths;
        }
    }
}

#endif
