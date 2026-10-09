using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
//[AddComponentMenu("  Runity/ Физика 2D/Коллайдер Круг 2D")]
public class Коллайдер2D : RunityComponent
{
    protected virtual void Awake()
    {
        оригинальныйКомпонент = GetComponent<Collider2D>();
    }
    void OnValidate() => Awake();
    public Collider2D оригинал => (Collider2D)оригинальныйКомпонент;

    public bool твёрдый
    {
        get => !оригинал.isTrigger;
        set => оригинал.isTrigger = !value;
    }

    public PhysicsMaterial2D материал
    {
        get => оригинал.sharedMaterial;
        set => оригинал.sharedMaterial = value;
    }
}

/*[CanEditMultipleObjects]
[CustomEditor(typeof(Коллайдер2D))]
public class Коллайдер2DEditor : RunityEditor<Коллайдер2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет физический объём");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<PhysicsMaterial2D>(title, value));
    }

}*/
