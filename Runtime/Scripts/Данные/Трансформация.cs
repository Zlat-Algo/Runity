using System;
using UnityEditor;
using UnityEngine;

public class Трансформация : IEquatable<Трансформация>
{
    [SerializeField] Transform значение;

    public Трансформация(Transform трансформация)
    {
        значение = трансформация;
    }

    public static implicit operator Transform(Трансформация трансформация)
        => трансформация.значение;

    public static implicit operator Трансформация(Transform трансформация)
        => new Трансформация(трансформация);

    public static Флажок operator ==(Трансформация первое, Трансформация второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Трансформация первое, Трансформация второе)
        => первое.значение != второе.значение;

    public bool Equals(Трансформация другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Трансформация другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение.ToString();

    public Объект объект => значение.gameObject;
    public Трансформация родитель
    {
        get => значение.parent;
        set => значение.parent = value;
    }

    public Направление3D позиция
    {
        get => значение.position;
        set => значение.position = value;
    }

    public Дробное позицияX
    {
        get => значение.position.x;
        set => значение.position = new Vector3(value, значение.position.y, значение.position.z);
    }

    public Дробное позицияY
    {
        get => значение.position.y;
        set => значение.position = new Vector3(значение.position.x, value, значение.position.z);
    }

    public Дробное позицияZ
    {
        get => значение.position.z;
        set => значение.position = new Vector3(значение.position.x, значение.position.y, value);
    }

    public Направление3D поворот
    {
        get => значение.eulerAngles;
        set => значение.eulerAngles = value;
    }

    public Дробное поворотX
    {
        get => значение.eulerAngles.x;
        set => значение.eulerAngles = new Vector3(value, значение.eulerAngles.y, значение.eulerAngles.z);
    }

    public Дробное поворотY
    {
        get => значение.eulerAngles.y;
        set => значение.eulerAngles = new Vector3(значение.eulerAngles.x, value, значение.eulerAngles.z);
    }

    public Дробное поворотZ
    {
        get => значение.eulerAngles.z;
        set => значение.eulerAngles = new Vector3(значение.eulerAngles.x, значение.eulerAngles.y, value);
    }

    public Направление3D размер
    {
        get => значение.localScale;
        set => значение.localScale = value;
    }

    public Дробное размерX
    {
        get => значение.localScale.x;
        set => значение.localScale = new Vector3(value, значение.localScale.y, значение.localScale.z);
    }

    public Дробное размерY
    {
        get => значение.localScale.y;
        set => значение.localScale = new Vector3(значение.localScale.x, value, значение.localScale.z);
    }

    public Дробное размерZ
    {
        get => значение.localScale.z;
        set => значение.localScale = new Vector3(значение.localScale.x, значение.localScale.y, value);
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Трансформация))] internal class ТрансформацияDrawer : ПолеDrawer { }
#endif