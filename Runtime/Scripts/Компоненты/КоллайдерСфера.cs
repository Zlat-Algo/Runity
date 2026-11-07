#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём в форме шара
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[AddComponentMenu("  Runity/ Физика 3D/ Коллайдеры/Коллайдер Сфера")]
public class КоллайдерСфера : Коллайдер3D
{
    SphereCollider сфера => (SphereCollider)оригинальныйКомпонент;

    /// <summary>
    /// Радиус шара
    /// </summary>
    public Дробное радиус { get => сфера.radius; set => сфера.radius = value; }

    /// <summary>
    /// Смещение шара относительно объекта
    /// </summary>
    public Направление3D центр { get => сфера.center; set => сфера.center = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерСфера))]
internal class КоллайдерСфераEditor : RunityEditor<КоллайдерСфера>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём в форме шара");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial>(title, value));

        Пробел();

        Синхрополе("Радиус", x => x.радиус,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Центр", x => x.центр,
            (title, value) => EditorGUILayout.Vector3Field(title, value));
    }
}
#endif
