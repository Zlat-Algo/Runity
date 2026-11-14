using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Срабатывает, когда кто-то заходит в объект или выходит из него. Подходит для порталов, ловушек, нажимных плит и зон, где что-то происходит
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Касание")]
public class Касание : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable()
    {
        // Выключенное касание больше никого не держит внутри
        foreach (GameObject объект in new List<GameObject>(внутриОбъекты)) Выход(объект);
    }

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// Чьё касание считается. Если теги не выбраны, то любого объекта
    /// </summary>
    public Теги теги { get => _теги; set => _теги = value ?? new Теги(); }

    [SerializeField] Флажок _толькоОдинРаз = нет;
    /// <summary>
    /// Если "да", то касание срабатывает только при первом входе
    /// </summary>
    public Флажок толькоОдинРаз { get => _толькоОдинРаз; set => _толькоОдинРаз = value; }

    [SerializeField] События _приВходе;
    /// <summary>
    /// Срабатывает, когда подходящий объект зашёл внутрь
    /// </summary>
    public События приВходе => _приВходе;

    [SerializeField] События _приВыходе;
    /// <summary>
    /// Срабатывает, когда подходящий объект вышел наружу
    /// </summary>
    public События приВыходе => _приВыходе;

    /// <summary>
    /// Даёт "да", пока внутри есть хотя бы один подходящий объект
    /// </summary>
    public Флажок внутри => внутриОбъекты.Count > 0;

    /// <summary>
    /// Сколько подходящих объектов сейчас внутри
    /// </summary>
    public Число сколькоВнутри => внутриОбъекты.Count;

    /// <summary>
    /// Кто последним зашёл внутрь
    /// </summary>
    public GameObject последний { get; private set; }

    /// <summary>
    /// Сколько раз срабатывал вход
    /// </summary>
    public Число входов { get; private set; }

    readonly List<GameObject> внутриОбъекты = new List<GameObject>();

    void Reset()
    {
        // Касание работает только с триггером: через обычный коллайдер нельзя пройти
        if (TryGetComponent(out Collider коллайдер)) коллайдер.isTrigger = true;
        if (TryGetComponent(out Collider2D коллайдер2D)) коллайдер2D.isTrigger = true;
    }

    void Вход(GameObject объект)
    {
        if (!enabled || !теги.Подходит(объект) || внутриОбъекты.Contains(объект)) return;
        if (толькоОдинРаз && входов > 0) return;

        внутриОбъекты.Add(объект);
        последний = объект;
        входов = (int)входов + 1;
        _приВходе.Вызвать();
    }

    void Выход(GameObject объект)
    {
        if (!внутриОбъекты.Remove(объект)) return;
        _приВыходе.Вызвать();
    }

    void Update()
    {
        // Удалённые и выключенные объекты не сообщают о выходе, поэтому проверяем их сами
        for (int i = внутриОбъекты.Count - 1; i >= 0; i--)
        {
            GameObject объект = внутриОбъекты[i];
            if (объект == null || !объект.activeInHierarchy)
            {
                внутриОбъекты.RemoveAt(i);
                _приВыходе.Вызвать();
            }
        }
    }

    void OnTriggerEnter(Collider другой) => Вход(Владелец(другой.transform, другой.attachedRigidbody));
    void OnTriggerExit(Collider другой) => Выход(Владелец(другой.transform, другой.attachedRigidbody));
    void OnTriggerEnter2D(Collider2D другой) => Вход(другой.attachedRigidbody != null ? другой.attachedRigidbody.gameObject : другой.gameObject);
    void OnTriggerExit2D(Collider2D другой) => Выход(другой.attachedRigidbody != null ? другой.attachedRigidbody.gameObject : другой.gameObject);

    // Если у объекта несколько коллайдеров на детях, считаем его одним целым
    static GameObject Владелец(Transform коллайдер, Rigidbody тело) => тело != null ? тело.gameObject : коллайдер.gameObject;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Касание))]
internal class КасаниеEditor : RunityEditor<Касание>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект замечает, когда в него заходят и когда из него выходят");

        Collider коллайдер = компонент.GetComponent<Collider>();
        Collider2D коллайдер2D = компонент.GetComponent<Collider2D>();
        if (коллайдер == null && коллайдер2D == null)
        {
            Текст("Добавьте объекту коллайдер, например Коллайдер куб", MessageType.Error);
        }
        else if ((коллайдер != null && !коллайдер.isTrigger) || (коллайдер2D != null && !коллайдер2D.isTrigger))
        {
            Текст("Коллайдер не пропускает внутрь. Включите у него «Триггер» - или нажмите кнопку", MessageType.Warning);
            if (GUILayout.Button("Сделать триггером"))
            {
                if (коллайдер != null) { Undo.RecordObject(коллайдер, "Триггер"); коллайдер.isTrigger = true; }
                if (коллайдер2D != null) { Undo.RecordObject(коллайдер2D, "Триггер"); коллайдер2D.isTrigger = true; }
            }
        }

        Пробел();

        ОтобразитьТеги("Теги", "_теги");
        Поле("Только один раз", x => x.толькоОдинРаз, (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При входе", "_приВходе");
        ОтобразитьСобытие("При выходе", "_приВыходе");
        ЗакончитьСобытия();

        if (Application.isPlaying)
        {
            Пробел();
            Текст(компонент.внутри ? $"Внутри: {компонент.сколькоВнутри}" : "Внутри никого нет");
        }
    }
}
#endif
