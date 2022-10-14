using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MarcosPereira.Terrain.Graph.Editor {
    public class InputFieldUIPart<TField> : BaseUIPart {
        private readonly string displayName;
        private readonly string tooltip;
        private readonly Action<ChangeEvent<TField>> callback;
        private VisualElement container;
        private TextField textField;
        private FloatField floatField;
        private IntegerField integerField;

        /// <summary>
        ///   Initializes a new instance of the <see cref="InputFieldUIPart{TField}"/> class.
        /// </summary>
        /// <param name="nodeUI"></param>
        /// <param name="fieldName">
        ///   The name of the field to attach to in the node's NodeModel or
        ///   NodeModel-derived class.
        /// </param>
        /// <param name="displayName"></param>
        /// <param name="tooltip"></param>
        /// <param name="changeCallback"></param>
        public InputFieldUIPart(
            Node nodeUI,
            string fieldName,
            string displayName = null,
            string tooltip = null,
            Action<ChangeEvent<TField>> changeCallback = null
        ) : base(nodeUI, fieldName) {
            this.displayName = displayName;
            this.tooltip = tooltip;
            this.callback = changeCallback;
        }

        public override VisualElement Root => this.container;

        protected override void BuildPartUI(VisualElement parent) {
            this.container = new VisualElement();
            this.container.style.marginBottom = 1;
            parent.Add(this.container);

            VisualElement field;

            string displayName;

            if (!string.IsNullOrEmpty(this.displayName)) {
                displayName = this.displayName;
            } else {
                // Convert field name from camelCase to Title Case
                displayName =
                    char.ToUpper(this.PartName[0], CultureInfo.InvariantCulture) +
                    Regex.Replace(this.PartName, "[A-Z]", " $0").Substring(1);
            }

            if (typeof(TField) == typeof(string)) {
                this.textField = new TextField(displayName) {
                    name = this.PartName
                };

                field = this.textField;
                this.container.Add(this.textField);
            } else if (typeof(TField) == typeof(float)) {
                this.floatField = new FloatField(displayName) {
                    name = this.PartName
                };

                field = this.floatField;
                this.container.Add(this.floatField);
            } else if (typeof(TField) == typeof(int)) {
                this.integerField = new IntegerField(displayName) {
                    name = this.PartName
                };

                field = this.integerField;
                this.container.Add(this.integerField);
            } else {
                throw new Exception("Unsupported field type.");
            }

            if (!string.IsNullOrEmpty(this.tooltip)) {
                field.tooltip = this.tooltip;
            }

            if (this.callback != null) {
                field.RegisterCallback(
                    new EventCallback<ChangeEvent<TField>>(this.callback)
                );
            }
        }

        protected override void UpdatePartFromModel() {
            FieldInfo fieldInfo = this.nodeModel.GetType().GetField(
                this.PartName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase
            );

            if (fieldInfo == null) {
                throw new Exception(
                    $"Field \"{this.PartName}\" not found in type " +
                    $"\"{this.nodeModel.GetType()}\"."
                );
            }

            object value = fieldInfo.GetValue(this.m_Model);

            if (typeof(TField) == typeof(string)) {
                this.textField.SetValueWithoutNotify((string) value);
            } else if (typeof(TField) == typeof(float)) {
                this.floatField.SetValueWithoutNotify((float) value);
            } else if (typeof(TField) == typeof(int)) {
                this.integerField.SetValueWithoutNotify((int) value);
            } else {
                throw new Exception("Unsupported TField type.");
            }
        }
    }
}
