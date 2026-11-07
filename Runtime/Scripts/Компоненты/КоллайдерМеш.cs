#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём точно по форме 3D-модели. Подходит для неподвижных стен, скал и зданий
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshCollider))]
[AddComponentMenu("  Runity/ Физика 3D/ Коллайдеры/Коллайдер Меш")]
public class КоллайдерМеш : Коллайдер3D
{
    MeshCollider меш => (MeshCollider)оригинальныйКомпонент;

    /// <summary>
    /// По какой модели строится объём. Обычно это та же модель, что у Моделлера
    /// </summary>
    public Mesh модель { get => меш.sharedMesh; set => меш.sharedMesh = value; }

    /// <summary>
    /// Если "да", то объём упрощается до выпуклой оболочки. Нужно для движущихся объектов и нетвёрдых коллайдеров
    /// </summary>
    public Флажок выпуклый { get => меш.convex; set => меш.convex = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерМеш))]
internal class КоллайдерМешEditor : RunityEditor<КоллайдерМеш>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём точно по форме модели");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial>(title, value));

        Пробел();

        Синхрополе("Модель", x => x.модель,
            (title, value) => ПолеОбъекта<Mesh>(title, value));

        Синхрополе("Выпуклый", x => x.выпуклый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (!компонент.выпуклый && (!компонент.твёрдый || компонент.TryGetComponent(out ФизическоеТело _)))
        {
            Текст("Невыпуклый коллайдер может быть только твёрдым и неподвижным. Включите Выпуклый", MessageType.Warning);
        }
    }
}
#endif
