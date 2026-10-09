using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Целое число. Альтернатива int
/// </summary>
[Serializable]
public struct Число : IEquatable<Число>, IComparable<Число>
{
    [SerializeField] int значение;

    public Число(int число)
    {
        значение = число;
    }

    public static implicit operator int(Число число)
        => число.значение;

    public static implicit operator Число(int число)
        => new Число(число);

    public static implicit operator string(Число число)
        => число.ToString();

    public static Флажок operator ==(Число первое, Число второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Число первое, Число второе)
        => первое.значение != второе.значение;

    public bool Equals(Число другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Число другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public int CompareTo(Число другое)
        => значение.CompareTo(другое.значение);

    public override string ToString()
        => значение.ToString();
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Число))] internal class ЧислоDrawer : ПолеDrawer { }
#endif