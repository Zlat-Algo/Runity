#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

public enum РежимСпрайтера { Обычный, Плитка, Рамка}

/// <summary>
/// Показывает 2D-картинку
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал/ 2D/Спрайтер")]
public class Спрайтер : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<SpriteRenderer>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public SpriteRenderer оригинал => (SpriteRenderer)оригинальныйКомпонент;

    /// <summary>
    /// Какая картинка показывается
    /// </summary>
    public Спрайт спрайт
    {
        get => оригинал.sprite;
        set => оригинал.sprite = value;
    }
    
    /// <summary>
    /// Цвет, на который умножается картинка
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    /// <summary>
    /// Показывает картинку зеркально слева направо
    /// </summary>
    public Флажок отзеркалитьГоризонтально
    {
        get => оригинал.flipX;
        set => оригинал.flipX = value;
    }

    /// <summary>
    /// Показывает картинку зеркально сверху вниз
    /// </summary>
    public Флажок отзеркалитьВертикально
    {
        get => оригинал.flipY;
        set => оригинал.flipY = value;
    }

    /// <summary>
    /// Как картинка заполняет размер объекта
    /// </summary>
    public РежимСпрайтера режим
    {
        get => оригинал.drawMode switch
        {
            SpriteDrawMode.Simple => РежимСпрайтера.Обычный,
            SpriteDrawMode.Tiled => РежимСпрайтера.Плитка,
            SpriteDrawMode.Sliced => РежимСпрайтера.Рамка,
            _ => РежимСпрайтера.Обычный
        };
        set => оригинал.drawMode = value switch
        {
            РежимСпрайтера.Обычный => SpriteDrawMode.Simple,
            РежимСпрайтера.Плитка => SpriteDrawMode.Tiled,
            РежимСпрайтера.Рамка => SpriteDrawMode.Sliced,
            _ => SpriteDrawMode.Simple
        };
    }

    /// <summary>
    /// Размер картинки в режимах Плитка и Рамка
    /// </summary>
    public Направление2D размер
    {
        get => оригинал.size;
        set => оригинал.size = value;
    }

    /// <summary>
    /// В режиме Плитка показывает только целые повторения картинки
    /// </summary>
    public Флажок целыеКусочкиПлитки
    {
        get => оригинал.tileMode == SpriteTileMode.Adaptive;
        set => оригинал.tileMode = value ? SpriteTileMode.Adaptive : SpriteTileMode.Continuous;
    }

    /// <summary>
    /// Ставит белый квадрат вместо картинки
    /// </summary>
    public void СпрайтБелогоКвадрата()
    {
        спрайт = НайтиАссет<Sprite>("1c08e50202374ce42bca6cb30aa4bbbf");
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Спрайтер))]
internal class СпрайтерEditor : RunityEditor<Спрайтер>
{
    //bool eventFoldout = false;


    protected override void Отрисовать()
    {
        Текст("Этот объект отрисовывает указанный спрайт");

        Пробел();

        Синхрополе("Спрайт", x => x.спрайт,
            (title, value) => ПолеОбъекта<Sprite>(title, value));

        Пробел();

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Пробел();

        Синхрополе("Отзеркалить горизонтально", x => x.отзеркалитьГоризонтально,
            (title, value) => EditorGUILayout.ToggleLeft(title, value));

        Синхрополе("Отзеркалить вертикально", x => x.отзеркалитьВертикально,
            (title, value) => EditorGUILayout.ToggleLeft(title, value));

        Пробел();

        EnumField(
            "Режим",
            component => component.режим,
            (component, value) =>
                component.режим = value
        );

        Пробел();

        if (компонент.режим == РежимСпрайтера.Плитка)
        {
            Синхрополе("Целые кусочки плитки", x => x.целыеКусочкиПлитки,
                (title, value) => EditorGUILayout.ToggleLeft(title, value));
        }

        if (компонент.режим == РежимСпрайтера.Плитка || компонент.режим == РежимСпрайтера.Рамка)
        {
            Синхрополе("Размер", x => x.размер,
                (title, value) => EditorGUILayout.Vector2Field(title, value));
        }

        Пробел();

        Кнопка("Заполнить спрайт", x => x.СпрайтБелогоКвадрата());
    }
}
#endif