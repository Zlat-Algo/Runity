#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Физический объём в форме линии
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EdgeCollider2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Коллайдеры/Коллайдер Линия 2D")]
public class КоллайдерЛиния2D : Коллайдер2D
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
[CustomEditor(typeof(КоллайдерЛиния2D))]
internal class КоллайдерЛиния2DEditor : RunityEditor<КоллайдерЛиния2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект имеет физический объём в виде линии");

        Пробел();

        Синхрополе("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<PhysicsMaterial2D>(title, value));
    }

}
#endif