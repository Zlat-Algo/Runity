#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Галочка в интерфейсе, которую можно включить и выключить. Подходит для настроек
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Галочка")]
public class Галочка : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Toggle>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Toggle оригинал => (Toggle)оригинальныйКомпонент;

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при каждом нажатии на галочку
    /// </summary>
    public События приИзменении => _приИзменении;

    [SerializeField] События _приВключении;
    /// <summary>
    /// Срабатывает, когда галочку поставили
    /// </summary>
    public События приВключении => _приВключении;

    [SerializeField] События _приВыключении;
    /// <summary>
    /// Срабатывает, когда галочку сняли
    /// </summary>
    public События приВыключении => _приВыключении;

    void Start()
    {
        оригинал.onValueChanged.AddListener(включена =>
        {
            _приИзменении.Вызвать();
            if (включена) _приВключении.Вызвать();
            else _приВыключении.Вызвать();
        });
    }

    /// <summary>
    /// Стоит ли галочка
    /// </summary>
    public Флажок включена
    {
        get => оригинал.isOn;
        set => оригинал.isOn = value;
    }

    /// <summary>
    /// Если "нет", то галочка станет серой и перестанет нажиматься
    /// </summary>
    public Флажок активна
    {
        get => оригинал.interactable;
        set => оригинал.interactable = value;
    }

    /// <summary>
    /// Надпись рядом с галочкой
    /// </summary>
    public Строка надпись
    {
        get
        {
            Text текст = GetComponentInChildren<Text>();
            return текст != null ? текст.text : "";
        }
        set
        {
            Text текст = GetComponentInChildren<Text>();
            if (текст != null) текст.text = value;
        }
    }

    /// <summary>
    /// Ставит галочку, если её нет, и снимает, если она есть
    /// </summary>
    public void Переключить() => включена = !включена;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Галочка))]
internal class ГалочкаEditor : RunityEditor<Галочка>
{
    protected override void Отрисовать()
    {
        Текст("Эту галочку игрок может ставить и снимать");

        Пробел();

        Поле("Надпись", x => x.надпись,
            (title, value) => EditorGUILayout.TextField(title, value));

        Синхрополе("Включена", x => x.включена,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Активна", x => x.активна,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ОтобразитьСобытие("При включении", "_приВключении");
        ОтобразитьСобытие("При выключении", "_приВыключении");
        ЗакончитьСобытия();
    }
}
#endif
