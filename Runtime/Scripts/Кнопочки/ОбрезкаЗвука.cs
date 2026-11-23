#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Обрезка звука прямо в Unity: выберите начало и конец (можно мышью прямо на волне), послушайте и сохраните новый файл .wav. Исходный звук не меняется
/// </summary>
public class ОбрезкаЗвука : EditorWindow
{
    const int столбиков = 4096;
    const float высотаВолны = 110;

    AudioClip звук;
    float[] данные;
    int каналов, частота;
    float длина;
    float начало, конец;
    float появление, затухание;
    bool нормализовать;

    // Громкость, заранее посчитанная по кусочкам: окно рисует волну только из неё, а не из миллионов отсчётов
    float[] пики;
    Texture2D волна;
    int ширинаВолны;

    enum Ручка { Нет, Начало, Конец }
    Ручка тянем;
    double началоПрослушивания = -1;
    float откудаСлушаем;

    [MenuItem("Assets/Runity/Редакторы/Обрезать звук", false, 200)]
    static void ОткрытьДляВыбранного() => Открыть(Selection.activeObject as AudioClip);

    [MenuItem("Assets/Runity/Редакторы/Обрезать звук", true)]
    static bool МожноОткрыть() => Selection.activeObject is AudioClip;

    [MenuItem("Runity/Редакторы/Обрезка звука", false, 50)]
    static void ОткрытьПустое() => Открыть(null);

    /// <summary>
    /// Открывает обрезку для звука
    /// </summary>
    public static void Открыть(AudioClip какой)
    {
        ОбрезкаЗвука окно = GetWindow<ОбрезкаЗвука>("Обрезка звука");
        окно.minSize = new Vector2(440, 300);
        if (какой != null) окно.Загрузить(какой);
    }

    void OnDisable()
    {
        Остановить();
        EditorApplication.update -= Следить;
        if (волна != null) DestroyImmediate(волна);
    }

    void Загрузить(AudioClip какой)
    {
        звук = какой;
        данные = null;
        каналов = какой.channels;
        частота = какой.frequency;
        длина = какой.length;
        начало = 0;
        конец = длина;
        // Волну Unity уже посчитал при импорте (её же показывает инспектор звука) - берём её, распаковывать звук не нужно
        пики = ПикиЗвука(какой);
        ширинаВолны = 0;
    }

    /// <summary>
    /// Волна звука для рисования: из данных импорта Unity, а если их нет - из распакованного звука
    /// </summary>
    public static float[] ПикиЗвука(AudioClip звук)
    {
        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(звук)) is AudioImporter импорт)
        {
            MethodInfo метод = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("GetMinMaxData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(AudioImporter) }, null);
            if (метод?.Invoke(null, new object[] { импорт }) is float[] наименьшееНаибольшее && наименьшееНаибольшее.Length > 0)
            {
                int каналов = Mathf.Max(1, звук.channels);
                int точек = наименьшееНаибольшее.Length / (2 * каналов);
                float[] пики = new float[точек];
                for (int i = 0; i < точек; i++)
                    for (int к = 0; к < каналов; к++)
                    {
                        int м = (i * каналов + к) * 2;
                        пики[i] = Mathf.Max(пики[i], Mathf.Abs(наименьшееНаибольшее[м]), Mathf.Abs(наименьшееНаибольшее[м + 1]));
                    }
                if (пики.Max() > 0) return пики;
            }
        }
        float[] данные = ПрочитатьОтсчёты(звук, out int к2, out _);
        return ПосчитатьПики(данные, к2, столбиков);
    }

    /// <summary>
    /// Наибольшая громкость в каждом из кусочков звука - для быстрого рисования волны
    /// </summary>
    public static float[] ПосчитатьПики(float[] данные, int каналов, int кусочков)
    {
        float[] пики = new float[кусочков];
        int кадров = данные.Length / Mathf.Max(1, каналов);
        if (кадров == 0) return пики;
        for (int i = 0; i < кадров; i++)
        {
            int кусок = (int)((long)i * кусочков / кадров);
            int основа = i * каналов;
            for (int к = 0; к < каналов; к++)
            {
                float a = Math.Abs(данные[основа + к]);
                if (a > пики[кусок]) пики[кусок] = a;
            }
        }
        return пики;
    }

    /// <summary>
    /// Читает звук как числа от -1 до 1. Если сжатый звук прочитать нельзя, ненадолго переключает его импорт, а потом возвращает как было
    /// </summary>
    public static float[] ПрочитатьОтсчёты(AudioClip звук, out int каналов, out int частота)
    {
        каналов = звук.channels;
        частота = звук.frequency;
        float[] отсчёты = new float[звук.samples * звук.channels];
        string путь = AssetDatabase.GetAssetPath(звук);
        AudioImporter импорт = AssetImporter.GetAtPath(путь) as AudioImporter;
        if (импорт == null || импорт.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad)
        {
            звук.LoadAudioData();
            звук.GetData(отсчёты, 0);
            return отсчёты;
        }
        AudioImporterSampleSettings было = импорт.defaultSampleSettings;
        AudioImporterSampleSettings на = было;
        на.loadType = AudioClipLoadType.DecompressOnLoad;
        импорт.defaultSampleSettings = на;
        импорт.SaveAndReimport();
        try
        {
            звук = AssetDatabase.LoadAssetAtPath<AudioClip>(путь);
            звук.LoadAudioData();
            отсчёты = new float[звук.samples * звук.channels];
            звук.GetData(отсчёты, 0);
            return отсчёты;
        }
        finally
        {
            импорт.defaultSampleSettings = было;
            импорт.SaveAndReimport();
        }
    }

    void OnGUI()
    {
        AudioClip выбранный = (AudioClip)EditorGUILayout.ObjectField("Звук", звук, typeof(AudioClip), false);
        if (выбранный != звук && выбранный != null) Загрузить(выбранный);
        if (звук == null || пики == null)
        {
            EditorGUILayout.HelpBox("Выберите звук или нажмите на звук в окне Project правой кнопкой - Runity - Редакторы - Обрезать звук", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Потяните за края на волне или щёлкните, чтобы передвинуть ближайший край", EditorStyles.miniLabel);
        Rect область = GUILayoutUtility.GetRect(10, высотаВолны, GUILayout.ExpandWidth(true));
        Волна(область);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.MinMaxSlider("Оставить", ref начало, ref конец, 0, длина);
        начало = Mathf.Clamp(EditorGUILayout.FloatField("Начало, сек", начало), 0, конец);
        конец = Mathf.Clamp(EditorGUILayout.FloatField("Конец, сек", конец), начало, длина);
        появление = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Плавное появление, сек", "Звук плавно нарастает в начале - без щелчка"), появление));
        затухание = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Плавное затухание, сек", "Звук плавно стихает в конце"), затухание));
        нормализовать = EditorGUILayout.Toggle(new GUIContent("Выровнять громкость", "Самое громкое место станет максимально громким, без искажений"), нормализовать);
        EditorGUILayout.LabelField($"Останется {конец - начало:0.00} сек из {длина:0.00}");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Послушать")) Послушать();
        if (GUILayout.Button("■ Стоп")) Остановить();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Сохранить обрезанный звук", GUILayout.Height(28)))
        {
            if (данные == null)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Обрезка звука", "Читаю звук " + звук.name, 0.3f);
                    данные = ПрочитатьОтсчёты(звук, out каналов, out частота);
                }
                finally { EditorUtility.ClearProgressBar(); }
            }
            string путь = Сохранить(звук, данные, каналов, частота, начало, конец, появление, затухание, нормализовать);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioClip>(путь);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }

    float ВремяВ(Rect область, float x) => Mathf.Clamp01((x - область.x) / область.width) * длина;
    float XВ(Rect область, float время) => область.x + область.width * (длина > 0 ? время / длина : 0);

    void Волна(Rect область)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown when e.button == 0 && область.Contains(e.mousePosition):
                float время = ВремяВ(область, e.mousePosition.x);
                тянем = Mathf.Abs(время - начало) <= Mathf.Abs(время - конец) ? Ручка.Начало : Ручка.Конец;
                Передвинуть(время);
                GUIUtility.hotControl = id;
                e.Use();
                break;
            case EventType.MouseDrag when GUIUtility.hotControl == id:
                Передвинуть(ВремяВ(область, e.mousePosition.x));
                e.Use();
                break;
            case EventType.MouseUp when GUIUtility.hotControl == id:
                GUIUtility.hotControl = 0;
                тянем = Ручка.Нет;
                e.Use();
                break;
            case EventType.Repaint:
                НарисоватьВолну(область);
                break;
        }
        EditorGUIUtility.AddCursorRect(область, MouseCursor.ResizeHorizontal);
    }

    void Передвинуть(float время)
    {
        if (тянем == Ручка.Начало) начало = Mathf.Min(время, конец);
        else if (тянем == Ручка.Конец) конец = Mathf.Max(время, начало);
        Repaint();
    }

    void НарисоватьВолну(Rect область)
    {
        int ширина = Mathf.Max(16, (int)область.width);
        int высота = (int)высотаВолны;
        if (волна == null || ширинаВолны != ширина)
        {
            if (волна != null) DestroyImmediate(волна);
            ширинаВолны = ширина;
            волна = new Texture2D(ширина, высота, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
            Color32[] пиксели = new Color32[ширина * высота];
            Color32 цвет = new Color32(255, 140, 50, 255);
            int середина = высота / 2;
            for (int x = 0; x < ширина; x++)
            {
                int от = x * пики.Length / ширина, до = Mathf.Max(от + 1, (x + 1) * пики.Length / ширина);
                float наиб = 0;
                for (int i = от; i < до && i < пики.Length; i++) наиб = Mathf.Max(наиб, пики[i]);
                int h = Mathf.CeilToInt(Mathf.Min(1, наиб) * (середина - 1));
                for (int y = середина - h; y <= середина + h; y++) пиксели[y * ширина + x] = цвет;
            }
            волна.SetPixels32(пиксели);
            волна.Apply();
        }
        EditorGUI.DrawRect(область, new Color(0.12f, 0.12f, 0.14f));
        GUI.DrawTexture(область, волна);
        float слева = XВ(область, начало), справа = XВ(область, конец);
        EditorGUI.DrawRect(new Rect(область.x, область.y, слева - область.x, область.height), new Color(0, 0, 0, 0.65f));
        EditorGUI.DrawRect(new Rect(справа, область.y, область.xMax - справа, область.height), new Color(0, 0, 0, 0.65f));
        EditorGUI.DrawRect(new Rect(слева - 1, область.y, 2, область.height), new Color(0.3f, 1, 0.4f));
        EditorGUI.DrawRect(new Rect(справа - 1, область.y, 2, область.height), new Color(1, 0.3f, 0.3f));
        if (началоПрослушивания >= 0)
        {
            float сейчас = откудаСлушаем + (float)(EditorApplication.timeSinceStartup - началоПрослушивания);
            EditorGUI.DrawRect(new Rect(XВ(область, сейчас), область.y, 1, область.height), Color.white);
        }
    }

    static MethodInfo Метод(string имя, params Type[] параметры) =>
        typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod(имя, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, параметры, null);

    void Послушать()
    {
        Остановить();
        MethodInfo играть = Метод("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool));
        if (играть == null) { ShowNotification(new GUIContent("Эта версия Unity не умеет проигрывать звук в окне")); return; }
        играть.Invoke(null, new object[] { звук, (int)(начало * звук.frequency), false });
        откудаСлушаем = начало;
        началоПрослушивания = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Следить;
        EditorApplication.update += Следить;
    }

    // Пока звук играет, двигаем белую черту по волне и останавливаем звук на конце
    void Следить()
    {
        if (началоПрослушивания < 0) { EditorApplication.update -= Следить; return; }
        if (откудаСлушаем + EditorApplication.timeSinceStartup - началоПрослушивания >= конец) { Остановить(); return; }
        Repaint();
    }

    void Остановить()
    {
        Метод("StopAllPreviewClips")?.Invoke(null, null);
        началоПрослушивания = -1;
        EditorApplication.update -= Следить;
        Repaint();
    }

    /// <summary>
    /// Сохраняет часть звука рядом с исходным в формате .wav и даёт путь к новому файлу
    /// </summary>
    public static string Сохранить(AudioClip звук, float[] данные, int каналов, int частота, float начало, float конец,
        float появление = 0, float затухание = 0, bool нормализовать = false)
    {
        int первый = Mathf.Clamp((int)(начало * частота), 0, данные.Length / каналов);
        int последний = Mathf.Clamp((int)(конец * частота), первый, данные.Length / каналов);
        int кадров = последний - первый;
        float[] часть = new float[кадров * каналов];
        Array.Copy(данные, первый * каналов, часть, 0, часть.Length);

        int кадровПоявления = (int)(появление * частота), кадровЗатухания = (int)(затухание * частота);
        float наиб = 0;
        for (int i = 0; i < кадров; i++)
        {
            float множитель = 1;
            if (i < кадровПоявления) множитель *= (float)i / кадровПоявления;
            if (кадров - 1 - i < кадровЗатухания) множитель *= (float)(кадров - 1 - i) / кадровЗатухания;
            for (int к = 0; к < каналов; к++)
            {
                часть[i * каналов + к] *= множитель;
                наиб = Mathf.Max(наиб, Mathf.Abs(часть[i * каналов + к]));
            }
        }
        if (нормализовать && наиб > 0.0001f)
            for (int i = 0; i < часть.Length; i++) часть[i] /= наиб * 1.01f;

        string исходный = AssetDatabase.GetAssetPath(звук);
        string путь = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(исходный), Path.GetFileNameWithoutExtension(исходный) + " (обрезано).wav").Replace('\\', '/'));
        File.WriteAllBytes(путь, ЗаписатьWav(часть, каналов, частота));
        AssetDatabase.ImportAsset(путь);
        return путь;
    }

    /// <summary>
    /// Превращает отсчёты от -1 до 1 в файл .wav (16 бит)
    /// </summary>
    public static byte[] ЗаписатьWav(float[] отсчёты, int каналов, int частота)
    {
        int байтов = отсчёты.Length * 2;
        byte[] файл = new byte[44 + байтов];
        void Число(int где, int значение, int размер) { for (int i = 0; i < размер; i++) файл[где + i] = (byte)(значение >> (8 * i)); }
        void Слово(int где, string текст) { for (int i = 0; i < 4; i++) файл[где + i] = (byte)текст[i]; }
        Слово(0, "RIFF"); Число(4, 36 + байтов, 4); Слово(8, "WAVE"); Слово(12, "fmt ");
        Число(16, 16, 4); Число(20, 1, 2); Число(22, каналов, 2); Число(24, частота, 4); Число(28, частота * каналов * 2, 4); Число(32, каналов * 2, 2); Число(34, 16, 2);
        Слово(36, "data"); Число(40, байтов, 4);
        for (int i = 0; i < отсчёты.Length; i++)
        {
            short s = (short)Mathf.RoundToInt(Mathf.Clamp(отсчёты[i], -1, 1) * 32767);
            файл[44 + i * 2] = (byte)s;
            файл[45 + i * 2] = (byte)(s >> 8);
        }
        return файл;
    }
}
#endif
