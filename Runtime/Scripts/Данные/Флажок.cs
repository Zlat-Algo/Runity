using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Значение "да/нет". Альтернатива bool (true/false)
/// </summary>
[Serializable]
public struct Флажок : IEquatable<Флажок>
{
    [SerializeField] bool значение;

    public Флажок(bool число)
    {
        значение = число;
    }

    public static implicit operator bool(Флажок флажок)
        => флажок.значение;

    public static implicit operator Флажок(bool бул)
        => new Флажок(бул);

    public static implicit operator int(Флажок флажок)
        => флажок.значение ? 1 : 0;

    public static explicit operator Флажок(int число)
        => число > 0;

    public static implicit operator string(Флажок флажок)
        => флажок.ToString();

    public static Флажок operator ==(Флажок первое, Флажок второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Флажок первое, Флажок второе)
        => первое.значение != второе.значение;

    public bool Equals(Флажок другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Флажок другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение ? "да" : "нет";
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Флажок))] internal class ФлажокDrawer : ПолеDrawer { }
#endif