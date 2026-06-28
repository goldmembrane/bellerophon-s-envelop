using System.Reflection;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{
    /// class Test : Object
    /// {
    /// [SerializeField] private int m_Name;
    /// }
    /// class TestEditor : CustomEditor
    /// {
    /// [SerializeField] private SerializedProperty {m_Name}Prop;
    /// }
    class CustomEditor : Editor
    {
        private void OnEnable()
        {
            if (target == null) return;

            FindProperties();
        }

        protected void FindProperties()
        {
            var type = this.GetType();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var field in type.GetFields(flags))
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    field.SetValue(this, FindProperty(field.Name));
                }
            }
        }

        protected SerializedProperty FindProperty(string name)
        {
            return serializedObject.FindProperty(name.Remove(name.Length - 4));
        }
    }
}