#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Засчитывает цель задания: при касании, из события или кода. Например, ставится на точку, до которой надо дойти, или на врага
/// </summary>
[AddComponentMenu("  Runity/ Механики/ Задания/Отметка задания")]
public class ОтметкаЗадания : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Строка _метка = "";
    /// <summary>
    /// Метка цели, которую засчитать. Она должна совпадать с меткой в цели задания
    /// </summary>
    public Строка метка { get => _метка; set => _метка = value; }

    [SerializeField] Задание _выдатьЗадание;
    /// <summary>
    /// Если указать задание, то вместо отметки оно будет выдано игроку
    /// </summary>
    public Задание выдатьЗадание { get => _выдатьЗадание; set => _выдатьЗадание = value; }

    [SerializeField] Флажок _приКасании = нет;
    /// <summary>
    /// Если "да", то отметка срабатывает, когда в зону объекта (триггер) входит объект с подходящим тегом
    /// </summary>
    public Флажок приКасании { get => _приКасании; set => _приКасании = value; }

    [SerializeField] Теги _теги = new Теги("Player");
    /// <summary>
    /// Кто может сработать касанием
    /// </summary>
    public Теги теги { get => _теги; set => _теги = value; }

    [SerializeField] Флажок _приУдалении = нет;
    /// <summary>
    /// Если "да", то отметка срабатывает, когда объект удаляется. Подходит для врагов: «победить 3 врагов»
    /// </summary>
    public Флажок приУдалении { get => _приУдалении; set => _приУдалении = value; }

    [SerializeField] Флажок _толькоОдинРаз = да;
    /// <summary>
    /// Если "да", то отметка срабатывает только один раз
    /// </summary>
    public Флажок толькоОдинРаз { get => _толькоОдинРаз; set => _толькоОдинРаз = value; }

    bool сработала;
    bool выходИзИгры;

    /// <summary>
    /// Засчитывает цель один раз или выдаёт задание
    /// </summary>
    public void Отметить()
    {
        if (толькоОдинРаз && сработала) return;

        ЖурналЗаданий журнал = ЖурналЗаданий.главный;
        if (журнал == null)
        {
            Консоль.НапечататьПредупреждение("На сцене нет Журнала заданий", this);
            return;
        }

        сработала = true;
        if (выдатьЗадание != null) журнал.Взять(выдатьЗадание);
        else журнал.Отметить(метка);
    }

    void OnTriggerEnter(Collider другой)
    {
        if (приКасании && теги.Подходит(другой.gameObject)) Отметить();
    }

    void OnTriggerEnter2D(Collider2D другой)
    {
        if (приКасании && теги.Подходит(другой.gameObject)) Отметить();
    }

    void OnApplicationQuit() => выходИзИгры = true;

    void OnDestroy()
    {
        // При смене сцены и выходе из игры объекты тоже удаляются, но это не победа над врагом
        if (приУдалении && !выходИзИгры && gameObject.scene.isLoaded) Отметить();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ОтметкаЗадания))]
internal class ОтметкаЗаданияEditor : RunityEditor<ОтметкаЗадания>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект засчитывает цель задания в Журнале заданий или выдаёт новое задание");

        Пробел();

        Поле("Выдать задание", x => x.выдатьЗадание, (title, value) => ПолеОбъекта<Задание>(title, value));
        if (компонент.выдатьЗадание == null)
        {
            Поле("Метка", x => x.метка, (title, value) => EditorGUILayout.TextField(title, value));
        }

        Пробел();

        Поле("При касании", x => x.приКасании, (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.приКасании) ОтобразитьТеги("Теги", "_теги");
        Поле("При удалении", x => x.приУдалении, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Только один раз", x => x.толькоОдинРаз, (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif
