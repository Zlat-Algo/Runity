using System;
using UnityEditor;

/// <summary>
/// Дробное число. Дробная часть отделяется точкой. Альтернатива float
/// </summary>
[Serializable]
public struct Дробное : IEquatable<Дробное>, IComparable<Дробное>
{
    float значение;

    public Дробное(float число)
    {
        значение = число;
    }

    public static implicit operator float(Дробное число)
        => число.значение;

    public static implicit operator Дробное(float число)
        => new Дробное(число);

    public static implicit operator double(Дробное число)
        => число.значение;

    public static implicit operator Дробное(double число)
        => new Дробное((float)число);

    public static implicit operator string(Дробное число)
        => число.ToString();

    public static Флажок operator ==(Дробное первое, Дробное второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Дробное первое, Дробное второе)
        => первое.значение != второе.значение;

    public bool Equals(Дробное другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Дробное другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public int CompareTo(Дробное другое)
        => значение.CompareTo(другое.значение);

    public override string ToString()
        => значение.ToString();
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Дробное))] internal class ДробноеDrawer : ПолеDrawer { }
#endif