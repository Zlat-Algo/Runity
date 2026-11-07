using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;

/// <summary>
/// Список сцен игры. Порядок здесь определяет их номера в компоненте Сцены.
/// Изменения сразу попадают в настройки сборки проекта
/// </summary>
[CreateAssetMenu(fileName = "Список сцен", menuName = "Runity/Список сцен", order = -99)]
public class СписокСцен : ScriptableObject
{
    // Тип SceneAsset есть только в редакторе, поэтому здесь хранится общий тип Object
    [SerializeField] List<Object> _сцены = new List<Object>();

    /// <summary>
    /// Файлы сцен по порядку. Первая сцена запускается при старте игры
    /// </summary>
    public List<Object> сцены => _сцены;

#if UNITY_EDITOR
    const string путьПоУмолчанию = "Assets/Список сцен.asset";

    /// <summary>
    /// Находит список сцен проекта или создаёт его и показывает в инспекторе
    /// </summary>
    [MenuItem("Runity/Список сцен", false, 0)]
    public static void ОткрытьВРедакторе()
    {
        СписокСцен список = Найти();

        if (список == null)
        {
            список = CreateInstance<СписокСцен>();
            список.ВзятьИзНастроекСборки();
            AssetDatabase.CreateAsset(список, путьПоУмолчанию);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = список;
        EditorGUIUtility.PingObject(список);
    }

    static СписокСцен Найти()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:СписокСцен"))
        {
            СписокСцен список = AssetDatabase.LoadAssetAtPath<СписокСцен>(AssetDatabase.GUIDToAssetPath(guid));
            if (список != null) return список;
        }
        return null;
    }

    /// <summary>
    /// Записывает этот список в настройки сборки проекта
    /// </summary>
    public void Применить()
    {
        List<EditorBuildSettingsScene> результат = new List<EditorBuildSettingsScene>();

        foreach (Object сцена in _сцены)
        {
            if (сцена != null)
            {
                результат.Add(new EditorBuildSettingsScene(AssetDatabase.GetAssetPath(сцена), true));
            }
        }

        EditorBuildSettings.scenes = результат.ToArray();
    }

    /// <summary>
    /// Заполняет этот список сценами из настроек сборки проекта
    /// </summary>
    public void ВзятьИзНастроекСборки()
    {
        _сцены.Clear();

        foreach (EditorBuildSettingsScene сцена in EditorBuildSettings.scenes)
        {
            SceneAsset файл = AssetDatabase.LoadAssetAtPath<SceneAsset>(сцена.path);
            if (файл != null) _сцены.Add(файл);
        }
    }

    /// <summary>
    /// Добавляет в конец списка все сцены из папки Assets, которых в нём ещё нет
    /// </summary>
    public void ДобавитьВсеСценыПроекта()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" }))
        {
            SceneAsset файл = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (файл != null && !_сцены.Contains(файл)) _сцены.Add(файл);
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(СписокСцен))]
internal class СписокСценEditor : Editor
{
    ReorderableList список;

    СписокСцен сценыПроекта => (СписокСцен)target;

    void OnEnable()
    {
        список = new ReorderableList(serializedObject, serializedObject.FindProperty("_сцены"), true, true, true, true);

        список.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Сцены игры");

        список.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty элемент = список.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.ObjectField(rect, элемент, typeof(SceneAsset), new GUIContent($"Номер {index}"));
        };
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Перетащите сюда сцены из окна Project. Номер слева можно использовать в компоненте Сцены. Сцена с номером 0 запустится первой. Изменения применяются сразу", MessageType.None);
        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        список.DoLayoutList();
        bool изменён = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (изменён)
        {
            сценыПроекта.Применить();
        }

        if (GUILayout.Button(new GUIContent("Добавить все сцены проекта", Справка.Описание(typeof(СписокСцен), "ДобавитьВсеСценыПроекта"))))
        {
            Undo.RecordObject(сценыПроекта, "Добавить все сцены проекта");
            сценыПроекта.ДобавитьВсеСценыПроекта();
            EditorUtility.SetDirty(сценыПроекта);
            сценыПроекта.Применить();
        }


        EditorGUILayout.Space();
        Справка.ОтобразитьВИнспекторе(typeof(СписокСцен), new[] { "сцены", "ДобавитьВсеСценыПроекта", "Применить" });
    }
}
#endif
