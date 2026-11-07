using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Позиция, поворот и размер объекта. Альтернатива Transform
/// </summary>
[Serializable]
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

    /// <summary>
    /// Объект, которому принадлежит эта трансформация
    /// </summary>
    public Объект объект => значение.gameObject;
    /// <summary>
    /// Трансформация родительского объекта
    /// </summary>
    public Трансформация родитель
    {
        get => значение.parent;
        set => значение.parent = value;
    }

    /// <summary>
    /// Где находится объект
    /// </summary>
    public Направление3D позиция
    {
        get => значение.position;
        set => значение.position = value;
    }

    /// <summary>
    /// Положение объекта по оси X
    /// </summary>
    public Дробное позицияX
    {
        get => значение.position.x;
        set => значение.position = new Vector3(value, значение.position.y, значение.position.z);
    }

    /// <summary>
    /// Положение объекта по оси Y
    /// </summary>
    public Дробное позицияY
    {
        get => значение.position.y;
        set => значение.position = new Vector3(значение.position.x, value, значение.position.z);
    }

    /// <summary>
    /// Положение объекта по оси Z
    /// </summary>
    public Дробное позицияZ
    {
        get => значение.position.z;
        set => значение.position = new Vector3(значение.position.x, значение.position.y, value);
    }

    /// <summary>
    /// Как повёрнут объект, в градусах
    /// </summary>
    public Направление3D поворот
    {
        get => значение.eulerAngles;
        set => значение.eulerAngles = value;
    }

    /// <summary>
    /// Поворот объекта вокруг оси X
    /// </summary>
    public Дробное поворотX
    {
        get => значение.eulerAngles.x;
        set => значение.eulerAngles = new Vector3(value, значение.eulerAngles.y, значение.eulerAngles.z);
    }

    /// <summary>
    /// Поворот объекта вокруг оси Y
    /// </summary>
    public Дробное поворотY
    {
        get => значение.eulerAngles.y;
        set => значение.eulerAngles = new Vector3(значение.eulerAngles.x, value, значение.eulerAngles.z);
    }

    /// <summary>
    /// Поворот объекта вокруг оси Z
    /// </summary>
    public Дробное поворотZ
    {
        get => значение.eulerAngles.z;
        set => значение.eulerAngles = new Vector3(значение.eulerAngles.x, значение.eulerAngles.y, value);
    }

    /// <summary>
    /// Размер объекта по трём осям
    /// </summary>
    public Направление3D размер
    {
        get => значение.localScale;
        set => значение.localScale = value;
    }

    /// <summary>
    /// Размер объекта по оси X
    /// </summary>
    public Дробное размерX
    {
        get => значение.localScale.x;
        set => значение.localScale = new Vector3(value, значение.localScale.y, значение.localScale.z);
    }

    /// <summary>
    /// Размер объекта по оси Y
    /// </summary>
    public Дробное размерY
    {
        get => значение.localScale.y;
        set => значение.localScale = new Vector3(значение.localScale.x, value, значение.localScale.z);
    }

    /// <summary>
    /// Размер объекта по оси Z
    /// </summary>
    public Дробное размерZ
    {
        get => значение.localScale.z;
        set => значение.localScale = new Vector3(значение.localScale.x, значение.localScale.y, value);
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Трансформация))] internal class ТрансформацияDrawer : ПолеDrawer { }
#endif