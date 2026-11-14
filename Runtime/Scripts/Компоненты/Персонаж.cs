#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Персонаж для 3D-игр: ходит по WASD, прыгает, поднимается по ступенькам и склонам. Физическое тело ему не нужно
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("  Runity/ Передвижение/ Персонаж/Персонаж")]
public class Персонаж : Передвижение
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<CharacterController>();
        // Иначе при высокой частоте кадров короткие шаги вниз пропускаются, и персонаж то стоит на земле, то нет
        оригинал.minMoveDistance = 0;
    }
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public CharacterController оригинал => (CharacterController)оригинальныйКомпонент;

    [SerializeField] Флажок _управляетИгрок = да;
    /// <summary>
    /// Если "да", то персонаж ходит по WASD и прыгает на пробел. Если "нет", его двигают из кода или событий
    /// </summary>
    public Флажок управляетИгрок { get => _управляетИгрок; set => _управляетИгрок = value; }

    [SerializeField] ОтносительноЧегоДвижение _относительноЧего;
    /// <summary>
    /// Относительно чего считается направление "вперёд"
    /// </summary>
    public ОтносительноЧегоДвижение относительноЧего { get => _относительноЧего; set => _относительноЧего = value; }

    [SerializeField] Флажок _поворачиватьПоДвижению = нет;
    /// <summary>
    /// Если "да", то персонаж разворачивается туда, куда идёт. Подходит для вида от третьего лица
    /// </summary>
    public Флажок поворачиватьПоДвижению { get => _поворачиватьПоДвижению; set => _поворачиватьПоДвижению = value; }

    [SerializeField] Дробное _силаПрыжка = 5;
    /// <summary>
    /// С какой скоростью персонаж взлетает при прыжке. 0 означает, что он не прыгает
    /// </summary>
    public Дробное силаПрыжка { get => _силаПрыжка; set => _силаПрыжка = value; }

    [SerializeField] Дробное _гравитация = 20;
    /// <summary>
    /// Насколько быстро персонаж падает
    /// </summary>
    public Дробное гравитация { get => _гравитация; set => _гравитация = value; }

    /// <summary>
    /// Рост персонажа
    /// </summary>
    public Дробное высота { get => оригинал.height; set => оригинал.height = value; }

    /// <summary>
    /// Толщина персонажа
    /// </summary>
    public Дробное радиус { get => оригинал.radius; set => оригинал.radius = value; }

    /// <summary>
    /// Самая высокая ступенька, на которую персонаж зашагнёт сам
    /// </summary>
    public Дробное высотаСтупеньки { get => оригинал.stepOffset; set => оригинал.stepOffset = value; }

    /// <summary>
    /// Самый крутой склон в градусах, по которому персонаж может подняться
    /// </summary>
    public Дробное крутизнаСклона { get => оригинал.slopeLimit; set => оригинал.slopeLimit = value; }

    [SerializeField] События _приПрыжке;
    /// <summary>
    /// Срабатывает в момент прыжка
    /// </summary>
    public События приПрыжке => _приПрыжке;

    [SerializeField] События _приПриземлении;
    /// <summary>
    /// Срабатывает, когда персонаж был в воздухе и встал на землю
    /// </summary>
    public События приПриземлении => _приПриземлении;

    /// <summary>
    /// Даёт "да", пока персонаж стоит на земле
    /// </summary>
    public Флажок наЗемле => оригинал.isGrounded;

    /// <summary>
    /// Скорость падения или взлёта прямо сейчас
    /// </summary>
    public Дробное вертикальнаяСкорость => скоростьY;

    /// <summary>
    /// С какой скоростью персонаж двигался по земле в последнем кадре. Удобно для спидометра
    /// </summary>
    public Направление3D горизонтальнаяСкорость { get; private set; }

    float скоростьY;
    Vector3 движениеИзКода;
    bool былНаЗемле = true;
    readonly System.Collections.Generic.List<РасширениеПерсонажа> расширения = new System.Collections.Generic.List<РасширениеПерсонажа>();

    internal void Подключить(РасширениеПерсонажа расширение)
    {
        if (!расширения.Contains(расширение)) расширения.Add(расширение);
    }

    internal void Отключить(РасширениеПерсонажа расширение) => расширения.Remove(расширение);

    Vector3 Вперёд()
    {
        Vector3 результат = относительноЧего switch
        {
            ОтносительноЧегоДвижение.ОтносительноСебя => transform.forward,
            ОтносительноЧегоДвижение.ОтносительноКамеры when Camera.main != null => Camera.main.transform.forward,
            _ => Vector3.forward
        };

        результат.y = 0;
        return результат.normalized;
    }

    // Опора - то, на чём стоит персонаж. Если она движется (лифт, платформа), персонаж едет вместе с ней
    Transform опора, новаяОпора;
    Vector3 точкаНаОпоре;
    float уголОпоры;

    void OnControllerColliderHit(ControllerColliderHit касание)
    {
        if (касание.normal.y > 0.5f) новаяОпора = касание.collider.transform;
    }

    /// <summary>
    /// То, на чём сейчас стоит персонаж. Пусто, если он в воздухе
    /// </summary>
    public Transform наЧёмСтоит => опора;

    void ЕхатьНаОпоре()
    {
        if (опора == null) return;

        Vector3 сдвиг = опора.TransformPoint(точкаНаОпоре) - transform.position;
        float поворот = Mathf.DeltaAngle(уголОпоры, опора.eulerAngles.y);
        if (сдвиг.sqrMagnitude > 0.000001f && сдвиг.sqrMagnitude < 4) оригинал.Move(сдвиг);
        if (Mathf.Abs(поворот) > 0.001f && Mathf.Abs(поворот) < 45) transform.Rotate(0, поворот, 0);
    }

    void ЗапомнитьОпору()
    {
        опора = оригинал.isGrounded ? новаяОпора : null;
        новаяОпора = null;
        if (опора == null) return;
        точкаНаОпоре = опора.InverseTransformPoint(transform.position);
        уголОпоры = опора.eulerAngles.y;
    }

    void Update()
    {
        ЕхатьНаОпоре();

        Vector3 движение = движениеИзКода;
        движениеИзКода = Vector3.zero;

        // На паузе и во время разговора клавиши не управляют персонажем
        if (управляетИгрок && Time.timeScale > 0)
        {
            Vector3 вперёд = Вперёд();
            Vector3 вправо = Vector3.Cross(Vector3.up, вперёд);
            int поХ = (Управление.вправоУдерживается ? 1 : 0) + (Управление.влевоУдерживается ? -1 : 0);
            int поZ = (Управление.вперёдУдерживается ? 1 : 0) + (Управление.назадУдерживается ? -1 : 0);
            движение += (вправо * поХ + вперёд * поZ).normalized * скорость;

            bool прыжокНужен = Управление.пробелНажат;
            foreach (РасширениеПерсонажа расширение in расширения) прыжокНужен |= расширение.ХочетПрыгнуть();
            if (прыжокНужен) Прыгнуть();
        }

        Vector3 поЗемле = new Vector3(движение.x, 0, движение.z);
        foreach (РасширениеПерсонажа расширение in расширения) поЗемле = расширение.ИзменитьДвижение(поЗемле);
        движение = new Vector3(поЗемле.x, движение.y, поЗемле.z);
        горизонтальнаяСкорость = поЗемле;

        if (оригинал.isGrounded && скоростьY < 0)
        {
            // Небольшое прижатие к земле, иначе на спуске персонаж "подпрыгивает"
            скоростьY = -2;
        }
        скоростьY -= гравитация * Время.времяСПрошлогоКадра;

        Vector3 горизонтально = new Vector3(движение.x, 0, движение.z);
        ОбновитьДвижение(горизонтально.sqrMagnitude > 0.01f);

        if (поворачиватьПоДвижению && горизонтально.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(горизонтально), 720 * Время.времяСПрошлогоКадра);
        }

        движение.y += скоростьY;
        новаяОпора = null;
        оригинал.Move(движение * Время.времяСПрошлогоКадра);
        ЗапомнитьОпору();

        if (оригинал.isGrounded && !былНаЗемле)
        {
            _приПриземлении.Вызвать();
            foreach (РасширениеПерсонажа расширение in расширения) расширение.ПриПриземлении();
        }
        былНаЗемле = оригинал.isGrounded;
    }

    /// <summary>
    /// Прыгает, если персонаж стоит на земле
    /// </summary>
    public void Прыгнуть()
    {
        if (!оригинал.isGrounded || силаПрыжка <= 0) return;

        скоростьY = силаПрыжка;
        _приПрыжке.Вызвать();
        foreach (РасширениеПерсонажа расширение in расширения) расширение.ПриПрыжке();
    }

    /// <summary>
    /// Подбрасывает персонажа вверх, даже если он в воздухе. Подходит для батутов
    /// </summary>
    public void Подбросить(Дробное сила) => скоростьY = сила;

    /// <summary>
    /// Двигает персонажа в этом кадре в указанном направлении со скоростью персонажа. Вызывайте каждый кадр
    /// </summary>
    public void ИдтиВ(Направление3D направление)
    {
        Vector3 куда = направление;
        куда.y = 0;
        движениеИзКода += куда.normalized * скорость;
    }

    /// <summary>
    /// Мгновенно переносит персонажа в указанную точку
    /// </summary>
    public void Телепортировать(Направление3D точка)
    {
        оригинал.enabled = нет;
        transform.position = точка;
        оригинал.enabled = да;
        скоростьY = 0;
        опора = null;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Персонаж))]
internal class ПерсонажEditor : ПередвижениеEditor<Персонаж>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект - персонаж для 3D-игры. Ему не нужны физическое тело и коллайдер: они уже встроены");

        Пробел();

        if (компонент.управляетИгрок) ПредупреждениеПроУправление();

        Поле("Управляет игрок", x => x.управляетИгрок,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Вперёд считается", x => x.относительноЧего,
            (title, value) => (ОтносительноЧегоДвижение)EditorGUILayout.EnumPopup(title, value));

        Поле("Поворачивать по движению", x => x.поворачиватьПоДвижению,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Сила прыжка", x => x.силаПрыжка,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Гравитация", x => x.гравитация,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Синхрополе("Высота", x => x.высота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Радиус", x => x.радиус,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Высота ступеньки", x => x.высотаСтупеньки,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Крутизна склона", x => x.крутизнаСклона,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 90));

        Пробел();

        ОтобразитьСобытияДвижения();
        НачатьСобытия();
        ОтобразитьСобытие("При прыжке", "_приПрыжке");
        ОтобразитьСобытие("При приземлении", "_приПриземлении");
        ЗакончитьСобытия();
    }
}
#endif
