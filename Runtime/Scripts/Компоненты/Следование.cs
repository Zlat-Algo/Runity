#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Плавно следует за другим объектом
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Следование")]
public class Следование : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Transform _цель;
    /// <summary>
    /// За кем следует объект
    /// </summary>
    public Transform цель { get => _цель; set => _цель = value; }

    [SerializeField] Флажок _запомнитьСмещение = да;
    /// <summary>
    /// Если "да", то на старте объект запоминает, где он стоит относительно цели, и держится так же
    /// </summary>
    public Флажок запомнитьСмещение { get => _запомнитьСмещение; set => _запомнитьСмещение = value; }

    [SerializeField] Направление3D _смещение = new Направление3D(0, 0, -10);
    /// <summary>
    /// Где держится объект относительно цели. Для 2D-камеры обычно (0, 0, -10)
    /// </summary>
    public Направление3D смещение { get => _смещение; set => _смещение = value; }

    [SerializeField] Дробное _плавность = 5;
    /// <summary>
    /// Как быстро объект догоняет цель. 0 означает, что он следует мгновенно
    /// </summary>
    public Дробное плавность { get => _плавность; set => _плавность = value; }

    [SerializeField] Флажок _поX = да;
    /// <summary>
    /// Следовать ли за целью по оси X
    /// </summary>
    public Флажок поX { get => _поX; set => _поX = value; }

    [SerializeField] Флажок _поY = да;
    /// <summary>
    /// Следовать ли за целью по оси Y
    /// </summary>
    public Флажок поY { get => _поY; set => _поY = value; }

    [SerializeField] Флажок _поZ = да;
    /// <summary>
    /// Следовать ли за целью по оси Z
    /// </summary>
    public Флажок поZ { get => _поZ; set => _поZ = value; }

    [SerializeField] Флажок _ограничитьОбласть = нет;
    /// <summary>
    /// Если "да", то объект не выезжает за границы. Так камера не покажет то, что за краем уровня
    /// </summary>
    public Флажок ограничитьОбласть { get => _ограничитьОбласть; set => _ограничитьОбласть = value; }

    [SerializeField] Направление3D _минимум = new Направление3D(-10, -10, -100);
    /// <summary>
    /// Самые маленькие координаты, до которых может доехать объект
    /// </summary>
    public Направление3D минимум { get => _минимум; set => _минимум = value; }

    [SerializeField] Направление3D _максимум = new Направление3D(10, 10, 100);
    /// <summary>
    /// Самые большие координаты, до которых может доехать объект
    /// </summary>
    public Направление3D максимум { get => _максимум; set => _максимум = value; }

    void Start()
    {
        if (запомнитьСмещение && цель != null)
        {
            смещение = transform.position - цель.position;
        }
    }

    // LateUpdate, чтобы цель уже успела сдвинуться в этом кадре и камера не дрожала
    void LateUpdate()
    {
        if (цель == null) return;

        Vector3 куда = Куда();
        transform.position = плавность > 0
            ? Vector3.Lerp(transform.position, куда, 1 - Mathf.Exp(-плавность * Время.времяСПрошлогоКадра))
            : куда;
    }

    Vector3 Куда()
    {
        Vector3 сейчас = transform.position;
        Vector3 куда = цель.position + (Vector3)смещение;

        if (!поX) куда.x = сейчас.x;
        if (!поY) куда.y = сейчас.y;
        if (!поZ) куда.z = сейчас.z;

        if (ограничитьОбласть)
        {
            куда = Vector3.Max(минимум, Vector3.Min(максимум, куда));
        }

        return куда;
    }

    /// <summary>
    /// Мгновенно переносит объект к цели. Удобно после телепортации игрока
    /// </summary>
    public void ПереместитьсяКЦели()
    {
        if (цель != null) transform.position = Куда();
    }

    void OnDrawGizmosSelected()
    {
        if (!ограничитьОбласть) return;

        Gizmos.color = Color.green;
        Vector3 мин = минимум;
        Vector3 макс = максимум;
        Gizmos.DrawWireCube((мин + макс) / 2, макс - мин);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Следование))]
internal class СледованиеEditor : RunityEditor<Следование>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект плавно следует за целью");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        Поле("Запомнить смещение", x => x.запомнитьСмещение,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (!компонент.запомнитьСмещение)
        {
            Поле("Смещение", x => x.смещение,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
        }

        Поле("Плавность", x => x.плавность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        EditorGUILayout.PrefixLabel(new GUIContent("Следовать по осям", "По каким осям объект повторяет движение цели"));
        Поле("X", x => x.поX, (title, value) => EditorGUILayout.ToggleLeft(title, value, GUILayout.Width(40)));
        Поле("Y", x => x.поY, (title, value) => EditorGUILayout.ToggleLeft(title, value, GUILayout.Width(40)));
        Поле("Z", x => x.поZ, (title, value) => EditorGUILayout.ToggleLeft(title, value, GUILayout.Width(40)));
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        Поле("Ограничить область", x => x.ограничитьОбласть,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.ограничитьОбласть)
        {
            Поле("Минимум", x => x.минимум,
                (title, value) => EditorGUILayout.Vector3Field(title, value));

            Поле("Максимум", x => x.максимум,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
        }
    }
}
#endif
