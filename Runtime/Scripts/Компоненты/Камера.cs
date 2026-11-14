using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Как камера рисует мир: как задано в проекте, по-3D (со светом и тенями) или по-2D (со светом для спрайтов)
/// </summary>
public enum ОтрисовкаКамеры { КакВПроекте, Отрисовка3D, Отрисовка2D }

/// <summary>
/// Камера, через которую игрок видит игру
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("  Runity/Камера")]
public class Камера : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<Camera>();
        //if (TryGetComponent(out UniversalAdditionalCameraData cameraData)) twinComponent = cameraData;
        главная = this;
        ПрименитьОтрисовку();
    }
    void OnValidate() => Awake();

    [SerializeField] ОтрисовкаКамеры _отрисовка = ОтрисовкаКамеры.КакВПроекте;
    /// <summary>
    /// Как камера рисует мир. В 2D-проекте 3D-сцена без этой настройки остаётся без света и теней: выберите "Отрисовка 3D".
    /// Если нужного отрисовщика нет в настройках URP, Runity добавит его туда сам, не меняя основной
    /// </summary>
    public ОтрисовкаКамеры отрисовка { get => _отрисовка; set { _отрисовка = value; ПрименитьОтрисовку(true); } }

    // Во время OnValidate создавать ассеты нельзя, поэтому отрисовщик добавляется чуть позже
    void ПрименитьОтрисовку(bool сразу = false)
    {
        if (оригинал == null || отрисовка == ОтрисовкаКамеры.КакВПроекте) return;
        if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return;

        int номер = НомерОтрисовщика(urp, отрисовка == ОтрисовкаКамеры.Отрисовка2D);
        if (номер >= 0)
        {
            оригинал.GetUniversalAdditionalCameraData().SetRenderer(номер);
            return;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            bool нужен2D = отрисовка == ОтрисовкаКамеры.Отрисовка2D;
            if (сразу)
            {
                ДобавитьОтрисовщик(urp, нужен2D);
                ПрименитьОтрисовку();
                return;
            }
            EditorApplication.delayCall += () =>
            {
                if (НомерОтрисовщика(urp, нужен2D) < 0) ДобавитьОтрисовщик(urp, нужен2D);
                if (this != null) ПрименитьОтрисовку();
            };
        }
#endif
    }

    static int НомерОтрисовщика(UniversalRenderPipelineAsset urp, bool нужен2D)
    {
        ReadOnlySpan<ScriptableRendererData> список = urp.rendererDataList;
        for (int i = 0; i < список.Length; i++)
        {
            if (список[i] != null && (список[i] is Renderer2DData) == нужен2D) return i;
        }
        return -1;
    }

#if UNITY_EDITOR
    // Добавляет отрисовщик в список настроек URP. Основной отрисовщик проекта не меняется
    static void ДобавитьОтрисовщик(UniversalRenderPipelineAsset urp, bool нужен2D)
    {
        string папка = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(urp)).Replace('\\', '/');
        ScriptableRendererData новый = нужен2D ? ScriptableObject.CreateInstance<Renderer2DData>() : (ScriptableRendererData)ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(новый, AssetDatabase.GenerateUniqueAssetPath($"{папка}/{(нужен2D ? "Renderer2D" : "Renderer3D")}.asset"));

        SerializedObject ассет = new SerializedObject(urp);
        SerializedProperty список = ассет.FindProperty("m_RendererDataList");
        список.arraySize++;
        список.GetArrayElementAtIndex(список.arraySize - 1).objectReferenceValue = новый;
        ассет.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Консоль.Напечатать($"Runity добавил {(нужен2D ? "2D" : "3D")}-отрисовщик в настройки URP ({AssetDatabase.GetAssetPath(urp)}), чтобы камера могла рисовать так, как выбрано. Основной отрисовщик проекта не изменился");
    }
#endif

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Camera оригинал => (Camera)оригинальныйКомпонент;

    /// <summary>
    /// "Да" для 3D-игр: дальние объекты кажутся меньше. "Нет" для 2D-игр
    /// </summary>
    public Флажок перспектива
    {
        get => !оригинал.orthographic;
        set => оригинал.orthographic = !value;
    }

    /// <summary>
    /// Сколько мира видно в 2D: чем больше число, тем дальше камера
    /// </summary>
    public Дробное размер
    {
        get => оригинал.orthographicSize;
        set => оригинал.orthographicSize = Математика.ОграничитьСнизу(value, 0.0001);
    }

    /// <summary>
    /// Объекты ближе этого расстояния не видны
    /// </summary>
    public Дробное близостьОтрисовки
    {
        get => оригинал.nearClipPlane;
        set
        {
            оригинал.nearClipPlane = Математика.ОграничитьСнизу(value, 0.0001);
            if (дальностьОтрисовки - value < 0.0001)
            {
                дальностьОтрисовки = value + 0.1;
            }
        }
    }

    /// <summary>
    /// Объекты дальше этого расстояния не видны
    /// </summary>
    public Дробное дальностьОтрисовки
    {
        get => оригинал.farClipPlane;
        set
        {
            оригинал.farClipPlane = Математика.ОграничитьСнизу(value, 0.0001);
            if (value - близостьОтрисовки < 0.0001)
            {
                близостьОтрисовки = value - 0.1;
            }
        }
    }

    /// <summary>
    /// Если "да", то фон будет одним цветом, а если "нет", то небом
    /// </summary>
    public Флажок залитьФонЦветом
    {
        get => оригинал.clearFlags == CameraClearFlags.SolidColor;
        set => оригинал.clearFlags = value ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
    }

    /// <summary>
    /// Цвет фона
    /// </summary>
    public Цвет фоновыйЦвет
    {
        get => оригинал.backgroundColor;
        set => оригинал.backgroundColor = value;
    }

    /// <summary>
    /// Камера, созданная последней
    /// </summary>
    public static Камера главная;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Камера))]
internal class КамераEditor : RunityEditor<Камера>
{
    protected override void Отрисовать()
    {
        Текст("Через этот объект смотрит игрок");

        Пробел();

        Синхрополе("Близость отрисовки", x => x.близостьОтрисовки,
            (title, value) => EditorGUILayout.FloatField(title, value));
        Синхрополе("Дальность отрисовки", x => x.дальностьОтрисовки,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Синхрополе("Перспектива", x => x.перспектива,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (!компонент.перспектива)
        {
            Синхрополе("Размер", x => x.размер,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        Синхрополе("Отрисовка", x => x.отрисовка,
            (title, value) => (ОтрисовкаКамеры)EditorGUILayout.EnumPopup(title, value));

        Пробел();

        Синхрополе("Залить фон цветом", x => x.залитьФонЦветом,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.залитьФонЦветом)
        {
            Синхрополе("Фоновый цвет", x => x.фоновыйЦвет,
                (title, value) => EditorGUILayout.ColorField(title, value));
        }
    }
}
#endif