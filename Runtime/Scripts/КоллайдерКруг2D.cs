using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[AddComponentMenu("  Runity/ Физика 2D/Коллайдер Круг 2D")]
public class КоллайдерКруг2D : Коллайдер2D
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

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(КоллайдерКруг2D))]
public class КоллайдерКруг2DEditor : RunityEditor<КоллайдерКруг2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет круглый физический объём");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<PhysicsMaterial2D>(title, value));
    }

}
#endif