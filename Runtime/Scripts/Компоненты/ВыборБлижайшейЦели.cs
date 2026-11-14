using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Выбирает для Навигационного агента ближайший объект из указанных. Расстояние считается по пути в обход препятствий, а не напрямую
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Выбор ближайшей цели")]
public class ВыборБлижайшейЦели : РасширениеАгента
{
    [SerializeField] List<Transform> _кандидаты = new List<Transform>();
    /// <summary>
    /// Из каких объектов выбирать
    /// </summary>
    public List<Transform> кандидаты { get => _кандидаты; set => _кандидаты = value; }

    [SerializeField] Флажок _искатьПоТегам = нет;
    /// <summary>
    /// Если "да", то кандидатами считаются ещё и все объекты сцены с выбранными тегами. Подходит для врагов, которые появляются во время игры
    /// </summary>
    public Флажок искатьПоТегам { get => _искатьПоТегам; set => _искатьПоТегам = value; }

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// Объекты с какими тегами искать, если включён поиск по тегам
    /// </summary>
    public Теги теги { get => _теги; set => _теги = value; }

    [SerializeField] Дробное _интервал = 0.5f;
    /// <summary>
    /// Раз во сколько секунд заново выбирать ближайший объект
    /// </summary>
    public Дробное интервал { get => _интервал; set => _интервал = value; }

    [SerializeField] Дробное _максимальноеРасстояние = 0;
    /// <summary>
    /// Объекты дальше этого расстояния по пути не выбираются. 0 означает, что расстояние не ограничено
    /// </summary>
    public Дробное максимальноеРасстояние { get => _максимальноеРасстояние; set => _максимальноеРасстояние = value; }

    [SerializeField] События _приСменеЦели;
    /// <summary>
    /// Срабатывает, когда ближайшим стал другой объект
    /// </summary>
    public События приСменеЦели => _приСменеЦели;

    [SerializeField] События _приПотереЦели;
    /// <summary>
    /// Срабатывает, когда ни до одного объекта не дойти и цели не стало
    /// </summary>
    public События приПотереЦели => _приПотереЦели;

    /// <summary>
    /// Какой объект сейчас ближайший. Пусто, если подходящих нет
    /// </summary>
    public Transform ближайший { get; private set; }

    /// <summary>
    /// Длина пути до ближайшего объекта
    /// </summary>
    public Дробное расстояние { get; private set; }

    float доПроверки;
    NavMeshPath путь;

    void FixedUpdate()
    {
        доПроверки -= Время.времяМеждуFixedUpdate;
        if (доПроверки > 0) return;

        доПроверки = интервал;
        ВыбратьСейчас();
    }

    /// <summary>
    /// Сразу выбирает ближайший объект и отправляет к нему агента
    /// </summary>
    public void ВыбратьСейчас()
    {
        if (!агент.оригинал.isOnNavMesh) return;

        Transform лучший = null;
        float лучшееРасстояние = float.MaxValue;

        foreach (Transform кандидат in Кандидаты())
        {
            float длина = ДлинаПути(кандидат.position);
            if (длина < лучшееРасстояние && (максимальноеРасстояние <= 0 || длина <= максимальноеРасстояние))
            {
                лучший = кандидат;
                лучшееРасстояние = длина;
            }
        }

        расстояние = лучший != null ? лучшееРасстояние : 0;
        if (лучший == ближайший) return;

        ближайший = лучший;
        агент.цель = лучший;

        if (лучший != null) _приСменеЦели.Вызвать();
        else _приПотереЦели.Вызвать();
    }

    IEnumerable<Transform> Кандидаты()
    {
        HashSet<Transform> выданные = new HashSet<Transform>();

        foreach (Transform кандидат in кандидаты)
        {
            if (Подходит(кандидат) && выданные.Add(кандидат)) yield return кандидат;
        }

        if (!искатьПоТегам || теги.любые) yield break;

        foreach (string тег in теги.список)
        {
            if (string.IsNullOrEmpty(тег)) continue;
            foreach (GameObject найденный in GameObject.FindGameObjectsWithTag(тег))
            {
                if (Подходит(найденный.transform) && выданные.Add(найденный.transform)) yield return найденный.transform;
            }
        }
    }

    bool Подходит(Transform кандидат) => кандидат != null && кандидат.gameObject.activeInHierarchy && кандидат != transform && !кандидат.IsChildOf(transform);

    // Длина пути по навигационной карте. Если дойти нельзя, получится бесконечность
    float ДлинаПути(Vector3 куда)
    {
        if (!NavMesh.SamplePosition(куда, out NavMeshHit наКарте, 2f, агент.оригинал.areaMask)) return float.MaxValue;

        if (путь == null) путь = new NavMeshPath();
        if (!NavMesh.CalculatePath(transform.position, наКарте.position, агент.оригинал.areaMask, путь) || путь.status != NavMeshPathStatus.PathComplete)
            return float.MaxValue;

        float длина = 0;
        Vector3[] углы = путь.corners;
        for (int i = 1; i < углы.Length; i++) длина += Vector3.Distance(углы[i - 1], углы[i]);
        return длина;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ВыборБлижайшейЦели))]
internal class ВыборБлижайшейЦелиEditor : RunityEditor<ВыборБлижайшейЦели>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект сам выбирает для Навигационного агента ближайшую цель. Расстояние считается по пути с обходом препятствий");

        Пробел();

        serializedObject.Update();
        ОтобразитьСвойство("Кандидаты", "_кандидаты");
        serializedObject.ApplyModifiedProperties();

        Поле("Искать по тегам", x => x.искатьПоТегам,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.искатьПоТегам)
        {
            ОтобразитьТеги("Теги", "_теги");
        }

        Поле("Интервал", x => x.интервал,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Максимальное расстояние", x => x.максимальноеРасстояние,
            (title, value) => EditorGUILayout.FloatField(title, value));

        if (Application.isPlaying)
        {
            Пробел();
            Текст(компонент.ближайший != null ? $"Ближайший: {компонент.ближайший.name}, путь {(float)компонент.расстояние:0.0}" : "Подходящих целей нет");
        }

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При смене цели", "_приСменеЦели");
        ОтобразитьСобытие("При потере цели", "_приПотереЦели");
        ЗакончитьСобытия();
    }
}
#endif
