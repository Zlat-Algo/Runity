#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём в форме капсулы. Лучше всего подходит для персонажей
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
[AddComponentMenu("  Runity/ Физика 3D/ Коллайдеры/Коллайдер Капсула")]
public class КоллайдерКапсула : Коллайдер3D
{
    CapsuleCollider капсула => (CapsuleCollider)оригинальныйКомпонент;

    /// <summary>
    /// Радиус капсулы
    /// </summary>
    public Дробное радиус { get => капсула.radius; set => капсула.radius = value; }

    /// <summary>
    /// Полная высота капсулы
    /// </summary>
    public Дробное высота { get => капсула.height; set => капсула.height = value; }

    /// <summary>
    /// Смещение капсулы относительно объекта
    /// </summary>
    public Направление3D центр { get => капсула.center; set => капсула.center = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерКапсула))]
internal class КоллайдерКапсулаEditor : RunityEditor<КоллайдерКапсула>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём в форме капсулы");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial>(title, value));

        Пробел();

        Синхрополе("Радиус", x => x.радиус,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Высота", x => x.высота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Центр", x => x.центр,
            (title, value) => EditorGUILayout.Vector3Field(title, value));
    }
}
#endif
