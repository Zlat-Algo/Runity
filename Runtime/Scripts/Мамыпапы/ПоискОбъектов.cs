using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Куда смотрит объект в 2D, если его не поворачивали
/// </summary>
public enum НаправлениеВзгляда2D { Вправо, Вверх, Влево, Вниз }

/// <summary>
/// Общие проверки для компонентов, которым нужно искать объекты вокруг себя
/// </summary>
public static class ПоискОбъектов
{
    /// <summary>
    /// Даёт все объекты с коллайдерами внутри круга (в 2D) или шара (в 3D)
    /// </summary>
    public static List<GameObject> ВРадиусе(Vector3 центр, float радиус, bool режим2D, int слои = ~0)
    {
        List<GameObject> результат = new List<GameObject>();

        void Добавить(GameObject владелец)
        {
            if (!результат.Contains(владелец))
                результат.Add(владелец);
        }

        if (режим2D)
        {
            foreach (Collider2D коллайдер in Physics2D.OverlapCircleAll(центр, радиус, слои))
                Добавить(коллайдер.attachedRigidbody != null ? коллайдер.attachedRigidbody.gameObject : коллайдер.gameObject);
        }
        else
        {
            foreach (Collider коллайдер in Physics.OverlapSphere(центр, радиус, слои, QueryTriggerInteraction.Collide))
                Добавить(коллайдер.attachedRigidbody != null ? коллайдер.attachedRigidbody.gameObject : коллайдер.gameObject);
        }

        return результат;
    }

    /// <summary>
    /// Куда смотрит объект. В 2D учитывается его поворот, а если есть Спрайтер, то и отзеркаливание
    /// </summary>
    public static Vector3 Взгляд(Transform кто, bool режим2D, НаправлениеВзгляда2D взгляд2D, bool учитыватьОтзеркаливание)
    {
        if (!режим2D) return кто.forward;

        Vector3 направление = взгляд2D switch
        {
            НаправлениеВзгляда2D.Вверх => кто.up,
            НаправлениеВзгляда2D.Влево => -кто.right,
            НаправлениеВзгляда2D.Вниз => -кто.up,
            _ => кто.right
        };

        if (учитыватьОтзеркаливание && кто.TryGetComponent(out SpriteRenderer отрисовщик) && отрисовщик.flipX)
            направление.x = -направление.x;

        return направление;
    }

    /// <summary>
    /// Даёт "да", если цель находится внутри конуса с указанным углом
    /// </summary>
    public static bool ВСекторе(Vector3 откуда, Vector3 взгляд, Vector3 куда, float угол, bool режим2D)
    {
        if (угол >= 360) return true;

        Vector3 доЦели = куда - откуда;
        if (режим2D)
        {
            доЦели.z = 0;
            взгляд.z = 0;
        }

        return доЦели == Vector3.zero || Vector3.Angle(взгляд, доЦели) <= угол / 2;
    }

    /// <summary>
    /// Даёт "да", если между двумя объектами нет препятствий. Триггеры препятствиями не считаются
    /// </summary>
    public static bool ПутьСвободен(Transform кто, Transform цель, bool режим2D, int слоиПрепятствий = ~0)
    {
        if (режим2D)
        {
            Vector3 откуда = кто.position;
            Vector3 куда = цель.position;
            откуда.z = 0;
            куда.z = 0;

            foreach (RaycastHit2D попадание in Physics2D.LinecastAll(откуда, куда, слоиПрепятствий))
            {
                if (попадание.collider.isTrigger || Свой(попадание.transform, кто)) continue;
                return Свой(попадание.transform, цель);
            }
            return true;
        }

        Vector3 направление = цель.position - кто.position;
        RaycastHit[] попадания = Physics.RaycastAll(кто.position, направление.normalized, направление.magnitude, слоиПрепятствий, QueryTriggerInteraction.Ignore);
        System.Array.Sort(попадания, (первое, второе) => первое.distance.CompareTo(второе.distance));

        foreach (RaycastHit попадание in попадания)
        {
            if (Свой(попадание.transform, кто)) continue;
            return Свой(попадание.transform, цель);
        }
        return true;
    }

    /// <summary>
    /// Даёт "да", если это тот же объект, его родитель или дочерний объект
    /// </summary>
    public static bool Свой(Transform первый, Transform второй)
        => первый.IsChildOf(второй) || второй.IsChildOf(первый);

    /// <summary>
    /// Даёт "да", если тег не указан или совпадает с тегом объекта
    /// </summary>
    public static bool ТегПодходит(GameObject объект, string тег)
        => string.IsNullOrEmpty(тег) || тег == "Untagged" || объект.CompareTag(тег);

    /// <summary>
    /// Даёт "да", если теги не выбраны или у объекта есть один из них
    /// </summary>
    public static bool ТегПодходит(GameObject объект, Теги теги)
        => теги == null || теги.Подходит(объект);

    // Раньше в компонентах хранился один тег. Он переносится в список, чтобы старые сцены не сломались
    internal static void ПеренестиТег(ref Строка старый, Теги новые)
    {
        string тег = старый;
        if (!string.IsNullOrEmpty(тег) && тег != "Untagged" && новые.любые) новые.Добавить(тег);
        старый = "";
    }
}
