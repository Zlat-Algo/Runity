#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Зона, которая толкает все объекты внутри в одну сторону: ветер, поток воздуха, гравитационный лифт
/// </summary>
[RequireComponent(typeof(AreaEffector2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Зоны/Зона силы")]
public class ЗонаСилы : Эффектор2D
{
    protected override bool нуженНетвёрдыйКоллайдер => true;
    AreaEffector2D зона => (AreaEffector2D)оригинальныйКомпонент;

    /// <summary>
    /// Куда толкает зона, в градусах: 0 - вправо, 90 - вверх, 180 - влево, 270 - вниз
    /// </summary>
    public Дробное угол { get => зона.forceAngle; set => зона.forceAngle = value; }

    /// <summary>
    /// Насколько сильно толкает зона
    /// </summary>
    public Дробное сила { get => зона.forceMagnitude; set => зона.forceMagnitude = value; }

    /// <summary>
    /// Если "да", то угол не зависит от поворота объекта
    /// </summary>
    public Флажок уголВМире { get => зона.useGlobalAngle; set => зона.useGlobalAngle = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ЗонаСилы))]
internal class ЗонаСилыEditor : Эффектор2DEditor<ЗонаСилы>
{
    protected override void Отрисовать()
    {
        Текст("Эта зона толкает всё, что в неё попадает");

        Пробел();

        ПроверитьКоллайдер();

        Синхрополе("Угол", x => x.угол,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 360));

        Синхрополе("Сила", x => x.сила,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Угол в мире", x => x.уголВМире,
            (title, value) => EditorGUILayout.Toggle(title, value));

        ОбщиеНастройки();
    }
}
#endif
