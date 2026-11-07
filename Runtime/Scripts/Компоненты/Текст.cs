#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// К какому краю или углу прижато содержимое: текст или элементы группы
/// </summary>
public enum Выравнивание { СлеваСверху, Сверху, СправаСверху, Слева, ПоЦентру, Справа, СлеваСнизу, Снизу, СправаСнизу }

/// <summary>
/// Показывает надпись в интерфейсе. Кириллица работает сразу, импортировать шрифты не нужно
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Текст")]
public class Текст : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Text>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Text оригинал => (Text)оригинальныйКомпонент;

    /// <summary>
    /// Что написано
    /// </summary>
    public Строка содержимое
    {
        get => оригинал.text;
        set => оригинал.text = value;
    }

    /// <summary>
    /// Цвет букв
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    /// <summary>
    /// Размер букв
    /// </summary>
    public Число размер
    {
        get => оригинал.fontSize;
        set => оригинал.fontSize = value;
    }

    /// <summary>
    /// Шрифт. Подойдёт любой файл .ttf или .otf из проекта
    /// </summary>
    public Font шрифт
    {
        get => оригинал.font;
        set => оригинал.font = value;
    }

    /// <summary>
    /// Если "да", то буквы будут жирными
    /// </summary>
    public Флажок жирный
    {
        get => оригинал.fontStyle == FontStyle.Bold || оригинал.fontStyle == FontStyle.BoldAndItalic;
        set => оригинал.fontStyle = value ? FontStyle.Bold : FontStyle.Normal;
    }

    /// <summary>
    /// По какому краю выровнен текст
    /// </summary>
    public Выравнивание выравнивание
    {
        get => (Выравнивание)(int)оригинал.alignment;
        set => оригинал.alignment = (TextAnchor)(int)value;
    }

    /// <summary>
    /// Если "да", то размер букв подбирается так, чтобы текст поместился
    /// </summary>
    public Флажок автоРазмер
    {
        get => оригинал.resizeTextForBestFit;
        set => оригинал.resizeTextForBestFit = value;
    }

    /// <summary>
    /// Показывает целое число
    /// </summary>
    public void Показать(Число значение) => содержимое = значение.ToString();

    /// <summary>
    /// Показывает дробное число
    /// </summary>
    public void Показать(Дробное значение) => содержимое = значение.ToString();

    /// <summary>
    /// Дописывает текст в конец
    /// </summary>
    public void Дописать(Строка текст) => содержимое = (string)содержимое + текст;

    /// <summary>
    /// Если содержимое - число, то прибавляет к нему указанное значение. Удобно для счётчиков
    /// </summary>
    public void Прибавить(Число значение)
    {
        int.TryParse(содержимое, out int текущее);
        содержимое = (текущее + значение).ToString();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Текст))]
internal class ТекстEditor : RunityEditor<Текст>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект показывает надпись в интерфейсе");

        Пробел();

        Синхрополе("Содержимое", x => x.содержимое,
            (title, value) => EditorGUILayout.TextField(title, value));

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Пробел();

        Синхрополе("Размер", x => x.размер,
            (title, value) => EditorGUILayout.IntField(title, value));

        Синхрополе("Шрифт", x => x.шрифт,
            (title, value) => ПолеОбъекта<Font>(title, value));

        Синхрополе("Жирный", x => x.жирный,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Автоматический размер", x => x.автоРазмер,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        EnumField("Выравнивание", x => x.выравнивание,
            (component, value) => component.выравнивание = value);
    }
}
#endif
