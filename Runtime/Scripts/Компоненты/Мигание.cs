using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Заставляет объект мигать: после урона, перед исчезновением или чтобы привлечь внимание
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Эффекты/Мигание")]
public class Мигание : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() => ПоказатьВсё(да);

    [SerializeField] Дробное _частота = 10;
    /// <summary>
    /// Сколько раз в секунду объект пропадает и появляется
    /// </summary>
    public Дробное частота { get => _частота; set => _частота = value; }

    [SerializeField] Дробное _длительность = 1;
    /// <summary>
    /// Сколько секунд длится мигание, если запустить его без указания времени
    /// </summary>
    public Дробное длительность { get => _длительность; set => _длительность = value; }

    [SerializeField] Флажок _бессмертиеВоВремяМигания = нет;
    /// <summary>
    /// Если "да", то пока объект мигает, его Здоровье не уменьшается. Классика для игроков после удара
    /// </summary>
    public Флажок бессмертиеВоВремяМигания { get => _бессмертиеВоВремяМигания; set => _бессмертиеВоВремяМигания = value; }

    [SerializeField] Флажок _мигатьПостоянно = нет;
    /// <summary>
    /// Если "да", то объект мигает всё время, пока компонент включён
    /// </summary>
    public Флажок мигатьПостоянно { get => _мигатьПостоянно; set => _мигатьПостоянно = value; }

    [SerializeField] События _приМиге;
    /// <summary>
    /// Срабатывает каждый раз, когда объект пропадает во время мигания
    /// </summary>
    public События приМиге => _приМиге;

    [SerializeField] События _приОкончании;
    /// <summary>
    /// Срабатывает, когда мигание закончилось
    /// </summary>
    public События приОкончании => _приОкончании;

    /// <summary>
    /// Даёт "да", пока объект мигает
    /// </summary>
    public Флажок мигает { get; private set; }

    Coroutine процесс;

    void Update()
    {
        if (мигатьПостоянно && !мигает) Мигнуть(float.PositiveInfinity);
        if (!мигатьПостоянно && мигает && float.IsPositiveInfinity(осталось)) Перестать();
    }

    float осталось;

    /// <summary>
    /// Мигает в течение Длительности. Удобно подключать к событию "При уроне"
    /// </summary>
    public void Мигнуть() => Мигнуть(длительность);

    /// <summary>
    /// Мигает указанное количество секунд
    /// </summary>
    public void Мигнуть(Дробное время)
    {
        if (процесс != null) StopCoroutine(процесс);
        процесс = StartCoroutine(Мигать(время));
    }

    /// <summary>
    /// Сразу прекращает мигание и показывает объект
    /// </summary>
    public void Перестать()
    {
        if (процесс != null) StopCoroutine(процесс);
        Закончить();
    }

    IEnumerator Мигать(float время)
    {
        мигает = да;
        осталось = время;
        if (бессмертиеВоВремяМигания && TryGetComponent(out Здоровье здоровье)) здоровье.бессмертие = да;

        bool видно = нет;
        float таймер = 0;

        while (осталось > 0)
        {
            float шаг = 0.5f / Mathf.Max(0.1f, частота);
            if (таймер <= 0)
            {
                ПоказатьВсё(видно);
                if (!видно) _приМиге.Вызвать();
                видно = !видно;
                таймер = шаг;
            }

            yield return null;
            таймер -= Время.времяСПрошлогоКадра;
            осталось -= Время.времяСПрошлогоКадра;
        }

        Закончить();
    }

    void Закончить()
    {
        bool былоМигание = мигает;
        мигает = нет;
        процесс = null;
        ПоказатьВсё(да);
        if (бессмертиеВоВремяМигания && TryGetComponent(out Здоровье здоровье)) здоровье.бессмертие = нет;
        if (былоМигание) _приОкончании.Вызвать();
    }

    void ПоказатьВсё(bool видно)
    {
        foreach (Renderer отрисовщик in GetComponentsInChildren<Renderer>())
        {
            отрисовщик.enabled = видно;
        }
        foreach (CanvasGroup группа in GetComponentsInChildren<CanvasGroup>())
        {
            группа.alpha = видно ? 1 : 0;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Мигание))]
internal class МиганиеEditor : RunityEditor<Мигание>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект умеет мигать. Подключите «Мигнуть» к событию «При уроне» у Здоровья, и игрок будет мигать после удара");

        Пробел();

        Поле("Частота", x => x.частота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Длительность", x => x.длительность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Бессмертие во время мигания", x => x.бессмертиеВоВремяМигания,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Мигать постоянно", x => x.мигатьПостоянно,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При миге", "_приМиге");
        ОтобразитьСобытие("При окончании", "_приОкончании");
        ЗакончитьСобытия();
    }
}
#endif
