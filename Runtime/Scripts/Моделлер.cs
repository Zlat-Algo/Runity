using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("  Runity/ Визуал 3D/Моделлер")]
public class Моделлер : RunityComponent
{
    public MeshFilter _meshFilter;
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
    }
    void OnValidate() => Awake();
    public MeshRenderer оригиналОтрисовщик => (MeshRenderer)оригинальныйКомпонент;
    public MeshFilter оригиналФильтр => _meshFilter;

    public Mesh модель { get => оригиналФильтр.sharedMesh; set => оригиналФильтр.sharedMesh = value; }
    public Material материал { get => оригиналОтрисовщик.sharedMaterial; set => оригиналОтрисовщик.sharedMaterial = value; }
    public bool отбрасываетТени
    {
        get => !(оригиналОтрисовщик.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off);
        set => оригиналОтрисовщик.shadowCastingMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Моделлер))]
public class МоделлерEditor : RunityEditor<Моделлер>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет визуальный объём");

        Пробел();

        Синхрополе("Модель", x => x.модель,
            (title, value) => Объект<Mesh>(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => Объект<Material>(title, value));

        Синхрополе("Отбрасывает тени", x => x.отбрасываетТени,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif