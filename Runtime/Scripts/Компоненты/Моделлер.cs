#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Готовые 3D-формы
/// </summary>
public enum ФормаМодели { Куб, Сфера, Капсула, Цилиндр, Плоскость, Квадрат }

/// <summary>
/// Показывает 3D-модель
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("  Runity/ Визуал/ 3D/Моделлер")]
public class Моделлер : RunityComponent
{
    [SerializeField, HideInInspector] MeshFilter _meshFilter;
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
    }
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity, который рисует модель
    /// </summary>
    public MeshRenderer оригиналОтрисовщик => (MeshRenderer)оригинальныйКомпонент;
    /// <summary>
    /// Оригинальный компонент Unity, который хранит модель
    /// </summary>
    public MeshFilter оригиналФильтр => _meshFilter;

    /// <summary>
    /// Какая 3D-модель показывается
    /// </summary>
    public Mesh модель { get => оригиналФильтр.sharedMesh; set => оригиналФильтр.sharedMesh = value; }
    /// <summary>
    /// Как выглядит поверхность модели
    /// </summary>
    public Material материал { get => оригиналОтрисовщик.sharedMaterial; set => оригиналОтрисовщик.sharedMaterial = value; }
    /// <summary>
    /// Появляется ли от объекта тень
    /// </summary>
    /// <summary>
    /// Ставит готовую форму: куб, шар и другие. Если материала нет, ставит стандартный
    /// </summary>
    public void СделатьФорму(ФормаМодели форма)
    {
        PrimitiveType тип = форма switch
        {
            ФормаМодели.Сфера => PrimitiveType.Sphere,
            ФормаМодели.Капсула => PrimitiveType.Capsule,
            ФормаМодели.Цилиндр => PrimitiveType.Cylinder,
            ФормаМодели.Плоскость => PrimitiveType.Plane,
            ФормаМодели.Квадрат => PrimitiveType.Quad,
            _ => PrimitiveType.Cube
        };

        // Готовые формы Unity умеет создавать только целым объектом, поэтому берём у временного объекта модель и материал
        GameObject образец = GameObject.CreatePrimitive(тип);
        модель = образец.GetComponent<MeshFilter>().sharedMesh;
        if (материал == null) материал = образец.GetComponent<MeshRenderer>().sharedMaterial;
        DestroyImmediate(образец);
    }

    /// <summary>
    /// Появляется ли от объекта тень
    /// </summary>
    public bool отбрасываетТени
    {
        get => !(оригиналОтрисовщик.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off);
        set => оригиналОтрисовщик.shadowCastingMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Моделлер))]
internal class МоделлерEditor : RunityEditor<Моделлер>
{
    ФормаМодели форма;

    protected override void Отрисовать()
    {
        Текст("Этот объект имеет визуальный объём");

        Пробел();

        Синхрополе("Модель", x => x.модель,
            (title, value) => ПолеОбъекта<Mesh>(title, value));

        Синхрополе("Материал", x => x.материал,
            (title, value) => ПолеОбъекта<Material>(title, value));

        Синхрополе("Отбрасывает тени", x => x.отбрасываетТени,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        форма = (ФормаМодели)EditorGUILayout.EnumPopup(Подпись("Готовая форма", nameof(Моделлер.СделатьФорму)), форма);
        if (GUILayout.Button("Сделать", GUILayout.Width(90)))
        {
            foreach (Моделлер моделлер in components)
            {
                Undo.RecordObjects(new Object[] { моделлер.оригиналФильтр, моделлер.оригиналОтрисовщик }, "Готовая форма");
                моделлер.СделатьФорму(форма);
            }
        }
        ЗакончитьГоризонтальнуюГруппу();
    }
}
#endif