using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Показывает и прячет целую группу элементов интерфейса, в том числе плавно: меню, окна, подсказки
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Группа интерфейса")]
public class ГруппаИнтерфейса : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    void Awake() => оригинальныйКомпонент = GetComponent<CanvasGroup>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public CanvasGroup оригинал => (CanvasGroup)оригинальныйКомпонент;

    /// <summary>
    /// Непрозрачность всей группы: 0 - не видно, 1 - видно полностью
    /// </summary>
    public Дробное непрозрачность
    {
        get => оригинал.alpha;
        set => оригинал.alpha = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Если "нет", то кнопки и поля группы не нажимаются
    /// </summary>
    public Флажок активна
    {
        get => оригинал.interactable;
        set => оригинал.interactable = value;
    }

    /// <summary>
    /// Если "нет", то клики мышью проходят сквозь группу
    /// </summary>
    public Флажок ловитМышь
    {
        get => оригинал.blocksRaycasts;
        set => оригинал.blocksRaycasts = value;
    }

    /// <summary>
    /// Даёт "да", если группа видна
    /// </summary>
    public Флажок видна => оригинал.alpha > 0;

    [SerializeField] Дробное _времяПерехода = 0.3;
    /// <summary>
    /// За сколько секунд группа плавно появляется или исчезает
    /// </summary>
    public Дробное времяПерехода { get => _времяПерехода; set => _времяПерехода = value; }

    [SerializeField] События _приПоявлении;
    /// <summary>
    /// Срабатывает, когда группа полностью появилась
    /// </summary>
    public События приПоявлении => _приПоявлении;

    [SerializeField] События _приИсчезновении;
    /// <summary>
    /// Срабатывает, когда группа полностью исчезла
    /// </summary>
    public События приИсчезновении => _приИсчезновении;

    /// <summary>
    /// Сразу показывает группу
    /// </summary>
    public void Показать() => Установить(1, да);

    /// <summary>
    /// Сразу прячет группу
    /// </summary>
    public void Спрятать() => Установить(0, да);

    /// <summary>
    /// Плавно показывает группу за Время перехода
    /// </summary>
    public void ПлавноПоказать() => Перейти(1);

    /// <summary>
    /// Плавно прячет группу за Время перехода
    /// </summary>
    public void ПлавноСпрятать() => Перейти(0);

    /// <summary>
    /// Плавно прячет группу, если она видна, и показывает, если спрятана
    /// </summary>
    public void Переключить() => Перейти(видна ? 0 : 1);

    void Перейти(float цель)
    {
        StopAllCoroutines();
        if (!isActiveAndEnabled || времяПерехода <= 0)
        {
            Установить(цель, да);
            return;
        }
        StartCoroutine(Переход(цель));
    }

    IEnumerator Переход(float цель)
    {
        // Во время паузы игры меню тоже должно появляться, поэтому время берётся без учёта скорости игры
        float начало = оригинал.alpha;
        for (float прошло = 0; прошло < времяПерехода; прошло += Время.времяСПрошлогоКадраБезУчётаСкорости)
        {
            Установить(Mathf.Lerp(начало, цель, прошло / времяПерехода), нет);
            yield return null;
        }
        Установить(цель, да);
    }

    void Установить(float значение, bool конец)
    {
        оригинал.alpha = значение;
        bool видимая = значение > 0;
        оригинал.interactable = видимая;
        оригинал.blocksRaycasts = видимая;

        if (!конец) return;
        if (значение >= 1) _приПоявлении.Вызвать();
        if (значение <= 0) _приИсчезновении.Вызвать();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ГруппаИнтерфейса))]
internal class ГруппаИнтерфейсаEditor : RunityEditor<ГруппаИнтерфейса>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект показывает и прячет всё, что лежит внутри него. Удобно для меню и окон");

        Пробел();

        Синхрополе("Непрозрачность", x => x.непрозрачность,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        Синхрополе("Активна", x => x.активна,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Ловит мышь", x => x.ловитМышь,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Время перехода", x => x.времяПерехода,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        Кнопка("Показать", x => x.Показать());
        Кнопка("Спрятать", x => x.Спрятать());
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При появлении", "_приПоявлении");
        ОтобразитьСобытие("При исчезновении", "_приИсчезновении");
        ЗакончитьСобытия();
    }
}
#endif
