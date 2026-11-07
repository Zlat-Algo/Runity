#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Относительно чего считается направление "вперёд"
/// </summary>
public enum ОтносительноЧегоДвижение { ОтносительноСебя, ОтносительноКамеры, ОтносительноМира }

/// <summary>
/// Игрок управляет этим объектом клавишами WASD в 3D
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТело))]
[AddComponentMenu("  Runity/ Передвижение/ Управление игроком/Передвижение 3D")]
public class Передвижение3D : Передвижение
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТело>();
    void OnValidate() => Awake();
    /// <summary>
    /// Физическое тело, которое двигает этот компонент
    /// </summary>
    public ФизическоеТело оригинал => (ФизическоеТело)оригинальныйКомпонент;

    [SerializeField] ОтносительноЧегоДвижение _относительноЧего;
    /// <summary>
    /// Относительно чего считается направление "вперёд"
    /// </summary>
    public ОтносительноЧегоДвижение относительноЧего { get => _относительноЧего; set => _относительноЧего = value; }

    [SerializeField] Флажок _поворачиватьПоДвижению = нет;
    /// <summary>
    /// Если "да", то объект разворачивается туда, куда идёт. Подходит для вида от третьего лица
    /// </summary>
    public Флажок поворачиватьПоДвижению { get => _поворачиватьПоДвижению; set => _поворачиватьПоДвижению = value; }

    [SerializeField] Дробное _скоростьПоворота = 720;
    /// <summary>
    /// Сколько градусов в секунду объект может повернуться
    /// </summary>
    public Дробное скоростьПоворота { get => _скоростьПоворота; set => _скоростьПоворота = value; }

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

    void Update()
    {
        Vector3 вперёд = Вперёд();
        Vector3 вправо = Vector3.Cross(Vector3.up, вперёд);

        int поХ = (Управление.вправоУдерживается ? 1 : 0) + (Управление.влевоУдерживается ? -1 : 0);
        int поZ = (Управление.вперёдУдерживается ? 1 : 0) + (Управление.назадУдерживается ? -1 : 0);

        Vector3 движение = (вправо * поХ + вперёд * поZ).normalized * скорость;

        оригинал.движениеX = движение.x;
        оригинал.движениеZ = движение.z;

        ОбновитьДвижение(движение != Vector3.zero);

        if (поворачиватьПоДвижению && движение != Vector3.zero)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(движение), скоростьПоворота * Время.времяСПрошлогоКадра);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Передвижение3D))]
internal class Передвижение3DEditor : ПередвижениеEditor<Передвижение3D>
{
    protected override void Отрисовать()
    {
        Текст("Этим объектом может управлять игрок, чтобы перемещаться в 3D. Не забудьте добавить коллайдер");

        Пробел();

        ПредупреждениеПроУправление();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Вперёд считается", x => x.относительноЧего,
            (title, value) => (ОтносительноЧегоДвижение)EditorGUILayout.EnumPopup(title, value));

        Пробел();

        Поле("Поворачивать по движению", x => x.поворачиватьПоДвижению,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.поворачиватьПоДвижению)
        {
            Поле("Скорость поворота", x => x.скоростьПоворота,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        ОтобразитьСобытияДвижения();
    }
}
#endif
