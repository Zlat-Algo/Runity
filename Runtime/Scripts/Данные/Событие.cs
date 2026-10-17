/*using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct Событие
{
    public GameObject объект;
    public Component компонент;

}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(Событие))]
public class СобытиеDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty objProp = property.FindPropertyRelative("объект");
        SerializedProperty compProp = property.FindPropertyRelative("компонент");

        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        Rect objRect = new Rect(position.x, position.y, position.width, singleLineHeight);
        Rect compRect = new Rect(position.x, position.y + singleLineHeight + spacing, position.width, singleLineHeight);

        // Поле для выбора объекта
        EditorGUI.PropertyField(objRect, objProp, new GUIContent("Объект"));

        // Определяем GameObject из ссылки (если в поле объект или компонент этого объекта)
        GameObject go = null;
        if (objProp.objectReferenceValue is GameObject g)
        {
            go = g;
        }
        else if (objProp.objectReferenceValue is Component c)
        {
            go = c.gameObject;
        }

        if (go != null)
        {
            // Получаем все компоненты объекта
            Component[] components = go.GetComponents<Component>();
            string[] options = new string[components.Length];
            int selectedIndex = 0;

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    options[i] = $"{i}: {components[i].GetType().Name}";
                    // Ищем текущий выбранный компонент, чтобы подсветить его в списке
                    if (components[i] == compProp.objectReferenceValue)
                    {
                        selectedIndex = i;
                    }
                }
                else
                {
                    options[i] = "Null Component";
                }
            }

            // Отрисовываем выпадающий список
            int newIndex = EditorGUI.Popup(compRect, "Компонент", selectedIndex, options);

            // Если индекс изменился, записываем новый компонент в свойство
            if (newIndex >= 0 && newIndex < components.Length)
            {
                compProp.objectReferenceValue = components[newIndex];
            }
        }
        else
        {
            // Если объект не выбран, выводим заблокированное предупреждение
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.Popup(compRect, "Компонент", 0, new string[] { "Сначала выберите объект" });
            EditorGUI.EndDisabledGroup();
            compProp.objectReferenceValue = null;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Высота для двух строк полей
        return EditorGUIUtility.singleLineHeight * 2 + 4f;
    }
}
#endif*/