using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Перестаёт рисовать объект, когда он далеко от камеры, и снова показывает, когда камера рядом. Ускоряет большие уровни
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал/ Оптимизация/Скрытие вдали")]
public class СкрытиеВдали : RunityComponent
{
    protected override void OnEnable()
    {
        все.Add(this);
        Найти();
    }

    protected override void OnDisable()
    {
        все.Remove(this);
        Показать(true);
    }

    [SerializeField] Дробное _дальность = 60;
    /// <summary>
    /// Дальше этого расстояния от камеры объект не рисуется
    /// </summary>
    public Дробное дальность { get => _дальность; set => _дальность = value; }

    [SerializeField] Флажок _вместеСДетьми = да;
    /// <summary>
    /// Если "да", то прячутся и все объекты внутри этого
    /// </summary>
    public Флажок вместеСДетьми { get => _вместеСДетьми; set { _вместеСДетьми = value; Найти(); } }

    [SerializeField] Флажок _выключатьПоведение = нет;
    /// <summary>
    /// Если "да", то вдали выключаются и компоненты с поведением, например движение и анимация. Объект совсем ничего не делает, пока его не видно
    /// </summary>
    public Флажок выключатьПоведение { get => _выключатьПоведение; set => _выключатьПоведение = value; }

    [SerializeField] Transform _камера;
    /// <summary>
    /// От чего считать расстояние. Если не указать, берётся главная камера
    /// </summary>
    public Transform камера { get => _камера; set => _камера = value; }

    [SerializeField] События _приСкрытии;
    /// <summary>
    /// Срабатывает, когда объект ушёл за дальность и спрятался
    /// </summary>
    public События приСкрытии => _приСкрытии;

    [SerializeField] События _приПоявлении;
    /// <summary>
    /// Срабатывает, когда объект снова оказался ближе дальности
    /// </summary>
    public События приПоявлении => _приПоявлении;

    /// <summary>
    /// Даёт "да", пока объект спрятан
    /// </summary>
    public Флажок скрыт { get; private set; }

    static readonly List<СкрытиеВдали> все = new List<СкрытиеВдали>();
    static int следующий;

    readonly List<Renderer> отрисовщики = new List<Renderer>();
    readonly List<Behaviour> поведения = new List<Behaviour>();

    // Каждый кадр проверяется только часть объектов, чтобы тысячи объектов не тормозили игру
    const int заКадр = 200;

    void Найти()
    {
        отрисовщики.Clear();
        поведения.Clear();
        if (вместеСДетьми) GetComponentsInChildren(true, отрисовщики);
        else GetComponents(отрисовщики);

        foreach (Behaviour поведение in вместеСДетьми ? GetComponentsInChildren<Behaviour>(true) : GetComponents<Behaviour>())
        {
            if (поведение == this || поведение is Camera || поведение is AudioListener || поведение is RunityComponent) continue;
            поведения.Add(поведение);
        }
    }

    void Показать(bool видно)
    {
        if (скрыт == !видно) return;
        скрыт = !видно;

        foreach (Renderer отрисовщик in отрисовщики) if (отрисовщик != null) отрисовщик.forceRenderingOff = !видно;
        if (выключатьПоведение) foreach (Behaviour поведение in поведения) if (поведение != null) поведение.enabled = видно;

        if (видно) _приПоявлении.Вызвать();
        else _приСкрытии.Вызвать();
    }

    void Проверить(Vector3 откуда)
    {
        Vector3 где = transform.position;
        float расстояние = дальность;
        Показать((где - откуда).sqrMagnitude <= расстояние * расстояние);
    }

    void LateUpdate()
    {
        // Проверку делает только первый объект из списка, сразу за всех
        if (все.Count == 0 || все[0] != this) return;

        Transform главная = Camera.main != null ? Camera.main.transform : null;
        int сколько = Mathf.Min(заКадр, все.Count);
        for (int i = 0; i < сколько; i++)
        {
            следующий = (следующий + 1) % все.Count;
            СкрытиеВдали объект = все[следующий];
            Transform откуда = объект.камера != null ? объект.камера : главная;
            if (откуда != null) объект.Проверить(откуда.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.6f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, дальность);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СкрытиеВдали))]
internal class СкрытиеВдалиEditor : RunityEditor<СкрытиеВдали>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект не рисуется, когда камера дальше указанного расстояния. Оранжевая сфера в сцене показывает дальность");

        Пробел();

        Поле("Дальность", x => x.дальность, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Вместе с детьми", x => x.вместеСДетьми, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Выключать поведение", x => x.выключатьПоведение, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Камера", x => x.камера, (title, value) => ПолеОбъекта<Transform>(title, value, true));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При скрытии", "_приСкрытии");
        ОтобразитьСобытие("При появлении", "_приПоявлении");
        ЗакончитьСобытия();
    }
}
#endif
