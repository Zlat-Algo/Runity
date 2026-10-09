using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
internal class ПолеDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty значение =
            property.FindPropertyRelative("значение");

        if (значение != null)
        {
            EditorGUI.PropertyField(
                position,
                значение,
                label
            );
        }
        else
        {
            EditorGUI.LabelField(
                position,
                label.text,
                "Не найдено поле «значение»"
            );
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty значение =
            property.FindPropertyRelative("значение");

        if (значение != null)
        {
            return EditorGUI.GetPropertyHeight(
                значение,
                label,
                true
            );
        }

        return EditorGUIUtility.singleLineHeight;
    }
}
#endif