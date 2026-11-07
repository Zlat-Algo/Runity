#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ползунок в интерфейсе, которым игрок выбирает число. Подходит, например, для громкости
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Слайдер")]
public class Слайдер : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Slider>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Slider оригинал => (Slider)оригинальныйКомпонент;

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при каждом движении ползунка
    /// </summary>
    public События приИзменении => _приИзменении;

    void Start()
    {
        оригинал.onValueChanged.AddListener(_ => _приИзменении.Вызвать());
    }

    /// <summary>
    /// Текущее значение ползунка
    /// </summary>
    public Дробное значение
    {
        get => оригинал.value;
        set => оригинал.value = value;
    }

    /// <summary>
    /// Значение ползунка в крайнем левом положении
    /// </summary>
    public Дробное минимум
    {
        get => оригинал.minValue;
        set => оригинал.minValue = value;
    }

    /// <summary>
    /// Значение ползунка в крайнем правом положении
    /// </summary>
    public Дробное максимум
    {
        get => оригинал.maxValue;
        set => оригинал.maxValue = value;
    }

    /// <summary>
    /// Если "да", то ползунок принимает только целые значения
    /// </summary>
    public Флажок толькоЦелые
    {
        get => оригинал.wholeNumbers;
        set => оригинал.wholeNumbers = value;
    }

    /// <summary>
    /// Если "нет", то игрок не сможет двигать ползунок
    /// </summary>
    public Флажок активен
    {
        get => оригинал.interactable;
        set => оригинал.interactable = value;
    }

    /// <summary>
    /// Меняет значение ползунка, но не запускает событие "При изменении"
    /// </summary>
    public void УстановитьБезСобытия(Дробное новоеЗначение) => оригинал.SetValueWithoutNotify(новоеЗначение);
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Слайдер))]
internal class СлайдерEditor : RunityEditor<Слайдер>
{
    protected override void Отрисовать()
    {
        Текст("Этим ползунком игрок может выбрать число");

        Пробел();

        Синхрополе("Значение", x => x.значение,
            (title, value) => EditorGUILayout.Slider(title, value, компонент.минимум, компонент.максимум));

        Синхрополе("Минимум", x => x.минимум,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Максимум", x => x.максимум,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Только целые", x => x.толькоЦелые,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Активен", x => x.активен,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ЗакончитьСобытия();
    }
}
#endif
