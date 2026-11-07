using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Платформа, на которую можно запрыгнуть снизу и спрыгнуть вниз сквозь неё
/// </summary>
[RequireComponent(typeof(PlatformEffector2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Зоны/Односторонняя платформа")]
public class ОдносторонняяПлатформа : Эффектор2D
{
    protected override bool нуженНетвёрдыйКоллайдер => false;
    PlatformEffector2D платформа => (PlatformEffector2D)оригинальныйКомпонент;

    /// <summary>
    /// Если "да", то сквозь платформу можно пройти снизу вверх
    /// </summary>
    public Флажок односторонняя { get => платформа.useOneWay; set => платформа.useOneWay = value; }

    /// <summary>
    /// Ширина твёрдой части сверху в градусах. 180 означает, что твёрдой будет вся верхняя половина
    /// </summary>
    public Дробное ширинаОпоры { get => платформа.surfaceArc; set => платформа.surfaceArc = value; }

    /// <summary>
    /// Если "да", то бока платформы не цепляют игрока при прыжке
    /// </summary>
    public Флажок скользкиеБока { get => !платформа.useSideFriction; set => платформа.useSideFriction = !value; }

    /// <summary>
    /// На время пропускает объекты сквозь платформу вниз. Удобно подключить к нажатию кнопки "вниз"
    /// </summary>
    public void ПропуститьВниз(Дробное время)
    {
        StopAllCoroutines();
        StartCoroutine(Пропускать(время));
    }

    IEnumerator Пропускать(float время)
    {
        платформа.rotationalOffset = 180;
        yield return new WaitForSeconds(время);
        платформа.rotationalOffset = 0;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ОдносторонняяПлатформа))]
internal class ОдносторонняяПлатформаEditor : Эффектор2DEditor<ОдносторонняяПлатформа>
{
    protected override void Отрисовать()
    {
        Текст("На эту платформу можно запрыгнуть снизу. Чтобы спрыгнуть вниз, вызовите Пропустить вниз");

        Пробел();

        ПроверитьКоллайдер();

        Синхрополе("Односторонняя", x => x.односторонняя,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Ширина опоры", x => x.ширинаОпоры,
            (title, value) => EditorGUILayout.Slider(title, value, 1, 360));

        Синхрополе("Скользкие бока", x => x.скользкиеБока,
            (title, value) => EditorGUILayout.Toggle(title, value));

        ОбщиеНастройки();
    }
}
#endif
