using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Объект подпрыгивает по кнопке. Нужно физическое тело
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТелоЛюбое))]
[AddComponentMenu("  Runity/ Передвижение/ Управление игроком/Прыжок")]
public class Прыжок : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТелоЛюбое>();
    void OnValidate() => Awake();
    /// <summary>
    /// Физическое тело, которое подпрыгивает
    /// </summary>
    public ФизическоеТелоЛюбое оригинал => (ФизическоеТелоЛюбое)оригинальныйКомпонент;


    /// <summary>
    /// С какой скоростью объект взлетает вверх
    /// </summary>
    [SerializeField] Дробное _силаПрыжка = 7;
    public Дробное силаПрыжка { get => _силаПрыжка; set => _силаПрыжка = value; }

    /// <summary>
    /// Прыгать по пробелу
    /// </summary>
    [SerializeField] Флажок _наПробел = да;
    public Флажок наПробел { get => _наПробел; set => _наПробел = value; }

    /// <summary>
    /// Прыгать по кнопке "вперёд" (W или стрелка вверх)
    /// </summary>
    [SerializeField] Флажок _наВперёд = нет;
    public Флажок наВперёд { get => _наВперёд; set => _наВперёд = value; }

    /// <summary>
    /// Прыгать по левой кнопке мыши
    /// </summary>
    [SerializeField] Флажок _наЛКМ = нет;
    public Флажок наЛКМ { get => _наЛКМ; set => _наЛКМ = value; }

    /// <summary>
    /// Если "да", то при зажатой кнопке объект подпрыгивает снова сразу после приземления
    /// </summary>
    [SerializeField] Флажок _покаДержишь = да;
    public Флажок покаДержишь { get => _покаДержишь; set => _покаДержишь = value; }

    /// <summary>
    /// Сколько дополнительных прыжков можно сделать в воздухе
    /// </summary>
    [SerializeField] Число _прыжковВВоздухе = 0;
    public Число прыжковВВоздухе { get => _прыжковВВоздухе; set => _прыжковВВоздухе = value; }

    /// <summary>
    /// Сколько прыжков в воздухе осталось до приземления
    /// </summary>
    [SerializeField] Число _прыжковВВоздухеОсталось = 0;
    public Число прыжковВВоздухеОсталось { get => _прыжковВВоздухеОсталось; set => _прыжковВВоздухеОсталось = value; }

    [SerializeField] События _приПрыжке;
    /// <summary>
    /// Срабатывает в момент каждого прыжка
    /// </summary>
    public События приПрыжке => _приПрыжке;

    [SerializeField] События _приПриземлении;
    /// <summary>
    /// Срабатывает, когда объект был в воздухе и коснулся поверхности
    /// </summary>
    public События приПриземлении => _приПриземлении;

    [SerializeField] События _приОтрывеОтЗемли;
    /// <summary>
    /// Срабатывает, когда объект перестал касаться всех поверхностей
    /// </summary>
    public События приОтрывеОтЗемли => _приОтрывеОтЗемли;

    List<Component> contacts = new List<Component>();
    /// <summary>
    /// Даёт "да", если объект стоит на чём-то или чего-то касается
    /// </summary>
    public Флажок чегоТоКасается => contacts.Count > 0;

    Флажок клавишаНажата => (наПробел && Управление.пробелНажат) || (наВперёд && Управление.вперёдНажато) || (наЛКМ && Управление.ЛКМНажата);
    Флажок клавишаУдерживается => (наПробел && Управление.пробелУдерживается) || (наВперёд && Управление.вперёдУдерживается) || (наЛКМ && Управление.ЛКМУдерживается);


    void Update()
    {
        if (contacts.RemoveAll(контакт => контакт == null) > 0 && contacts.Count == 0)
        {
            _приОтрывеОтЗемли.Вызвать();
        }

        if (клавишаНажата && (прыжковВВоздухеОсталось != 0 || чегоТоКасается))
        {
            if (прыжковВВоздухеОсталось > 0 && !чегоТоКасается)
            {
                прыжковВВоздухеОсталось--;
            }
            Прыгнуть();
        }
    }

    void КогдаКоснулсяПоверхности()
    {
        ВосстановитьПрыжкиВВоздухе();
        if (contacts.Count == 1)
        {
            _приПриземлении.Вызвать();
        }
        if (клавишаУдерживается && покаДержишь)
        {
            Прыгнуть();
        }
    }

    /// <summary>
    /// Подпрыгивает, даже если объект в воздухе
    /// </summary>
    public void Прыгнуть(Дробное множительПрыжка)
    {
        if (оригинал is ФизическоеТело)
        {
            ((ФизическоеТело)оригинал).движениеY = силаПрыжка * множительПрыжка;
        }
        else if (оригинал is ФизическоеТело2D)
        {
            ((ФизическоеТело2D)оригинал).движениеY = силаПрыжка * множительПрыжка;
        }
        _приПрыжке.Вызвать();
    }

    /// <summary>
    /// Подпрыгивает, даже если объект в воздухе
    /// </summary>
    public void Прыгнуть()
    {
        Прыгнуть(1);
    }

    /// <summary>
    /// Возвращает все прыжки в воздухе
    /// </summary>
    public void ВосстановитьПрыжкиВВоздухе()
    {
        прыжковВВоздухеОсталось = прыжковВВоздухе;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (contacts.IndexOf(collision.collider) == -1)
        {
            if (TryGetComponent(out Collider myCollider) && collision.thisCollider == myCollider)
            {
                contacts.Add(collision.collider);
                КогдаКоснулсяПоверхности();
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (TryGetComponent(out Collider myCollider) && collision.thisCollider == myCollider)
        {
            КогдаПерестаёмКасаться(collision.collider);
        }
    }

    void КогдаПерестаёмКасаться(Component поверхность)
    {
        if (contacts.Remove(поверхность) && contacts.Count == 0)
        {
            _приОтрывеОтЗемли.Вызвать();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (contacts.IndexOf(collision.collider) == -1)
        {
            if (TryGetComponent(out Collider2D myCollider) && collision.otherCollider == myCollider)
            {
                contacts.Add(collision.collider);
                КогдаКоснулсяПоверхности();
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (TryGetComponent(out Collider2D myCollider) && collision.otherCollider == myCollider)
        {
            КогдаПерестаёмКасаться(collision.collider);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Прыжок))]
internal class ПрыжокEditor : RunityEditor<Прыжок>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект может прыгать по оси Y при нажатии на указанную кнопку");

        Пробел();

        ПредупреждениеПроУправление();

        Поле("Сила прыжка", x => x.силаПрыжка,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Поле("На пробел", x => x.наПробел,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("На вперёд", x => x.наВперёд,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("На ЛКМ", x => x.наЛКМ,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Пока держишь", x => x.покаДержишь,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Прыжков в воздухе", x => x.прыжковВВоздухе,
            (title, value) => EditorGUILayout.IntField(title, value));

        Поле("Прыжков в воздухе осталось", x => x.прыжковВВоздухеОсталось,
            (title, value) => EditorGUILayout.IntField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При прыжке", "_приПрыжке");
        ОтобразитьСобытие("При приземлении", "_приПриземлении");
        ОтобразитьСобытие("При отрыве от земли", "_приОтрывеОтЗемли");
        ЗакончитьСобытия();
    }
}
#endif