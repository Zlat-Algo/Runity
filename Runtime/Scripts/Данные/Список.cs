using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Список элементов с динамическим размером.
/// Альтернатива List<T>.
/// </summary>
[Serializable]
public class Список<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList
{
    [SerializeField] List<T> значение = new List<T>();

    public Список() { }

    public Список(int вместимость)
    {
        значение = new List<T>(вместимость);
    }

    public Список(IEnumerable<T> коллекция)
    {
        значение = new List<T>(коллекция);
    }

    // Количество элементов
    public int Count => значение.Count;
    public Число количество => Count;

    // Вместимость внутреннего списка
    public Число вместимость
    {
        get => значение.Capacity;
        set => значение.Capacity = value;
    }

    // Доступ по индексу
    public T this[int индекс]
    {
        get => значение[индекс];
        set => значение[индекс] = value;
    }

    // Добавление
    public void Add(T элемент)
        => значение.Add(элемент);

    public void добавить(T элемент)
        => значение.Add(элемент);

    // Вставка по индексу
    public void Insert(int индекс, T элемент)
        => значение.Insert(индекс, элемент);

    public void вставить(Число индекс, T элемент)
        => значение.Insert(индекс, элемент);

    // Удаление
    public bool Remove(T элемент)
        => значение.Remove(элемент);

    public bool удалить(T элемент)
        => значение.Remove(элемент);

    public void RemoveAt(int индекс)
        => значение.RemoveAt(индекс);

    public void удалитьПоИндексу(Число индекс)
        => значение.RemoveAt(индекс);

    public void Clear()
        => значение.Clear();

    public void очистить()
        => значение.Clear();

    // Поиск
    public bool Contains(T элемент)
        => значение.Contains(элемент);

    public Флажок содержит(T элемент)
        => значение.Contains(элемент);

    public int IndexOf(T элемент)
        => значение.IndexOf(элемент);

    public Число узнатьИндекс(T элемент)
        => значение.IndexOf(элемент);

    // Копирование
    public void CopyTo(T[] массив, int индекс)
        => значение.CopyTo(массив, индекс);

    public void КопироватьВ(T[] массив, Число индекс)
        => значение.CopyTo(массив, индекс);

    // Перебор через foreach
    public List<T>.Enumerator GetEnumerator()
        => значение.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => значение.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => значение.GetEnumerator();

    // Состояние коллекции
    public bool IsReadOnly => false;

    // Негeneric ICollection
    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot
        => ((ICollection)значение).SyncRoot;

    void ICollection.CopyTo(Array массив, int индекс)
        => ((ICollection)значение).CopyTo(массив, индекс);

    // Негeneric IList
    bool IList.IsReadOnly => false;

    bool IList.IsFixedSize => false;

    object IList.this[int индекс]
    {
        get => ((IList)значение)[индекс];
        set => ((IList)значение)[индекс] = value;
    }

    int IList.Add(object элемент)
        => ((IList)значение).Add(элемент);

    bool IList.Contains(object элемент)
        => ((IList)значение).Contains(элемент);

    int IList.IndexOf(object элемент)
        => ((IList)значение).IndexOf(элемент);

    void IList.Insert(int индекс, object элемент)
        => ((IList)значение).Insert(индекс, элемент);

    void IList.Remove(object элемент)
        => ((IList)значение).Remove(элемент);

    void IList.RemoveAt(int индекс)
        => ((IList)значение).RemoveAt(индекс);


    public T случайныйЭлемент
    {
        get
        {
            if (значение.Count > 0)
            {
                 return значение[Случайности.СлучайноеИзДиапазона(0, значение.Count - 1)];
            }
            else
            {
                Консоль.НапечататьОшибку($"Невозможно получить случайный элемент из списка {this}, так как он пуст");
                return default;
            }
        }
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Список<>))] internal class СписокDrawer : ПолеDrawer { }
#endif