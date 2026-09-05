using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public enum РежимФизическогоТела { Динамичное, ПодчиняетсяТолькоКоду, Неподвижное}


[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 2D/Физическое тело 2D")]
public class ФизическоеТело2D : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Rigidbody2D>();
    void OnValidate() => Awake();
    public Rigidbody2D оригинал => (Rigidbody2D)оригинальныйКомпонент;

    public bool работает
    {
        get => оригинал.simulated;
        set => оригинал.simulated = value;
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

    public float масса
    {
        get => оригинал.mass;
        set => оригинал.mass = value;
    }

    public float гравитация
    {
        get => оригинал.gravityScale;
        set => оригинал.gravityScale = value;
    }

    public Vector2 движение
    {
        get => оригинал.linearVelocity;
        set => оригинал.linearVelocity = value;
    }

    public float замедлениеПриДвижении
    {
        get => оригинал.linearDamping;
        set => оригинал.linearDamping = value;
    }

    public float вращение
    {
        get => оригинал.angularVelocity;
        set => оригинал.angularVelocity = value;
    }

    public float замедлениеПриВращении
    {
        get => оригинал.angularDamping;
        set => оригинал.angularDamping = value;
    }

    //public string тегНаКоторыйРеагируютСобытия;

    [SerializeField] UnityEvent _приНачалеСтолкновения;
    public UnityEvent приНачалеСтолкновения => _приНачалеСтолкновения;

    [SerializeField] UnityEvent _приПродолженииСтолкновения;
    public UnityEvent приПродолженииСтолкновения => _приПродолженииСтолкновения;

    [SerializeField] UnityEvent _приКонцеСтолкновения;
    public UnityEvent приКонцеСтолкновения => _приКонцеСтолкновения;

    [SerializeField] UnityEvent _приНачалеПересечения;
    public UnityEvent приНачалеПересечения => _приНачалеПересечения;

    [SerializeField] UnityEvent _приПродолженииПересечения;
    public UnityEvent приПродолженииПересечения => _приПродолженииПересечения;

    [SerializeField] UnityEvent _приКонцеПересечения;
    public UnityEvent приКонцеПересечения => _приКонцеПересечения;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ФизическоеТело2D))]
public class ФизическоеТело2DEditor : RunityEditor<ФизическоеТело2D>
{
    bool eventFoldout = false;

    public override void OnInspectorGUI()
    {
        Текст("Этот объект подчиняется физике и что-то делает при пересечении и столкновении с другими объектами");

        Пробел();

        Синхрополе("Работает", x => x.работает,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<PhysicsMaterial2D>(title, value));
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

            Событие("Когда столкнулся", "_приНачалеСтолкновения");
            Событие("Пока продолжает контакт", "_приПродолженииСтолкновения");
            Событие("Когда контакт прекратился", "_приКонцеСтолкновения");
            Пробел();
            Событие("Когда пересёкся", "_приНачалеПересечения");
            Событие("Пока продолжает пересекаться", "_приПродолженииПересечения");
            Событие("Когда закончил пересекаться", "_приКонцеПересечения");

            ЗакончитьСобытия();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

    }
}
#endif