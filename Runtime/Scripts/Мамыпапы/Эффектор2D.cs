#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Заготовка для 2D-зон, которые влияют на объекты: платформ, конвейеров, ветра, воды и притяжения.
/// Сама включает у коллайдера нужные настройки
/// </summary>
[DisallowMultipleComponent]
public abstract class Эффектор2D : RunityComponent
{
    protected virtual void Awake()
    {
        оригинальныйКомпонент = GetComponent<Effector2D>();
        if (TryGetComponent(out Collider2D коллайдер))
        {
            коллайдер.usedByEffector = true;
            if (нуженНетвёрдыйКоллайдер) коллайдер.isTrigger = true;
        }
    }
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Effector2D оригинал => (Effector2D)оригинальныйКомпонент;

    /// <summary>
    /// Зоны, сквозь которые объекты проходят, должны быть нетвёрдыми
    /// </summary>
    protected abstract bool нуженНетвёрдыйКоллайдер { get; }

    /// <summary>
    /// На объекты каких физических слоёв действует зона
    /// </summary>
    public LayerMask слои
    {
        get => оригинал.colliderMask;
        set
        {
            оригинал.useColliderMask = true;
            оригинал.colliderMask = value;
        }
    }
}

#if UNITY_EDITOR
internal abstract class Эффектор2DEditor<T> : RunityEditor<T> where T : Эффектор2D
{
    protected void ПроверитьКоллайдер()
    {
        if (!компонент.TryGetComponent(out Collider2D _))
        {
            Текст("Добавьте этому объекту 2D-коллайдер: он задаёт форму зоны", MessageType.Error);
            Пробел();
        }
    }

    protected void ОбщиеНастройки()
    {
        Синхрополе("Слои", x => x.слои, ПолеСлоёв);
    }
}
#endif
