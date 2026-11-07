#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Соединяет объекты пружиной: она растягивается и тянет их обратно
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpringJoint))]
[AddComponentMenu("  Runity/ Физика 3D/ Соединения/Пружина")]
public class Пружина : Соединение3D
{
    SpringJoint пружина => (SpringJoint)оригинальныйКомпонент;

    /// <summary>
    /// Насколько тугая пружина: чем больше, тем сильнее она тянет
    /// </summary>
    public Дробное жёсткость { get => пружина.spring; set => пружина.spring = value; }

    /// <summary>
    /// Насколько быстро успокаиваются колебания
    /// </summary>
    public Дробное затухание { get => пружина.damper; set => пружина.damper = value; }

    /// <summary>
    /// До какой длины пружина может сжаться, не сопротивляясь
    /// </summary>
    public Дробное минимальнаяДлина { get => пружина.minDistance; set => пружина.minDistance = value; }

    /// <summary>
    /// До какой длины пружина может растянуться, не сопротивляясь
    /// </summary>
    public Дробное максимальнаяДлина { get => пружина.maxDistance; set => пружина.maxDistance = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Пружина))]
internal class ПружинаEditor : Соединение3DEditor<Пружина>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект соединён пружиной с другим объектом или с точкой в мире");

        Пробел();

        ОбщиеНастройки();

        Пробел();

        Синхрополе("Жёсткость", x => x.жёсткость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Затухание", x => x.затухание,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Минимальная длина", x => x.минимальнаяДлина,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Максимальная длина", x => x.максимальнаяДлина,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
