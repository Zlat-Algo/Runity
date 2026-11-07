#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// В какую сторону заполняется полоска
/// </summary>
public enum НаправлениеПрогрессбара { Вправо, Влево, Вверх, Вниз }

/// <summary>
/// Полоска в интерфейсе: здоровье, загрузка, опыт. Может сама следить за компонентом Здоровье
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Прогрессбар")]
public class Прогрессбар : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Image _полоска;
    /// <summary>
    /// Картинка, которая заполняется
    /// </summary>
    public Image полоска { get => _полоска; set { _полоска = value; if (!Application.isPlaying) ПоказатьСразу(); } }

    [SerializeField] Дробное _значение = 1;
    /// <summary>
    /// Текущее значение: от 0 до Максимума
    /// </summary>
    public Дробное значение
    {
        get => _значение;
        set
        {
            float прошлое = _значение;
            _значение = Математика.Ограничить(value, 0, максимум);

            if (!Application.isPlaying)
            {
                ПоказатьСразу();
                return;
            }
            if (прошлое == _значение) return;

            _приИзменении.Вызвать();
            if (_значение >= максимум) _приЗаполнении.Вызвать();
            if (_значение <= 0) _приОпустошении.Вызвать();
        }
    }

    [SerializeField] Дробное _максимум = 1;
    /// <summary>
    /// Значение, при котором полоска заполнена целиком
    /// </summary>
    public Дробное максимум
    {
        get => _максимум;
        set
        {
            _максимум = value;
            if (!Application.isPlaying) ПоказатьСразу();
        }
    }

    // В редакторе полоска меняется сразу, без плавности, чтобы было видно настоящее значение
    void ПоказатьСразу()
    {
        if (полоска == null) return;
        НастроитьПолоску();
        полоска.fillAmount = доля;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(полоска);
#endif
    }

    [SerializeField] Здоровье _источник;
    /// <summary>
    /// Если указать Здоровье, то полоска будет показывать его автоматически
    /// </summary>
    public Здоровье источник { get => _источник; set => _источник = value; }

    [SerializeField] НаправлениеПрогрессбара _направление;
    /// <summary>
    /// В какую сторону заполняется полоска
    /// </summary>
    public НаправлениеПрогрессбара направление { get => _направление; set { _направление = value; if (!Application.isPlaying) ПоказатьСразу(); } }

    [SerializeField] Дробное _плавность = 10;
    /// <summary>
    /// Как быстро полоска догоняет значение. 0 означает, что она меняется мгновенно
    /// </summary>
    public Дробное плавность { get => _плавность; set => _плавность = value; }

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при изменении значения
    /// </summary>
    public События приИзменении => _приИзменении;

    [SerializeField] События _приЗаполнении;
    /// <summary>
    /// Срабатывает, когда полоска заполнилась целиком
    /// </summary>
    public События приЗаполнении => _приЗаполнении;

    [SerializeField] События _приОпустошении;
    /// <summary>
    /// Срабатывает, когда полоска опустела
    /// </summary>
    public События приОпустошении => _приОпустошении;

    /// <summary>
    /// Заполненность от 0 до 1
    /// </summary>
    public Дробное доля => максимум > 0 ? значение / максимум : 0;

    /// <summary>
    /// Прибавляет к текущему значению. Подходит для опыта или загрузки
    /// </summary>
    public void Прибавить(Дробное сколько) => значение = (float)значение + сколько;

    /// <summary>
    /// Заполняет полоску до конца
    /// </summary>
    public void Заполнить() => значение = максимум;

    /// <summary>
    /// Опустошает полоску
    /// </summary>
    public void Опустошить() => значение = 0;

    void Start()
    {
        if (полоска == null) return;

        НастроитьПолоску();
        полоска.fillAmount = доля;
    }

    void Update()
    {
        if (источник != null)
        {
            максимум = источник.максимальное;
            значение = источник.текущее;
        }

        if (полоска == null) return;

        полоска.fillAmount = плавность > 0
            ? Mathf.MoveTowards(полоска.fillAmount, доля, плавность * Время.времяСПрошлогоКадраБезУчётаСкорости / 10)
            : доля;
    }

    internal void НастроитьПолоску()
    {
        полоска.type = Image.Type.Filled;

        bool горизонтально = направление == НаправлениеПрогрессбара.Вправо || направление == НаправлениеПрогрессбара.Влево;
        полоска.fillMethod = горизонтально ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
        полоска.fillOrigin = направление == НаправлениеПрогрессбара.Вправо || направление == НаправлениеПрогрессбара.Вверх ? 0 : 1;
    }

    void OnValidate()
    {
        if (полоска == null) return;

        НастроитьПолоску();
        полоска.fillAmount = доля;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Прогрессбар))]
internal class ПрогрессбарEditor : RunityEditor<Прогрессбар>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект показывает полоску, например здоровья или загрузки");

        Пробел();

        Поле("Полоска", x => x.полоска,
            (title, value) => ПолеОбъекта<Image>(title, value, true));

        if (компонент.полоска == null)
        {
            Текст("Укажите картинку, которая будет заполняться. Проще всего создать прогрессбар через меню Runity - Интерфейс", MessageType.Warning);
        }
        else if (компонент.полоска.sprite == null)
        {
            Текст("Полоске нужен спрайт, иначе она не будет заполняться", MessageType.Warning);
        }

        Пробел();

        Поле("Значение", x => x.значение,
            (title, value) => EditorGUILayout.Slider(title, value, 0, компонент.максимум));

        Поле("Максимум", x => x.максимум,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Показывать здоровье", x => x.источник,
            (title, value) => ПолеОбъекта<Здоровье>(title, value, true));

        Пробел();

        Поле("Направление", x => x.направление,
            (title, value) => (НаправлениеПрогрессбара)EditorGUILayout.EnumPopup(title, value));

        Поле("Плавность", x => x.плавность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ОтобразитьСобытие("При заполнении", "_приЗаполнении");
        ОтобразитьСобытие("При опустошении", "_приОпустошении");
        ЗакончитьСобытия();
    }
}
#endif
