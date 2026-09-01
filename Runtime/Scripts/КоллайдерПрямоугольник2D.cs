using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("  Runity/ Физика 2D/Коллайдер Прямоугольник 2D")]
public class КоллайдерПрямоугольник2D : Коллайдер2D
{
    protected override void Awake()
    {
        //оригинальныйКомпонент = GetComponent<BoxCollider2D>();
        base.Awake();
        ((BoxCollider2D)оригинал).autoTiling = true;
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
[CustomEditor(typeof(КоллайдерПрямоугольник2D))]
public class КоллайдерПрямоугольник2DEditor : RunityEditor<КоллайдерПрямоугольник2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет прямоугольный физический объём");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<PhysicsMaterial2D>(title, value));
    }

}
#endif