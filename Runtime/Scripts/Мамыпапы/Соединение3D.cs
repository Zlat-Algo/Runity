#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Заготовка для 3D-соединений: шарнира, пружины и жёсткого крепления
/// </summary>
[RequireComponent(typeof(ФизическоеТело))]
public abstract class Соединение3D : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    protected virtual void Awake() => оригинальныйКомпонент = GetComponent<Joint>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Joint оригинал => (Joint)оригинальныйКомпонент;

    /// <summary>
    /// С каким объектом соединён этот. Если пусто, то объект прикреплён к точке в мире
    /// </summary>
    public GameObject связанС
    {
        get => оригинал.connectedBody != null ? оригинал.connectedBody.gameObject : null;
        set => оригинал.connectedBody = value != null ? value.GetComponent<Rigidbody>() : null;
    }

    /// <summary>
    /// Где на этом объекте находится крепление, относительно его центра
    /// </summary>
    public Направление3D точкаКрепления { get => оригинал.anchor; set => оригинал.anchor = value; }

    /// <summary>
    /// Если "да", то соединённые объекты могут сталкиваться друг с другом
    /// </summary>
    public Флажок сталкиватьсяДругСДругом { get => оригинал.enableCollision; set => оригинал.enableCollision = value; }

    /// <summary>
    /// С какой силой нужно потянуть, чтобы соединение порвалось. 0 означает, что оно не рвётся
    /// </summary>
    public Дробное прочность
    {
        get => float.IsInfinity(оригинал.breakForce) ? 0 : оригинал.breakForce;
        set => оригинал.breakForce = value <= 0 ? float.PositiveInfinity : (float)value;
    }

    [SerializeField] internal События _приРазрыве;
    /// <summary>
    /// Срабатывает, когда соединение порвалось от слишком сильного рывка
    /// </summary>
    public События приРазрыве => _приРазрыве;

    void OnJointBreak(float сила) => _приРазрыве.Вызвать();

    /// <summary>
    /// Сразу разрывает соединение
    /// </summary>
    public void Разорвать()
    {
        _приРазрыве.Вызвать();
        // Сначала убираем себя: пока этот компонент есть, Unity не даёт удалить соединение
        Destroy(this);
        Destroy(оригинал);
    }
}

#if UNITY_EDITOR
internal abstract class Соединение3DEditor<T> : RunityEditor<T> where T : Соединение3D
{
    protected void ОбщиеНастройки()
    {
        Синхрополе("Связан с", x => x.связанС,
            (title, value) => ПолеОбъекта<GameObject>(title, value, true));

        if (компонент.связанС != null && компонент.связанС.GetComponent<Rigidbody>() == null)
        {
            Текст("У связанного объекта должно быть Физическое тело", MessageType.Warning);
        }

        Синхрополе("Точка крепления", x => x.точкаКрепления,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Синхрополе("Сталкиваться друг с другом", x => x.сталкиватьсяДругСДругом,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Прочность", x => x.прочность,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }

    protected void ОбщиеСобытия()
    {
        НачатьСобытия();
        ОтобразитьСобытие("При разрыве", "_приРазрыве");
        ЗакончитьСобытия();
    }
}
#endif
