using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Вещь для инвентаря: монета, ключ, зелье. Название вещи - это название её файла
/// </summary>
[CreateAssetMenu(fileName = "Новая вещь", menuName = "Runity/Вещь", order = -97)]
public class Вещь : ScriptableObject
{
    [SerializeField] Sprite _иконка;
    [SerializeField] Sprite _иконкаОтсутствия;
    [SerializeField, Min(1)] int _максимумВЯчейке = 99;
    [SerializeField, TextArea] string _описание;
    [SerializeField] bool _учитывается = true;

    /// <summary>
    /// Название вещи. Оно совпадает с названием файла
    /// </summary>
    public Строка название => name;

    /// <summary>
    /// Картинка вещи в ячейке инвентаря
    /// </summary>
    public Sprite иконка { get => _иконка; set => _иконка = value; }

    /// <summary>
    /// Картинка, которая видна, когда вещи нет. Например, серый силуэт ключа. Если не указать, будет видна полупрозрачная иконка
    /// </summary>
    public Sprite иконкаОтсутствия { get => _иконкаОтсутствия; set => _иконкаОтсутствия = value; }

    /// <summary>
    /// Сколько таких вещей помещается в одну ячейку. Остальные займут следующую ячейку
    /// </summary>
    public int максимумВЯчейке { get => Mathf.Max(1, _максимумВЯчейке); set => _максимумВЯчейке = value; }

    /// <summary>
    /// Описание для игрока
    /// </summary>
    public Строка описание { get => _описание; set => _описание = value; }

    /// <summary>
    /// Если "нет", то вещь не появляется в списках и её нельзя положить в инвентарь. Удобно, чтобы отключить тестовые вещи, не удаляя их
    /// </summary>
    public bool учитывается { get => _учитывается; set => _учитывается = value; }

    /// <summary>
    /// Находит загруженную вещь по названию. Если такой нет, будет пусто
    /// </summary>
    public static Вещь Найти(Строка название)
    {
        return Resources.FindObjectsOfTypeAll<Вещь>().FirstOrDefault(вещь => вещь.name == название && вещь.учитывается);
    }

#if UNITY_EDITOR
    static List<Вещь> всеВещи;

    /// <summary>
    /// Все вещи проекта, которые учитываются в игре. Есть только в редакторе
    /// </summary>
    public static List<Вещь> ВсеВещиПроекта()
    {
        if (всеВещи == null)
        {
            всеВещи = AssetDatabase.FindAssets("t:Вещь")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Вещь>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(вещь => вещь != null)
                .OrderBy(вещь => вещь.name)
                .ToList();

            EditorApplication.projectChanged -= Сбросить;
            EditorApplication.projectChanged += Сбросить;
        }
        return всеВещи.Where(вещь => вещь != null && вещь.учитывается).ToList();
    }

    static void Сбросить() => всеВещи = null;

    void OnValidate() => всеВещи = null;
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(Вещь))]
internal class ВещьEditor : Editor
{
    // Картинка в окне Project: иконка вещи
    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        => ПревьюАссетов.ИзСпрайта(((Вещь)target).иконка, width, height) ?? base.RenderStaticPreview(assetPath, subAssets, width, height);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox($"Вещь «{target.name}». Название меняется переименованием файла", MessageType.None);
        EditorGUILayout.Space();

        Свойство("_иконка", "Иконка", "иконка");
        Свойство("_иконкаОтсутствия", "Иконка отсутствия", "иконкаОтсутствия");
        Свойство("_максимумВЯчейке", "Максимум в ячейке", "максимумВЯчейке");
        Свойство("_описание", "Описание", "описание");
        Свойство("_учитывается", "Учитывается", "учитывается");

        serializedObject.ApplyModifiedProperties();

        Sprite иконка = ((Вещь)target).иконка;
        if (иконка != null)
        {
            EditorGUILayout.Space();
            Rect место = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            GUI.DrawTextureWithTexCoords(место, иконка.texture, new Rect(иконка.textureRect.x / иконка.texture.width, иконка.textureRect.y / иконка.texture.height,
                иконка.textureRect.width / иконка.texture.width, иконка.textureRect.height / иконка.texture.height));
        }
    }

    void Свойство(string поле, string подпись, string член)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(поле), new GUIContent(подпись, Справка.Описание(typeof(Вещь), член)));
    }
}

/// <summary>
/// Поле вещи выглядит как выпадающий список всех вещей проекта
/// </summary>
[CustomPropertyDrawer(typeof(Вещь))]
internal class ВещьDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        Вещь новая = Выбрать(position, label, (Вещь)property.objectReferenceValue);
        if (EditorGUI.EndChangeCheck()) property.objectReferenceValue = новая;
        EditorGUI.EndProperty();
    }

    public static Вещь Выбрать(GUIContent подпись, Вещь текущая)
    {
        return Выбрать(EditorGUILayout.GetControlRect(), подпись, текущая);
    }

    public static Вещь Выбрать(Rect место, GUIContent подпись, Вещь текущая)
    {
        List<Вещь> вещи = Вещь.ВсеВещиПроекта();
        if (текущая != null && !вещи.Contains(текущая)) вещи.Insert(0, текущая);

        List<string> названия = new List<string> { "Нет" };
        названия.AddRange(вещи.Select(вещь => вещь.учитывается ? вещь.name : $"{вещь.name} (не учитывается)"));
        названия.Add("");
        названия.Add("Создать новую вещь...");

        int выбрано = текущая != null ? вещи.IndexOf(текущая) + 1 : 0;
        int новое = EditorGUI.Popup(место, подпись, выбрано, названия.Select(н => new GUIContent(н)).ToArray());

        if (новое == названия.Count - 1)
        {
            Вещь созданная = Создать();
            return созданная != null ? созданная : текущая;
        }

        if (новое == выбрано || новое == названия.Count - 2) return текущая;
        return новое == 0 ? null : вещи[новое - 1];
    }

    static Вещь Создать()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Вещи")) AssetDatabase.CreateFolder("Assets", "Вещи");

        Вещь вещь = ScriptableObject.CreateInstance<Вещь>();
        AssetDatabase.CreateAsset(вещь, AssetDatabase.GenerateUniqueAssetPath("Assets/Вещи/Новая вещь.asset"));
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(вещь);
        return вещь;
    }
}
#endif
