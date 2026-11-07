using UnityEngine;

/// <summary>
/// Заготовка для компонентов, которые меняют поведение Персонажа на том же объекте: банни-хоп, рывок, двойной прыжок
/// </summary>
[RequireComponent(typeof(Персонаж))]
public abstract class РасширениеПерсонажа : RunityComponent
{
    protected override void OnEnable() => персонаж.Подключить(this);
    protected override void OnDisable()
    {
        if (_персонаж != null) _персонаж.Отключить(this);
    }

    Персонаж _персонаж;
    /// <summary>
    /// Персонаж, которого расширяет этот компонент
    /// </summary>
    public Персонаж персонаж => _персонаж != null ? _персонаж : _персонаж = GetComponent<Персонаж>();

    /// <summary>
    /// Здесь расширение может изменить горизонтальное движение, которое хочет сделать персонаж в этом кадре
    /// </summary>
    protected internal virtual Vector3 ИзменитьДвижение(Vector3 желаемое) => желаемое;

    /// <summary>
    /// Если расширение вернёт "да", персонаж попробует прыгнуть, даже если игрок не нажимал прыжок
    /// </summary>
    protected internal virtual bool ХочетПрыгнуть() => false;

    /// <summary>
    /// Вызывается в момент прыжка
    /// </summary>
    protected internal virtual void ПриПрыжке() { }

    /// <summary>
    /// Вызывается, когда персонаж встал на землю
    /// </summary>
    protected internal virtual void ПриПриземлении() { }
}
