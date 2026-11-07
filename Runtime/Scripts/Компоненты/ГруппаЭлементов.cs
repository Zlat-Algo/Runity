#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Как расставляются дочерние элементы
/// </summary>
public enum РежимГруппы { Горизонтально, Вертикально, Сеткой }

/// <summary>
/// Сама расставляет дочерние элементы интерфейса по горизонтали, вертикали или сеткой. Не нужно двигать каждую кнопку вручную
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Группа элементов")]
public class ГруппаЭлементов : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<LayoutGroup>();
        if (оригинальныйКомпонент == null && Application.isPlaying) режим = РежимГруппы.Вертикально;
    }
    void OnValidate() => оригинальныйКомпонент = GetComponent<LayoutGroup>();
    void Reset()
    {
#if UNITY_EDITOR
        // Во время добавления компонента Unity не разрешает добавлять другие, поэтому расстановка включается чуть позже
        EditorApplication.delayCall += () =>
        {
            if (this != null && GetComponent<LayoutGroup>() == null) режим = РежимГруппы.Вертикально;
        };
#endif
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public LayoutGroup оригинал => (LayoutGroup)оригинальныйКомпонент;

    HorizontalOrVerticalLayoutGroup линия => оригинальныйКомпонент as HorizontalOrVerticalLayoutGroup;
    GridLayoutGroup сетка => оригинальныйКомпонент as GridLayoutGroup;

    /// <summary>
    /// Как раскладываются дочерние элементы
    /// </summary>
    public РежимГруппы режим
    {
        get => оригинальныйКомпонент switch
        {
            HorizontalLayoutGroup _ => РежимГруппы.Горизонтально,
            GridLayoutGroup _ => РежимГруппы.Сеткой,
            _ => РежимГруппы.Вертикально
        };
        set
        {
            LayoutGroup старая = GetComponent<LayoutGroup>();
            if (старая != null && режим == value && оригинальныйКомпонент == старая) return;

            // Unity не даёт держать на объекте две раскладки, поэтому старую нужно убрать сразу
            if (старая != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) Undo.DestroyObjectImmediate(старая);
                else
#endif
                DestroyImmediate(старая);
            }

            System.Type тип = value switch
            {
                РежимГруппы.Горизонтально => typeof(HorizontalLayoutGroup),
                РежимГруппы.Сеткой => typeof(GridLayoutGroup),
                _ => typeof(VerticalLayoutGroup)
            };

#if UNITY_EDITOR
            if (!Application.isPlaying) оригинальныйКомпонент = Undo.AddComponent(gameObject, тип);
            else
#endif
            оригинальныйКомпонент = gameObject.AddComponent(тип);

            if (линия != null)
            {
                линия.childControlWidth = true;
                линия.childControlHeight = true;
                линия.childForceExpandWidth = false;
                линия.childForceExpandHeight = false;
            }
        }
    }

    /// <summary>
    /// Расстояние между элементами
    /// </summary>
    public Дробное отступМежду
    {
        get => линия != null ? линия.spacing : сетка.spacing.x;
        set
        {
            if (линия != null) линия.spacing = value;
            else сетка.spacing = new Vector2(value, value);
        }
    }

    /// <summary>
    /// Расстояние от краёв до элементов
    /// </summary>
    public Число поля
    {
        get => оригинал.padding.left;
        set => оригинал.padding = new RectOffset(value, value, value, value);
    }

    /// <summary>
    /// К какому краю прижимаются элементы
    /// </summary>
    public Выравнивание выравнивание
    {
        get => (Выравнивание)(int)оригинал.childAlignment;
        set => оригинал.childAlignment = (TextAnchor)(int)value;
    }

    /// <summary>
    /// По горизонтали и вертикали: если "да", то элементы растягиваются, чтобы заполнить всё место
    /// </summary>
    public Флажок растягиватьЭлементы
    {
        get => линия != null && (линия.childForceExpandWidth || линия.childForceExpandHeight);
        set
        {
            if (линия == null) return;
            линия.childForceExpandWidth = value;
            линия.childForceExpandHeight = value;
        }
    }

    /// <summary>
    /// В сетке: размер каждой клетки
    /// </summary>
    public Направление2D размерКлетки
    {
        get => сетка != null ? сетка.cellSize : Vector2.zero;
        set { if (сетка != null) сетка.cellSize = value; }
    }

    /// <summary>
    /// В сетке: сколько элементов в одной строке. 0 означает, сколько поместится
    /// </summary>
    public Число столбцов
    {
        get => сетка != null && сетка.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? сетка.constraintCount : 0;
        set
        {
            if (сетка == null) return;
            сетка.constraint = value > 0 ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
            if (value > 0) сетка.constraintCount = value;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ГруппаЭлементов))]
internal class ГруппаЭлементовEditor : RunityEditor<ГруппаЭлементов>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект сам расставляет всё, что лежит внутри него");

        Пробел();

        if (компонент.оригинал == null)
        {
            Кнопка("Включить группу", () => компонент.режим = РежимГруппы.Вертикально);
            return;
        }

        Поле("Режим", x => x.режим,
            (title, value) => (РежимГруппы)EditorGUILayout.EnumPopup(title, value));

        Синхрополе("Отступ между", x => x.отступМежду,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Поля", x => x.поля,
            (title, value) => EditorGUILayout.IntField(title, value));

        EnumField("Выравнивание", x => x.выравнивание, (component, value) => component.выравнивание = value);

        if (компонент.режим == РежимГруппы.Сеткой)
        {
            Синхрополе("Размер клетки", x => x.размерКлетки,
                (title, value) => EditorGUILayout.Vector2Field(title, value));

            Синхрополе("Столбцов", x => x.столбцов,
                (title, value) => EditorGUILayout.IntField(title, value));
        }
        else
        {
            Синхрополе("Растягивать элементы", x => x.растягиватьЭлементы,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }
    }
}
#endif
