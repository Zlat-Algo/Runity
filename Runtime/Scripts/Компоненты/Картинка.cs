#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Картинка в интерфейсе: иконка, фон, рамка
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Картинка")]
public class Картинка : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Image>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Image оригинал => (Image)оригинальныйКомпонент;

    /// <summary>
    /// Какая картинка показывается
    /// </summary>
    public Спрайт спрайт
    {
        get => оригинал.sprite;
        set => оригинал.sprite = value;
    }

    /// <summary>
    /// Цвет, на который умножается картинка. Через него можно сделать картинку прозрачной
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    /// <summary>
    /// Если "да", то картинка не растягивается, а сохраняет свои пропорции
    /// </summary>
    public Флажок сохранятьПропорции
    {
        get => оригинал.preserveAspect;
        set => оригинал.preserveAspect = value;
    }

    /// <summary>
    /// Как картинка заполняет свой размер. Рамка растягивает только середину, Плитка повторяет картинку
    /// </summary>
    public РежимСпрайтера режим
    {
        get => оригинал.type switch
        {
            Image.Type.Sliced => РежимСпрайтера.Рамка,
            Image.Type.Tiled => РежимСпрайтера.Плитка,
            _ => РежимСпрайтера.Обычный
        };
        set => оригинал.type = value switch
        {
            РежимСпрайтера.Рамка => Image.Type.Sliced,
            РежимСпрайтера.Плитка => Image.Type.Tiled,
            _ => Image.Type.Simple
        };
    }

    /// <summary>
    /// Если "нет", то клики мышью проходят сквозь картинку к тому, что под ней
    /// </summary>
    public Флажок ловитМышь
    {
        get => оригинал.raycastTarget;
        set => оригинал.raycastTarget = value;
    }

    /// <summary>
    /// Делает размер объекта таким же, как у самой картинки
    /// </summary>
    public void ПодогнатьРазмер() => оригинал.SetNativeSize();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Картинка))]
internal class КартинкаEditor : RunityEditor<Картинка>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект показывает картинку в интерфейсе");

        Пробел();

        Синхрополе("Спрайт", x => x.спрайт,
            (title, value) => ПолеОбъекта<Sprite>(title, value));

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        EnumField("Режим", x => x.режим, (component, value) => component.режим = value);

        Синхрополе("Сохранять пропорции", x => x.сохранятьПропорции,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Ловит мышь", x => x.ловитМышь,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Кнопка("Подогнать размер", x => x.ПодогнатьРазмер());
    }
}
#endif
