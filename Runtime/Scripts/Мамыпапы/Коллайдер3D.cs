using UnityEngine;

/// <summary>
/// Заготовка для всех 3D-коллайдеров
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Коллайдер3D : RunityComponent
{
    protected virtual void Awake()
    {
        оригинальныйКомпонент = GetComponent<Collider>();
    }
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Collider оригинал => (Collider)оригинальныйКомпонент;

    /// <summary>
    /// "Да" - другие объекты сталкиваются с этим. "Нет" - проходят сквозь него, и срабатывает пересечение
    /// </summary>
    public bool твёрдый
    {
        get => !оригинал.isTrigger;
        set => оригинал.isTrigger = !value;
    }

    /// <summary>
    /// Физический материал: определяет трение и упругость
    /// </summary>
    public PhysicsMaterial материал
    {
        get => оригинал.sharedMaterial;
        set => оригинал.sharedMaterial = value;
    }
}
