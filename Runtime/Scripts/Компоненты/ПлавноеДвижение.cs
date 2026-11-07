using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Как меняется скорость во время плавного движения
/// </summary>
public enum ВидПлавности { Равномерно, ПлавныйСтарт, ПлавнаяОстановка, ПлавноСОбеихСторон, СПружинкой }

/// <summary>
/// Плавно двигает, поворачивает и меняет размер объекта за указанное время: двери, лифты, всплывающие окна
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Плавное движение")]
public class ПлавноеДвижение : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Дробное _длительность = 1;
    /// <summary>
    /// За сколько секунд выполняется каждое движение
    /// </summary>
    public Дробное длительность { get => _длительность; set => _длительность = value; }

    [SerializeField] ВидПлавности _плавность = ВидПлавности.ПлавноСОбеихСторон;
    /// <summary>
    /// Как меняется скорость во время движения
    /// </summary>
    public ВидПлавности плавность { get => _плавность; set => _плавность = value; }

    [SerializeField] Флажок _вЛокальныхКоординатах = нет;
    /// <summary>
    /// Если "да", то движение считается относительно родителя. Удобно для элементов внутри других объектов
    /// </summary>
    public Флажок вЛокальныхКоординатах { get => _вЛокальныхКоординатах; set => _вЛокальныхКоординатах = value; }

    [SerializeField] События _приЗавершении;
    /// <summary>
    /// Срабатывает, когда любое движение закончилось
    /// </summary>
    public События приЗавершении => _приЗавершении;

    /// <summary>
    /// Даёт "да", пока объект плавно движется
    /// </summary>
    public Флажок идёт => движение != null || поворот != null || изменениеРазмера != null;

    Coroutine движение;
    Coroutine поворот;
    Coroutine изменениеРазмера;

    Vector3 позиция
    {
        get => вЛокальныхКоординатах ? transform.localPosition : transform.position;
        set
        {
            if (вЛокальныхКоординатах) transform.localPosition = value;
            else transform.position = value;
        }
    }

    /// <summary>
    /// Плавно перемещает объект в указанную точку
    /// </summary>
    public void ПереместитьВ(Направление3D точка)
    {
        Vector3 начало = позиция;
        Vector3 конец = точка;
        Запустить(ref движение, к => позиция = Vector3.LerpUnclamped(начало, конец, к), () => движение = null);
    }

    /// <summary>
    /// Плавно сдвигает объект на указанное расстояние от текущего места
    /// </summary>
    public void СдвинутьНа(Направление3D смещение) => ПереместитьВ((Vector3)позиция + (Vector3)смещение);

    /// <summary>
    /// Плавно перемещает объект к другому объекту
    /// </summary>
    public void ПереместитьК(Transform цель)
    {
        if (цель == null) return;
        Vector3 начало = transform.position;
        Запустить(ref движение, к => transform.position = Vector3.LerpUnclamped(начало, цель != null ? цель.position : начало, к), () => движение = null);
    }

    /// <summary>
    /// Плавно поворачивает объект на указанные углы. Для 2D используйте Z
    /// </summary>
    public void ПовернутьНа(Направление3D углы)
    {
        Quaternion начало = transform.localRotation;
        Vector3 насколько = углы;
        Запустить(ref поворот, к => transform.localRotation = начало * Quaternion.Euler(насколько * к), () => поворот = null);
    }

    /// <summary>
    /// Плавно меняет размер объекта до указанного
    /// </summary>
    public void ИзменитьРазмерДо(Направление3D размер)
    {
        Vector3 начало = transform.localScale;
        Vector3 конец = размер;
        Запустить(ref изменениеРазмера, к => transform.localScale = Vector3.LerpUnclamped(начало, конец, к), () => изменениеРазмера = null);
    }

    /// <summary>
    /// Плавно увеличивает объект во столько раз. 0.5 уменьшит его вдвое
    /// </summary>
    public void Увеличить(Дробное воСколькоРаз) => ИзменитьРазмерДо(transform.localScale * воСколькоРаз);

    /// <summary>
    /// Останавливает все движения там, где объект сейчас находится
    /// </summary>
    public void Остановить()
    {
        StopAllCoroutines();
        движение = поворот = изменениеРазмера = null;
    }

    void Запустить(ref Coroutine канал, System.Action<float> шаг, System.Action конец)
    {
        if (канал != null) StopCoroutine(канал);
        канал = StartCoroutine(Процесс(шаг, конец));
    }

    IEnumerator Процесс(System.Action<float> шаг, System.Action конец)
    {
        for (float прошло = 0; прошло < длительность; прошло += Время.времяСПрошлогоКадра)
        {
            шаг(Плавность(прошло / длительность));
            yield return null;
        }

        шаг(1);
        конец();
        _приЗавершении.Вызвать();
    }

    float Плавность(float t) => плавность switch
    {
        ВидПлавности.ПлавныйСтарт => t * t,
        ВидПлавности.ПлавнаяОстановка => 1 - (1 - t) * (1 - t),
        ВидПлавности.ПлавноСОбеихСторон => t * t * (3 - 2 * t),
        ВидПлавности.СПружинкой => 1 + 2.7f * Mathf.Pow(t - 1, 3) + 1.7f * Mathf.Pow(t - 1, 2),
        _ => t
    };
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ПлавноеДвижение))]
internal class ПлавноеДвижениеEditor : RunityEditor<ПлавноеДвижение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект умеет плавно двигаться, поворачиваться и менять размер. Движения запускаются из событий или кода, например «Сдвинуть на (0, 3, 0)» откроет дверь вверх");

        Пробел();

        Поле("Длительность", x => x.длительность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Плавность", x => x.плавность,
            (title, value) => (ВидПлавности)EditorGUILayout.EnumPopup(title, value));

        Поле("В локальных координатах", x => x.вЛокальныхКоординатах,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При завершении", "_приЗавершении");
        ЗакончитьСобытия();
    }
}
#endif
