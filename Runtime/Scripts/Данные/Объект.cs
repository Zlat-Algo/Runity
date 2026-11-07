using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Игровой объект. Альтернатива GameObject
/// </summary>
[Serializable]
public class Объект : IEquatable<Объект>
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

    public static implicit operator UnityEngine.Object(Объект объект)
        => объект.значение;

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

    /// <summary>
    /// Позиция, поворот и размер объекта
    /// </summary>
    public Трансформация трансформация => значение.transform;
    /// <summary>
    /// Внутри какого объекта лежит этот объект
    /// </summary>
    public Объект родитель
    {
        get => трансформация.родитель.объект;
        set => трансформация.родитель = value.трансформация;
    }
    /// <summary>
    /// Название объекта
    /// </summary>
    public Строка имя
    {
        get => значение.name;
        set => значение.name = value;
    }
    /// <summary>
    /// Метка объекта. Её можно проверять при столкновениях
    /// </summary>
    public Строка тег
    {
        get => значение.tag;
        set => значение.tag = value;
    }
    /// <summary>
    /// Если "нет", то объект выключен и не виден
    /// </summary>
    public Флажок активный
    {
        get => значение.activeSelf;
        set => значение.SetActive(value);
    }

    /// <summary>
    /// Даёт компонент указанного типа с этого объекта
    /// </summary>
    public ТипКомпонента НайтиКомпонент<ТипКомпонента>() => значение.GetComponent<ТипКомпонента>();
    /// <summary>
    /// Ищет компонент и даёт "да", если он есть
    /// </summary>
    public Флажок ПопробоватьНайтиКомпонент<ТипКомпонента>(out ТипКомпонента компонент) => значение.TryGetComponent(out компонент);
    /// <summary>
    /// Даёт "да", если у объекта есть один из этих тегов
    /// </summary>
    public Флажок ПроверитьТег(params Строка[] теги)
    {
        foreach(Строка тег in теги)
        {
            if (тег == null || тег == "") continue;
            if (значение.CompareTag(тег)) return true;
        }
        return false;
    }

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Объект))] internal class ОбъектDrawer : ПолеDrawer { }
#endif

