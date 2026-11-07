#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Объект подчиняется физике в 3D и что-то делает при столкновениях и пересечениях
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 3D/Физическое тело")]
public class ФизическоеТело : ФизическоеТелоЛюбое
{
    void Awake() => оригинальныйКомпонент = GetComponent<Rigidbody>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Rigidbody оригинал => (Rigidbody)оригинальныйКомпонент;

    protected override void OnEnable()
    {
        оригинал.detectCollisions = да;
    }
    protected override void OnDisable()
    {
        оригинал.detectCollisions = нет;
    }

    /// <summary>
    /// "Нет" означает, что объект двигается только из кода, а силы и столкновения на него не влияют
    /// </summary>
    public Флажок подчиняетсяФизике
    {
        get => !оригинал.isKinematic;
        set => оригинал.isKinematic = !value;
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
    /// Падает ли объект вниз
    /// </summary>
    public Флажок гравитация
    {
        get => оригинал.useGravity;
        set => оригинал.useGravity = value;
    }

    /// <summary>
    /// Текущая скорость объекта по всем трём осям
    /// </summary>
    public Направление3D движение
    {
        get => оригинал.linearVelocity;
        set => оригинал.linearVelocity = value;
    }

    /// <summary>
    /// Текущая скорость объекта по оси X
    /// </summary>
    public Дробное движениеX
    {
        get => оригинал.linearVelocity.x;
        set => оригинал.linearVelocity = new Vector3(value, оригинал.linearVelocity.y, оригинал.linearVelocity.z);
    }

    /// <summary>
    /// Текущая скорость объекта по оси Y
    /// </summary>
    public Дробное движениеY
    {
        get => оригинал.linearVelocity.y;
        set => оригинал.linearVelocity = new Vector3(оригинал.linearVelocity.x, value, оригинал.linearVelocity.z);
    }

    /// <summary>
    /// Текущая скорость объекта по оси Z
    /// </summary>
    public Дробное движениеZ
    {
        get => оригинал.linearVelocity.z;
        set => оригинал.linearVelocity = new Vector3(оригинал.linearVelocity.x, оригинал.linearVelocity.y, value);
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
    public Направление3D вращение
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
    /// Может ли физика поворачивать объект. Для персонажей обычно выключают, чтобы они не падали
    /// </summary>
    public Флажок позволитьВращаться
    {
        get => !оригинал.freezeRotation;
        set => оригинал.freezeRotation = !value;
    }

    /// <summary>
    /// Толкает объект в указанном направлении
    /// </summary>
    public void Толкнуть(Направление3D сила)
    {
        оригинал.AddForce(сила, ForceMode.Impulse);
    }

    Флажок ТегПодходит(GameObject сКемСтолкнулись)
    {
        ПоискОбъектов.ПеренестиТег(ref _тегНаКоторыйРеагируютСобытия, _теги);
        return _теги.Подходит(сКемСтолкнулись);
    }

    [SerializeField, HideInInspector] Строка _тегНаКоторыйРеагируютСобытия;

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// События сработают только с объектами этих тегов. Если теги не выбраны, то с любыми объектами
    /// </summary>
    public Теги теги => _теги;

    [SerializeField] События _приНачалеСтолкновения;
    /// <summary>
    /// Срабатывает в момент столкновения с твёрдым объектом
    /// </summary>
    public События приНачалеСтолкновения => _приНачалеСтолкновения;
    void OnCollisionEnter(Collision collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приНачалеСтолкновения.Вызвать();
    }

    [SerializeField] События _приПродолженииСтолкновения;
    /// <summary>
    /// Срабатывает каждый физический кадр, пока объекты касаются друг друга
    /// </summary>
    public События приПродолженииСтолкновения => _приПродолженииСтолкновения;
    void OnCollisionStay(Collision collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приПродолженииСтолкновения.Вызвать();
    }

    [SerializeField] События _приКонцеСтолкновения;
    /// <summary>
    /// Срабатывает, когда объекты перестали касаться
    /// </summary>
    public События приКонцеСтолкновения => _приКонцеСтолкновения;
    void OnCollisionExit(Collision collision)
    {
        if (ТегПодходит(collision.gameObject))
            _приКонцеСтолкновения.Вызвать();
    }

    [SerializeField] События _приНачалеПересечения;
    /// <summary>
    /// Срабатывает, когда объект вошёл в нетвёрдый коллайдер
    /// </summary>
    public События приНачалеПересечения => _приНачалеПересечения;
    void OnTriggerEnter(Collider other)
    {
        if (ТегПодходит(other.gameObject))
            _приНачалеПересечения.Вызвать();
    }

    [SerializeField] События _приПродолженииПересечения;
    /// <summary>
    /// Срабатывает каждый физический кадр, пока объект находится внутри нетвёрдого коллайдера
    /// </summary>
    public События приПродолженииПересечения => _приПродолженииПересечения;
    void OnTriggerStay(Collider other)
    {
        if (ТегПодходит(other.gameObject))
            _приПродолженииПересечения.Вызвать();
    }

    [SerializeField] События _приКонцеПересечения;
    /// <summary>
    /// Срабатывает, когда объект вышел из нетвёрдого коллайдера
    /// </summary>
    public События приКонцеПересечения => _приКонцеПересечения;
    void OnTriggerExit(Collider other)
    {
        if (ТегПодходит(other.gameObject))
            _приКонцеПересечения.Вызвать();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ФизическоеТело))]
internal class ФизическоеТелоEditor : RunityEditor<ФизическоеТело>
{
    bool eventFoldout = false;

    protected override void Отрисовать()
    {
        Текст("Этот объект подчиняется физике и что-то делает при пересечении и столкновении с другими объектами. Для столкновений ему нужен коллайдер");

        Пробел();

        Синхрополе("Подчиняется физике", x => x.подчиняетсяФизике,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.подчиняетсяФизике)
        {
            Синхрополе("Масса", x => x.масса,
                (title, value) => EditorGUILayout.FloatField(title, value));
            Синхрополе("Гравитация", x => x.гравитация,
                (title, value) => EditorGUILayout.Toggle(title, value));

            Пробел();

            Синхрополе("Движение", x => x.движение,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
            Синхрополе("Замедление при движении", x => x.замедлениеПриДвижении,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Пробел();

            Синхрополе("Позволить вращаться", x => x.позволитьВращаться,
                (title, value) => EditorGUILayout.Toggle(title, value));
            Синхрополе("Вращение", x => x.вращение,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
            Синхрополе("Замедление при вращении", x => x.замедлениеПриВращении,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        if (eventFoldout)
        {
            ОтобразитьТеги("Теги, на которые реагируют события", "_теги");

            Пробел();

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
