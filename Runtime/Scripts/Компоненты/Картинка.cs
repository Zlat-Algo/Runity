using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Как картинка заполняет свой размер
/// </summary>
public enum РежимКартинки { Обычный, Рамка, Плитка, Заполнение }

/// <summary>
/// Чем раскрашивается картинка
/// </summary>
public enum ОкраскаКартинки { Цвет, Градиент, ПоЗаполненности }

/// <summary>
/// Куда идёт градиент: от первого цвета к последнему
/// </summary>
public enum НаправлениеГрадиента { СлеваНаправо, СправаНалево, СнизуВверх, СверхуВниз }

/// <summary>
/// В какую сторону заполняется картинка
/// </summary>
public enum НаправлениеЗаполнения { Вправо, Влево, Вверх, Вниз, ПоЧасовой, ПротивЧасовой }

/// <summary>
/// Картинка в интерфейсе: иконка, фон, рамка или полоска здоровья, которая заполняется
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Картинка")]
public class Картинка : RunityComponent, IMeshModifier
{
    const string guidБелогоКвадрата = "1c08e50202374ce42bca6cb30aa4bbbf";
    static Sprite белыйСпрайт;

    void Awake() => оригинальныйКомпонент = GetComponent<Image>();
    void OnValidate()
    {
        Awake();
        if (оригинал == null) return;
        if (режим == РежимКартинки.Заполнение) НастроитьЗаполнение();
        оригинал.SetVerticesDirty();
    }
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
    /// Как картинка заполняет свой размер. Рамка растягивает только середину, Плитка повторяет картинку, Заполнение показывает часть картинки, как полоска здоровья
    /// </summary>
    public РежимКартинки режим
    {
        get => оригинал.type switch
        {
            Image.Type.Sliced => РежимКартинки.Рамка,
            Image.Type.Tiled => РежимКартинки.Плитка,
            Image.Type.Filled => РежимКартинки.Заполнение,
            _ => РежимКартинки.Обычный
        };
        set
        {
            оригинал.type = value switch
            {
                РежимКартинки.Рамка => Image.Type.Sliced,
                РежимКартинки.Плитка => Image.Type.Tiled,
                РежимКартинки.Заполнение => Image.Type.Filled,
                _ => Image.Type.Simple
            };
            if (value == РежимКартинки.Заполнение) НастроитьЗаполнение();
        }
    }

    /// <summary>
    /// Если "нет", то клики мышью проходят сквозь картинку к тому, что под ней
    /// </summary>
    public Флажок ловитМышь
    {
        get => оригинал.raycastTarget;
        set => оригинал.raycastTarget = value;
    }

    #region Окраска

    [SerializeField] ОкраскаКартинки _окраска;
    /// <summary>
    /// Чем раскрашивается картинка: одним цветом, градиентом или цветом, который зависит от заполненности
    /// </summary>
    public ОкраскаКартинки окраска
    {
        get => _окраска;
        set
        {
            if (_окраска == value) return;
            _окраска = value;
            // Градиент сам задаёт цвета, поэтому прежний цвет не должен их перекрашивать
            if (value != ОкраскаКартинки.Цвет) цвет = new Цвет(1, 1, 1, ((Color)цвет).a);
            Перерисовать();
        }
    }

    [SerializeField] Gradient _градиент = Градиент(new Color(1f, 0.85f, 0.3f), new Color(0.9f, 0.3f, 0.4f));
    /// <summary>
    /// Цвета, которые плавно переходят друг в друга по картинке
    /// </summary>
    public Gradient градиент { get => _градиент; set { _градиент = value; Перерисовать(); } }

    [SerializeField] НаправлениеГрадиента _направлениеГрадиента;
    /// <summary>
    /// В какую сторону идёт градиент
    /// </summary>
    public НаправлениеГрадиента направлениеГрадиента { get => _направлениеГрадиента; set { _направлениеГрадиента = value; Перерисовать(); } }

    [SerializeField] Gradient _цветПоЗаполненности = Градиент(new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.85f, 0.3f));
    /// <summary>
    /// Какого цвета картинка при разной заполненности: слева пустая, справа полная. Так полоска здоровья может краснеть, когда здоровья мало
    /// </summary>
    public Gradient цветПоЗаполненности { get => _цветПоЗаполненности; set { _цветПоЗаполненности = value; Перерисовать(); } }

    static Gradient Градиент(params Color[] цвета)
    {
        GradientColorKey[] ключи = new GradientColorKey[цвета.Length];
        for (int i = 0; i < цвета.Length; i++) ключи[i] = new GradientColorKey(цвета[i], цвета.Length == 1 ? 0 : (float)i / (цвета.Length - 1));

        Gradient градиент = new Gradient();
        градиент.SetKeys(ключи, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return градиент;
    }

    void Перерисовать()
    {
        if (оригинал != null) оригинал.SetVerticesDirty();
    }

    #endregion

    #region Заполнение

    [SerializeField] Дробное _значение = 1;
    /// <summary>
    /// Насколько заполнена картинка: от 0 до Максимума. Работает в режиме Заполнение
    /// </summary>
    public Дробное значение
    {
        get => _значение;
        set
        {
            float прошлое = _значение;
            _значение = Математика.Ограничить(value, 0, максимум);

            if (!Application.isPlaying)
            {
                ПоказатьСразу();
                return;
            }
            if (прошлое == _значение) return;

            _приИзменении.Вызвать();
            if (_значение >= максимум) _приЗаполнении.Вызвать();
            if (_значение <= 0) _приОпустошении.Вызвать();
        }
    }

    [SerializeField] Дробное _максимум = 1;
    /// <summary>
    /// Значение, при котором картинка заполнена целиком
    /// </summary>
    public Дробное максимум
    {
        get => _максимум;
        set
        {
            _максимум = value;
            if (!Application.isPlaying) ПоказатьСразу();
        }
    }

    [SerializeField] Здоровье _источник;
    /// <summary>
    /// Если указать Здоровье, то картинка будет заполняться по нему сама
    /// </summary>
    public Здоровье источник { get => _источник; set { _источник = value; if (!Application.isPlaying) ПоказатьСразу(); } }

    [SerializeField] НаправлениеЗаполнения _направление;
    /// <summary>
    /// В какую сторону заполняется картинка
    /// </summary>
    public НаправлениеЗаполнения направление { get => _направление; set { _направление = value; if (!Application.isPlaying) ПоказатьСразу(); } }

    [SerializeField] Дробное _плавность = 10;
    /// <summary>
    /// Как быстро картинка догоняет значение. 0 означает, что она меняется мгновенно
    /// </summary>
    public Дробное плавность { get => _плавность; set => _плавность = value; }

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает при изменении значения
    /// </summary>
    public События приИзменении => _приИзменении;

    [SerializeField] События _приЗаполнении;
    /// <summary>
    /// Срабатывает, когда картинка заполнилась целиком
    /// </summary>
    public События приЗаполнении => _приЗаполнении;

    [SerializeField] События _приОпустошении;
    /// <summary>
    /// Срабатывает, когда картинка опустела
    /// </summary>
    public События приОпустошении => _приОпустошении;

    /// <summary>
    /// Заполненность от 0 до 1
    /// </summary>
    public Дробное доля => максимум > 0 ? значение / максимум : 0;

    /// <summary>
    /// Какая часть картинки видна прямо сейчас, от 0 до 1. Из-за плавности она догоняет долю не сразу
    /// </summary>
    public Дробное видимаяДоля => оригинал.fillAmount;

    /// <summary>
    /// Прибавляет к текущему значению. Подходит для опыта или загрузки
    /// </summary>
    public void Прибавить(Дробное сколько) => значение = (float)значение + сколько;

    /// <summary>
    /// Заполняет картинку до конца
    /// </summary>
    public void Заполнить() => значение = максимум;

    /// <summary>
    /// Опустошает картинку
    /// </summary>
    public void Опустошить() => значение = 0;

    void ВзятьИзИсточника()
    {
        if (источник == null) return;
        _максимум = источник.максимальное;
        if (Application.isPlaying) значение = источник.текущее;
        else _значение = Математика.Ограничить(источник.текущее, 0, _максимум);
    }

    // В редакторе картинка меняется сразу, без плавности, чтобы было видно настоящее значение
    void ПоказатьСразу()
    {
        if (оригинал == null || режим != РежимКартинки.Заполнение) return;
        ВзятьИзИсточника();
        НастроитьЗаполнение();
        оригинал.fillAmount = доля;
#if UNITY_EDITOR
        EditorUtility.SetDirty(оригинал);
#endif
    }

    internal void НастроитьЗаполнение()
    {
        if (оригинал.sprite == null)
        {
            Sprite белый = НайтиАссет<Sprite>(guidБелогоКвадрата);
            if (белый == null)
            {
                if (белыйСпрайт == null) белыйСпрайт = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one / 2);
                белый = белыйСпрайт;
            }
            оригинал.sprite = белый;
        }

        switch (направление)
        {
            case НаправлениеЗаполнения.Вправо:
            case НаправлениеЗаполнения.Влево:
                оригинал.fillMethod = Image.FillMethod.Horizontal;
                оригинал.fillOrigin = направление == НаправлениеЗаполнения.Вправо ? 0 : 1;
                break;
            case НаправлениеЗаполнения.Вверх:
            case НаправлениеЗаполнения.Вниз:
                оригинал.fillMethod = Image.FillMethod.Vertical;
                оригинал.fillOrigin = направление == НаправлениеЗаполнения.Вверх ? 0 : 1;
                break;
            default:
                оригинал.fillMethod = Image.FillMethod.Radial360;
                оригинал.fillOrigin = (int)Image.Origin360.Top;
                оригинал.fillClockwise = направление == НаправлениеЗаполнения.ПоЧасовой;
                break;
        }
    }

    void Start()
    {
        if (режим != РежимКартинки.Заполнение) return;

        ВзятьИзИсточника();
        НастроитьЗаполнение();
        оригинал.fillAmount = доля;
    }

    void Update()
    {
        if (режим != РежимКартинки.Заполнение) return;

        ВзятьИзИсточника();

        оригинал.fillAmount = плавность > 0
            ? Mathf.MoveTowards(оригинал.fillAmount, доля, плавность * Время.времяСПрошлогоКадраБезУчётаСкорости / 10)
            : (float)доля;
    }

    #endregion

    /// <summary>
    /// Делает размер объекта таким же, как у самой картинки
    /// </summary>
    public void ПодогнатьРазмер() => оригинал.SetNativeSize();

    #region Раскраска вершин

    void IMeshModifier.ModifyMesh(Mesh mesh) { }

    void IMeshModifier.ModifyMesh(VertexHelper вершины)
    {
        if (!isActiveAndEnabled || вершины.currentVertCount == 0) return;

        if (окраска == ОкраскаКартинки.ПоЗаполненности)
        {
            Color цветДоли = цветПоЗаполненности.Evaluate(режим == РежимКартинки.Заполнение ? оригинал.fillAmount : (float)доля);
            UIVertex вершина = new UIVertex();
            for (int i = 0; i < вершины.currentVertCount; i++)
            {
                вершины.PopulateUIVertex(ref вершина, i);
                вершина.color = (Color)вершина.color * цветДоли;
                вершины.SetUIVertex(вершина, i);
            }
        }
        else if (окраска == ОкраскаКартинки.Градиент)
        {
            if (вершины.currentVertCount == 4) ГрадиентПолосами(вершины);
            else ГрадиентПоВершинам(вершины);
        }
    }

    bool горизонтальныйГрадиент => направлениеГрадиента == НаправлениеГрадиента.СлеваНаправо || направлениеГрадиента == НаправлениеГрадиента.СправаНалево;
    bool обратныйГрадиент => направлениеГрадиента == НаправлениеГрадиента.СправаНалево || направлениеГрадиента == НаправлениеГрадиента.СверхуВниз;

    // Где точка находится на градиенте. Считается по всей картинке, поэтому у наполовину заполненной полоски видна только половина градиента
    float МестоНаГрадиенте(Vector3 позиция)
    {
        Rect область = ((RectTransform)transform).rect;
        float место = горизонтальныйГрадиент
            ? Mathf.InverseLerp(область.xMin, область.xMax, позиция.x)
            : Mathf.InverseLerp(область.yMin, область.yMax, позиция.y);
        return обратныйГрадиент ? 1 - место : место;
    }

    void ГрадиентПоВершинам(VertexHelper вершины)
    {
        UIVertex вершина = new UIVertex();
        for (int i = 0; i < вершины.currentVertCount; i++)
        {
            вершины.PopulateUIVertex(ref вершина, i);
            вершина.color = (Color)вершина.color * градиент.Evaluate(МестоНаГрадиенте(вершина.position));
            вершины.SetUIVertex(вершина, i);
        }
    }

    // У простой картинки всего 4 вершины, и цвета из середины градиента потерялись бы. Поэтому картинка режется на полосы по ключам градиента
    void ГрадиентПолосами(VertexHelper вершины)
    {
        UIVertex[] углы = new UIVertex[4];
        for (int i = 0; i < 4; i++) вершины.PopulateUIVertex(ref углы[i], i);

        // Порядок вершин у Image: левая нижняя, левая верхняя, правая верхняя, правая нижняя
        UIVertex начало1 = углы[0], начало2 = горизонтальныйГрадиент ? углы[1] : углы[3];
        UIVertex конец1 = горизонтальныйГрадиент ? углы[3] : углы[1], конец2 = углы[2];

        Rect область = ((RectTransform)transform).rect;
        float от = горизонтальныйГрадиент ? начало1.position.x : начало1.position.y;
        float до = горизонтальныйГрадиент ? конец1.position.x : конец1.position.y;
        float минимум = горизонтальныйГрадиент ? область.xMin : область.yMin;
        float длина = горизонтальныйГрадиент ? область.width : область.height;
        if (Mathf.Approximately(от, до) || длина <= 0) { ГрадиентПоВершинам(вершины); return; }

        List<float> разрезы = new List<float> { 0, 1 };
        foreach (GradientColorKey ключ in градиент.colorKeys) ДобавитьРазрез(разрезы, ключ.time);
        foreach (GradientAlphaKey ключ in градиент.alphaKeys) ДобавитьРазрез(разрезы, ключ.time);
        разрезы.Sort();

        void ДобавитьРазрез(List<float> список, float время)
        {
            float место = обратныйГрадиент ? 1 - время : время;
            float s = (минимум + место * длина - от) / (до - от);
            if (s > 0.001f && s < 0.999f && !список.Exists(x => Mathf.Abs(x - s) < 0.001f)) список.Add(s);
        }

        вершины.Clear();
        foreach (float s in разрезы)
        {
            UIVertex a = Смешать(начало1, конец1, s), b = Смешать(начало2, конец2, s);
            Color цвет = градиент.Evaluate(МестоНаГрадиенте(a.position));
            a.color = (Color)a.color * цвет;
            b.color = (Color)b.color * цвет;
            вершины.AddVert(a);
            вершины.AddVert(b);
        }

        for (int i = 0; i + 1 < разрезы.Count; i++)
        {
            int k = i * 2;
            вершины.AddTriangle(k, k + 1, k + 3);
            вершины.AddTriangle(k + 3, k + 2, k);
        }
    }

    static UIVertex Смешать(UIVertex a, UIVertex b, float t)
    {
        UIVertex результат = a;
        результат.position = Vector3.Lerp(a.position, b.position, t);
        результат.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
        результат.color = Color32.Lerp(a.color, b.color, t);
        return результат;
    }

    #endregion
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Картинка))]
internal class КартинкаEditor : RunityEditor<Картинка>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying && компонент.режим == РежимКартинки.Заполнение;

    protected override void Отрисовать()
    {
        Текст("Этот объект показывает картинку в интерфейсе. В режиме Заполнение из неё получается полоска здоровья, загрузки или опыта");

        Пробел();

        Синхрополе("Спрайт", x => x.спрайт,
            (title, value) => ПолеОбъекта<Sprite>(title, value));

        EnumField("Режим", x => x.режим, (component, value) => component.режим = value);

        if ((компонент.режим == РежимКартинки.Рамка || компонент.режим == РежимКартинки.Плитка) && компонент.оригинал.sprite != null && компонент.оригинал.sprite.border == Vector4.zero)
        {
            Текст("У спрайта не размечены края. Разметьте их в Sprite Editor, иначе картинка будет растягиваться целиком", MessageType.Warning);
        }

        Пробел();

        Поле("Окраска", x => x.окраска,
            (title, value) => (ОкраскаКартинки)EditorGUILayout.EnumPopup(title, value));

        switch (компонент.окраска)
        {
            case ОкраскаКартинки.Цвет:
                Синхрополе("Цвет", x => x.цвет,
                    (title, value) => EditorGUILayout.ColorField(title, value));
                break;

            case ОкраскаКартинки.Градиент:
                Поле("Градиент", x => x.градиент,
                    (title, value) => EditorGUILayout.GradientField(title, value));
                Поле("Направление градиента", x => x.направлениеГрадиента,
                    (title, value) => (НаправлениеГрадиента)EditorGUILayout.EnumPopup(title, value));
                break;

            case ОкраскаКартинки.ПоЗаполненности:
                Поле("Цвет по заполненности", x => x.цветПоЗаполненности,
                    (title, value) => EditorGUILayout.GradientField(title, value));
                if (компонент.режим != РежимКартинки.Заполнение)
                {
                    Текст("Цвет по заполненности работает в режиме Заполнение", MessageType.Info);
                }
                break;
        }

        if (компонент.режим == РежимКартинки.Заполнение)
        {
            Пробел();

            Поле("Показывать здоровье", x => x.источник,
                (title, value) => ПолеОбъекта<Здоровье>(title, value, true));

            // Пока картинка берёт значения у Здоровья, вручную их менять бесполезно
            if (компонент.источник == null)
            {
                Поле("Значение", x => x.значение,
                    (title, value) => EditorGUILayout.Slider(title, value, 0, компонент.максимум));

                Поле("Максимум", x => x.максимум,
                    (title, value) => EditorGUILayout.FloatField(title, value));
            }
            else
            {
                Отметить(nameof(Картинка.значение), nameof(Картинка.максимум));
            }

            Поле("Направление", x => x.направление,
                (title, value) => (НаправлениеЗаполнения)EditorGUILayout.EnumPopup(title, value));

            Поле("Плавность", x => x.плавность,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        Синхрополе("Сохранять пропорции", x => x.сохранятьПропорции,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Ловит мышь", x => x.ловитМышь,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Кнопка("Подогнать размер", x => x.ПодогнатьРазмер());

        if (компонент.режим == РежимКартинки.Заполнение)
        {
            Пробел();

            НачатьСобытия();
            ОтобразитьСобытие("При изменении", "_приИзменении");
            ОтобразитьСобытие("При заполнении", "_приЗаполнении");
            ОтобразитьСобытие("При опустошении", "_приОпустошении");
            ЗакончитьСобытия();
        }
    }
}
#endif
