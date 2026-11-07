#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Сам находит путь к цели и обходит препятствия. Ему нужна Навигационная карта
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Навигационный агент")]
public class НавигационныйАгент : Передвижение
{
    void Awake() => оригинальныйКомпонент = GetComponent<NavMeshAgent>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public NavMeshAgent оригинал => (NavMeshAgent)оригинальныйКомпонент;

    /// <summary>
    /// К какому объекту идёт агент
    /// </summary>
    [SerializeField] Transform _цель;
    public Transform цель { get => _цель; set => _цель = value; }

    /// <summary>
    /// Как быстро агент набирает скорость
    /// </summary>
    public float ускорение
    {
        get => оригинал.acceleration;
        set => оригинал.acceleration = value;
    }

    /// <summary>
    /// Как быстро агент поворачивается, в градусах в секунду
    /// </summary>
    public float скоростьПоворота
    {
        get => оригинал.angularSpeed;
        set => оригинал.angularSpeed = value;
    }

    /// <summary>
    /// На каком расстоянии от цели агент останавливается
    /// </summary>
    public float зонаДостиженияЦели
    {
        get => оригинал.stoppingDistance;
        set => оригинал.stoppingDistance = value;
    }

    [SerializeField] События _приДостиженииЦели;
    /// <summary>
    /// Срабатывает один раз, когда агент дошёл до цели
    /// </summary>
    public События приДостиженииЦели => _приДостиженииЦели;

    Vector3 предыдущаяПозиция;
    float предыдущаяСкорость;
    bool цельДостигнута;

    private void FixedUpdate()
    {
        //if (вРедакторе) return;

        if (оригинал.isOnNavMesh && !оригинал.pathPending && оригинал.hasPath)
        {
            ОбновитьДвижение(оригинал.velocity.sqrMagnitude > 0.01f);

            if (оригинал.remainingDistance <= оригинал.stoppingDistance && !цельДостигнута)
            {
                цельДостигнута = true;
                _приДостиженииЦели.Вызвать();
            }
            else if (оригинал.remainingDistance > оригинал.stoppingDistance)
            {
                цельДостигнута = false;
            }
        }

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
internal class НавигационныйАгентEditor : ПередвижениеEditor<НавигационныйАгент>
{
    protected override void Отрисовать()
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

        int newIndex = EditorGUILayout.Popup(new GUIContent("Тип агента", "Размер и способности агента. Типы настраиваются в окне Navigation"), oldIndex, System.Array.ConvertAll(agentNames, имя => new GUIContent(имя)));
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
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        Синхрополе("Зона достижения цели", x => x.зонаДостиженияЦели,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        ОтобразитьСобытияДвижения();
        НачатьСобытия();
        ОтобразитьСобытие("При достижении цели", "_приДостиженииЦели");
        ЗакончитьСобытия();
    }
}
#endif