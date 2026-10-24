using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public enum РежимФизическогоТела { Динамичное, ПодчиняетсяТолькоКоду, Неподвижное}


[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 2D/Физическое тело 2D")]
public class ФизическоеТело2D : ФизическоеТелоЛюбое
{
    void Awake() => оригинальныйКомпонент = GetComponent<Rigidbody2D>();
    void OnValidate() => Awake();
    public Rigidbody2D оригинал => (Rigidbody2D)оригинальныйКомпонент;

    protected override void OnEnable()
    {
        оригинал.simulated = да;
    }
    protected override void OnDisable()
    {
        оригинал.simulated = нет;
    }

    public PhysicsMaterial2D материал
    {
        get => оригинал.sharedMaterial;
        set => оригинал.sharedMaterial = value;
    }

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

    public Дробное масса
    {
        get => оригинал.mass;
        set => оригинал.mass = value;
    }

    public Дробное гравитация
    {
        get => оригинал.gravityScale;
        set => оригинал.gravityScale = value;
    }

    public Направление2D движение
    {
        get => оригинал.linearVelocity;
        set => оригинал.linearVelocity = value;
    }

    public Дробное движениеX
    {
        get => оригинал.linearVelocityX;
        set => оригинал.linearVelocityX = value;
    }

    public Дробное движениеY
    {
        get => оригинал.linearVelocityY;
        set => оригинал.linearVelocityY = value;
    }

    public Дробное замедлениеПриДвижении
    {
        get => оригинал.linearDamping;
        set => оригинал.linearDamping = value;
    }

    public Дробное вращение
    {
        get => оригинал.angularVelocity;
        set => оригинал.angularVelocity = value;
    }

    public Дробное замедлениеПриВращении
    {
        get => оригинал.angularDamping;
        set => оригинал.angularDamping = value;
    }

    public Флажок позволитьВращаться
    {
        get => !оригинал.freezeRotation;
        set => оригинал.freezeRotation = !value;
    }

    //public string тегНаКоторыйРеагируютСобытия;

    [SerializeField] События _приНачалеСтолкновения;
    public События приНачалеСтолкновения => _приНачалеСтолкновения;
    void OnCollisionEnter2D() => _приНачалеСтолкновения.Вызвать();

    [SerializeField] События _приПродолженииСтолкновения;
    public События приПродолженииСтолкновения => _приПродолженииСтолкновения;
    void OnCollisionStay2D() => _приПродолженииСтолкновения.Вызвать();

    [SerializeField] События _приКонцеСтолкновения;
    public События приКонцеСтолкновения => _приКонцеСтолкновения;
    void OnCollisionExit2D() => _приКонцеСтолкновения.Вызвать();

    [SerializeField] События _приНачалеПересечения;
    public События приНачалеПересечения => _приНачалеПересечения;
    void OnTriggerEnter2D() => _приНачалеПересечения.Вызвать();

    [SerializeField] События _приПродолженииПересечения;
    public События приПродолженииПересечения => _приПродолженииПересечения;
    void OnTriggerStay2D() => _приПродолженииПересечения.Вызвать();

    [SerializeField] События _приКонцеПересечения;
    public События приКонцеПересечения => _приКонцеПересечения;
    void OnTriggerExit2D() => _приКонцеПересечения.Вызвать();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ФизическоеТело2D))]
internal class ФизическоеТело2DEditor : RunityEditor<ФизическоеТело2D>
{
    bool eventFoldout = false;

    public override void OnInspectorGUI()
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

        eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        if (eventFoldout)
        {
            //component.тегНаКоторыйРеагируютСобытия = EditorGUILayout.TagField("Тег, на который реагируют события", component.тегНаКоторыйРеагируютСобытия);

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