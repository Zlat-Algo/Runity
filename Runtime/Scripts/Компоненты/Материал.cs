#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Для чего материал
/// </summary>
public enum ВидМатериала { Обычный, БезОсвещения, Спрайт2D }

/// <summary>
/// Материал с понятными настройками: цвет, текстура, блеск, свечение и прозрачность. Внутри хранит настоящий материал Unity,
/// поэтому его можно перетаскивать в любое поле материала, например в Моделлер
/// </summary>
[CreateAssetMenu(fileName = "Новый материал", menuName = "Runity/Материал", order = -94)]
public class Материал : ScriptableObject
{
    [SerializeField] Material _оригинал;
    /// <summary>
    /// Настоящий материал Unity, который хранится внутри этого ассета
    /// </summary>
    public Material оригинал => _оригинал;

    public static implicit operator Material(Материал материал) => материал != null ? материал._оригинал : null;

    [SerializeField] ВидМатериала _вид;
    /// <summary>
    /// Обычный реагирует на свет, Без освещения всегда одного цвета, Спрайт 2D нужен для спрайтов с 2D-светом
    /// </summary>
    public ВидМатериала вид { get => _вид; set { _вид = value; Обновить(); } }

    [SerializeField] Color _цвет = Color.white;
    /// <summary>
    /// Цвет материала. Текстура умножается на него
    /// </summary>
    public Цвет цвет { get => _цвет; set { _цвет = value; Обновить(); } }

    [SerializeField] Texture2D _текстура;
    /// <summary>
    /// Картинка, которой покрыт объект
    /// </summary>
    public Texture2D текстура { get => _текстура; set { _текстура = value; Обновить(); } }

    [SerializeField] Texture2D _картаНеровностей;
    /// <summary>
    /// Карта нормалей: делает поверхность рельефной без лишних полигонов
    /// </summary>
    public Texture2D картаНеровностей { get => _картаНеровностей; set { _картаНеровностей = value; Обновить(); } }

    [SerializeField] Направление2D _повторТекстуры = Vector2.one;
    /// <summary>
    /// Сколько раз текстура повторяется по ширине и высоте
    /// </summary>
    public Направление2D повторТекстуры { get => _повторТекстуры; set { _повторТекстуры = value; Обновить(); } }

    [SerializeField, Range(0, 1)] float _металличность = 0;
    /// <summary>
    /// Насколько поверхность похожа на металл: от 0 до 1
    /// </summary>
    public Дробное металличность { get => _металличность; set { _металличность = Mathf.Clamp01(value); Обновить(); } }

    [SerializeField, Range(0, 1)] float _гладкость = 0.5f;
    /// <summary>
    /// Насколько поверхность гладкая и блестящая: от 0 до 1
    /// </summary>
    public Дробное гладкость { get => _гладкость; set { _гладкость = Mathf.Clamp01(value); Обновить(); } }

    [SerializeField, ColorUsage(false, true)] Color _свечение = Color.black;
    /// <summary>
    /// Цвет, которым материал светится сам. Чёрный - не светится
    /// </summary>
    public Цвет свечение { get => _свечение; set { _свечение = value; Обновить(); } }

    [SerializeField] Флажок _прозрачный = false;
    /// <summary>
    /// Если "да", то сквозь материал видно то, что за ним. Насколько прозрачно - задаётся непрозрачностью цвета
    /// </summary>
    public Флажок прозрачный { get => _прозрачный; set { _прозрачный = value; Обновить(); } }

    static bool urp => GraphicsSettings.currentRenderPipeline != null;

    Shader НужныйШейдер()
    {
        string имя = вид switch
        {
            ВидМатериала.БезОсвещения => urp ? "Universal Render Pipeline/Unlit" : "Unlit/Texture",
            ВидМатериала.Спрайт2D => urp ? "Universal Render Pipeline/2D/Sprite-Lit-Default" : "Sprites/Default",
            _ => urp ? "Universal Render Pipeline/Lit" : "Standard"
        };
        return Shader.Find(имя);
    }

    /// <summary>
    /// Переносит все настройки во внутренний материал. Вызывается само при любом изменении
    /// </summary>
    public void Обновить()
    {
        if (_оригинал == null) return;

        Shader шейдер = НужныйШейдер();
        if (шейдер != null && _оригинал.shader != шейдер) _оригинал.shader = шейдер;

        Задать("_BaseColor", _цвет);
        Задать("_Color", _цвет);
        Задать("_BaseMap", _текстура);
        Задать("_MainTex", _текстура);
        foreach (string имя in new[] { "_BaseMap", "_MainTex" })
        {
            if (_оригинал.HasProperty(имя)) _оригинал.SetTextureScale(имя, повторТекстуры);
        }

        Задать("_BumpMap", _картаНеровностей);
        Ключ("_NORMALMAP", _картаНеровностей != null);

        Задать("_Metallic", _металличность);
        Задать("_Smoothness", _гладкость);
        Задать("_Glossiness", _гладкость);

        Задать("_EmissionColor", _свечение);
        bool светится = _свечение.maxColorComponent > 0.001f;
        Ключ("_EMISSION", светится);
        _оригинал.globalIlluminationFlags = светится ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

        if (вид != ВидМатериала.Спрайт2D) НастроитьПрозрачность();

#if UNITY_EDITOR
        EditorUtility.SetDirty(_оригинал);
#endif
    }

    void НастроитьПрозрачность()
    {
        bool прозрачно = прозрачный;

        // В URP прозрачность включается через тип поверхности, в старом шейдере Standard - через режим
        Задать("_Surface", прозрачно ? 1f : 0f);
        Задать("_Blend", 0f);
        Задать("_Mode", прозрачно ? 3f : 0f);
        Задать("_SrcBlend", (float)(прозрачно ? BlendMode.SrcAlpha : BlendMode.One));
        Задать("_DstBlend", (float)(прозрачно ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        Задать("_SrcBlendAlpha", (float)(прозрачно ? BlendMode.One : BlendMode.One));
        Задать("_DstBlendAlpha", (float)(прозрачно ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        Задать("_ZWrite", прозрачно ? 0f : 1f);
        Ключ("_SURFACE_TYPE_TRANSPARENT", прозрачно && urp);
        Ключ("_ALPHAPREMULTIPLY_ON", false);
        Ключ("_ALPHABLEND_ON", прозрачно && !urp);
        _оригинал.SetOverrideTag("RenderType", прозрачно ? "Transparent" : "Opaque");
        _оригинал.renderQueue = прозрачно ? (int)RenderQueue.Transparent : -1;
        _оригинал.SetShaderPassEnabled("ShadowCaster", !прозрачно);
    }

    void Задать(string имя, Color значение) { if (_оригинал.HasProperty(имя)) _оригинал.SetColor(имя, значение); }
    void Задать(string имя, float значение) { if (_оригинал.HasProperty(имя)) _оригинал.SetFloat(имя, значение); }
    void Задать(string имя, Texture значение) { if (_оригинал.HasProperty(имя)) _оригинал.SetTexture(имя, значение); }

    void Ключ(string ключ, bool включён)
    {
        if (включён) _оригинал.EnableKeyword(ключ);
        else _оригинал.DisableKeyword(ключ);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_оригинал == null) EditorApplication.delayCall += СоздатьВнутреннийМатериал;
        else Обновить();
    }

    // Настоящий материал кладётся внутрь ассета, поэтому в окне Project его видно, если раскрыть ассет
    internal void СоздатьВнутреннийМатериал()
    {
        if (this == null || _оригинал != null || !AssetDatabase.Contains(this)) return;

        Shader шейдер = НужныйШейдер();
        if (шейдер == null) return;

        _оригинал = new Material(шейдер) { name = name };
        AssetDatabase.AddObjectToAsset(_оригинал, this);
        Обновить();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssetIfDirty(this);
    }
#endif
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Материал))]
internal class МатериалEditor : Editor
{
    // Картинка в окне Project: шар с этим материалом
    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        => ПревьюАссетов.ИзОбъекта(материал.оригинал, assetPath, width, height) ?? base.RenderStaticPreview(assetPath, subAssets, width, height);

    Материал материал => (Материал)target;
    Editor предпросмотр;

    void OnDisable()
    {
        if (предпросмотр != null) DestroyImmediate(предпросмотр);
    }

    void OnEnable()
    {
        foreach (Object цель in targets) ((Материал)цель).СоздатьВнутреннийМатериал();
    }

    GUIContent Подпись(string текст, string член) => new GUIContent(текст, Справка.Описание(typeof(Материал), член));

    void Поле(string название, string член)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_" + член), Подпись(название, член));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Перетащите этот материал на объект в сцене или в поле Материал у Моделлера", MessageType.None);
        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        Поле("Вид", "вид");
        Поле("Цвет", "цвет");
        Поле("Текстура", "текстура");
        if (материал.вид == ВидМатериала.Обычный)
        {
            Поле("Карта неровностей", "картаНеровностей");
        }
        if (материал.вид != ВидМатериала.Спрайт2D)
        {
            Поле("Повтор текстуры", "повторТекстуры");
        }
        if (материал.вид == ВидМатериала.Обычный)
        {
            Поле("Металличность", "металличность");
            Поле("Гладкость", "гладкость");
            Поле("Свечение", "свечение");
        }
        if (материал.вид != ВидМатериала.Спрайт2D)
        {
            Поле("Прозрачный", "прозрачный");
        }

        bool изменён = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (изменён) foreach (Object цель in targets) ((Материал)цель).Обновить();

        if (материал.оригинал != null)
        {
            EditorGUILayout.Space();
            Rect область = GUILayoutUtility.GetRect(64, 128, GUILayout.ExpandWidth(true));
            if (предпросмотр == null || предпросмотр.target != материал.оригинал)
            {
                if (предпросмотр != null) DestroyImmediate(предпросмотр);
                предпросмотр = CreateEditor(материал.оригинал);
            }
            предпросмотр.OnInteractivePreviewGUI(область, EditorStyles.helpBox);
        }

        EditorGUILayout.Space();
        Справка.ОтобразитьВИнспекторе(typeof(Материал), new[] { "вид", "цвет", "текстура", "картаНеровностей", "повторТекстуры", "металличность", "гладкость", "свечение", "прозрачный", "оригинал", "Обновить" });
    }
}
#endif
