#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Считает что угодно: очки, монеты, жизни, патроны, убитых врагов. Пишет число в текст, умеет пределы, цель и сохранение между запусками
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Счётчик")]
public class Счётчик : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] int _значение;
    /// <summary>
    /// Сколько сейчас насчитано
    /// </summary>
    public Число значение { get => _значение; set => Установить(value); }

    [SerializeField] int _начальное;
    /// <summary>
    /// С какого числа начинается счёт и куда он возвращается при сбросе
    /// </summary>
    public Число начальное { get => _начальное; set => _начальное = value; }

    [SerializeField] Флажок _ограничить = нет;
    /// <summary>
    /// Если "да", число не выходит за минимум и максимум
    /// </summary>
    public Флажок ограничить { get => _ограничить; set => _ограничить = value; }

    [SerializeField] int _минимум;
    /// <summary>
    /// Меньше этого число не бывает, если включено «Ограничить»
    /// </summary>
    public Число минимум { get => _минимум; set => _минимум = value; }

    [SerializeField] int _максимум = 100;
    /// <summary>
    /// Больше этого число не бывает, если включено «Ограничить»
    /// </summary>
    public Число максимум { get => _максимум; set => _максимум = value; }

    [SerializeField] int _цель;
    /// <summary>
    /// Число, при достижении которого срабатывает «При достижении цели». 0 - цели нет
    /// </summary>
    public Число цель { get => _цель; set => _цель = value; }

    [SerializeField] Graphic _текст;
    /// <summary>
    /// Текст, в котором показывается число. Если пусто, берётся текст с этого же объекта
    /// </summary>
    public Graphic текст { get => _текст; set { _текст = value; ОбновитьТекст(); } }

    [SerializeField] Строка _текстДо = "";
    /// <summary>
    /// Что писать перед числом, например «Монеты: »
    /// </summary>
    public Строка текстДо { get => _текстДо; set { _текстДо = value; ОбновитьТекст(); } }

    [SerializeField] Строка _текстПосле = "";
    /// <summary>
    /// Что писать после числа, например « из 10»
    /// </summary>
    public Строка текстПосле { get => _текстПосле; set { _текстПосле = value; ОбновитьТекст(); } }

    [SerializeField] Строка _сохранятьПодИменем = "";
    /// <summary>
    /// Если указать имя, число запоминается между запусками игры. Пусто - не запоминать
    /// </summary>
    public Строка сохранятьПодИменем { get => _сохранятьПодИменем; set => _сохранятьПодИменем = value; }

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при любом изменении числа
    /// </summary>
    public События приИзменении => _приИзменении;

    [SerializeField] События _приДостиженииЦели;
    /// <summary>
    /// Срабатывает, когда число дошло до цели
    /// </summary>
    public События приДостиженииЦели => _приДостиженииЦели;

    [SerializeField] События _приМаксимуме;
    /// <summary>
    /// Срабатывает, когда число упёрлось в максимум
    /// </summary>
    public События приМаксимуме => _приМаксимуме;

    [SerializeField] События _приМинимуме;
    /// <summary>
    /// Срабатывает, когда число упёрлось в минимум, например закончились жизни
    /// </summary>
    public События приМинимуме => _приМинимуме;

    void Start()
    {
        if (текст == null) _текст = ЗаписьТекста.НайтиРядом(this);
        _значение = !string.IsNullOrEmpty(сохранятьПодИменем) && Сохранения.Есть(сохранятьПодИменем) ? (int)Сохранения.ЗагрузитьЧисло(сохранятьПодИменем, _начальное) : _начальное;
        ОбновитьТекст();
    }

    /// <summary>
    /// Прибавляет к числу. Чтобы отнять, передайте отрицательное
    /// </summary>
    public void Прибавить(Число сколько) => Установить(_значение + (int)сколько);

    /// <summary>
    /// Прибавляет единицу
    /// </summary>
    public void Прибавить() => Прибавить(1);

    /// <summary>
    /// Отнимает от числа
    /// </summary>
    public void Убавить(Число сколько) => Установить(_значение - (int)сколько);

    /// <summary>
    /// Отнимает единицу
    /// </summary>
    public void Убавить() => Убавить(1);

    /// <summary>
    /// Возвращает число к начальному
    /// </summary>
    public void Сбросить() => Установить(_начальное);

    /// <summary>
    /// Ставит числу новое значение
    /// </summary>
    public void Установить(Число новое)
    {
        int число = новое;
        if (ограничить) число = Mathf.Clamp(число, _минимум, Mathf.Max(_минимум, _максимум));
        if (число == _значение) return;

        int было = _значение;
        _значение = число;
        ОбновитьТекст();
        if (!string.IsNullOrEmpty(сохранятьПодИменем) && Application.isPlaying) PlayerPrefs.SetInt(сохранятьПодИменем, число);
        if (!Application.isPlaying) return;

        _приИзменении.Вызвать();
        if (_цель != 0 && ((было < _цель && число >= _цель) || (было > _цель && число <= _цель))) _приДостиженииЦели.Вызвать();
        if (ограничить && число == _максимум) _приМаксимуме.Вызвать();
        if (ограничить && число == _минимум) _приМинимуме.Вызвать();
    }

    void ОбновитьТекст()
    {
        if (текст != null) ЗаписьТекста.Записать(текст, (string)текстДо + _значение + (string)текстПосле);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Счётчик))]
internal class СчётчикEditor : RunityEditor<Счётчик>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект считает: очки, монеты, жизни. Меняйте число методами Прибавить, Убавить и Сбросить, например из событий");

        Пробел();

        Поле("Значение", x => x.значение, (title, value) => EditorGUILayout.IntField(title, value));
        Поле("Начальное", x => x.начальное, (title, value) => EditorGUILayout.IntField(title, value));
        Поле("Цель", x => x.цель, (title, value) => EditorGUILayout.IntField(title, value));
        Поле("Ограничить", x => x.ограничить, (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.ограничить)
        {
            Поле("Минимум", x => x.минимум, (title, value) => EditorGUILayout.IntField(title, value));
            Поле("Максимум", x => x.максимум, (title, value) => EditorGUILayout.IntField(title, value));
        }

        Пробел();

        Поле("Текст", x => x.текст, (title, value) => ПолеОбъекта<Graphic>(title, value, true));
        Поле("Текст до", x => x.текстДо, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Текст после", x => x.текстПосле, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Сохранять под именем", x => x.сохранятьПодИменем, (title, value) => EditorGUILayout.TextField(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        Кнопка("+1", "Прибавить единицу", () => { foreach (Счётчик счётчик in components) счётчик.Прибавить(); });
        Кнопка("-1", "Отнять единицу", () => { foreach (Счётчик счётчик in components) счётчик.Убавить(); });
        Кнопка("Сбросить", x => x.Сбросить());
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ОтобразитьСобытие("При достижении цели", "_приДостиженииЦели");
        if (компонент.ограничить)
        {
            ОтобразитьСобытие("При максимуме", "_приМаксимуме");
            ОтобразитьСобытие("При минимуме", "_приМинимуме");
        }
        ЗакончитьСобытия();
    }
}
#endif
