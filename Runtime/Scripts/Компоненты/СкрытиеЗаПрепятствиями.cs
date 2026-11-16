using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Не рисует объект, когда его целиком загораживают другие объекты: смотришь на стену - и то, что за ней, видеокарта не рисует.
/// Работает и для неподвижных, и для движущихся объектов. Загораживают только объекты с коллайдерами (стены, дома, скалы).
/// Проверка лёгкая: за кадр проверяется лишь часть объектов, и только те, что попадают в поле зрения камеры
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал/ Оптимизация/Скрытие за препятствиями")]
public class СкрытиеЗаПрепятствиями : RunityComponent
{
    protected override void OnEnable()
    {
        все.Add(this);
        Найти();
    }

    protected override void OnDisable()
    {
        все.Remove(this);
        Показать(true);
    }

    [SerializeField] Флажок _вместеСДетьми = да;
    /// <summary>
    /// Если "да", прячутся и все части внутри объекта, например вся модель дома
    /// </summary>
    public Флажок вместеСДетьми { get => _вместеСДетьми; set { _вместеСДетьми = value; Найти(); } }

    [SerializeField] Дробное _запас = 0.5f;
    /// <summary>
    /// На сколько метров вокруг объекта проверка заглядывает дальше его краёв. Чем больше, тем раньше объект появляется,
    /// когда выглядывает из-за угла, но тем реже он прячется
    /// </summary>
    public Дробное запас { get => _запас; set => _запас = Mathf.Max(0, value); }

    [SerializeField] Флажок _оставлятьТени = да;
    /// <summary>
    /// Если "да", спрятанный объект продолжает отбрасывать тень: тень бывает видна, даже когда сам объект за стеной
    /// </summary>
    public Флажок оставлятьТени { get => _оставлятьТени; set => _оставлятьТени = value; }

    [SerializeField] LayerMask _загораживающиеСлои = Physics.DefaultRaycastLayers;
    /// <summary>
    /// Объекты каких слоёв загораживают обзор. Например, можно не считать стеклянные стены, положив их в отдельный слой
    /// </summary>
    public LayerMask загораживающиеСлои { get => _загораживающиеСлои; set => _загораживающиеСлои = value; }

    [SerializeField] Camera _камера;
    /// <summary>
    /// Для какой камеры проверять видимость. Если пусто - главная камера
    /// </summary>
    public Camera камера { get => _камера; set => _камера = value; }

    [SerializeField] События _приСкрытии;
    /// <summary>
    /// Срабатывает, когда объект спрятался за препятствием
    /// </summary>
    public События приСкрытии => _приСкрытии;

    [SerializeField] События _приПоявлении;
    /// <summary>
    /// Срабатывает, когда объект снова стал виден
    /// </summary>
    public События приПоявлении => _приПоявлении;

    /// <summary>
    /// Даёт "да", пока объект спрятан за препятствием
    /// </summary>
    public Флажок скрыт { get; private set; }

    /// <summary>
    /// Сколько объектов всего проверяется за один кадр. Больше - объекты быстрее появляются, меньше - игра быстрее
    /// </summary>
    public static int проверокЗаКадр = 40;

    static readonly List<СкрытиеЗаПрепятствиями> все = new List<СкрытиеЗаПрепятствиями>();
    static int следующий;
    static readonly Plane[] плоскости = new Plane[6];
    static readonly Vector3[] точки = new Vector3[9];

    readonly List<Renderer> отрисовщики = new List<Renderer>();
    int загороженПодряд;

    void Найти()
    {
        Показать(true);
        отрисовщики.Clear();
        if (вместеСДетьми) GetComponentsInChildren(true, отрисовщики);
        else GetComponents(отрисовщики);
        отрисовщики.RemoveAll(о => о is ParticleSystemRenderer || о is TrailRenderer || о is LineRenderer);
    }

    void Показать(bool видно)
    {
        if (скрыт == !видно) return;
        скрыт = !видно;
        foreach (Renderer отрисовщик in отрисовщики) ВидимостьОтрисовщиков.Заслонён(отрисовщик, !видно, оставлятьТени);

        if (!Application.isPlaying) return;
        if (видно) _приПоявлении.Вызвать();
        else _приСкрытии.Вызвать();
    }

    // Границы всех частей объекта
    bool Границы(out Bounds границы)
    {
        границы = default;
        bool есть = false;
        foreach (Renderer отрисовщик in отрисовщики)
        {
            if (отрисовщик == null || !отрисовщик.enabled || !отрисовщик.gameObject.activeInHierarchy) continue;
            if (!есть) { границы = отрисовщик.bounds; есть = true; }
            else границы.Encapsulate(отрисовщик.bounds);
        }
        return есть;
    }

    void Проверить(Camera глаз)
    {
        if (!Границы(out Bounds границы)) return;
        границы.Expand(запас * 2);
        Vector3 откуда = глаз.transform.position;

        // Вне поля зрения Unity и так не рисует объект. Держим его видимым, чтобы он не мелькнул пустотой, когда в него повернутся
        if (границы.Contains(откуда) || !GeometryUtility.TestPlanesAABB(плоскости, границы))
        {
            загороженПодряд = 0;
            Показать(true);
            return;
        }

        Vector3 центр = границы.center, край = границы.extents;
        точки[0] = центр;
        for (int i = 0; i < 8; i++)
            точки[i + 1] = центр + Vector3.Scale(край, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));

        foreach (Vector3 точка in точки)
        {
            if (!Physics.Linecast(откуда, точка, out RaycastHit попадание, загораживающиеСлои, QueryTriggerInteraction.Ignore) || попадание.transform.IsChildOf(transform))
            {
                // Хватит и одного луча, дошедшего до объекта: он виден
                загороженПодряд = 0;
                Показать(true);
                return;
            }
        }

        // Прячем только после двух проверок подряд, чтобы объект не мерцал на краю препятствия
        загороженПодряд++;
        if (загороженПодряд >= 2) Показать(false);
    }

    void LateUpdate()
    {
        // Проверку делает только первый объект из списка, сразу за всех
        if (все.Count == 0 || все[0] != this) return;

        Camera главная = Camera.main;
        Camera плоскостиДля = null;
        int сколько = Mathf.Min(проверокЗаКадр, все.Count);
        for (int i = 0; i < сколько; i++)
        {
            следующий = (следующий + 1) % все.Count;
            СкрытиеЗаПрепятствиями объект = все[следующий];
            Camera глаз = объект.камера != null ? объект.камера : главная;
            if (глаз == null) continue;
            if (глаз != плоскостиДля)
            {
                GeometryUtility.CalculateFrustumPlanes(глаз, плоскости);
                плоскостиДля = глаз;
            }
            объект.Проверить(глаз);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Границы(out Bounds границы)) return;
        границы.Expand(запас * 2);
        Gizmos.color = скрыт ? new Color(1, 0.3f, 0.3f, 0.6f) : new Color(0.3f, 1, 0.5f, 0.6f);
        Gizmos.DrawWireCube(границы.center, границы.size);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СкрытиеЗаПрепятствиями))]
internal class СкрытиеЗаПрепятствиямиEditor : RunityEditor<СкрытиеЗаПрепятствиями>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект не рисуется, когда его целиком загораживают стены и другие объекты с коллайдерами. Рамка в сцене показывает проверяемую область: зелёная - виден, красная - спрятан");

        Пробел();

        Поле("Вместе с детьми", x => x.вместеСДетьми, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Запас", x => x.запас, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Оставлять тени", x => x.оставлятьТени, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Загораживающие слои", x => x.загораживающиеСлои, (title, value) => ПолеСлоёв(title, value));
        Поле("Камера", x => x.камера, (title, value) => ПолеОбъекта<Camera>(title, value, true));

        if (Application.isPlaying) Текст(компонент.скрыт ? "Сейчас спрятан" : "Сейчас виден");

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При скрытии", "_приСкрытии");
        ОтобразитьСобытие("При появлении", "_приПоявлении");
        ЗакончитьСобытия();
    }
}
#endif
