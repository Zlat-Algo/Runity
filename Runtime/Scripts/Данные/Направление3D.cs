using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Трёхмерное направление. Состоит из трёх дробных чисел - x, y и z. Альтернатива Vector3
/// </summary>
public struct Направление3D : IEquatable<Направление3D>
{
    Vector3 значение;

    public Дробное x
    {
        get => значение.x;
        set
        {
            значение = new Vector3(value, значение.y, значение.z);
        }
    }

    public Дробное y
    {
        get => значение.y;
        set
        {
            значение = new Vector3(значение.x, value, значение.z);
        }
    }

    public Дробное z
    {
        get => значение.z;
        set
        {
            значение = new Vector3(значение.x, значение.y, value);
        }
    }

    /// <summary>
    /// x: 1, y: 0, z: 0
    /// </summary>
    public static Направление3D вправо => new Направление3D(1, 0, 0);

    /// <summary>
    /// x: -1, y: 0, z: 0
    /// </summary>
    public static Направление3D влево => new Направление3D(-1, 0, 0);

    /// <summary>
    /// x: 0, y: 1, z: 0
    /// </summary>
    public static Направление3D вверх => new Направление3D(0, 1, 0);

    /// <summary>
    /// x: 0, y: -1, z: 0
    /// </summary>
    public static Направление3D вниз => new Направление3D(0, -1, 0);

    /// <summary>
    /// x: 0, y: 0, z: 1
    /// </summary>
    public static Направление3D вперёд => new Направление3D(0, 0, 1);

    /// <summary>
    /// x: 0, y: 0, z: -1
    /// </summary>
    public static Направление3D назад => new Направление3D(0, 0, -1);

    /// <summary>
    /// x: 1, y: 1, z: 1
    /// </summary>
    public static Направление3D один => new Направление3D(1, 1, 1);

    /// <summary>
    /// x: 0, y: 0, z: 0
    /// </summary>
    public static Направление3D ноль => new Направление3D(0, 0, 0);

    /// <summary>
    /// Длина направления
    /// </summary>
    public Дробное длина => значение.magnitude;

    /// <summary>
    /// Направление с длиной, равной единице
    /// </summary>
    public Направление3D единичное => значение.normalized;

    public Направление3D(Vector3 вектор)
    {
        значение = вектор;
    }

    public Направление3D(Дробное x, Дробное y)
    {
        значение = new Vector3(x, y);
    }

    public Направление3D(Дробное x, Дробное y, Дробное z)
    {
        значение = new Vector3(x, y, z);
    }

    public static implicit operator Vector3(Направление3D направление)
        => направление.значение;

    public static implicit operator Направление3D(Vector3 вектор)
        => new Направление3D(вектор);

    public static implicit operator Направление2D(Направление3D вектор)
        => (Vector2)(Vector3)вектор;

    public static implicit operator Направление3D(Направление2D вектор)
        => (Vector3)(Vector2)вектор;

    public static Направление3D operator +(Направление3D первое, Направление3D второе)
        => (Vector3)первое + (Vector3)второе;

    public static Направление3D operator -(Направление3D первое, Направление3D второе)
        => (Vector3)первое - (Vector3)второе;

    public static Направление3D operator *(Направление3D первое, Направление3D второе)
        => Vector3.Scale(первое, второе);

    public static Направление3D operator /(Направление3D первое, Направление3D второе)
    => new Направление3D(
        первое.x / второе.x,
        первое.y / второе.y,
        первое.z / второе.z
    );

    public static Флажок operator ==(Направление3D первое, Направление3D второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Направление3D первое, Направление3D второе)
        => первое.значение != второе.значение;

    public static Направление3D operator +(Направление3D первое, Дробное второе)
    => (Vector3)первое + (Vector3.one * второе);

    public static Направление3D operator +(Дробное второе, Направление3D первое)
    => (Vector3)первое + (Vector3.one * второе);

    public static Направление3D operator -(Направление3D первое, Дробное второе)
    => (Vector3)первое - (Vector3.one * второе);

    public static Направление3D operator -(Дробное первое, Направление3D второе)
    => (Vector3.one * первое) - (Vector3)второе;

    public static Направление3D operator *(Направление3D первое, Дробное второе)
        => (Vector3)первое * (float)второе;

    public static Направление3D operator *(Дробное второе, Направление3D первое)
        => (Vector3)первое * (float)второе;

    public static Направление3D operator /(Направление3D первое, Дробное второе)
        => (Vector3)первое / (float)второе;

    public static Направление3D operator /(Дробное первое, Направление3D второе)
    => (Vector3.one * первое) / второе;

    public bool Equals(Направление3D другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Направление3D другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение.ToString();

    public void Повернуть(Направление3D угол)
    {
        значение = Quaternion.Euler(угол) * значение;
    }

    public void ПовернутьПоX(Дробное угол)
    {
        значение = Quaternion.Euler(угол, 0, 0) * значение;
    }

    public void ПовернутьПоY(Дробное угол)
    {
        значение = Quaternion.Euler(0, угол, 0) * значение;
    }

    public void ПовернутьПоZ(Дробное угол)
    {
        значение = Quaternion.Euler(0, 0, угол) * значение;
    }

    public Направление3D ЕслиПовернуть(Направление3D угол)
    {
        return Quaternion.Euler(угол) * значение;
    }

    public Направление3D ЕслиПовернутьПоX(Дробное угол)
    {
        return Quaternion.Euler(угол, 0, 0) * значение;
    }

    public Направление3D ЕслиПовернутьПоY(Дробное угол)
    {
        return Quaternion.Euler(0, угол, 0) * значение;
    }

    public Направление3D ЕслиПовернутьПоZ(Дробное угол)
    {
        return Quaternion.Euler(0, 0, угол) * значение;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Направление3D))] internal class Направление3DDrawer : ПолеDrawer { }
#endif
