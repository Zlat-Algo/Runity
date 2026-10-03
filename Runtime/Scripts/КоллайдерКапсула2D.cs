using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
[AddComponentMenu("  Runity/ Физика 2D/Коллайдер Капсула 2D")]
public class КоллайдерКапсула2D : Коллайдер2D
{
    /*void Awake()
    {
        оригинальныйКомпонент = GetComponent<CircleCollider2D>();
        //оригинал.autoTiling = true;
    }
    void OnValidate() => Awake();
    public CircleCollider2D оригинал => (CircleCollider2D)оригинальныйКомпонент;

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

[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерКапсула2D))]
public class КоллайдерКапсула2DEditor : RunityEditor<КоллайдерКапсула2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет капсульный физический объём");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<PhysicsMaterial2D>(title, value));
    }

}
