using System;
using UnityEditor;
using UnityEngine;

public class Объект : UnityEngine.Object, IEquatable<Объект>
{
    [SerializeField] GameObject значение;

    public Объект(GameObject объект)
    {
        значение = объект;
    }

    public static implicit operator GameObject(Объект объект)
        => объект.значение;

    public static implicit operator Объект(GameObject объект)
        => new Объект(объект);

    public static Флажок operator ==(Объект первое, Объект второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Объект первое, Объект второе)
        => первое.значение != второе.значение;

    public bool Equals(Объект другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Объект другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение.ToString();

    public Трансформация трансформация => значение.transform;
    public Объект родитель
    {
        get => трансформация.родитель.объект;
        set => трансформация.родитель = value.трансформация;
    }
    public Строка имя
    {
        get => значение.name;
        set => значение.name = value;
    }
    public Строка тег
    {
        get => значение.tag;
        set => значение.tag = value;
    }
    public Флажок активный
    {
        get => значение.activeSelf;
        set => значение.SetActive(value);
    }

    public ТипКомпонента НайтиКомпонент<ТипКомпонента>() => значение.GetComponent<ТипКомпонента>();

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Объект))] internal class ОбъектDrawer : ПолеDrawer { }
#endif

