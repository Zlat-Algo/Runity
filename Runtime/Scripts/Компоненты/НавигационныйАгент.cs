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

    [SerializeField] Transform _цель;
    /// <summary>
    /// К какому объекту идёт агент. Если убрать цель, агент остановится
    /// </summary>
    public Transform цель
    {
        get => _цель;
        set
        {
            if (_цель == value) return;
            _цель = value;
            остановленУЦели = false;
            нужноОбновитьПуть = true;
            if (value == null && Application.isPlaying) Остановиться();
        }
    }

    [SerializeField] Флажок _продолжатьПреследование = да;
    /// <summary>
    /// Если "да", то агент идёт за целью и после того, как дошёл до неё. Если "нет", то он остановится, как только дойдёт
    /// </summary>
    public Флажок продолжатьПреследование { get => _продолжатьПреследование; set => _продолжатьПреследование = value; }

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
    /// Срабатывает один раз, когда агент дошёл до цели или до точки
    /// </summary>
    public События приДостиженииЦели => _приДостиженииЦели;

    /// <summary>
    /// Даёт "да", пока агент идёт к цели или к точке
    /// </summary>
    public Флажок идёт => оригинал.isOnNavMesh && (оригинал.pathPending || оригинал.hasPath && оригинал.remainingDistance > оригинал.stoppingDistance + 0.05f);

    /// <summary>
    /// Даёт "да", если агент стоит у цели или у точки, куда шёл
    /// </summary>
    public Флажок дошёл => цельДостигнута;

    /// <summary>
    /// Куда агент идёт сейчас
    /// </summary>
    public Направление3D точкаНазначения => оригинал.destination;

    /// <summary>
    /// Сколько ещё идти по пути до цели
    /// </summary>
    public Дробное оставшеесяРасстояние => оригинал.isOnNavMesh && оригинал.hasPath ? оригинал.remainingDistance : 0;

    Vector3 предыдущаяПозиция;
    float предыдущаяСкорость;
    bool цельДостигнута;
    bool остановленУЦели;
    bool нужноОбновитьПуть = true;

    /// <summary>
    /// Отправляет агента в точку. Цель при этом убирается
    /// </summary>
    public void ИдтиВТочку(Направление3D точка)
    {
        _цель = null;
        остановленУЦели = false;
        цельДостигнута = false;
        if (оригинал.isOnNavMesh) оригинал.SetDestination(точка);
    }

    /// <summary>
    /// Отправляет агента к объекту
    /// </summary>
    public void ИдтиК(Transform новаяЦель) => цель = новаяЦель;

    /// <summary>
    /// Останавливает агента на месте. Цель остаётся, и агент снова пойдёт к ней после вызова «Продолжить преследование»
    /// </summary>
    public void Остановиться()
    {
        остановленУЦели = true;
        if (оригинал.isOnNavMesh) оригинал.ResetPath();
    }

    /// <summary>
    /// Снова отправляет агента к цели, если он остановился
    /// </summary>
    public void ПродолжитьПреследование()
    {
        остановленУЦели = false;
        нужноОбновитьПуть = true;
    }

    private void FixedUpdate()
    {
        if (оригинал.isOnNavMesh && !оригинал.pathPending && оригинал.hasPath)
        {
            ОбновитьДвижение(оригинал.velocity.sqrMagnitude > 0.01f);

            // Агент Unity останавливается не ровно в точке, поэтому нужен небольшой запас
            float зона = оригинал.stoppingDistance + 0.05f;
            if (оригинал.remainingDistance <= зона && !цельДостигнута)
            {
                цельДостигнута = true;
                if (цель != null && !продолжатьПреследование) Остановиться();
                _приДостиженииЦели.Вызвать();
            }
            else if (оригинал.remainingDistance > зона)
            {
                цельДостигнута = false;
            }
        }
        else if (!оригинал.pathPending)
        {
            ОбновитьДвижение(false);
        }

        if (цель != null && !остановленУЦели && (нужноОбновитьПуть || цель.position != предыдущаяПозиция) && оригинал.isOnNavMesh)
        {
            нужноОбновитьПуть = false;
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

        Поле("Продолжать преследование", x => x.продолжатьПреследование,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Зона достижения цели", x => x.зонаДостиженияЦели,
            (title, value) => EditorGUILayout.FloatField(title, value));

        if (компонент.GetComponent<РасширениеАгента>() != null)
        {
            Текст("Цель выбирают расширения агента ниже", MessageType.Info);
        }

        Пробел();

        ОтобразитьСобытияДвижения();
        НачатьСобытия();
        ОтобразитьСобытие("При достижении цели", "_приДостиженииЦели");
        ЗакончитьСобытия();
    }
}
#endif
