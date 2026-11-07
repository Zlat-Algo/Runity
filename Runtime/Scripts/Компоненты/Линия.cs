using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Рисует линию по точкам. Может соединять объекты: подходит для лазеров, верёвок и прицелов
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
[AddComponentMenu("  Runity/ Эффекты/Линия")]
public class Линия : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<LineRenderer>();

    void OnValidate()
    {
        Awake();
        МатериалЛиний.ПроверитьМатериал(оригинал);
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public LineRenderer оригинал => (LineRenderer)оригинальныйКомпонент;

    /// <summary>
    /// Толщина линии
    /// </summary>
    public Дробное толщина
    {
        get => оригинал.widthMultiplier;
        set => оригинал.widthMultiplier = value;
    }

    /// <summary>
    /// Цвет в начале линии
    /// </summary>
    public Цвет цветНачала
    {
        get => оригинал.startColor;
        set => оригинал.startColor = value;
    }

    /// <summary>
    /// Цвет в конце линии
    /// </summary>
    public Цвет цветКонца
    {
        get => оригинал.endColor;
        set => оригинал.endColor = value;
    }

    /// <summary>
    /// Если "да", то последняя точка соединяется с первой
    /// </summary>
    public Флажок замкнутая
    {
        get => оригинал.loop;
        set => оригинал.loop = value;
    }

    /// <summary>
    /// Если "да", то точки задаются в координатах мира. Если "нет", то относительно этого объекта, и линия двигается вместе с ним
    /// </summary>
    public Флажок вКоординатахМира
    {
        get => оригинал.useWorldSpace;
        set => оригинал.useWorldSpace = value;
    }

    /// <summary>
    /// Если "да", то концы линии закруглены
    /// </summary>
    public Флажок круглыеКонцы
    {
        get => оригинал.numCapVertices > 0;
        set => оригинал.numCapVertices = value ? 6 : 0;
    }

    /// <summary>
    /// Сколько точек у линии
    /// </summary>
    public Число количествоТочек => оригинал.positionCount;

    [SerializeField] List<Transform> _соединяет = new List<Transform>();
    /// <summary>
    /// Если указать объекты, то линия будет проходить через них каждый кадр, даже когда они двигаются
    /// </summary>
    public List<Transform> соединяет => _соединяет;

    void LateUpdate()
    {
        if (_соединяет.Count == 0) return;

        оригинал.positionCount = _соединяет.Count;
        for (int i = 0; i < _соединяет.Count; i++)
        {
            if (_соединяет[i] != null)
            {
                Vector3 точка = _соединяет[i].position;
                оригинал.SetPosition(i, вКоординатахМира ? точка : transform.InverseTransformPoint(точка));
            }
        }
    }

    /// <summary>
    /// Добавляет точку в конец линии
    /// </summary>
    public void ДобавитьТочку(Направление3D точка)
    {
        оригинал.positionCount++;
        оригинал.SetPosition(оригинал.positionCount - 1, точка);
    }

    /// <summary>
    /// Меняет положение точки с указанным номером. Точки считаются с нуля
    /// </summary>
    public void УстановитьТочку(Число номер, Направление3D точка)
    {
        if (номер >= оригинал.positionCount) оригинал.positionCount = номер + 1;
        оригинал.SetPosition(номер, точка);
    }

    /// <summary>
    /// Даёт положение точки с указанным номером
    /// </summary>
    public Направление3D ПолучитьТочку(Число номер) => оригинал.GetPosition(номер);

    /// <summary>
    /// Делает линию из двух точек: от начала до конца
    /// </summary>
    public void Провести(Направление3D начало, Направление3D конец)
    {
        оригинал.positionCount = 2;
        оригинал.SetPosition(0, начало);
        оригинал.SetPosition(1, конец);
    }

    /// <summary>
    /// Убирает все точки
    /// </summary>
    public void Очистить()
    {
        _соединяет.Clear();
        оригинал.positionCount = 0;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Линия))]
internal class ЛинияEditor : RunityEditor<Линия>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект рисует линию. Точки можно задать из кода или указать объекты, которые линия будет соединять");

        Пробел();

        Синхрополе("Толщина", x => x.толщина,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Цвет начала", x => x.цветНачала,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Цвет конца", x => x.цветКонца,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Замкнутая", x => x.замкнутая,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Круглые концы", x => x.круглыеКонцы,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("В координатах мира", x => x.вКоординатахМира,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        serializedObject.Update();
        ОтобразитьСвойство("Соединяет объекты", "_соединяет");
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
