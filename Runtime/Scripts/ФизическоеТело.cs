using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 3D/Физическое тело")]
public class ФизическоеТело : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Rigidbody>();
    void OnValidate() => Awake();
    public Rigidbody оригинал => (Rigidbody)оригинальныйКомпонент;

    /*public bool работает
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
    }*/

    public bool подчиняетсяФизике
    {
        get => !оригинал.isKinematic;
        set => оригинал.isKinematic = !value;
    }

    public float масса
    {
        get => оригинал.mass;
        set => оригинал.mass = value;
    }

    /*public float гравитация
    {
        get => оригинал.gravityScale;
        set => оригинал.gravityScale = value;
    }*/

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

    public Vector3 вращение
    {
        get => оригинал.angularVelocity;
        set => оригинал.angularVelocity = value;
    }

    public float замедлениеПриВращении
    {
        get => оригинал.angularDamping;
        set => оригинал.angularDamping = value;
    }

    public string тегНаКоторыйРеагируютСобытия { get; set; }

    [SerializeField] UnityEvent _приНачалеСтолкновения;
    public UnityEvent приНачалеСтолкновения => _приНачалеСтолкновения;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приНачалеСтолкновения.Invoke();
        }
    }

    [SerializeField] UnityEvent _приПродолженииСтолкновения;
    public UnityEvent приПродолженииСтолкновения => _приПродолженииСтолкновения;
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приПродолженииСтолкновения.Invoke();
        }
    }

    [SerializeField] UnityEvent _приКонцеСтолкновения;
    public UnityEvent приКонцеСтолкновения => _приКонцеСтолкновения;
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приКонцеСтолкновения.Invoke();
        }
    }

    [SerializeField] UnityEvent _приНачалеПересечения;
    public UnityEvent приНачалеПересечения => _приНачалеПересечения;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приНачалеПересечения.Invoke();
        }
    }

    [SerializeField] UnityEvent _приПродолженииПересечения;
    public UnityEvent приПродолженииПересечения => _приПродолженииПересечения;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приПродолженииПересечения.Invoke();
        }
    }

    [SerializeField] UnityEvent _приКонцеПересечения;
    public UnityEvent приКонцеПересечения => _приКонцеПересечения;
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(тегНаКоторыйРеагируютСобытия))
        {
            _приКонцеПересечения.Invoke();
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ФизическоеТело))]
public class ФизическоеТелоEditor : RunityEditor<ФизическоеТело>
{
    bool eventFoldout = false;

    public override void OnInspectorGUI()
    {
        Текст("Этот объект подчиняется физике и что-то делает при пересечении и столкновении с другими объектами");

        Пробел();

        /*Синхрополе("Работает", x => x.работает,
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
        }*/

        Синхрополе("Подчиняется физике", x => x.подчиняетсяФизике,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        if (eventFoldout)
        {
            Поле("Тег, на который реагируют события", x => x.тегНаКоторыйРеагируютСобытия,
                (title, value) => EditorGUILayout.TagField(title, value));
            //компонент.тегиНаКоторыеРеагируютСобытия = EditorGUILayout.TagField("Тег, на который реагируют события", component.тегНаКоторыйРеагируютСобытия);
            
            Пробел();

            НачатьСобытия();

            Событие("При начале столкновения", "_приНачалеСтолкновения");
            Событие("При продолжении столкновения", "_приПродолженииСтолкновения");
            Событие("При конце столкновения", "_приКонцеСтолкновения");
            Пробел();
            Событие("При начале пересечения", "_приНачалеПересечения");
            Событие("При продолжении пересечения", "_приПродолженииПересечения");
            Событие("При конце пересечения", "_приКонцеПересечения");

            ЗакончитьСобытия();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

    }
}
#endif