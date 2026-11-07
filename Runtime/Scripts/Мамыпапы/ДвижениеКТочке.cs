#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Заготовка для объектов, которые сами ходят по точкам: Брождение и Патрулирование.
/// Двигает объект через Навигационного агента, физическое тело или, если их нет, напрямую
/// </summary>
[DisallowMultipleComponent]
public abstract class ДвижениеКТочке : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() => Остановиться();

    [SerializeField] float _скорость = 2;
    /// <summary>
    /// С какой скоростью объект идёт к точке
    /// </summary>
    public float скорость { get => _скорость; set => _скорость = value; }

    [SerializeField] Флажок _режим2D = да;
    /// <summary>
    /// "Да" для 2D-игр. Объект будет двигаться по осям X и Y, а не X и Z
    /// </summary>
    public Флажок режим2D { get => _режим2D; set => _режим2D = value; }

    [SerializeField] Флажок _толькоПоГоризонтали = нет;
    /// <summary>
    /// В 2D объект двигается только влево и вправо. Включите для платформеров, чтобы не мешать гравитации
    /// </summary>
    public Флажок толькоПоГоризонтали { get => _толькоПоГоризонтали; set => _толькоПоГоризонтали = value; }

    [SerializeField] Флажок _отзеркаливатьСпрайт = да;
    /// <summary>
    /// Если "да", то Спрайтер будет отзеркаливаться при движении влево
    /// </summary>
    public Флажок отзеркаливатьСпрайт { get => _отзеркаливатьСпрайт; set => _отзеркаливатьСпрайт = value; }

    [SerializeField] float _зонаДостиженияТочки = .1f;
    /// <summary>
    /// На каком расстоянии от точки считается, что объект до неё дошёл
    /// </summary>
    public float зонаДостиженияТочки { get => _зонаДостиженияТочки; set => _зонаДостиженияТочки = value; }

    [SerializeField] internal События _приНачалеПути;
    /// <summary>
    /// Срабатывает, когда объект пошёл к новой точке
    /// </summary>
    public События приНачалеПути => _приНачалеПути;

    [SerializeField] internal События _приДостиженииТочки;
    /// <summary>
    /// Срабатывает, когда объект дошёл до точки
    /// </summary>
    public События приДостиженииТочки => _приДостиженииТочки;

    /// <summary>
    /// Даёт "да", пока объект идёт к точке
    /// </summary>
    public Флажок идёт { get; private set; }

    /// <summary>
    /// Пока "да", объект стоит на месте. Удобно включать, когда он кого-то заметил
    /// </summary>
    public Флажок пауза
    {
        get => _пауза;
        set
        {
            _пауза = value;
            if (value) ОстановитьТело();
        }
    }
    Флажок _пауза;

    /// <summary>
    /// Ставит объект на паузу
    /// </summary>
    public void Приостановить() => пауза = да;

    /// <summary>
    /// Снимает объект с паузы
    /// </summary>
    public void Продолжить() => пауза = нет;

    protected float времяВПути { get; private set; }

    Vector3 точка;
    ФизическоеТело2D тело2D;
    ФизическоеТело тело3D;
    НавигационныйАгент агент;
    Спрайтер спрайтер;

    protected virtual void Awake()
    {
        TryGetComponent(out тело2D);
        TryGetComponent(out тело3D);
        TryGetComponent(out агент);
        TryGetComponent(out спрайтер);
    }

    protected void ИдтиК(Vector3 новаяТочка)
    {
        точка = новаяТочка;
        идёт = да;
        времяВПути = 0;

        if (агент != null)
        {
            агент.скорость = скорость;
            агент.оригинал.SetDestination(точка);
        }
        _приНачалеПути.Вызвать();
    }

    protected void Остановиться()
    {
        идёт = нет;
        ОстановитьТело();
    }

    void ОстановитьТело()
    {
        if (агент != null && агент.оригинал.isOnNavMesh)
        {
            агент.оригинал.ResetPath();
        }
        else if (тело2D != null)
        {
            if (толькоПоГоризонтали) тело2D.движениеX = 0;
            else тело2D.движение = Vector2.zero;
        }
        else if (тело3D != null)
        {
            тело3D.движениеX = 0;
            тело3D.движениеZ = 0;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (пауза || !идёт) return;

        времяВПути += Время.времяМеждуFixedUpdate;

        if (агент != null)
        {
            if (агент.оригинал.isOnNavMesh && !агент.оригинал.pathPending && агент.оригинал.remainingDistance <= зонаДостиженияТочки)
            {
                Дошёл();
            }
            return;
        }

        Vector3 доТочки = точка - transform.position;
        if (режим2D)
        {
            доТочки.z = 0;
            if (толькоПоГоризонтали) доТочки.y = 0;
        }
        else if (тело3D != null)
        {
            доТочки.y = 0;
        }

        if (доТочки.magnitude <= зонаДостиженияТочки)
        {
            Дошёл();
            return;
        }

        Vector3 движение = доТочки.normalized * скорость;

        if (тело2D != null)
        {
            if (толькоПоГоризонтали) тело2D.движениеX = движение.x;
            else тело2D.движение = (Vector2)движение;
        }
        else if (тело3D != null)
        {
            тело3D.движениеX = движение.x;
            тело3D.движениеZ = движение.z;
        }
        else
        {
            transform.position += Vector3.ClampMagnitude(движение * Время.времяМеждуFixedUpdate, доТочки.magnitude);
        }

        if (отзеркаливатьСпрайт && спрайтер != null && Mathf.Abs(движение.x) > 0.01f)
        {
            спрайтер.отзеркалитьГоризонтально = движение.x < 0;
        }
    }

    void Дошёл()
    {
        Остановиться();
        _приДостиженииТочки.Вызвать();
        ПриДостиженииТочки();
    }

    /// <summary>
    /// Здесь наследник решает, что делать дальше
    /// </summary>
    protected abstract void ПриДостиженииТочки();
}

#if UNITY_EDITOR
internal abstract class ДвижениеКТочкеEditor<T> : RunityEditor<T> where T : ДвижениеКТочке
{
    protected void ОбщиеНастройки()
    {
        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Зона достижения точки", x => x.зонаДостиженияТочки,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.режим2D)
        {
            Поле("Только по горизонтали", x => x.толькоПоГоризонтали,
                (title, value) => EditorGUILayout.Toggle(title, value));

            Поле("Отзеркаливать спрайт", x => x.отзеркаливатьСпрайт,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }
    }

    protected void ОбщиеСобытия()
    {
        НачатьСобытия();
        ОтобразитьСобытие("При начале пути", "_приНачалеПути");
        ОтобразитьСобытие("При достижении точки", "_приДостиженииТочки");
        ЗакончитьСобытия();
    }
}
#endif
