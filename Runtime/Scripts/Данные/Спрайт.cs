using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class Спрайт : IEquatable<Спрайт>
{
    [SerializeField] Sprite значение;

    public Спрайт(Sprite спрайт)
    {
        значение = спрайт;
    }

    public static implicit operator Sprite(Спрайт спрайт)
        => спрайт.значение;

    public static implicit operator Спрайт(Sprite спрайт)
        => new Спрайт(спрайт);

    public static Флажок operator ==(Спрайт первое, Спрайт второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Спрайт первое, Спрайт второе)
        => первое.значение != второе.значение;

    public bool Equals(Спрайт другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Спрайт другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение.ToString();
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Спрайт))]
internal class СпрайтDrawer : ПолеDrawer { }
#endif
