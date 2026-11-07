#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Держит объекты на расстоянии, как верёвка или жёсткий стержень. Подходит для крюка-кошки и качелей
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DistanceJoint2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Соединения/Верёвка 2D")]
public class Верёвка2D : Соединение2D
{
    DistanceJoint2D верёвка => (DistanceJoint2D)оригинальныйКомпонент;

    /// <summary>
    /// Длина верёвки
    /// </summary>
    public Дробное длина { get => верёвка.distance; set => верёвка.distance = value; }

    /// <summary>
    /// Если "да", то длина берётся из расстояния между объектами на старте
    /// </summary>
    public Флажок автоматическаяДлина { get => верёвка.autoConfigureDistance; set => верёвка.autoConfigureDistance = value; }

    /// <summary>
    /// Если "да", то объекты могут сближаться, как на верёвке. Если "нет", то расстояние всегда одно, как у стержня
    /// </summary>
    public Флажок гибкая { get => верёвка.maxDistanceOnly; set => верёвка.maxDistanceOnly = value; }

    /// <summary>
    /// Подтягивает объект: укорачивает верёвку на указанную длину
    /// </summary>
    public void Подтянуть(Дробное насколько)
    {
        автоматическаяДлина = нет;
        длина = Mathf.Max(0.1f, длина - насколько);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Верёвка2D))]
internal class Верёвка2DEditor : Соединение2DEditor<Верёвка2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект привязан к другому объекту или к точке в мире");

        Пробел();

        ОбщиеНастройки();

        Пробел();

        Синхрополе("Автоматическая длина", x => x.автоматическаяДлина,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (!компонент.автоматическаяДлина)
        {
            Синхрополе("Длина", x => x.длина,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Синхрополе("Гибкая", x => x.гибкая,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
