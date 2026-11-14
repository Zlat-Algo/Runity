using System.Collections.Generic;
using Unity.AI.Navigation;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Запекает карту, по которой ходят Навигационные агенты
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshSurface))]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Навигационная карта")]
public class НавигационнаяКарта : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<NavMeshSurface>();
        if (Application.isPlaying && просчитыватьПриЗапуске) Просчитать();
    }
    void OnValidate() => оригинальныйКомпонент = GetComponent<NavMeshSurface>();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public NavMeshSurface оригинал => (NavMeshSurface)оригинальныйКомпонент;

    [SerializeField] int _типАгентов;
    /// <summary>
    /// Номер типа агентов, для которых просчитывается карта
    /// </summary>
    public int типАгентов
    {
        get
        {
            int ID = оригинал.agentTypeID;
            int count = NavMesh.GetSettingsCount();
            for(int i = 0; i < count; i++)
            {
                if (NavMesh.GetSettingsByIndex(i).agentTypeID == ID)
                {
                    return i;
                }
            }
            return -1;
        }
        set => оригинал.agentTypeID = NavMesh.GetSettingsByIndex(value).agentTypeID;
    }

    [SerializeField] Флажок _просчитыватьПриЗапуске = нет;
    /// <summary>
    /// Если "да", то пути просчитываются сами при запуске игры. Удобно, если уровень собирается во время игры
    /// </summary>
    public Флажок просчитыватьПриЗапуске { get => _просчитыватьПриЗапуске; set => _просчитыватьПриЗапуске = value; }

    /// <summary>
    /// Заново просчитывает пути. Нужно делать после изменения уровня. В редакторе карта сохраняется в папку рядом со сценой
    /// </summary>
    public void Просчитать()
    {
        оригинал.BuildNavMesh();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            СохранитьВПроект();
            return;
        }
#endif

        // Агенты, появившиеся раньше карты, сами на неё не встают
        foreach (NavMeshAgent агент in FindObjectsByType<NavMeshAgent>())
        {
            if (агент.isActiveAndEnabled && !агент.isOnNavMesh) агент.Warp(агент.transform.position);
        }
    }

#if UNITY_EDITOR
    // Без сохранения просчитанная карта живёт только до закрытия сцены
    void СохранитьВПроект()
    {
        string путьСцены = gameObject.scene.path;
        if (string.IsNullOrEmpty(путьСцены))
        {
            Консоль.НапечататьПредупреждение("Сохраните сцену, чтобы навигационная карта сохранилась вместе с ней", this);
            return;
        }

        string папка = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(путьСцены), System.IO.Path.GetFileNameWithoutExtension(путьСцены)).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(папка)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(путьСцены).Replace('\\', '/'), System.IO.Path.GetFileNameWithoutExtension(путьСцены));
        string путь = $"{папка}/Навигационная карта {gameObject.name}.asset";

        AssetDatabase.DeleteAsset(путь);
        AssetDatabase.CreateAsset(оригинал.navMeshData, путь);
        EditorUtility.SetDirty(оригинал);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(НавигационнаяКарта))]
internal class НавигационнаяКартаEditor : RunityEditor<НавигационнаяКарта>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект просчитывает пути, по которым Навигационные агенты идут к своей цели");

        Пробел();

        int oldIndex = 0;
        string[] agentNames = new string[NavMesh.GetSettingsCount()];
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            NavMeshBuildSettings agentData = NavMesh.GetSettingsByIndex(i);
            if (agentData.agentTypeID == компонент.оригинал.agentTypeID)
            {
                oldIndex = i;
            }
            agentNames[i] = (NavMesh.GetSettingsNameFromID(agentData.agentTypeID));
        }

        int newIndex = EditorGUILayout.Popup(Подпись("Тип агентов", nameof(НавигационнаяКарта.типАгентов)), oldIndex, System.Array.ConvertAll(agentNames, имя => new GUIContent(имя)));
        компонент.оригинал.agentTypeID = NavMesh.GetSettingsByIndex(newIndex).agentTypeID;

        Поле("Просчитывать при запуске", x => x.просчитыватьПриЗапуске,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Кнопка("Открыть настройки агентов", () => EditorApplication.ExecuteMenuItem("Window/AI/Navigation"));
        Кнопка("Просчитать", x => x.Просчитать());
    }
}
#endif
