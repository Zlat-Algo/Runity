using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТелоЛюбое))]
[AddComponentMenu("  Runity/ Передвижение/Прыжок")]
public class Прыжок : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТелоЛюбое>();
    void OnValidate() => Awake();
    public ФизическоеТелоЛюбое оригинал => (ФизическоеТелоЛюбое)оригинальныйКомпонент;


    [SerializeField] Дробное _силаПрыжка = 7;
    public Дробное силаПрыжка { get => _силаПрыжка; set => _силаПрыжка = value; }

    [SerializeField] Флажок _наПробел = да;
    public Флажок наПробел { get => _наПробел; set => _наПробел = value; }

    [SerializeField] Флажок _наВперёд = нет;
    public Флажок наВперёд { get => _наВперёд; set => _наВперёд = value; }

    [SerializeField] Флажок _наЛКМ = нет;
    public Флажок наЛКМ { get => _наЛКМ; set => _наЛКМ = value; }

    [SerializeField] Флажок _покаДержишь = да;
    public Флажок покаДержишь { get => _покаДержишь; set => _покаДержишь = value; }

    [SerializeField] Число _прыжковВВоздухе = 0;
    public Число прыжковВВоздухе { get => _прыжковВВоздухе; set => _прыжковВВоздухе = value; }

    [SerializeField] Число _прыжковВВоздухеОсталось = 0;
    public Число прыжковВВоздухеОсталось { get => _прыжковВВоздухеОсталось; set => _прыжковВВоздухеОсталось = value; }

    List<Component> contacts = new List<Component>();
    public Флажок чегоТоКасается => contacts.Count > 0;

    Флажок клавишаНажата => (наПробел && Управление.пробелНажат) || (наВперёд && Управление.вперёдНажато) || (наЛКМ && Управление.ЛКМНажата);
    Флажок клавишаУдерживается => (наПробел && Управление.пробелУдерживается) || (наВперёд && Управление.вперёдУдерживается) || (наЛКМ && Управление.ЛКМУдерживается);


    void Update()
    {
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
        if (клавишаУдерживается && покаДержишь)
        {
            Прыгнуть();
        }
    }

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
    }

    public void Прыгнуть()
    {
        Прыгнуть(1);
    }

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
            contacts.Remove(collision.collider);
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
            contacts.Remove(collision.collider);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Прыжок))]
internal class ПрыжокEditor : RunityEditor<Прыжок>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект может прыгать по оси Y при нажатии на указанную кнопку");

        Пробел();

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
    }
}
#endif