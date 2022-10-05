// using UnityEditor;
// // using UnityEditor.UIElements;
// using UnityEngine.UIElements;

// namespace MarcosPereira.Terrain {
//     [CustomEditor(typeof(TerrainGraphDebug))]
//     public class TerrainGraphDebugInspector : Editor {
//         public VisualTreeAsset inspectorXML;

//         public override VisualElement CreateInspectorGUI() {
//             // Create a new VisualElement to be the root of our inspector UI
//             var inspector = new VisualElement();

//             // Attach a default inspector to the foldout
//             // Commented out - throws error on startup that disappears on script reload. Weird.
//             // InspectorElement.FillDefaultInspector(inspector, this.serializedObject, this);

//             // Load from default reference
//             this.inspectorXML.CloneTree(inspector);

//             inspector.Query<Button>("BuildNow").First().clicked += () =>
//                 ((TerrainGraphDebug) this.target).BuildNow();

//             // Return the finished inspector UI
//             return inspector;
//         }
//     }
// }
