using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Несколько тегов сразу. Если не выбран ни один, подходит любой объект
/// </summary>
[Serializable]
public class Теги
{
    [SerializeField] List<string> значение = new List<string>();

    public Теги() { }

    public Теги(params string[] теги)
    {
        foreach (string тег in теги) Добавить(тег);
    }

    public static implicit operator Теги(string тег) => new Теги(тег);
    public static implicit operator Теги(Строка тег) => new Теги((string)тег);

    /// <summary>
    /// Выбранные теги
    /// </summary>
    public IReadOnlyList<string> список => значение;

    /// <summary>
    /// Даёт "да", если не выбран ни один тег, то есть подходит любой объект
    /// </summary>
    public Флажок любые => значение.Count == 0;

    /// <summary>
    /// Даёт "да", если у объекта есть один из выбранных тегов или теги не выбраны
    /// </summary>
    public Флажок Подходит(GameObject объект)
    {
        if (значение.Count == 0) return true;
        if (объект == null) return false;

        foreach (string тег in значение)
        {
            if (!string.IsNullOrEmpty(тег) && объект.CompareTag(тег)) return true;
        }
        return false;
    }

    /// <summary>
    /// Даёт "да", если этот тег выбран
    /// </summary>
    public Флажок Содержит(Строка тег) => значение.Contains(тег);

    /// <summary>
    /// Добавляет тег к выбранным
    /// </summary>
    public void Добавить(Строка тег)
    {
        if (!string.IsNullOrEmpty(тег) && !значение.Contains(тег)) значение.Add(тег);
    }

    /// <summary>
    /// Убирает тег из выбранных
    /// </summary>
    public void Убрать(Строка тег) => значение.Remove(тег);

    /// <summary>
    /// Убирает все теги. После этого подходит любой объект
    /// </summary>
    public void Очистить() => значение.Clear();

    public override string ToString() => значение.Count == 0 ? "Любой тег" : string.Join(", ", значение);
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Теги))]
internal class ТегиDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty список = property.FindPropertyRelative("значение");
        List<string> выбранные = new List<string>();
        for (int i = 0; i < список.arraySize; i++) выбранные.Add(список.GetArrayElementAtIndex(i).stringValue);

        Rect поле = EditorGUI.PrefixLabel(position, label);
        string надпись = выбранные.Count == 0 ? "Любой тег" : string.Join(", ", выбранные);

        if (EditorGUI.DropdownButton(поле, new GUIContent(надпись, label.tooltip), FocusType.Keyboard))
        {
            SerializedObject объект = property.serializedObject;
            string путь = список.propertyPath;
            GenericMenu меню = new GenericMenu();

            меню.AddItem(new GUIContent("Любой тег"), выбранные.Count == 0, () => Изменить(объект, путь, теги => теги.Clear()));
            меню.AddSeparator("");

            foreach (string тег in UnityEditorInternal.InternalEditorUtility.tags)
            {
                string этот = тег;
                меню.AddItem(new GUIContent(этот), выбранные.Contains(этот), () => Изменить(объект, путь, теги =>
                {
                    if (!теги.Remove(этот)) теги.Add(этот);
                }));
            }

            меню.DropDown(поле);
        }

        EditorGUI.EndProperty();
    }

    // Меню закрывается уже после отрисовки, поэтому изменения записываются отдельно
    static void Изменить(SerializedObject объект, string путь, Action<List<string>> изменение)
    {
        объект.Update();
        SerializedProperty список = объект.FindProperty(путь);

        List<string> теги = new List<string>();
        for (int i = 0; i < список.arraySize; i++) теги.Add(список.GetArrayElementAtIndex(i).stringValue);

        изменение(теги);

        список.arraySize = теги.Count;
        for (int i = 0; i < теги.Count; i++) список.GetArrayElementAtIndex(i).stringValue = теги[i];
        объект.ApplyModifiedProperties();
    }
}
#endif
