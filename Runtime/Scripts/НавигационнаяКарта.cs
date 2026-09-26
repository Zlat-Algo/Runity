using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshSurface))]
[AddComponentMenu("  Runity/ Передвижение/Навигационная карта")]

public class НавигационнаяКарта : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<NavMeshSurface>();
    void OnValidate() => Awake();
    public NavMeshSurface оригинал => (NavMeshSurface)оригинальныйКомпонент;

    [SerializeField] int _типАгентов;
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

    public void Просчитать()
    {
        оригинал.BuildNavMesh();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(НавигационнаяКарта))]
public class НавигационнаяКартаEditor : RunityEditor<НавигационнаяКарта>
{
    public override void OnInspectorGUI()
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

        int newIndex = EditorGUILayout.Popup("Тип агентов", oldIndex, agentNames);
        компонент.оригинал.agentTypeID = NavMesh.GetSettingsByIndex(newIndex).agentTypeID;

        Пробел();

        Кнопка("Открыть настройки агентов", () => EditorApplication.ExecuteMenuItem("Window/AI/Navigation"));
        Кнопка("Просчитать", () => компонент.оригинал.BuildNavMesh());
    }
}
#endif
