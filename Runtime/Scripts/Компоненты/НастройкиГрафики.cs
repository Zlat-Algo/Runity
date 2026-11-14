#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Как игра занимает экран
/// </summary>
public enum РежимЭкрана { НеМенять, ПолныйЭкран, ОкноНаВесьЭкран, Окно }

/// <summary>
/// Насколько сильно сглаживаются лесенки на краях объектов
/// </summary>
public enum Сглаживание { Нет, Двойное, Четверное, Восьмикратное }

/// <summary>
/// В каком качестве грузятся текстуры
/// </summary>
public enum КачествоТекстур { Полное, Половина, Четверть, Восьмая }

/// <summary>
/// Настройки графики игры в одном месте. Применяются сами при запуске игры, поэтому лезть в настройки проекта не нужно.
/// Их можно менять и во время игры, например из меню настроек
/// </summary>
[CreateAssetMenu(fileName = имяВРесурсах, menuName = "Runity/Настройки графики", order = -96)]
public class НастройкиГрафики : ScriptableObject
{
    const string имяВРесурсах = "НастройкиГрафики";

    [SerializeField] Флажок _применятьПриЗапуске = true;
    /// <summary>
    /// Если "да", то настройки применяются сами при запуске игры
    /// </summary>
    public Флажок применятьПриЗапуске { get => _применятьПриЗапуске; set => _применятьПриЗапуске = value; }

    [SerializeField] int _уровеньКачества = -1;
    /// <summary>
    /// Какой уровень качества из настроек Unity включить. -1 означает, что уровень не меняется
    /// </summary>
    public Число уровеньКачества { get => _уровеньКачества; set { _уровеньКачества = value; ПрименитьВИгре(); } }

    [SerializeField] Флажок _вертикальнаяСинхронизация = true;
    /// <summary>
    /// Если "да", то кадры подстраиваются под частоту монитора. Картинка не рвётся, но кадров не больше, чем у монитора
    /// </summary>
    public Флажок вертикальнаяСинхронизация { get => _вертикальнаяСинхронизация; set { _вертикальнаяСинхронизация = value; ПрименитьВИгре(); } }

    [SerializeField] Число _ограничениеКадров = 0;
    /// <summary>
    /// Сколько кадров в секунду рисовать не больше. 0 означает, что кадры не ограничены. Работает, когда выключена вертикальная синхронизация
    /// </summary>
    public Число ограничениеКадров { get => _ограничениеКадров; set { _ограничениеКадров = value; ПрименитьВИгре(); } }

    [SerializeField] РежимЭкрана _режимЭкрана = РежимЭкрана.НеМенять;
    /// <summary>
    /// Как игра занимает экран. В редакторе не меняется, только в собранной игре
    /// </summary>
    public РежимЭкрана режимЭкрана { get => _режимЭкрана; set { _режимЭкрана = value; ПрименитьВИгре(); } }

    [SerializeField] Число _ширинаОкна = 1280;
    /// <summary>
    /// Ширина окна игры в режиме «Окно»
    /// </summary>
    public Число ширинаОкна { get => _ширинаОкна; set { _ширинаОкна = value; ПрименитьВИгре(); } }

    [SerializeField] Число _высотаОкна = 720;
    /// <summary>
    /// Высота окна игры в режиме «Окно»
    /// </summary>
    public Число высотаОкна { get => _высотаОкна; set { _высотаОкна = value; ПрименитьВИгре(); } }

    [SerializeField] Сглаживание _сглаживание = Сглаживание.Нет;
    /// <summary>
    /// Сглаживание краёв объектов. Чем оно сильнее, тем красивее и тем больше нагрузка
    /// </summary>
    public Сглаживание сглаживание { get => _сглаживание; set { _сглаживание = value; ПрименитьВИгре(); } }

    [SerializeField] Дробное _масштабОтрисовки = 1;
    /// <summary>
    /// Во сколько раз изменить чёткость картинки. Меньше 1 - мыльнее, но быстрее. Больше 1 - чётче, но медленнее
    /// </summary>
    public Дробное масштабОтрисовки { get => _масштабОтрисовки; set { _масштабОтрисовки = Mathf.Clamp(value, 0.1f, 2f); ПрименитьВИгре(); } }

    [SerializeField] Флажок _тени = true;
    /// <summary>
    /// Если "нет", то 3D-тени не рисуются. Это сильно ускоряет игру на слабых компьютерах
    /// </summary>
    public Флажок тени { get => _тени; set { _тени = value; ПрименитьВИгре(); } }

    [SerializeField] Дробное _дальностьТеней = 50;
    /// <summary>
    /// Как далеко от камеры видны тени
    /// </summary>
    public Дробное дальностьТеней { get => _дальностьТеней; set { _дальностьТеней = value; ПрименитьВИгре(); } }

    [SerializeField] КачествоТекстур _качествоТекстур = КачествоТекстур.Полное;
    /// <summary>
    /// В каком качестве грузить текстуры. Пониженное качество экономит видеопамять
    /// </summary>
    public КачествоТекстур качествоТекстур { get => _качествоТекстур; set { _качествоТекстур = value; ПрименитьВИгре(); } }

    [SerializeField] Флажок _чёткиеТекстурыПодУглом = true;
    /// <summary>
    /// Если "да", то текстуры на полу и стенах, видимые под углом, остаются чёткими
    /// </summary>
    public Флажок чёткиеТекстурыПодУглом { get => _чёткиеТекстурыПодУглом; set { _чёткиеТекстурыПодУглом = value; ПрименитьВИгре(); } }

    /// <summary>
    /// Настройки графики, которые применились при запуске игры
    /// </summary>
    public static НастройкиГрафики текущие { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ПриЗапускеИгры()
    {
        текущие = Resources.Load<НастройкиГрафики>(имяВРесурсах);
        if (текущие != null && текущие.применятьПриЗапуске) текущие.Применить();
    }

    void ПрименитьВИгре()
    {
        if (Application.isPlaying) Применить();
    }

    /// <summary>
    /// Применяет все настройки прямо сейчас
    /// </summary>
    public void Применить()
    {
        if (_уровеньКачества >= 0 && _уровеньКачества < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != _уровеньКачества)
            QualitySettings.SetQualityLevel(_уровеньКачества, true);

        QualitySettings.vSyncCount = вертикальнаяСинхронизация ? 1 : 0;
        Application.targetFrameRate = ограничениеКадров > 0 ? (int)ограничениеКадров : -1;

        int выборка = сглаживание switch { Сглаживание.Двойное => 2, Сглаживание.Четверное => 4, Сглаживание.Восьмикратное => 8, _ => 1 };
        QualitySettings.antiAliasing = выборка == 1 ? 0 : выборка;
        QualitySettings.globalTextureMipmapLimit = (int)качествоТекстур;
        QualitySettings.anisotropicFiltering = чёткиеТекстурыПодУглом ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;
        QualitySettings.shadows = тени ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
        QualitySettings.shadowDistance = тени ? (float)дальностьТеней : 0;

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
#if UNITY_EDITOR
            ЗапомнитьНастройкиURP(urp);
#endif
            urp.msaaSampleCount = выборка;
            urp.renderScale = Mathf.Clamp(масштабОтрисовки, 0.1f, 2f);
            urp.shadowDistance = тени ? (float)дальностьТеней : 0;
        }

        if (!Application.isEditor && режимЭкрана != РежимЭкрана.НеМенять)
        {
            FullScreenMode режим = режимЭкрана switch
            {
                РежимЭкрана.ПолныйЭкран => FullScreenMode.ExclusiveFullScreen,
                РежимЭкрана.ОкноНаВесьЭкран => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
            bool окно = режим == FullScreenMode.Windowed;
            Screen.SetResolution(окно ? (int)ширинаОкна : Screen.currentResolution.width, окно ? (int)высотаОкна : Screen.currentResolution.height, режим);
        }
    }

#if UNITY_EDITOR
    // В редакторе настройки конвейера хранятся в файле, поэтому после выхода из игры их нужно вернуть как было
    static UniversalRenderPipelineAsset изменённый;
    static int былаВыборка;
    static float былМасштаб, былаДальность;

    static void ЗапомнитьНастройкиURP(UniversalRenderPipelineAsset urp)
    {
        if (изменённый != null) return;
        изменённый = urp;
        былаВыборка = urp.msaaSampleCount;
        былМасштаб = urp.renderScale;
        былаДальность = urp.shadowDistance;
        EditorApplication.playModeStateChanged += ВернутьНастройкиURP;
    }

    static void ВернутьНастройкиURP(PlayModeStateChange состояние)
    {
        if (состояние != PlayModeStateChange.ExitingPlayMode && состояние != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ВернутьНастройкиURP;
        if (изменённый == null) return;

        изменённый.msaaSampleCount = былаВыборка;
        изменённый.renderScale = былМасштаб;
        изменённый.shadowDistance = былаДальность;
        изменённый = null;
    }

    internal const string путь = "Assets/Resources/" + имяВРесурсах + ".asset";

    /// <summary>
    /// Находит настройки графики проекта или создаёт их и показывает в инспекторе
    /// </summary>
    [MenuItem("Runity/Настройки графики", false, 1)]
    public static void ОткрытьВРедакторе()
    {
        НастройкиГрафики настройки = AssetDatabase.LoadAssetAtPath<НастройкиГрафики>(путь);
        if (настройки == null)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:НастройкиГрафики"))
            {
                настройки = AssetDatabase.LoadAssetAtPath<НастройкиГрафики>(AssetDatabase.GUIDToAssetPath(guid));
                if (настройки != null) break;
            }
        }

        if (настройки == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(путь));
            настройки = CreateInstance<НастройкиГрафики>();
            AssetDatabase.CreateAsset(настройки, путь);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = настройки;
        EditorGUIUtility.PingObject(настройки);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(НастройкиГрафики))]
internal class НастройкиГрафикиEditor : Editor
{
    НастройкиГрафики настройки => (НастройкиГрафики)target;

    GUIContent Подпись(string текст, string член) => new GUIContent(текст, Справка.Описание(typeof(НастройкиГрафики), член));

    void Поле(string название, string член)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_" + член), Подпись(название, член));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Здесь собраны настройки графики игры. Они применятся сами, когда игра запустится", MessageType.None);

        if (AssetDatabase.GetAssetPath(настройки) != НастройкиГрафики.путь)
        {
            EditorGUILayout.HelpBox("Игра найдёт эти настройки, только если они лежат в папке Assets/Resources и называются НастройкиГрафики", MessageType.Warning);
            if (GUILayout.Button("Перенести в нужное место"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(НастройкиГрафики.путь));
                AssetDatabase.Refresh();
                string ошибка = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(настройки), НастройкиГрафики.путь);
                if (!string.IsNullOrEmpty(ошибка)) Debug.LogError(ошибка);
            }
        }

        EditorGUILayout.Space();
        serializedObject.Update();

        Поле("Применять при запуске", "применятьПриЗапуске");

        SerializedProperty уровень = serializedObject.FindProperty("_уровеньКачества");
        string[] уровни = new[] { "Не менять" }.Concat(QualitySettings.names).ToArray();
        уровень.intValue = EditorGUILayout.Popup(Подпись("Уровень качества", "уровеньКачества"), уровень.intValue + 1, уровни.Select(у => new GUIContent(у)).ToArray()) - 1;

        EditorGUILayout.Space();
        Поле("Вертикальная синхронизация", "вертикальнаяСинхронизация");
        if (!настройки.вертикальнаяСинхронизация) Поле("Ограничение кадров", "ограничениеКадров");
        Поле("Режим экрана", "режимЭкрана");
        if (настройки.режимЭкрана == РежимЭкрана.Окно)
        {
            Поле("Ширина окна", "ширинаОкна");
            Поле("Высота окна", "высотаОкна");
        }

        EditorGUILayout.Space();
        Поле("Сглаживание", "сглаживание");
        SerializedProperty масштаб = serializedObject.FindProperty("_масштабОтрисовки").FindPropertyRelative("значение");
        масштаб.floatValue = EditorGUILayout.Slider(Подпись("Масштаб отрисовки", "масштабОтрисовки"), масштаб.floatValue, 0.1f, 2f);
        Поле("Тени", "тени");
        if (настройки.тени) Поле("Дальность теней", "дальностьТеней");
        Поле("Качество текстур", "качествоТекстур");
        Поле("Чёткие текстуры под углом", "чёткиеТекстурыПодУглом");

        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button(Подпись("Применить сейчас", "Применить"))) настройки.Применить();
        }

        EditorGUILayout.Space();
        Справка.ОтобразитьВИнспекторе(typeof(НастройкиГрафики), new[] { "применятьПриЗапуске", "уровеньКачества", "вертикальнаяСинхронизация", "ограничениеКадров", "режимЭкрана", "ширинаОкна", "высотаОкна", "сглаживание", "масштабОтрисовки", "тени", "дальностьТеней", "качествоТекстур", "чёткиеТекстурыПодУглом", "Применить", "ОткрытьВРедакторе" });
    }
}
#endif
