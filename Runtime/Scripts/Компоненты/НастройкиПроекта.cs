#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif
using UnityEngine;

/// <summary>
/// Чем рисуется игра. URP умеет 2D-свет, тени и эффекты, встроенный конвейер проще, но Unity его больше не развивает
/// </summary>
public enum КонвейерОтрисовки { Встроенный, URP }

/// <summary>
/// Для какой игры настроен проект
/// </summary>
public enum ВидПроекта { Проект2D, Проект3D }

/// <summary>
/// Как игра узнаёт о нажатиях клавиш. Runity работает с новой системой ввода
/// </summary>
public enum СистемаВвода { Старая, Новая, Обе }

/// <summary>
/// Как смешиваются цвета и свет. Линейное пространство даёт более правильный свет
/// </summary>
public enum ЦветовоеПространство { Линейное, Гамма }

/// <summary>
/// Главные настройки проекта в одном месте: название и иконка игры, 2D или 3D, конвейер отрисовки, система ввода и окно игры.
/// Изменения сразу попадают в настройки Unity
/// </summary>
[CreateAssetMenu(fileName = "Настройки проекта", menuName = "Runity/Настройки проекта", order = -95)]
public class НастройкиПроекта : ScriptableObject
{
#if UNITY_EDITOR
    /// <summary>
    /// Название игры. Так будет называться окно и файл игры
    /// </summary>
    public Строка название { get => PlayerSettings.productName; set => PlayerSettings.productName = value; }

    /// <summary>
    /// Название компании или имя автора
    /// </summary>
    public Строка компания { get => PlayerSettings.companyName; set => PlayerSettings.companyName = value; }

    /// <summary>
    /// Версия игры, например 1.0
    /// </summary>
    public Строка версия { get => PlayerSettings.bundleVersion; set => PlayerSettings.bundleVersion = value; }

    /// <summary>
    /// Иконка игры. Подойдёт любая квадратная картинка
    /// </summary>
    public Texture2D иконка
    {
        get
        {
            Texture2D[] иконки = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            return иконки != null && иконки.Length > 0 ? иконки[0] : null;
        }
        set => PlayerSettings.SetIcons(NamedBuildTarget.Unknown, value != null ? new[] { value } : new Texture2D[0], IconKind.Any);
    }

    /// <summary>
    /// 2D или 3D. От этого зависит, как импортируются картинки, как выглядит окно сцены и какой отрисовщик у URP
    /// </summary>
    public ВидПроекта вид
    {
        get => EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D ? ВидПроекта.Проект2D : ВидПроекта.Проект3D;
        set
        {
            EditorSettings.defaultBehaviorMode = value == ВидПроекта.Проект2D ? EditorBehaviorMode.Mode2D : EditorBehaviorMode.Mode3D;
            foreach (SceneView окно in SceneView.sceneViews) окно.in2DMode = value == ВидПроекта.Проект2D;
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp) ВыбратьОтрисовщик(urp, value);
        }
    }

    /// <summary>
    /// Чем рисуется игра: URP или встроенным конвейером
    /// </summary>
    public КонвейерОтрисовки конвейер
    {
        get => GraphicsSettings.defaultRenderPipeline == null ? КонвейерОтрисовки.Встроенный : КонвейерОтрисовки.URP;
        set
        {
            RenderPipelineAsset новый = null;
            if (value == КонвейерОтрисовки.URP)
            {
                UniversalRenderPipelineAsset urp = НайтиURP();
                if (urp == null)
                {
                    Debug.LogError("В проекте нет ассета URP. Создайте его через Create - Rendering - URP Asset (with Universal Renderer) и попробуйте снова");
                    return;
                }
                ВыбратьОтрисовщик(urp, вид);
                новый = urp;
            }

            GraphicsSettings.defaultRenderPipeline = новый;
            int был = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = новый;
            }
            QualitySettings.SetQualityLevel(был, false);
        }
    }

    /// <summary>
    /// Какая система ввода включена. После смены Unity нужно перезапустить
    /// </summary>
    public СистемаВвода системаВвода
    {
        get
        {
            SerializedProperty свойство = СвойствоВвода(out _);
            return свойство == null ? СистемаВвода.Новая : (СистемаВвода)Mathf.Clamp(свойство.intValue, 0, 2);
        }
        set
        {
            SerializedProperty свойство = СвойствоВвода(out SerializedObject настройки);
            if (свойство == null || свойство.intValue == (int)value) return;
            свойство.intValue = (int)value;
            настройки.ApplyModifiedProperties();

            if (EditorUtility.DisplayDialog("Система ввода", "Новая система ввода заработает после перезапуска Unity. Перезапустить сейчас?", "Перезапустить", "Потом"))
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }
    }

    /// <summary>
    /// Как смешиваются цвета и свет
    /// </summary>
    public ЦветовоеПространство цветовоеПространство
    {
        get => PlayerSettings.colorSpace == ColorSpace.Gamma ? ЦветовоеПространство.Гамма : ЦветовоеПространство.Линейное;
        set => PlayerSettings.colorSpace = value == ЦветовоеПространство.Гамма ? ColorSpace.Gamma : ColorSpace.Linear;
    }

    /// <summary>
    /// Как игра открывается: на весь экран или в окне
    /// </summary>
    public FullScreenMode режимЭкрана { get => PlayerSettings.fullScreenMode; set => PlayerSettings.fullScreenMode = value; }

    /// <summary>
    /// Ширина окна игры, если она запускается в окне
    /// </summary>
    public Число ширинаОкна { get => PlayerSettings.defaultScreenWidth; set => PlayerSettings.defaultScreenWidth = value; }

    /// <summary>
    /// Высота окна игры, если она запускается в окне
    /// </summary>
    public Число высотаОкна { get => PlayerSettings.defaultScreenHeight; set => PlayerSettings.defaultScreenHeight = value; }

    /// <summary>
    /// Если "да", то окно игры можно растягивать мышкой
    /// </summary>
    public Флажок можноМенятьРазмерОкна { get => PlayerSettings.resizableWindow; set => PlayerSettings.resizableWindow = value; }

    /// <summary>
    /// Если "да", то игра не замирает, когда игрок переключился на другое окно
    /// </summary>
    public Флажок работатьВФоне { get => PlayerSettings.runInBackground; set => PlayerSettings.runInBackground = value; }

    /// <summary>
    /// Если "да", то при запуске показывается заставка Unity
    /// </summary>
    public Флажок заставкаUnity { get => PlayerSettings.SplashScreen.show; set => PlayerSettings.SplashScreen.show = value; }

    /// <summary>
    /// Если "да", то видеокарта сама не рисует объекты, которые загорожены другими, в том числе движущиеся. Работает в 3D с URP и отрисовкой Forward+
    /// </summary>
    public Флажок отсечениеЗаслонённых
    {
        get
        {
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)) return false;
            SerializedObject ассет = new SerializedObject(urp);
            SerializedProperty режим = ассет.FindProperty("m_GPUResidentDrawerMode");
            return режим != null && режим.intValue != 0 && urp.gpuResidentDrawerEnableOcclusionCullingInCameras;
        }
        set
        {
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)) return;
            Undo.RecordObject(urp, "Отсечение заслонённых");
            // Режим хранится числом: 0 - выключено, 1 - рисовать видеокартой
            SerializedObject ассет = new SerializedObject(urp);
            SerializedProperty режим = ассет.FindProperty("m_GPUResidentDrawerMode");
            if (режим != null) { режим.intValue = value ? 1 : 0; ассет.ApplyModifiedProperties(); }
            urp.gpuResidentDrawerEnableOcclusionCullingInCameras = value;
            EditorUtility.SetDirty(urp);

            // Без этого в собранной игре нужные шейдеры вырезаются и отсечение не работает
            if (value)
            {
                Object[] файл = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (файл.Length > 0)
                {
                    SerializedObject графика = new SerializedObject(файл[0]);
                    SerializedProperty вырезание = графика.FindProperty("m_BrgStripping");
                    if (вырезание != null) { вырезание.intValue = 2; графика.ApplyModifiedProperties(); }
                }
            }
        }
    }

    /// <summary>
    /// Почему отсечение заслонённых не может работать в этом проекте. Пусто, если может
    /// </summary>
    public Строка почемуНетОтсечения
    {
        get
        {
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)) return "Нужен конвейер URP";
            return urp.IsGPUResidentDrawerSupportedBySRP(out string причина, out _) ? "" : причина;
        }
    }

    static SerializedProperty СвойствоВвода(out SerializedObject настройки)
    {
        Object[] файл = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        настройки = файл.Length > 0 ? new SerializedObject(файл[0]) : null;
        return настройки?.FindProperty("activeInputHandler");
    }

    static UniversalRenderPipelineAsset НайтиURP()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset текущий) return текущий;
        return AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(ассет => ассет != null);
    }

    // У URP разные отрисовщики для 2D и 3D. Нужный ищется в списке ассета, а если его нет, создаётся рядом
    static void ВыбратьОтрисовщик(UniversalRenderPipelineAsset urp, ВидПроекта вид)
    {
        SerializedObject ассет = new SerializedObject(urp);
        SerializedProperty список = ассет.FindProperty("m_RendererDataList");
        SerializedProperty поУмолчанию = ассет.FindProperty("m_DefaultRendererIndex");
        if (список == null || поУмолчанию == null) return;

        bool нужен2D = вид == ВидПроекта.Проект2D;
        for (int i = 0; i < список.arraySize; i++)
        {
            Object данные = список.GetArrayElementAtIndex(i).objectReferenceValue;
            if (данные != null && (данные is Renderer2DData) == нужен2D)
            {
                поУмолчанию.intValue = i;
                ассет.ApplyModifiedProperties();
                return;
            }
        }

        string папка = Path.GetDirectoryName(AssetDatabase.GetAssetPath(urp));
        ScriptableRendererData новый = нужен2D ? CreateInstance<Renderer2DData>() : (ScriptableRendererData)CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(новый, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(папка, нужен2D ? "Renderer2D.asset" : "Renderer3D.asset").Replace('\\', '/')));

        список.arraySize++;
        список.GetArrayElementAtIndex(список.arraySize - 1).objectReferenceValue = новый;
        поУмолчанию.intValue = список.arraySize - 1;
        ассет.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    const string путьПоУмолчанию = "Assets/Настройки проекта.asset";

    /// <summary>
    /// Находит настройки проекта или создаёт их и показывает в инспекторе
    /// </summary>
    [MenuItem("Runity/Настройки проекта", false, 2)]
    public static void ОткрытьВРедакторе()
    {
        НастройкиПроекта настройки = AssetDatabase.FindAssets("t:НастройкиПроекта")
            .Select(guid => AssetDatabase.LoadAssetAtPath<НастройкиПроекта>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(ассет => ассет != null);

        if (настройки == null)
        {
            настройки = CreateInstance<НастройкиПроекта>();
            AssetDatabase.CreateAsset(настройки, путьПоУмолчанию);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = настройки;
        EditorGUIUtility.PingObject(настройки);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(НастройкиПроекта))]
internal class НастройкиПроектаEditor : Editor
{
    НастройкиПроекта н => (НастройкиПроекта)target;

    GUIContent Подпись(string текст, string член) => new GUIContent(текст, Справка.Описание(typeof(НастройкиПроекта), член));

    void Поле<TЗначение>(string название, string член, TЗначение значение, System.Func<GUIContent, TЗначение, TЗначение> рисовать, System.Action<TЗначение> записать)
    {
        EditorGUI.BeginChangeCheck();
        TЗначение новое = рисовать(Подпись(название, член), значение);
        if (EditorGUI.EndChangeCheck()) записать(новое);
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Главные настройки проекта. Всё, что вы здесь меняете, сразу попадает в настройки Unity", MessageType.None);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Игра", EditorStyles.boldLabel);
        Поле("Название", "название", (string)н.название, (п, з) => EditorGUILayout.DelayedTextField(п, з), з => н.название = з);
        Поле("Компания", "компания", (string)н.компания, (п, з) => EditorGUILayout.DelayedTextField(п, з), з => н.компания = з);
        Поле("Версия", "версия", (string)н.версия, (п, з) => EditorGUILayout.DelayedTextField(п, з), з => н.версия = з);
        Поле("Иконка", "иконка", н.иконка, (п, з) => (Texture2D)EditorGUILayout.ObjectField(п, з, typeof(Texture2D), false), з => н.иконка = з);
        Поле("Заставка Unity", "заставкаUnity", (bool)н.заставкаUnity, (п, з) => EditorGUILayout.Toggle(п, з), з => н.заставкаUnity = з);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Проект", EditorStyles.boldLabel);
        Поле("Вид", "вид", н.вид, (п, з) => (ВидПроекта)EditorGUILayout.EnumPopup(п, з), з => н.вид = з);
        Поле("Конвейер отрисовки", "конвейер", н.конвейер, (п, з) => (КонвейерОтрисовки)EditorGUILayout.EnumPopup(п, з), з => н.конвейер = з);
        if (н.конвейер == КонвейерОтрисовки.Встроенный)
        {
            EditorGUILayout.HelpBox("Со встроенным конвейером не работают 2D-свет и материалы URP: они станут розовыми", MessageType.Warning);
        }
        Поле("Система ввода", "системаВвода", н.системаВвода, (п, з) => (СистемаВвода)EditorGUILayout.EnumPopup(п, з), з => н.системаВвода = з);
        if (н.системаВвода == СистемаВвода.Старая)
        {
            EditorGUILayout.HelpBox("Управление Runity работает только с новой системой ввода", MessageType.Warning);
        }
        Поле("Отсечение заслонённых", "отсечениеЗаслонённых", (bool)н.отсечениеЗаслонённых, (п, з) => EditorGUILayout.Toggle(п, з), з => н.отсечениеЗаслонённых = з);
        if (н.отсечениеЗаслонённых && !string.IsNullOrEmpty(н.почемуНетОтсечения))
        {
            EditorGUILayout.HelpBox("Отсечение включено, но пока не работает: " + н.почемуНетОтсечения, MessageType.Warning);
        }
        Поле("Цветовое пространство", "цветовоеПространство", н.цветовоеПространство, (п, з) => (ЦветовоеПространство)EditorGUILayout.EnumPopup(п, з), з => н.цветовоеПространство = з);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Окно игры", EditorStyles.boldLabel);
        Поле("Режим экрана", "режимЭкрана", н.режимЭкрана, (п, з) => (FullScreenMode)EditorGUILayout.EnumPopup(п, з), з => н.режимЭкрана = з);
        Поле("Ширина окна", "ширинаОкна", (int)н.ширинаОкна, (п, з) => EditorGUILayout.DelayedIntField(п, з), з => н.ширинаОкна = з);
        Поле("Высота окна", "высотаОкна", (int)н.высотаОкна, (п, з) => EditorGUILayout.DelayedIntField(п, з), з => н.высотаОкна = з);
        Поле("Можно менять размер окна", "можноМенятьРазмерОкна", (bool)н.можноМенятьРазмерОкна, (п, з) => EditorGUILayout.Toggle(п, з), з => н.можноМенятьРазмерОкна = з);
        Поле("Работать в фоне", "работатьВФоне", (bool)н.работатьВФоне, (п, з) => EditorGUILayout.Toggle(п, з), з => н.работатьВФоне = з);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ещё", EditorStyles.boldLabel);
        if (GUILayout.Button("Список сцен")) СписокСцен.ОткрытьВРедакторе();
        if (GUILayout.Button("Настройки графики")) НастройкиГрафики.ОткрытьВРедакторе();

        EditorGUILayout.Space();
        Справка.ОтобразитьВИнспекторе(typeof(НастройкиПроекта), new[] { "название", "компания", "версия", "иконка", "заставкаUnity", "вид", "конвейер", "системаВвода", "отсечениеЗаслонённых", "почемуНетОтсечения", "цветовоеПространство", "режимЭкрана", "ширинаОкна", "высотаОкна", "можноМенятьРазмерОкна", "работатьВФоне", "ОткрытьВРедакторе" });
    }
}
#endif
