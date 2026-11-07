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
    void Awake() => оригинальныйКомпонент = GetComponent<NavMeshSurface>();
    void OnValidate() => Awake();
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

    /// <summary>
    /// Заново просчитывает пути. Нужно делать после изменения уровня
    /// </summary>
    public void Просчитать()
    {
        оригинал.BuildNavMesh();
    }
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

        Пробел();

        Кнопка("Открыть настройки агентов", () => EditorApplication.ExecuteMenuItem("Window/AI/Navigation"));
        Кнопка("Просчитать", x => x.Просчитать());
    }
}
#endif
