#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём, повторяющий форму спрайта
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Коллайдеры/Коллайдер Полигональный 2D")]
public class КоллайдерПолигональный2D : Коллайдер2D
{
    protected override void Awake()
    {
        //оригинальныйКомпонент = GetComponent<BoxCollider2D>();
        base.Awake();
        ((PolygonCollider2D)оригинал).autoTiling = true;
    }
    /*void OnValidate() => Awake();
    public BoxCollider2D оригинал => (BoxCollider2D)оригинальныйКомпонент;

    public bool твёрдый
    {
        get => !оригинал.isTrigger;
        set => оригинал.isTrigger = !value;
    }

    public PhysicsMaterial2D материал
    {
        get => оригинал.sharedMaterial;
        set => оригинал.sharedMaterial = value;
    }*/
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерПолигональный2D))]
internal class КоллайдерПолигональный2DEditor : RunityEditor<КоллайдерПолигональный2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём, состоящий из настраиваемых полигонов");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial2D>(title, value));
    }

}
#endif