using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[AddComponentMenu("  Runity/ Передвижение/Навигационный агент")]
public class НавигационныйАгент : Передвижение
{
    void Awake() => оригинальныйКомпонент = GetComponent<NavMeshAgent>();
    void OnValidate() => Awake();
    public NavMeshAgent оригинал => (NavMeshAgent)оригинальныйКомпонент;

    [SerializeField] Transform _цель;
    public Transform цель { get => _цель; set => _цель = value; }

    public float ускорение
    {
        get => оригинал.acceleration;
        set => оригинал.acceleration = value;
    }

    public float скоростьПоворота
    {
        get => оригинал.angularSpeed;
        set => оригинал.angularSpeed = value;
    }

    public float зонаДостиженияЦели
    {
        get => оригинал.stoppingDistance;
        set => оригинал.stoppingDistance = value;
    }

    Vector3 предыдущаяПозиция;
    float предыдущаяСкорость;

    private void FixedUpdate()
    {
        if (цель != null && цель.position != предыдущаяПозиция)
        {
            предыдущаяПозиция = цель.position;
            оригинал.SetDestination(цель.position);
        }
        if (скорость != предыдущаяСкорость)
        {
            предыдущаяСкорость = скорость;
            оригинал.speed = скорость;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(НавигационныйАгент))]
public class НавигационныйАгентEditor : RunityEditor<НавигационныйАгент>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект умеет находить путь к цели, обходя все препятствия. Ему обязательно нужна Навигационная карта");

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

        int newIndex = EditorGUILayout.Popup("Тип агента", oldIndex, agentNames);
        компонент.оригинал.agentTypeID = NavMesh.GetSettingsByIndex(newIndex).agentTypeID;

        Пробел();

        Синхрополе("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Ускорение", x => x.ускорение,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Скорость поворота", x => x.скоростьПоворота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => Объект<Transform>(title, value, true));

        Синхрополе("Зона достижения цели", x => x.зонаДостиженияЦели,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif