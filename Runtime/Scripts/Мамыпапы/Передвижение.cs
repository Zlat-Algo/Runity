#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Заготовка для всех компонентов, которые передвигают объект
/// </summary>
[DisallowMultipleComponent]
public abstract class Передвижение : RunityComponent
{
    [SerializeField] float _скорость = 5;
    /// <summary>
    /// С какой скоростью передвигается объект
    /// </summary>
    public float скорость { get => _скорость; set => _скорость = value; }

    /// <summary>
    /// Даёт "да", пока объект движется
    /// </summary>
    public Флажок движется { get; private set; }

    [SerializeField] internal События _приНачалеДвижения;
    /// <summary>
    /// Срабатывает, когда объект стоял и начал двигаться
    /// </summary>
    public События приНачалеДвижения => _приНачалеДвижения;

    [SerializeField] internal События _приОстановке;
    /// <summary>
    /// Срабатывает, когда объект двигался и остановился
    /// </summary>
    public События приОстановке => _приОстановке;

    protected void ОбновитьДвижение(Флажок сейчасДвижется)
    {
        if (движется == сейчасДвижется) return;

        движется = сейчасДвижется;
        if (движется)
        {
            _приНачалеДвижения.Вызвать();
        }
        else
        {
            _приОстановке.Вызвать();
        }
    }
}

#if UNITY_EDITOR
internal abstract class ПередвижениеEditor<T> : RunityEditor<T> where T : Передвижение
{
    protected void ОтобразитьСобытияДвижения()
    {
        НачатьСобытия();
        ОтобразитьСобытие("При начале движения", "_приНачалеДвижения");
        ОтобразитьСобытие("При остановке", "_приОстановке");
        ЗакончитьСобытия();
    }
}
#endif
