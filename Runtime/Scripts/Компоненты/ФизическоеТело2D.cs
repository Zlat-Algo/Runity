#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public enum РежимФизическогоТела { Динамичное, ПодчиняетсяТолькоКоду, Неподвижное}


/// <summary>
/// Объект подчиняется физике в 2D и что-то делает при столкновениях и пересечениях
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 2D/Физическое тело 2D")]
public class ФизическоеТело2D : ФизическоеТелоЛюбое
{
    internal bool eventFoldout = false;
    void Awake() => оригинальныйКомпонент = GetComponent<Rigidbody2D>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Rigidbody2D оригинал => (Rigidbody2D)оригинальныйКомпонент;

    protected override void OnEnable()
    {
        оригинал.simulated = да;
    }
    protected override void OnDisable()
    {
        оригинал.simulated = нет;
    }

    /// <summary>
    /// Физический материал: определяет трение и упругость
    /// </summary>
    public PhysicsMaterial2D материал
    {
        get => оригинал.sharedMaterial;
        set => оригинал.sharedMaterial = value;
    }

    /// <summary>
    /// Динамичное падает и сталкивается, Подчиняется только коду двигается только из кода, Неподвижное стоит на месте
    /// </summary>
    public РежимФизическогоТела режим
    {
        get => оригинал.bodyType switch
        {
            RigidbodyType2D.Dynamic => РежимФизическогоТела.Динамичное,
            RigidbodyType2D.Kinematic => РежимФизическогоТела.ПодчиняетсяТолькоКоду,
            RigidbodyType2D.Static => РежимФизическогоТела.Неподвижное,
            _ => РежимФизическогоТела.Неподвижное
        };
        set => оригинал.bodyType = value switch
        {
            РежимФизическогоТела.Динамичное => RigidbodyType2D.Dynamic,
            РежимФизическогоТела.ПодчиняетсяТолькоКоду => RigidbodyType2D.Kinematic,
            РежимФизическогоТела.Неподвижное => RigidbodyType2D.Static,
            _ => RigidbodyType2D.Static
        };
    }

    /// <summary>
    /// Масса объекта: тяжёлые объекты сложнее сдвинуть
    /// </summary>
    public Дробное масса
    {
        get => оригинал.mass;
        set => оригинал.mass = value;
    }

    /// <summary>
    /// Насколько сильно объект падает вниз. 0 означает, что не падает совсем
    /// </summary>
    public Дробное гравитация
    {
        get => оригинал.gravityScale;
        set => оригинал.gravityScale = value;
    }

    /// <summary>
    /// Текущая скорость объекта по обеим осям
    /// </summary>
    public Направление2D движение
    {
        get => оригинал.linearVelocity;
        set => оригинал.linearVelocity = value;
    }

    /// <summary>
    /// Текущая скорость объекта по оси X
    /// </summary>
    public Дробное движениеX
    {
        get => оригинал.linearVelocityX;
        set => оригинал.linearVelocityX = value;
    }

    /// <summary>
    /// Текущая скорость объекта по оси Y
    /// </summary>
    public Дробное движениеY
    {
        get => оригинал.linearVelocityY;
        set => оригинал.linearVelocityY = value;
    }

    /// <summary>
    /// Насколько быстро объект сам замедляется при движении
    /// </summary>
    public Дробное замедлениеПриДвижении
    {
        get => оригинал.linearDamping;
        set => оригинал.linearDamping = value;
    }

    /// <summary>
    /// Текущая скорость вращения объекта
    /// </summary>
    public Дробное вращение
    {
        get => оригинал.angularVelocity;
        set => оригинал.angularVelocity = value;
    }

    /// <summary>
    /// Насколько быстро объект сам замедляется при вращении
    /// </summary>
    public Дробное замедлениеПриВращении
    {
        get => оригинал.angularDamping;
        set => оригинал.angularDamping = value;
    }

    /// <summary>
    /// Может ли физика поворачивать объект. Для персонажей обычно выключают
    /// </summary>
    public Флажок позволитьВращаться
    {
        get => !оригинал.freezeRotation;
        set => оригинал.freezeRotation = !value;
    }

    Флажок ТегПодходит(GameObject сКемСтолкнулись)
    {
        ПоискОбъектов.ПеренестиТег(ref _тегНаКоторыйРеагируютСобытия, _теги);
        return _теги.Подходит(сКемСтолкнулись);
    }

    /// <summary>
    /// События сработают только с объектами этого тега. Untagged означает любые объекты
    /// </summary>
    [SerializeField, HideInInspector] Строка _тегНаКоторыйРеагируютСобытия;

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// События сработают только с объектами этих тегов. Если теги не выбраны, то с любыми объектами
    /// </summary>
    public Теги теги => _теги;

    /// <summary>
    /// Срабатывает в момент столкновения с твёрдым объектом
    /// </summary>
    [SerializeField] События _приНачалеСтолкновения;
    public События приНачалеСтолкновения => _приНачалеСтолкновения;
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приНачалеСтолкновения.Вызвать();
    }

    /// <summary>
    /// Срабатывает каждый физический кадр, пока объекты касаются друг друга
    /// </summary>
    [SerializeField] События _приПродолженииСтолкновения;
    public События приПродолженииСтолкновения => _приПродолженииСтолкновения;
    void OnCollisionStay2D(Collision2D collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приПродолженииСтолкновения.Вызвать();
    }

    /// <summary>
    /// Срабатывает, когда объекты перестали касаться
    /// </summary>
    [SerializeField] События _приКонцеСтолкновения;
    public События приКонцеСтолкновения => _приКонцеСтолкновения;
    void OnCollisionExit2D(Collision2D collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приКонцеСтолкновения.Вызвать();
    }

    /// <summary>
    /// Срабатывает, когда объект вошёл в нетвёрдый коллайдер
    /// </summary>
    [SerializeField] События _приНачалеПересечения;
    public События приНачалеПересечения => _приНачалеПересечения;
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (ТегПодходит(collider.gameObject))
            _приНачалеПересечения.Вызвать();
    }

    /// <summary>
    /// Срабатывает каждый физический кадр, пока объект находится внутри нетвёрдого коллайдера
    /// </summary>
    [SerializeField] События _приПродолженииПересечения;
    public События приПродолженииПересечения => _приПродолженииПересечения;
    void OnTriggerStay2D(Collider2D collider)
    {
        if (ТегПодходит(collider.gameObject))
            _приПродолженииПересечения.Вызвать();
    }

    /// <summary>
    /// Срабатывает, когда объект вышел из нетвёрдого коллайдера
    /// </summary>
    [SerializeField] События _приКонцеПересечения;
    public События приКонцеПересечения => _приКонцеПересечения;
    void OnTriggerExit2D(Collider2D collider)
    {
        if (ТегПодходит(collider.gameObject))
            _приКонцеПересечения.Вызвать();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ФизическоеТело2D))]
internal class ФизическоеТело2DEditor : RunityEditor<ФизическоеТело2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект подчиняется физике и что-то делает при пересечении и столкновении с другими объектами");

        Пробел();

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial2D>(title, value));
        EnumField(
            "Режим",
            component => component.режим,
            (component, value) =>
                component.режим = value
        );

        Пробел();

        if (компонент.режим == РежимФизическогоТела.Динамичное)
        {
            Синхрополе("Масса", x => x.масса,
                (title, value) => EditorGUILayout.FloatField(title, value));
            Синхрополе("Гравитация", x => x.гравитация,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Пробел();

            Синхрополе("Движение", x => x.движение,
                (title, value) => EditorGUILayout.Vector2Field(title, value));
            Синхрополе("Замедление при движении", x => x.замедлениеПриДвижении,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Пробел();

            Синхрополе("Позволить вращаться", x => x.позволитьВращаться,
                (title, value) => EditorGUILayout.Toggle(title, value));
            Синхрополе("Вращение", x => x.вращение,
                (title, value) => EditorGUILayout.FloatField(title, value));
            Синхрополе("Замедление при вращении", x => x.замедлениеПриВращении,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        компонент.eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(компонент.eventFoldout, "События");
        if (компонент.eventFoldout)
        {
            ОтобразитьТеги("Теги, на которые реагируют события", "_теги");

            НачатьСобытия();

            ОтобразитьСобытие("Когда столкнулся", "_приНачалеСтолкновения");
            ОтобразитьСобытие("Пока продолжает контакт", "_приПродолженииСтолкновения");
            ОтобразитьСобытие("Когда контакт прекратился", "_приКонцеСтолкновения");
            Пробел();
            ОтобразитьСобытие("Когда пересёкся", "_приНачалеПересечения");
            ОтобразитьСобытие("Пока продолжает пересекаться", "_приПродолженииПересечения");
            ОтобразитьСобытие("Когда закончил пересекаться", "_приКонцеПересечения");

            ЗакончитьСобытия();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

    }
}
#endif