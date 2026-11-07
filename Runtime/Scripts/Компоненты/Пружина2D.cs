#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Соединяет объекты пружиной: она растягивается и тянет их обратно
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpringJoint2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Соединения/Пружина 2D")]
public class Пружина2D : Соединение2D
{
    SpringJoint2D пружина => (SpringJoint2D)оригинальныйКомпонент;

    /// <summary>
    /// Длина пружины в спокойном состоянии
    /// </summary>
    public Дробное длина { get => пружина.distance; set => пружина.distance = value; }

    /// <summary>
    /// Если "да", то длина берётся из расстояния между объектами на старте
    /// </summary>
    public Флажок автоматическаяДлина { get => пружина.autoConfigureDistance; set => пружина.autoConfigureDistance = value; }

    /// <summary>
    /// Насколько тугая пружина: чем больше, тем быстрее она возвращается
    /// </summary>
    public Дробное жёсткость { get => пружина.frequency; set => пружина.frequency = value; }

    /// <summary>
    /// Насколько быстро успокаиваются колебания: от 0 до 1
    /// </summary>
    public Дробное затухание { get => пружина.dampingRatio; set => пружина.dampingRatio = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Пружина2D))]
internal class Пружина2DEditor : Соединение2DEditor<Пружина2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект соединён пружиной с другим объектом или с точкой в мире");

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

        Синхрополе("Жёсткость", x => x.жёсткость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Затухание", x => x.затухание,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
