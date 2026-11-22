using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Готовый вид эффектов экрана. «Свой» - ничего не меняет, остальные сразу настраивают все эффекты
/// </summary>
public enum ВидЭффектов { Свой, Обычный, Кино, Сказка, Ужасы, Ретро, Сон, ЧёрноБелое }

/// <summary>
/// Тонирование: как яркие цвета укладываются в экран
/// </summary>
public enum Тонирование { Нет, Нейтральное, Кино }

/// <summary>
/// Эффекты экрана (постобработка): свечение ярких мест, затемнение краёв, цвет, размытие, зерно плёнки и другие.
/// Действует на всю картинку камеры. Можно выбрать готовый вид или настроить каждый эффект
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
[AddComponentMenu("  Runity/Эффекты Экрана")]
public class ЭффектыЭкрана : RunityComponent
{
    protected override void OnEnable()
    {
        оригинальныйКомпонент = GetComponent<Volume>();
        base.OnEnable();
        Применить();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (настройки != null) DestroyImmediate(настройки);
        настройки = null;
    }

    void OnValidate()
    {
        if (isActiveAndEnabled) Применить();
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Volume оригинал => (Volume)оригинальныйКомпонент;

    // Настройки Unity собираются заново из полей этого компонента, поэтому их не нужно хранить отдельным ассетом
    VolumeProfile настройки;

    [SerializeField] ВидЭффектов _вид = ВидЭффектов.Обычный;
    /// <summary>
    /// Готовый вид эффектов. При выборе сразу меняет настройки всех эффектов
    /// </summary>
    public ВидЭффектов вид { get => _вид; set { _вид = value; ЗадатьВид(value); Применить(); } }

    [SerializeField, Диапазон(0, 1)] float _сила = 1;
    /// <summary>
    /// Насколько сильно действуют все эффекты сразу: 0 - не действуют, 1 - полностью
    /// </summary>
    public Дробное сила { get => _сила; set { _сила = Mathf.Clamp01(value); Применить(); } }

    [SerializeField] VolumeProfile _готовыйПрофиль;
    /// <summary>
    /// Если указан профиль эффектов Unity (Volume Profile), используется он, а настройки ниже не действуют
    /// </summary>
    public VolumeProfile готовыйПрофиль { get => _готовыйПрофиль; set { _готовыйПрофиль = value; Применить(); } }

    [SerializeField] int _приоритет;
    /// <summary>
    /// Если на сцене несколько эффектов экрана, побеждает тот, у кого приоритет больше
    /// </summary>
    public Число приоритет { get => _приоритет; set { _приоритет = value; Применить(); } }

    // Свечение
    [SerializeField] bool _свечение = true;
    /// <summary>
    /// Яркие места светятся и мягко расплываются: огонь, лампы, магия
    /// </summary>
    public Флажок свечение { get => _свечение; set { _свечение = value; Применить(); } }
    [SerializeField, Диапазон(0, 5)] float _силаСвечения = 0.6f;
    /// <summary>
    /// Насколько сильно светятся яркие места
    /// </summary>
    public Дробное силаСвечения { get => _силаСвечения; set { _силаСвечения = value; Применить(); } }
    [SerializeField, Диапазон(0, 3)] float _порогСвечения = 0.9f;
    /// <summary>
    /// С какой яркости места начинают светиться. Меньше - светится больше мест
    /// </summary>
    public Дробное порогСвечения { get => _порогСвечения; set { _порогСвечения = value; Применить(); } }
    [SerializeField, Диапазон(0, 1)] float _размерСвечения = 0.7f;
    /// <summary>
    /// Насколько далеко расплывается свечение
    /// </summary>
    public Дробное размерСвечения { get => _размерСвечения; set { _размерСвечения = value; Применить(); } }
    [SerializeField] Color _цветСвечения = Color.white;
    /// <summary>
    /// Оттенок свечения
    /// </summary>
    public Color цветСвечения { get => _цветСвечения; set { _цветСвечения = value; Применить(); } }

    // Виньетка
    [SerializeField] bool _затемнениеКраёв = true;
    /// <summary>
    /// Края экрана темнее середины - взгляд сам идёт в центр
    /// </summary>
    public Флажок затемнениеКраёв { get => _затемнениеКраёв; set { _затемнениеКраёв = value; Применить(); } }
    [SerializeField, Диапазон(0, 1)] float _силаЗатемнения = 0.25f;
    /// <summary>
    /// Насколько темнеют края
    /// </summary>
    public Дробное силаЗатемнения { get => _силаЗатемнения; set { _силаЗатемнения = value; Применить(); } }
    [SerializeField, Диапазон(0.01f, 1)] float _мягкостьЗатемнения = 0.4f;
    /// <summary>
    /// Насколько плавно затемнение переходит в середину
    /// </summary>
    public Дробное мягкостьЗатемнения { get => _мягкостьЗатемнения; set { _мягкостьЗатемнения = value; Применить(); } }
    [SerializeField] Color _цветЗатемнения = Color.black;
    /// <summary>
    /// Цвет краёв. Красный - хорошо для получения урона
    /// </summary>
    public Color цветЗатемнения { get => _цветЗатемнения; set { _цветЗатемнения = value; Применить(); } }

    // Цвет
    [SerializeField, Диапазон(-3, 3)] float _яркость;
    /// <summary>
    /// Яркость всей картинки: 0 - как есть, больше - светлее, меньше - темнее
    /// </summary>
    public Дробное яркость { get => _яркость; set { _яркость = value; Применить(); } }
    [SerializeField, Диапазон(-100, 100)] float _контраст = 10;
    /// <summary>
    /// Контраст: больше - тёмное темнее, а светлое светлее
    /// </summary>
    public Дробное контраст { get => _контраст; set { _контраст = value; Применить(); } }
    [SerializeField, Диапазон(-100, 100)] float _насыщенность = 10;
    /// <summary>
    /// Насыщенность цветов: -100 - чёрно-белое, 0 - как есть, больше - сочнее
    /// </summary>
    public Дробное насыщенность { get => _насыщенность; set { _насыщенность = value; Применить(); } }
    [SerializeField, Диапазон(-180, 180)] float _сдвигОттенка;
    /// <summary>
    /// Сдвигает все цвета по кругу оттенков
    /// </summary>
    public Дробное сдвигОттенка { get => _сдвигОттенка; set { _сдвигОттенка = value; Применить(); } }
    [SerializeField] Color _фильтрЦвета = Color.white;
    /// <summary>
    /// Картинка будто видна через цветное стекло. Белый - без фильтра
    /// </summary>
    public Color фильтрЦвета { get => _фильтрЦвета; set { _фильтрЦвета = value; Применить(); } }
    [SerializeField, Диапазон(-100, 100)] float _теплота;
    /// <summary>
    /// Баланс белого: больше - теплее (жёлтые тона), меньше - холоднее (синие)
    /// </summary>
    public Дробное теплота { get => _теплота; set { _теплота = value; Применить(); } }
    [SerializeField] Тонирование _тонирование = Тонирование.Нейтральное;
    /// <summary>
    /// Как очень яркие цвета укладываются в экран. «Кино» - насыщенно и контрастно, как в фильмах
    /// </summary>
    public Тонирование тонирование { get => _тонирование; set { _тонирование = value; Применить(); } }

    // Размытие и плёнка
    [SerializeField] bool _размытиеДали;
    /// <summary>
    /// Дальние объекты размываются, как в фотоаппарате
    /// </summary>
    public Флажок размытиеДали { get => _размытиеДали; set { _размытиеДали = value; Применить(); } }
    [SerializeField, Минимум(0)] float _началоРазмытия = 15;
    /// <summary>
    /// С какого расстояния от камеры начинается размытие
    /// </summary>
    public Дробное началоРазмытия { get => _началоРазмытия; set { _началоРазмытия = value; Применить(); } }
    [SerializeField, Минимум(0)] float _конецРазмытия = 60;
    /// <summary>
    /// На каком расстоянии размытие становится полным
    /// </summary>
    public Дробное конецРазмытия { get => _конецРазмытия; set { _конецРазмытия = value; Применить(); } }
    [SerializeField, Диапазон(0, 1)] float _размытиеДвижения;
    /// <summary>
    /// Смазывание картинки при быстром повороте камеры. 0 - нет
    /// </summary>
    public Дробное размытиеДвижения { get => _размытиеДвижения; set { _размытиеДвижения = value; Применить(); } }
    [SerializeField, Диапазон(0, 1)] float _расслоениеЦвета;
    /// <summary>
    /// По краям экрана цвета слегка расходятся, как в дешёвом объективе. 0 - нет
    /// </summary>
    public Дробное расслоениеЦвета { get => _расслоениеЦвета; set { _расслоениеЦвета = value; Применить(); } }
    [SerializeField, Диапазон(0, 1)] float _зерно;
    /// <summary>
    /// Шум, как на старой плёнке. 0 - нет
    /// </summary>
    public Дробное зерно { get => _зерно; set { _зерно = value; Применить(); } }
    [SerializeField, Диапазон(-1, 1)] float _искажениеЛинзы;
    /// <summary>
    /// Выпуклость картинки: больше нуля - «рыбий глаз», меньше - вогнутость. 0 - нет
    /// </summary>
    public Дробное искажениеЛинзы { get => _искажениеЛинзы; set { _искажениеЛинзы = value; Применить(); } }

    /// <summary>
    /// Плавно меняет силу всех эффектов за указанное время в секундах. Удобно для вспышек, оглушения и переходов
    /// </summary>
    public void ИзменитьСилу(Дробное новаяСила, Дробное время)
    {
        StopAllCoroutines();
        if (!Application.isPlaying || время <= 0) { сила = новаяСила; return; }
        StartCoroutine(Изменять(новаяСила, время));
    }

    /// <summary>
    /// Ненадолго окрашивает края экрана: например, красным при получении урона
    /// </summary>
    public void Вспышка(Color цвет, Дробное время)
    {
        if (!Application.isPlaying) return;
        StartCoroutine(Вспыхнуть(цвет, время));
    }

    /// <summary>
    /// Края экрана на мгновение вспыхивают красным - удобно подключить к событию «При уроне» у Здоровья
    /// </summary>
    public void ВспышкаУрона() => Вспышка(new Color(0.75f, 0, 0), 0.45f);

    IEnumerator Изменять(float до, float время)
    {
        float от = _сила;
        for (float прошло = 0; прошло < время; прошло += Time.deltaTime)
        {
            сила = Mathf.Lerp(от, до, прошло / время);
            yield return null;
        }
        сила = до;
    }

    IEnumerator Вспыхнуть(Color цвет, float время)
    {
        bool былоВключено = _затемнениеКраёв;
        float былаСила = _силаЗатемнения;
        Color былЦвет = _цветЗатемнения;
        _затемнениеКраёв = true;
        _цветЗатемнения = цвет;
        for (float прошло = 0; прошло < время; прошло += Time.deltaTime)
        {
            _силаЗатемнения = Mathf.Lerp(0.55f, былоВключено ? былаСила : 0, прошло / время);
            Применить();
            yield return null;
        }
        _затемнениеКраёв = былоВключено;
        _силаЗатемнения = былаСила;
        _цветЗатемнения = былЦвет;
        Применить();
    }

    void ЗадатьВид(ВидЭффектов какой)
    {
        if (какой == ВидЭффектов.Свой) return;

        // Сначала всё обычное, потом отличия вида
        _свечение = true; _силаЗатемнения = 0.25f; _порогСвечения = 0.9f; _размерСвечения = 0.7f; _цветСвечения = Color.white; _силаСвечения = 0.6f;
        _затемнениеКраёв = true; _мягкостьЗатемнения = 0.4f; _цветЗатемнения = Color.black;
        _яркость = 0; _контраст = 10; _насыщенность = 10; _сдвигОттенка = 0; _фильтрЦвета = Color.white; _теплота = 0; _тонирование = Тонирование.Нейтральное;
        _размытиеДали = false; _размытиеДвижения = 0; _расслоениеЦвета = 0; _зерно = 0; _искажениеЛинзы = 0;

        switch (какой)
        {
            case ВидЭффектов.Кино:
                _тонирование = Тонирование.Кино; _контраст = 20; _насыщенность = 0; _силаЗатемнения = 0.35f; _зерно = 0.15f; _теплота = 8; _силаСвечения = 0.8f;
                break;
            case ВидЭффектов.Сказка:
                _силаСвечения = 1.5f; _порогСвечения = 0.7f; _насыщенность = 30; _контраст = 5; _теплота = 12; _цветСвечения = new Color(1, 0.9f, 1); _силаЗатемнения = 0.2f;
                break;
            case ВидЭффектов.Ужасы:
                _насыщенность = -60; _контраст = 35; _яркость = -0.4f; _силаЗатемнения = 0.55f; _зерно = 0.45f; _расслоениеЦвета = 0.25f; _теплота = -25; _фильтрЦвета = new Color(0.85f, 1, 0.9f);
                break;
            case ВидЭффектов.Ретро:
                _насыщенность = -25; _контраст = 15; _теплота = 30; _фильтрЦвета = new Color(1, 0.92f, 0.8f); _зерно = 0.5f; _силаЗатемнения = 0.45f; _искажениеЛинзы = 0.15f; _расслоениеЦвета = 0.15f;
                break;
            case ВидЭффектов.Сон:
                _силаСвечения = 2.5f; _порогСвечения = 0.5f; _размерСвечения = 0.9f; _насыщенность = -15; _контраст = -15; _сдвигОттенка = 15; _размытиеДали = true; _началоРазмытия = 5; _конецРазмытия = 25; _цветЗатемнения = new Color(0.3f, 0.2f, 0.5f); _силаЗатемнения = 0.4f;
                break;
            case ВидЭффектов.ЧёрноБелое:
                _насыщенность = -100; _контраст = 25; _зерно = 0.25f; _силаЗатемнения = 0.35f;
                break;
        }
    }

    /// <summary>
    /// Переносит настройки в эффекты Unity. Вызывается само при любом изменении
    /// </summary>
    public void Применить()
    {
        Volume объём = оригинал != null ? оригинал : GetComponent<Volume>();
        if (объём == null) return;
        оригинальныйКомпонент = объём;
        объём.isGlobal = true;
        объём.weight = _сила;
        объём.priority = _приоритет;
        ВключитьПостобработкуКамеры();

        if (_готовыйПрофиль != null)
        {
            объём.sharedProfile = _готовыйПрофиль;
            return;
        }

        if (настройки == null)
        {
            настройки = ScriptableObject.CreateInstance<VolumeProfile>();
            настройки.name = "Эффекты экрана";
            настройки.hideFlags = HideFlags.DontSave;
        }
        объём.sharedProfile = настройки;

        Bloom свет = Эффект<Bloom>();
        свет.active = _свечение;
        свет.intensity.Override(_силаСвечения);
        свет.threshold.Override(_порогСвечения);
        свет.scatter.Override(_размерСвечения);
        свет.tint.Override(_цветСвечения);

        Vignette края = Эффект<Vignette>();
        края.active = _затемнениеКраёв;
        края.intensity.Override(_силаЗатемнения);
        края.smoothness.Override(_мягкостьЗатемнения);
        края.color.Override(_цветЗатемнения);

        ColorAdjustments цвет = Эффект<ColorAdjustments>();
        цвет.postExposure.Override(_яркость);
        цвет.contrast.Override(_контраст);
        цвет.saturation.Override(_насыщенность);
        цвет.hueShift.Override(_сдвигОттенка);
        цвет.colorFilter.Override(_фильтрЦвета);

        WhiteBalance белый = Эффект<WhiteBalance>();
        белый.active = _теплота != 0;
        белый.temperature.Override(_теплота);

        Tonemapping тон = Эффект<Tonemapping>();
        тон.mode.Override(_тонирование switch { Тонирование.Кино => TonemappingMode.ACES, Тонирование.Нейтральное => TonemappingMode.Neutral, _ => TonemappingMode.None });

        DepthOfField даль = Эффект<DepthOfField>();
        даль.active = _размытиеДали;
        даль.mode.Override(DepthOfFieldMode.Gaussian);
        даль.gaussianStart.Override(_началоРазмытия);
        даль.gaussianEnd.Override(Mathf.Max(_началоРазмытия + 0.01f, _конецРазмытия));

        MotionBlur движение = Эффект<MotionBlur>();
        движение.active = _размытиеДвижения > 0;
        движение.intensity.Override(_размытиеДвижения);

        ChromaticAberration расслоение = Эффект<ChromaticAberration>();
        расслоение.active = _расслоениеЦвета > 0;
        расслоение.intensity.Override(_расслоениеЦвета);

        FilmGrain плёнка = Эффект<FilmGrain>();
        плёнка.active = _зерно > 0;
        плёнка.intensity.Override(_зерно);

        LensDistortion линза = Эффект<LensDistortion>();
        линза.active = _искажениеЛинзы != 0;
        линза.intensity.Override(_искажениеЛинзы);
    }

    T Эффект<T>() where T : VolumeComponent
    {
        if (!настройки.TryGet(out T эффект)) эффект = настройки.Add<T>();
        return эффект;
    }

    // Без постобработки у камеры эффекты не видны, поэтому включаем её сами
    static void ВключитьПостобработкуКамеры()
    {
        Camera камера = Camera.main;
        if (камера == null || !камера.TryGetComponent(out UniversalAdditionalCameraData данные) || данные.renderPostProcessing) return;
        данные.renderPostProcessing = true;
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(данные);
#endif
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ЭффектыЭкрана))]
internal class ЭффектыЭкранаEditor : RunityEditor<ЭффектыЭкрана>
{
    protected override void Отрисовать()
    {
        Текст("Эффекты действуют на всю картинку камеры. Выберите готовый вид или настройте эффекты сами. Тонкие настройки - в «Прочее»");

        Синхрополе("Вид", x => x.вид,
            (title, value) => (ВидЭффектов)EditorGUILayout.EnumPopup(title, value));
        Синхрополе("Сила", x => x.сила,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        if (компонент.готовыйПрофиль != null)
        {
            Текст("Используется готовый профиль эффектов Unity, настройки ниже не действуют", MessageType.Warning);
            return;
        }

        Пробел();
        Синхрополе("Свечение", x => x.свечение,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.свечение)
            Синхрополе("Сила свечения", x => x.силаСвечения,
                (title, value) => EditorGUILayout.Slider(title, value, 0, 5));
        Синхрополе("Затемнение краёв", x => x.затемнениеКраёв,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.затемнениеКраёв)
            Синхрополе("Сила затемнения", x => x.силаЗатемнения,
                (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        Пробел();
        Синхрополе("Яркость", x => x.яркость,
            (title, value) => EditorGUILayout.Slider(title, value, -3, 3));
        Синхрополе("Контраст", x => x.контраст,
            (title, value) => EditorGUILayout.Slider(title, value, -100, 100));
        Синхрополе("Насыщенность", x => x.насыщенность,
            (title, value) => EditorGUILayout.Slider(title, value, -100, 100));
        Синхрополе("Тонирование", x => x.тонирование,
            (title, value) => (Тонирование)EditorGUILayout.EnumPopup(title, value));
    }
}
#endif
