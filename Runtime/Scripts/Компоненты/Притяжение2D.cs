#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Зона, которая притягивает объекты к центру или отталкивает от него: магнит, чёрная дыра, взрывная волна
/// </summary>
[RequireComponent(typeof(PointEffector2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Зоны/Притяжение 2D")]
public class Притяжение2D : Эффектор2D
{
    protected override bool нуженНетвёрдыйКоллайдер => true;
    PointEffector2D точка => (PointEffector2D)оригинальныйКомпонент;

    /// <summary>
    /// Насколько сильно действует зона
    /// </summary>
    public Дробное сила
    {
        get => Mathf.Abs(точка.forceMagnitude);
        set => точка.forceMagnitude = притягивает ? -Mathf.Abs(value) : Mathf.Abs(value);
    }

    /// <summary>
    /// "Да" - тянет к центру, "нет" - отталкивает
    /// </summary>
    public Флажок притягивает
    {
        get => точка.forceMagnitude < 0;
        set => точка.forceMagnitude = value ? -Mathf.Abs(точка.forceMagnitude) : Mathf.Abs(точка.forceMagnitude);
    }

    /// <summary>
    /// Если "да", то вблизи центра зона действует сильнее, чем на краю
    /// </summary>
    public Флажок сильнееВЦентре
    {
        get => точка.forceMode != EffectorForceMode2D.Constant;
        set => точка.forceMode = value ? EffectorForceMode2D.InverseLinear : EffectorForceMode2D.Constant;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Притяжение2D))]
internal class Притяжение2DEditor : Эффектор2DEditor<Притяжение2D>
{
    protected override void Отрисовать()
    {
        Текст("Эта зона притягивает объекты к своему центру или отталкивает от него");

        Пробел();

        ПроверитьКоллайдер();

        Синхрополе("Притягивает", x => x.притягивает,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Сила", x => x.сила,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Сильнее в центре", x => x.сильнееВЦентре,
            (title, value) => EditorGUILayout.Toggle(title, value));

        ОбщиеНастройки();
    }
}
#endif
