using UnityEngine;

/// <summary>
/// Действия с целым объектом. В событиях они находятся в разделе «Объект»
/// </summary>
public sealed class ДействияОбъекта
{
    readonly GameObject объект;

    internal ДействияОбъекта(GameObject объект) => this.объект = объект;

    /// <summary>
    /// Включает объект
    /// </summary>
    public void Включить() => объект.SetActive(true);

    /// <summary>
    /// Выключает объект. Выключенный объект не виден и ничего не делает
    /// </summary>
    public void Выключить() => объект.SetActive(false);

    /// <summary>
    /// Выключает включённый объект и включает выключенный
    /// </summary>
    public void Переключить() => объект.SetActive(!объект.activeSelf);

    /// <summary>
    /// Удаляет объект
    /// </summary>
    public void Удалить() => Object.Destroy(объект);

    /// <summary>
    /// Удаляет объект через указанное количество секунд
    /// </summary>
    public void УдалитьЧерез(Дробное секунд) => Object.Destroy(объект, секунд);

    /// <summary>
    /// Создаёт копию объекта на том же месте
    /// </summary>
    public void СоздатьКопию() => Object.Instantiate(объект, объект.transform.position, объект.transform.rotation, объект.transform.parent);

    /// <summary>
    /// Переносит объект туда, где стоит другой объект
    /// </summary>
    public void ПереместитьК(GameObject цель)
    {
        if (цель != null) объект.transform.position = цель.transform.position;
    }

    /// <summary>
    /// Кладёт объект внутрь другого объекта. Он будет двигаться вместе с ним
    /// </summary>
    public void ПоложитьВнутрь(GameObject родитель)
    {
        объект.transform.SetParent(родитель != null ? родитель.transform : null, true);
    }

    /// <summary>
    /// Достаёт объект из родителя, после чего он ни от кого не зависит
    /// </summary>
    public void ДостатьИзРодителя() => объект.transform.SetParent(null, true);

    /// <summary>
    /// Если "да", то объект включён
    /// </summary>
    public Флажок включён
    {
        get => объект.activeSelf;
        set => объект.SetActive(value);
    }

    /// <summary>
    /// Название объекта
    /// </summary>
    public Строка имя
    {
        get => объект.name;
        set => объект.name = value;
    }

    /// <summary>
    /// Тег объекта. Тег должен быть заранее создан в настройках тегов
    /// </summary>
    public Строка тег
    {
        get => объект.tag;
        set => объект.tag = value;
    }

    /// <summary>
    /// Название слоя объекта. Слой должен быть заранее создан в настройках слоёв
    /// </summary>
    public Строка слой
    {
        get => LayerMask.LayerToName(объект.layer);
        set
        {
            int номер = LayerMask.NameToLayer(value);
            if (номер >= 0) объект.layer = номер;
            else Debug.LogWarning($"Слоя «{value}» нет в проекте", объект);
        }
    }

    /// <summary>
    /// Где находится объект
    /// </summary>
    public Направление3D позиция
    {
        get => объект.transform.position;
        set => объект.transform.position = value;
    }

    /// <summary>
    /// Поворот объекта в градусах
    /// </summary>
    public Направление3D поворот
    {
        get => объект.transform.eulerAngles;
        set => объект.transform.eulerAngles = value;
    }

    /// <summary>
    /// Размер объекта. 1 - обычный размер
    /// </summary>
    public Направление3D размер
    {
        get => объект.transform.localScale;
        set => объект.transform.localScale = value;
    }
}

/// <summary>
/// Действия с любым компонентом, который можно выключать. В событиях они лежат в разделе этого компонента
/// </summary>
public sealed class ДействияКомпонента
{
    readonly Component компонент;

    // Здесь нельзя добавлять скрытые методы: события показывают все методы этого класса
    internal ДействияКомпонента(Component компонент) => this.компонент = компонент;

    /// <summary>
    /// Включает компонент
    /// </summary>
    public void ВключитьКомпонент() => компонентВключён = true;

    /// <summary>
    /// Выключает компонент. Сам объект остаётся включённым
    /// </summary>
    public void ВыключитьКомпонент() => компонентВключён = false;

    /// <summary>
    /// Выключает включённый компонент и включает выключенный
    /// </summary>
    public void ПереключитьКомпонент() => компонентВключён = !компонентВключён;

    /// <summary>
    /// Удаляет компонент с объекта
    /// </summary>
    public void УдалитьКомпонент() => Object.Destroy(компонент);

    /// <summary>
    /// Если "да", то компонент включён
    /// </summary>
    public Флажок компонентВключён
    {
        get => компонент switch
        {
            Behaviour поведение => поведение.enabled,
            Renderer отрисовщик => отрисовщик.enabled,
            Collider коллайдер => коллайдер.enabled,
            _ => true
        };
        set
        {
            switch (компонент)
            {
                case Behaviour поведение: поведение.enabled = value; break;
                case Renderer отрисовщик: отрисовщик.enabled = value; break;
                case Collider коллайдер: коллайдер.enabled = value; break;
            }
        }
    }
}
