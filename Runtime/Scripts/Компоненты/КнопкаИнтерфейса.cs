#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Кнопка в интерфейсе, на которую игрок может нажать мышкой
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Кнопка")]
public class КнопкаИнтерфейса : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Button>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Button оригинал => (Button)оригинальныйКомпонент;

    [SerializeField] События _приНажатии;
    /// <summary>
    /// Срабатывает, когда игрок нажал на кнопку
    /// </summary>
    public События приНажатии => _приНажатии;

    void Start()
    {
        оригинал.onClick.AddListener(() => _приНажатии.Вызвать());
    }

    /// <summary>
    /// Надпись на кнопке
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
    /// Если "нет", то кнопка станет серой и перестанет нажиматься
    /// </summary>
    public Флажок активна
    {
        get => оригинал.interactable;
        set => оригинал.interactable = value;
    }

    /// <summary>
    /// Нажимает на кнопку из кода
    /// </summary>
    public void Нажать() => оригинал.onClick.Invoke();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КнопкаИнтерфейса))]
internal class КнопкаИнтерфейсаEditor : RunityEditor<КнопкаИнтерфейса>
{
    protected override void Отрисовать()
    {
        Текст("На этот объект можно нажать мышкой, и он что-то сделает");

        Пробел();

        Поле("Надпись", x => x.надпись,
            (title, value) => EditorGUILayout.TextField(title, value));

        Синхрополе("Активна", x => x.активна,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При нажатии", "_приНажатии");
        ЗакончитьСобытия();
    }
}
#endif
