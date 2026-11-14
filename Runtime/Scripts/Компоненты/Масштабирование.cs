#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Плавно меняет размер объекта и может повторять это: пульсировать, расти или сжиматься
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Масштабирование")]
public class Масштабирование : ПлавноеИзменение
{
    [SerializeField] Направление3D _воСколькоРаз = new Vector3(1.2f, 1.2f, 1.2f);
    /// <summary>
    /// Во сколько раз изменить размер по каждой оси. 2 - вдвое больше, 0.5 - вдвое меньше, 1 - без изменений
    /// </summary>
    public Направление3D воСколькоРаз { get => _воСколькоРаз; set => _воСколькоРаз = value; }

    /// <summary>
    /// С какого размера начинается изменение
    /// </summary>
    public Направление3D начало { get; private set; }

    Vector3 конец => Vector3.Scale(начало, воСколькоРаз);

    protected override void ЗапомнитьНачало() => начало = transform.localScale;

    protected override void Применить(float доля) => transform.localScale = Vector3.LerpUnclamped(начало, конец, доля);

    protected override void ПеренестиНачалоВКонец() => начало = конец;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Масштабирование))]
internal class МасштабированиеEditor : ПлавноеИзменениеEditor<Масштабирование>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект плавно меняет размер. Например, с повтором «Туда-сюда» он будет пульсировать");

        Пробел();

        Поле("Во сколько раз", x => x.воСколькоРаз,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Пробел();

        ОбщиеНастройки();

        Пробел();

        КнопкиЗапуска();
        ОбщиеСобытия();
    }
}
#endif
