#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём в форме коробки
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("  Runity/ Физика 3D/ Коллайдеры/Коллайдер Куб")]
public class КоллайдерКуб : Коллайдер3D
{
    BoxCollider коробка => (BoxCollider)оригинальныйКомпонент;

    /// <summary>
    /// Размер коробки по трём осям
    /// </summary>
    public Направление3D размер { get => коробка.size; set => коробка.size = value; }

    /// <summary>
    /// Смещение коробки относительно объекта
    /// </summary>
    public Направление3D центр { get => коробка.center; set => коробка.center = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерКуб))]
internal class КоллайдерКубEditor : RunityEditor<КоллайдерКуб>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём в форме коробки");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial>(title, value));

        Пробел();

        Синхрополе("Размер", x => x.размер,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Синхрополе("Центр", x => x.центр,
            (title, value) => EditorGUILayout.Vector3Field(title, value));
    }
}
#endif
