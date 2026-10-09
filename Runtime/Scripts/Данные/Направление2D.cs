using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Двухмерное направление. Состоит из двух дробных чисел - x и y. Альтернатива Vector2
/// </summary>
public struct Направление2D : IEquatable<Направление2D>
{
    Vector2 значение;

    public Дробное x
    {
        get => значение.x;
        set
        {
            значение = new Vector2(value, значение.y);
        }
    }

    public Дробное y
    {
        get => значение.y;
        set
        {
            значение = new Vector2(значение.x, value);
        }
    }

    /// <summary>
    /// x: 1, y: 0
    /// </summary>
    public static Направление2D вправо => new Направление2D(1, 0);

    /// <summary>
    /// x: -1, y: 0
    /// </summary>
    public static Направление2D влево => new Направление2D(-1, 0);

    /// <summary>
    /// x: 0, y: 1
    /// </summary>
    public static Направление2D вверх => new Направление2D(0, 1);

    /// <summary>
    /// x: 0, y: -1
    /// </summary>
    public static Направление2D вниз => new Направление2D(0, -1);

    /// <summary>
    /// x: 1, y: 1
    /// </summary>
    public static Направление2D один => new Направление2D(1, 1);

    /// <summary>
    /// x: 0, y: 0
    /// </summary>
    public static Направление2D ноль => new Направление2D(0, 0);

    /// <summary>
    /// Длина направления
    /// </summary>
    public Дробное длина => значение.magnitude;
    /// <summary>
    /// Направление с длиной, равной единице
    /// </summary>
    public Направление2D единичное => значение.normalized;

    public Направление2D(Vector2 вектор)
    {
        значение = вектор;
    }

    public Направление2D(Дробное x, Дробное y)
    {
        значение = new Vector2(x, y);
    }

    public static implicit operator Vector2(Направление2D направление)
        => направление.значение;

    public static implicit operator Направление2D(Vector2 вектор)
        => new Направление2D(вектор);

    public static implicit operator Vector3(Направление2D направление)
        => направление.значение;

    public static implicit operator Направление2D(Vector3 вектор)
        => new Направление2D(вектор);

    public static Направление2D operator +(Направление2D первое, Направление2D второе)
        => (Vector2)первое + (Vector2)второе;

    public static Направление2D operator -(Направление2D первое, Направление2D второе)
        => (Vector2)первое - (Vector2)второе;

    public static Направление2D operator *(Направление2D первое, Направление2D второе)
        => (Vector2)первое * (Vector2)второе;

    public static Направление2D operator /(Направление2D первое, Направление2D второе)
        => (Vector2)первое / (Vector2)второе;

    public static Флажок operator ==(Направление2D первое, Направление2D второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Направление2D первое, Направление2D второе)
        => первое.значение != второе.значение;

    public static Направление2D operator +(Направление2D первое, Дробное второе)
        => (Vector2)первое + (Vector2.one * второе);

    public static Направление2D operator +(Дробное второе, Направление2D первое)
        => (Vector2)первое + (Vector2.one * второе);

    public static Направление2D operator -(Направление2D первое, Дробное второе)
        => (Vector2)первое - (Vector2.one * второе);

    public static Направление2D operator -(Дробное первое, Направление2D второе)
        => (Vector2.one * первое) - (Vector2)второе;

    public static Направление2D operator *(Направление2D первое, Дробное второе)
        => (Vector2)первое * (float)второе;

    public static Направление2D operator *(Дробное второе, Направление2D первое)
        => (Vector2)первое * (float)второе;

    public static Направление2D operator /(Направление2D первое, Дробное второе)
        => (Vector2)первое / (float)второе;

    public static Направление2D operator /(Дробное первое, Направление2D второе)
        => (Vector2.one * первое) / (Vector2)второе;

    public bool Equals(Направление2D другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Направление2D другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение.ToString();

    public void Повернуть(Дробное угол)
    {
        значение = Quaternion.Euler(0, 0, угол) * значение;
    }

    public Направление2D ЕслиПовернуть(Дробное угол)
    {
        return (Направление3D)(Quaternion.Euler(0, 0, угол) * значение);
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Направление2D))] internal class Направление2DDrawer : ПолеDrawer { }
#endif