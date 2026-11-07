#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// К чему по горизонтали привязан элемент интерфейса
/// </summary>
public enum ПривязкаПоШирине { Слева, ПоЦентру, Справа, Растянуть }

/// <summary>
/// К чему по вертикали привязан элемент интерфейса
/// </summary>
public enum ПривязкаПоВысоте { Сверху, ПоЦентру, Снизу, Растянуть }

/// <summary>
/// Где находится элемент интерфейса: к какому краю родителя он прижат, его размер и отступы
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Расположение")]
public class Расположение : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<RectTransform>();
    void OnValidate() => Awake();

    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public RectTransform оригинал => (RectTransform)оригинальныйКомпонент;

    /// <summary>
    /// К какому краю родителя элемент прижат по горизонтали. Растянуть - элемент занимает всю ширину
    /// </summary>
    public ПривязкаПоШирине поШирине
    {
        get
        {
            float мин = оригинал.anchorMin.x, макс = оригинал.anchorMax.x;
            if (!Mathf.Approximately(мин, макс)) return ПривязкаПоШирине.Растянуть;
            return мин < 0.25f ? ПривязкаПоШирине.Слева : мин > 0.75f ? ПривязкаПоШирине.Справа : ПривязкаПоШирине.ПоЦентру;
        }
        set => Привязать(value, поВысоте);
    }

    /// <summary>
    /// К какому краю родителя элемент прижат по вертикали. Растянуть - элемент занимает всю высоту
    /// </summary>
    public ПривязкаПоВысоте поВысоте
    {
        get
        {
            float мин = оригинал.anchorMin.y, макс = оригинал.anchorMax.y;
            if (!Mathf.Approximately(мин, макс)) return ПривязкаПоВысоте.Растянуть;
            return мин < 0.25f ? ПривязкаПоВысоте.Снизу : мин > 0.75f ? ПривязкаПоВысоте.Сверху : ПривязкаПоВысоте.ПоЦентру;
        }
        set => Привязать(поШирине, value);
    }

    /// <summary>
    /// Смещение элемента от точки привязки
    /// </summary>
    public Направление2D позиция
    {
        get => оригинал.anchoredPosition;
        set => оригинал.anchoredPosition = value;
    }

    /// <summary>
    /// Ширина элемента. Если элемент растянут по ширине, это добавка к ширине родителя
    /// </summary>
    public Дробное ширина
    {
        get => оригинал.sizeDelta.x;
        set => оригинал.sizeDelta = new Vector2(value, оригинал.sizeDelta.y);
    }

    /// <summary>
    /// Высота элемента. Если элемент растянут по высоте, это добавка к высоте родителя
    /// </summary>
    public Дробное высота
    {
        get => оригинал.sizeDelta.y;
        set => оригинал.sizeDelta = new Vector2(оригинал.sizeDelta.x, value);
    }

    /// <summary>
    /// Поворот элемента в градусах
    /// </summary>
    public Дробное поворот
    {
        get => оригинал.localEulerAngles.z;
        set => оригинал.localEulerAngles = new Vector3(0, 0, value);
    }

    /// <summary>
    /// Отступ от краёв родителя для растянутого элемента
    /// </summary>
    public Дробное отступ
    {
        get => поШирине == ПривязкаПоШирине.Растянуть ? оригинал.offsetMin.x : оригинал.offsetMin.y;
        set
        {
            // Отступ меняет только растянутые оси, размер по остальным остаётся прежним
            if (поШирине == ПривязкаПоШирине.Растянуть)
            {
                оригинал.offsetMin = new Vector2(value, оригинал.offsetMin.y);
                оригинал.offsetMax = new Vector2(-value, оригинал.offsetMax.y);
            }
            if (поВысоте == ПривязкаПоВысоте.Растянуть)
            {
                оригинал.offsetMin = new Vector2(оригинал.offsetMin.x, value);
                оригинал.offsetMax = new Vector2(оригинал.offsetMax.x, -value);
            }
        }
    }

    /// <summary>
    /// Прижимает элемент к краю родителя. Элемент встаёт вплотную к этому краю
    /// </summary>
    public void Привязать(ПривязкаПоШирине ширина, ПривязкаПоВысоте высота)
    {
        Vector2 размер = оригинал.rect.size;

        float x = ширина switch { ПривязкаПоШирине.Слева => 0, ПривязкаПоШирине.Справа => 1, _ => 0.5f };
        float y = высота switch { ПривязкаПоВысоте.Снизу => 0, ПривязкаПоВысоте.Сверху => 1, _ => 0.5f };

        оригинал.anchorMin = new Vector2(ширина == ПривязкаПоШирине.Растянуть ? 0 : x, высота == ПривязкаПоВысоте.Растянуть ? 0 : y);
        оригинал.anchorMax = new Vector2(ширина == ПривязкаПоШирине.Растянуть ? 1 : x, высота == ПривязкаПоВысоте.Растянуть ? 1 : y);
        оригинал.pivot = new Vector2(x, y);

        оригинал.sizeDelta = new Vector2(ширина == ПривязкаПоШирине.Растянуть ? 0 : размер.x, высота == ПривязкаПоВысоте.Растянуть ? 0 : размер.y);
        оригинал.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Растягивает элемент на всего родителя
    /// </summary>
    public void Растянуть() => Привязать(ПривязкаПоШирине.Растянуть, ПривязкаПоВысоте.Растянуть);

    /// <summary>
    /// Сдвигает элемент на указанное расстояние
    /// </summary>
    public void Сдвинуть(Направление2D насколько) => позиция = (Vector2)позиция + (Vector2)насколько;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Расположение))]
internal class РасположениеEditor : RunityEditor<Расположение>
{
    static readonly string[] надписиШирины = { "Слева", "Центр", "Справа", "↔" };
    static readonly string[] надписиВысоты = { "Сверху", "Центр", "Снизу", "↕" };

    protected override void Отрисовать()
    {
        Текст("Этот компонент решает, где стоит элемент интерфейса. Нажмите на клетку, чтобы прижать элемент к краю или растянуть его");

        Пробел();

        Отметить(nameof(Расположение.поШирине), nameof(Расположение.поВысоте), nameof(Расположение.Привязать), nameof(Расположение.Растянуть));
        EditorGUILayout.LabelField(new GUIContent("Привязка", Справка.Описание(typeof(Расположение), nameof(Расположение.Привязать))), EditorStyles.boldLabel);

        ПривязкаПоШирине ширина = компонент.поШирине;
        ПривязкаПоВысоте высота = компонент.поВысоте;

        Rect область = GUILayoutUtility.GetRect(220, 5 * 26);
        float клетка = Mathf.Min(52, (область.width - 60) / 4);
        float x0 = область.x + 60;

        for (int столбец = 0; столбец < 4; столбец++)
        {
            GUI.Label(new Rect(x0 + столбец * клетка, область.y, клетка, 24), надписиШирины[столбец], EditorStyles.centeredGreyMiniLabel);
        }

        for (int строка = 0; строка < 4; строка++)
        {
            float y = область.y + 26 + строка * 26;
            GUI.Label(new Rect(область.x, y, 58, 24), надписиВысоты[строка], EditorStyles.miniLabel);

            for (int столбец = 0; столбец < 4; столбец++)
            {
                bool выбрана = (int)ширина == столбец && (int)высота == строка;
                Color было = GUI.backgroundColor;
                if (выбрана) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);

                Rect место = new Rect(x0 + столбец * клетка + 1, y + 1, клетка - 2, 22);
                if (GUI.Button(место, ЗначокКлетки(столбец, строка), EditorStyles.miniButton))
                {
                    foreach (Расположение элемент in components)
                    {
                        Undo.RecordObject(элемент.оригинал, "Привязка");
                        элемент.Привязать((ПривязкаПоШирине)столбец, (ПривязкаПоВысоте)строка);
                    }
                }

                GUI.backgroundColor = было;
            }
        }

        Пробел();

        Синхрополе("Позиция", x => x.позиция,
            (title, value) => EditorGUILayout.Vector2Field(title, value));

        if (ширина != ПривязкаПоШирине.Растянуть)
        {
            Синхрополе("Ширина", x => x.ширина,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        if (высота != ПривязкаПоВысоте.Растянуть)
        {
            Синхрополе("Высота", x => x.высота,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        if (ширина == ПривязкаПоШирине.Растянуть || высота == ПривязкаПоВысоте.Растянуть)
        {
            Синхрополе("Отступ", x => x.отступ,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Синхрополе("Поворот", x => x.поворот,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }

    static string ЗначокКлетки(int столбец, int строка)
    {
        if (столбец == 3 && строка == 3) return "⛶";
        if (столбец == 3) return "↔";
        if (строка == 3) return "↕";
        return "•";
    }
}
#endif
