#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Плавно перемещает объект и может повторять движение: туда-сюда, по кругу или дальше и дальше
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Перемещение")]
public class Перемещение : ПлавноеИзменение
{
    [SerializeField] Направление3D _смещение = new Vector3(0, 2, 0);
    /// <summary>
    /// На сколько сдвинуться от начального места
    /// </summary>
    public Направление3D смещение { get => _смещение; set => _смещение = value; }

    [SerializeField] Transform _цель;
    /// <summary>
    /// Если указать объект, то движение пойдёт к нему, а смещение не учитывается
    /// </summary>
    public Transform цель { get => _цель; set => _цель = value; }

    [SerializeField] Флажок _вЛокальныхКоординатах = да;
    /// <summary>
    /// Если "да", то смещение считается относительно родителя. Объект внутри движущейся платформы будет двигаться вместе с ней
    /// </summary>
    public Флажок вЛокальныхКоординатах { get => _вЛокальныхКоординатах; set => _вЛокальныхКоординатах = value; }

    /// <summary>
    /// Откуда начинается движение
    /// </summary>
    public Направление3D начало { get; private set; }

    Vector3 позиция
    {
        get => вЛокальныхКоординатах ? transform.localPosition : transform.position;
        set
        {
            if (вЛокальныхКоординатах) transform.localPosition = value;
            else transform.position = value;
        }
    }

    Vector3 конец
    {
        get
        {
            if (цель == null) return (Vector3)начало + (Vector3)смещение;
            return вЛокальныхКоординатах && transform.parent != null ? transform.parent.InverseTransformPoint(цель.position) : цель.position;
        }
    }

    protected override void ЗапомнитьНачало() => начало = позиция;

    protected override void Применить(float доля) => позиция = Vector3.LerpUnclamped(начало, конец, доля);

    protected override void ПеренестиНачалоВКонец() => начало = конец;

    void OnDrawGizmosSelected()
    {
        if (цель != null) return;

        Vector3 откуда = Application.isPlaying ? (Vector3)начало : позиция;
        Vector3 куда = откуда + (Vector3)смещение;
        if (вЛокальныхКоординатах && transform.parent != null)
        {
            откуда = transform.parent.TransformPoint(откуда);
            куда = transform.parent.TransformPoint(куда);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(откуда, куда);
        Gizmos.DrawWireSphere(куда, 0.15f);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Перемещение))]
internal class ПеремещениеEditor : ПлавноеИзменениеEditor<Перемещение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект плавно перемещается. Голубая линия в сцене показывает, куда он поедет");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        if (компонент.цель == null)
        {
            Поле("Смещение", x => x.смещение,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
        }

        Поле("В локальных координатах", x => x.вЛокальныхКоординатах,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        ОбщиеНастройки();

        Пробел();

        КнопкиЗапуска();
        ОбщиеСобытия();
    }
}
#endif
