#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Зона с водой: лёгкие объекты всплывают, тяжёлые тонут, а течение может их сносить
/// </summary>
[RequireComponent(typeof(BuoyancyEffector2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Зоны/Вода 2D")]
public class Вода2D : Эффектор2D
{
    protected override bool нуженНетвёрдыйКоллайдер => true;
    BuoyancyEffector2D вода => (BuoyancyEffector2D)оригинальныйКомпонент;

    /// <summary>
    /// На какой высоте поверхность воды, относительно центра объекта
    /// </summary>
    public Дробное уровеньПоверхности { get => вода.surfaceLevel; set => вода.surfaceLevel = value; }

    /// <summary>
    /// Плотность воды: чем больше, тем лучше всплывают объекты
    /// </summary>
    public Дробное плотность { get => вода.density; set => вода.density = value; }

    /// <summary>
    /// Сила течения
    /// </summary>
    public Дробное течение { get => вода.flowMagnitude; set => вода.flowMagnitude = value; }

    /// <summary>
    /// Куда направлено течение, в градусах: 0 - вправо, 180 - влево
    /// </summary>
    public Дробное уголТечения { get => вода.flowAngle; set => вода.flowAngle = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Вода2D))]
internal class Вода2DEditor : Эффектор2DEditor<Вода2D>
{
    protected override void Отрисовать()
    {
        Текст("В этой зоне объекты плавают. Насколько хорошо, зависит от их массы");

        Пробел();

        ПроверитьКоллайдер();

        Синхрополе("Уровень поверхности", x => x.уровеньПоверхности,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Плотность", x => x.плотность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Течение", x => x.течение,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Угол течения", x => x.уголТечения,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 360));

        ОбщиеНастройки();
    }
}
#endif
