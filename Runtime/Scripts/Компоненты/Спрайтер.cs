using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public enum РежимСпрайтера { Обычный, Плитка, Рамка}

[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал 2D/Спрайтер")]
public class Спрайтер : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<SpriteRenderer>();
    void OnValidate() => Awake();
    public SpriteRenderer оригинал => (SpriteRenderer)оригинальныйКомпонент;

    public Спрайт спрайт
    {
        get => оригинал.sprite;
        set => оригинал.sprite = value;
    }
    
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    public Флажок отзеркалитьГоризонтально
    {
        get => оригинал.flipX;
        set => оригинал.flipX = value;
    }

    public Флажок отзеркалитьВертикально
    {
        get => оригинал.flipY;
        set => оригинал.flipY = value;
    }

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

    public Направление2D размер
    {
        get => оригинал.size;
        set => оригинал.size = value;
    }

    public Флажок целыеКусочкиПлитки
    {
        get => оригинал.tileMode == SpriteTileMode.Adaptive;
        set => оригинал.tileMode = value ? SpriteTileMode.Adaptive : SpriteTileMode.Continuous;
    }

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


    public override void OnInspectorGUI()
    {
        Текст("Этот объект отрисовывает указанный спрайт");

        Пробел();

        Синхрополе("Спрайт", x => x.спрайт,
            (title, value) => Объект<Sprite>(title, value));

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

        Кнопка("Заполнить спрайт", () => { компонент.СпрайтБелогоКвадрата(); });
    }
}
#endif