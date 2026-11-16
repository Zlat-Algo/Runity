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
[DefaultExecutionOrder(1000)]
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
    /// Насколько широко видит камера в перспективе, в градусах. Больше - шире, как у рыбьего глаза
    /// </summary>
    public Дробное уголОбзора
    {
        get => оригинал.fieldOfView;
        set => оригинал.fieldOfView = Mathf.Clamp(value, 1, 179);
    }

    /// <summary>
    /// Какие слои видит камера. Так можно спрятать от неё, например, слой интерфейса мини-карты
    /// </summary>
    public LayerMask слои
    {
        get => оригинал.cullingMask;
        set => оригинал.cullingMask = value;
    }

    /// <summary>
    /// Если камер несколько, та, у которой число больше, рисуется поверх
    /// </summary>
    public Дробное порядок
    {
        get => оригинал.depth;
        set => оригинал.depth = value;
    }

    /// <summary>
    /// Какую часть экрана занимает камера: от 0 до 1 по каждой стороне. Например, для игры вдвоём на одном экране
    /// </summary>
    public Rect областьЭкрана
    {
        get => оригинал.rect;
        set => оригинал.rect = value;
    }

    /// <summary>
    /// Если "да", на картинку камеры действуют эффекты постобработки: свечение, цветокоррекция и другие
    /// </summary>
    public Флажок постобработка
    {
        get => оригинал.GetUniversalAdditionalCameraData().renderPostProcessing;
        set => оригинал.GetUniversalAdditionalCameraData().renderPostProcessing = value;
    }

    /// <summary>
    /// Если "да", края объектов сглаживаются и не выглядят лесенкой
    /// </summary>
    public Флажок сглаживание
    {
        get => оригинал.GetUniversalAdditionalCameraData().antialiasing != AntialiasingMode.None;
        set => оригинал.GetUniversalAdditionalCameraData().antialiasing = value ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
    }

    /// <summary>
    /// Даёт "да", пока камера трясётся
    /// </summary>
    public Флажок трясётся => осталосьТрясти > 0;

    float силаТряски, длительностьТряски, осталосьТрясти;
    Vector3 смещениеТряски;

    /// <summary>
    /// Трясёт камеру: от удара, взрыва или землетрясения. Сила - насколько далеко она смещается, длительность - в секундах. Тряска плавно затухает
    /// </summary>
    public void Встряхнуть(Дробное сила, Дробное длительность)
    {
        силаТряски = Mathf.Max(силаТряски * (осталосьТрясти / Mathf.Max(длительностьТряски, 0.0001f)), сила);
        длительностьТряски = Mathf.Max(0.01f, длительность);
        осталосьТрясти = длительностьТряски;
    }

    /// <summary>
    /// Слегка трясёт камеру, как от удара
    /// </summary>
    public void Встряхнуть() => Встряхнуть(0.15f, 0.25f);

    // Тряска добавляется после всех движений камеры (например, Следования) и убирается перед следующим кадром,
    // поэтому камера не уплывает со своего места
    void Update()
    {
        if (!Application.isPlaying || смещениеТряски == Vector3.zero) return;
        transform.localPosition -= смещениеТряски;
        смещениеТряски = Vector3.zero;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || осталосьТрясти <= 0) return;
        осталосьТрясти -= Time.unscaledDeltaTime;
        float затухание = Mathf.Clamp01(осталосьТрясти / длительностьТряски);
        смещениеТряски = UnityEngine.Random.insideUnitSphere * силаТряски * затухание;
        transform.localPosition += смещениеТряски;
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

        if (компонент.перспектива)
        {
            Синхрополе("Угол обзора", x => x.уголОбзора,
                (title, value) => EditorGUILayout.Slider(title, value, 1, 179));
        }
        Синхрополе("Слои", x => x.слои,
            (title, value) => ПолеСлоёв(title, value));
        Синхрополе("Порядок", x => x.порядок,
            (title, value) => EditorGUILayout.FloatField(title, value));
        Синхрополе("Область экрана", x => x.областьЭкрана,
            (title, value) => EditorGUILayout.RectField(title, value));

        Пробел();

        Синхрополе("Постобработка", x => x.постобработка,
            (title, value) => EditorGUILayout.Toggle(title, value));
        Синхрополе("Сглаживание", x => x.сглаживание,
            (title, value) => EditorGUILayout.Toggle(title, value));

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