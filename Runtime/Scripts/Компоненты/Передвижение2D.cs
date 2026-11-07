#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Игрок управляет этим объектом клавишами WASD в 2D
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТело2D))]
[AddComponentMenu("  Runity/ Передвижение/ Управление игроком/Передвижение 2D")]
public class Передвижение2D : Передвижение
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТело2D>();
    void OnValidate() => Awake();
    /// <summary>
    /// Физическое тело, которое двигает этот компонент
    /// </summary>
    public ФизическоеТело2D оригинал => (ФизическоеТело2D)оригинальныйКомпонент;

    /// <summary>
    /// Может ли объект двигаться вверх
    /// </summary>
    [SerializeField] Флажок _вверх = да;
    public Флажок вверх { get => _вверх; set => _вверх = value; }

    /// <summary>
    /// Может ли объект двигаться вниз
    /// </summary>
    [SerializeField] Флажок _вниз = да;
    public Флажок вниз { get => _вниз; set => _вниз = value; }

    /// <summary>
    /// Может ли объект двигаться влево
    /// </summary>
    [SerializeField] Флажок _влево = да;
    public Флажок влево { get => _влево; set => _влево = value; }

    /// <summary>
    /// Может ли объект двигаться вправо
    /// </summary>
    [SerializeField] Флажок _вправо = да;
    public Флажок вправо { get => _вправо; set => _вправо = value; }

    [SerializeField] Флажок _отзеркаливатьСпрайт = нет;
    /// <summary>
    /// Если "да", то при движении влево Спрайтер этого объекта отзеркалится
    /// </summary>
    public Флажок отзеркаливатьСпрайт { get => _отзеркаливатьСпрайт; set => _отзеркаливатьСпрайт = value; }

    void Update()
    {
        Направление2D движение = скорость * new Направление2D(
            (Управление.вправоУдерживается && вправо ? 1 : 0) +
            (Управление.влевоУдерживается && влево ? -1 : 0),
            (Управление.вперёдУдерживается && вверх ? 1 : 0) +
            (Управление.назадУдерживается && вниз ? -1 : 0)).единичное;

        if (влево || вправо)
        {
            оригинал.движениеX = движение.x;
        }
        if (вверх || вниз)
        {
            оригинал.движениеY = движение.y;
        }

        ОбновитьДвижение(Или(движение.x != 0, движение.y != 0));

        if (отзеркаливатьСпрайт && движение.x != 0 && TryGetComponent(out Спрайтер спрайтер))
        {
            спрайтер.отзеркалитьГоризонтально = движение.x < 0;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Передвижение2D))]
internal class Передвижение2DСверхуEditor : ПередвижениеEditor<Передвижение2D>
{
    protected override void Отрисовать()
    {
        Текст("Этим объектом может управлять игрок, чтобы перемещаться по двум осям");

        Пробел();

        ПредупреждениеПроУправление();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        GUILayout.Label("Объект сможет перемещаться");

        Поле("Вверх", x => x.вверх,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Вниз", x => x.вниз,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Влево", x => x.влево,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Вправо", x => x.вправо,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Отзеркаливать спрайт", x => x.отзеркаливатьСпрайт,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        ОтобразитьСобытияДвижения();
    }
}
#endif