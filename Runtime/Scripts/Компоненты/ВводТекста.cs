#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Поле в интерфейсе, куда игрок может написать текст
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(InputField))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Ввод текста")]
public class ВводТекста : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<InputField>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public InputField оригинал => (InputField)оригинальныйКомпонент;

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при каждом изменении текста
    /// </summary>
    public События приИзменении => _приИзменении;

    [SerializeField] События _приЗавершении;
    /// <summary>
    /// Срабатывает, когда игрок закончил ввод: нажал Enter или кликнул мимо
    /// </summary>
    public События приЗавершении => _приЗавершении;

    void Start()
    {
        оригинал.onValueChanged.AddListener(_ => _приИзменении.Вызвать());
        оригинал.onEndEdit.AddListener(_ => _приЗавершении.Вызвать());
    }

    /// <summary>
    /// Что сейчас написано в поле
    /// </summary>
    public Строка содержимое
    {
        get => оригинал.text;
        set => оригинал.text = value;
    }

    /// <summary>
    /// Серая подсказка, которую видно, пока поле пустое
    /// </summary>
    public Строка подсказка
    {
        get => оригинал.placeholder is Text текст ? текст.text : "";
        set
        {
            if (оригинал.placeholder is Text текст) текст.text = value;
        }
    }

    /// <summary>
    /// Сколько символов можно ввести. 0 означает без ограничений
    /// </summary>
    public Число максимумСимволов
    {
        get => оригинал.characterLimit;
        set => оригинал.characterLimit = value;
    }

    /// <summary>
    /// Если "да", то вместо букв будут видны звёздочки
    /// </summary>
    public Флажок пароль
    {
        get => оригинал.inputType == InputField.InputType.Password;
        set => оригинал.inputType = value ? InputField.InputType.Password : InputField.InputType.Standard;
    }

    /// <summary>
    /// Если "да", то в поле можно вводить только целые числа
    /// </summary>
    public Флажок толькоЧисла
    {
        get => оригинал.contentType == InputField.ContentType.IntegerNumber;
        set => оригинал.contentType = value ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
    }

    /// <summary>
    /// Стирает всё, что написано
    /// </summary>
    public void Очистить() => содержимое = "";

    /// <summary>
    /// Ставит курсор в поле, чтобы можно было сразу печатать
    /// </summary>
    public void Выбрать() => оригинал.ActivateInputField();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ВводТекста))]
internal class ВводТекстаEditor : RunityEditor<ВводТекста>
{
    protected override void Отрисовать()
    {
        Текст("В этом поле игрок может что-нибудь написать");

        Пробел();

        Синхрополе("Содержимое", x => x.содержимое,
            (title, value) => EditorGUILayout.TextField(title, value));

        Поле("Подсказка", x => x.подсказка,
            (title, value) => EditorGUILayout.TextField(title, value));

        Синхрополе("Максимум символов", x => x.максимумСимволов,
            (title, value) => EditorGUILayout.IntField(title, value));

        Синхрополе("Пароль", x => x.пароль,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Только числа", x => x.толькоЧисла,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ОтобразитьСобытие("При завершении", "_приЗавершении");
        ЗакончитьСобытия();
    }
}
#endif
