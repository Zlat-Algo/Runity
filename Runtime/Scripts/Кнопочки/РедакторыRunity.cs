#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Обрезка звука прямо в Unity: выберите начало и конец, послушайте и сохраните новый файл .wav. Исходный звук не меняется
/// </summary>
public class ОбрезкаЗвука : EditorWindow
{
    AudioClip звук;
    float[] данные;
    int каналов, частота;
    float начало, конец;
    float появление, затухание;
    bool нормализовать;
    Texture2D волна;

    [MenuItem("Assets/Runity/Обрезать звук", false, 40)]
    static void ОткрытьДляВыбранного() => Открыть(Selection.activeObject as AudioClip);

    [MenuItem("Assets/Runity/Обрезать звук", true)]
    static bool МожноОткрыть() => Selection.activeObject is AudioClip;

    [MenuItem("Runity/Редакторы/Обрезка звука", false, 50)]
    static void ОткрытьПустое() => Открыть(null);

    /// <summary>
    /// Открывает обрезку для звука
    /// </summary>
    public static void Открыть(AudioClip какой)
    {
        ОбрезкаЗвука окно = GetWindow<ОбрезкаЗвука>("Обрезка звука");
        окно.minSize = new Vector2(420, 260);
        if (какой != null) окно.Загрузить(какой);
    }

    void OnDisable()
    {
        Остановить();
        if (волна != null) DestroyImmediate(волна);
    }

    void Загрузить(AudioClip какой)
    {
        звук = какой;
        данные = ПрочитатьОтсчёты(какой, out каналов, out частота);
        начало = 0;
        конец = какой.length;
        if (волна != null) DestroyImmediate(волна);
        волна = null;
    }

    /// <summary>
    /// Читает звук как числа от -1 до 1. Если звук сжат в памяти, ненадолго переключает его импорт, а потом возвращает как было
    /// </summary>
    public static float[] ПрочитатьОтсчёты(AudioClip звук, out int каналов, out int частота)
    {
        каналов = звук.channels;
        частота = звук.frequency;
        string путь = AssetDatabase.GetAssetPath(звук);
        AudioImporter импорт = AssetImporter.GetAtPath(путь) as AudioImporter;
        AudioImporterSampleSettings было = импорт != null ? импорт.defaultSampleSettings : default;
        bool переключить = импорт != null && было.loadType != AudioClipLoadType.DecompressOnLoad;
        if (переключить)
        {
            AudioImporterSampleSettings на = было;
            на.loadType = AudioClipLoadType.DecompressOnLoad;
            импорт.defaultSampleSettings = на;
            импорт.SaveAndReimport();
            звук = AssetDatabase.LoadAssetAtPath<AudioClip>(путь);
        }
        try
        {
            звук.LoadAudioData();
            float[] отсчёты = new float[звук.samples * звук.channels];
            звук.GetData(отсчёты, 0);
            return отсчёты;
        }
        finally
        {
            if (переключить)
            {
                импорт.defaultSampleSettings = было;
                импорт.SaveAndReimport();
            }
        }
    }

    void OnGUI()
    {
        AudioClip выбранный = (AudioClip)EditorGUILayout.ObjectField("Звук", звук, typeof(AudioClip), false);
        if (выбранный != звук && выбранный != null) Загрузить(выбранный);
        if (звук == null || данные == null)
        {
            EditorGUILayout.HelpBox("Выберите звук или нажмите на звук в окне Project правой кнопкой - Runity - Обрезать звук", MessageType.Info);
            return;
        }

        float длина = звук.length;
        Rect область = GUILayoutUtility.GetRect(position.width - 10, 90);
        НарисоватьВолну(область, длина);

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
            string путь = Сохранить(звук, данные, каналов, частота, начало, конец, появление, затухание, нормализовать);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioClip>(путь);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }

    void НарисоватьВолну(Rect область, float длина)
    {
        int ширина = Mathf.Max(16, (int)область.width);
        if (волна == null || волна.width != ширина)
        {
            if (волна != null) DestroyImmediate(волна);
            волна = new Texture2D(ширина, 90, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            Color[] пиксели = new Color[ширина * 90];
            int кадров = данные.Length / каналов;
            for (int x = 0; x < ширина; x++)
            {
                int от = (int)((long)кадров * x / ширина), до = Mathf.Max(от + 1, (int)((long)кадров * (x + 1) / ширина));
                float наиб = 0;
                for (int i = от; i < до && i < кадров; i++)
                    for (int к = 0; к < каналов; к++) наиб = Mathf.Max(наиб, Mathf.Abs(данные[i * каналов + к]));
                int высота = Mathf.CeilToInt(наиб * 44);
                for (int y = 45 - высота; y <= 45 + высота && y < 90; y++) if (y >= 0) пиксели[y * ширина + x] = new Color(1, 0.55f, 0.2f);
            }
            волна.SetPixels(пиксели);
            волна.Apply();
        }
        EditorGUI.DrawRect(область, new Color(0.12f, 0.12f, 0.14f));
        GUI.DrawTexture(область, волна);
        float слева = область.x + область.width * начало / длина, справа = область.x + область.width * конец / длина;
        EditorGUI.DrawRect(new Rect(область.x, область.y, слева - область.x, область.height), new Color(0, 0, 0, 0.6f));
        EditorGUI.DrawRect(new Rect(справа, область.y, область.xMax - справа, область.height), new Color(0, 0, 0, 0.6f));
    }

    static MethodInfo Метод(string имя, params Type[] параметры) =>
        typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod(имя, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, параметры, null);

    void Послушать()
    {
        Остановить();
        MethodInfo играть = Метод("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool));
        if (играть == null) { ShowNotification(new GUIContent("Эта версия Unity не умеет проигрывать звук в окне")); return; }
        играть.Invoke(null, new object[] { звук, (int)(начало * звук.frequency), false });
        double стоп = EditorApplication.timeSinceStartup + (конец - начало);
        void Следить()
        {
            if (EditorApplication.timeSinceStartup < стоп) return;
            EditorApplication.update -= Следить;
            Остановить();
        }
        EditorApplication.update += Следить;
    }

    static void Остановить() => Метод("StopAllPreviewClips")?.Invoke(null, null);

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
        using MemoryStream поток = new MemoryStream();
        using BinaryWriter запись = new BinaryWriter(поток);
        int байтов = отсчёты.Length * 2;
        запись.Write(new[] { 'R', 'I', 'F', 'F' });
        запись.Write(36 + байтов);
        запись.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
        запись.Write(16);
        запись.Write((short)1);
        запись.Write((short)каналов);
        запись.Write(частота);
        запись.Write(частота * каналов * 2);
        запись.Write((short)(каналов * 2));
        запись.Write((short)16);
        запись.Write(new[] { 'd', 'a', 't', 'a' });
        запись.Write(байтов);
        foreach (float отсчёт in отсчёты) запись.Write((short)Mathf.RoundToInt(Mathf.Clamp(отсчёт, -1, 1) * 32767));
        запись.Flush();
        return поток.ToArray();
    }
}

/// <summary>
/// Удаление фона у картинки: фон того же цвета, что края картинки, становится прозрачным. Подходит для картинок на однотонном фоне.
/// Исходная картинка не меняется, рядом сохраняется новая .png
/// </summary>
public class УдалениеФона : EditorWindow
{
    Texture2D картинка;
    float допуск = 0.12f;
    float смягчение = 0.06f;
    bool толькоСнаружи = true;
    bool обрезатьКрая = true;
    Texture2D предпросмотр;

    [MenuItem("Assets/Runity/Убрать фон картинки", false, 41)]
    static void ОткрытьДляВыбранной() => Открыть(Selection.activeObject as Texture2D ?? (Selection.activeObject as Sprite)?.texture);

    [MenuItem("Assets/Runity/Убрать фон картинки", true)]
    static bool МожноОткрыть() => Selection.activeObject is Texture2D || Selection.activeObject is Sprite;

    [MenuItem("Runity/Редакторы/Удаление фона", false, 51)]
    static void ОткрытьПустое() => Открыть(null);

    /// <summary>
    /// Открывает удаление фона для картинки
    /// </summary>
    public static void Открыть(Texture2D какая)
    {
        УдалениеФона окно = GetWindow<УдалениеФона>("Удаление фона");
        окно.minSize = new Vector2(360, 420);
        окно.картинка = какая;
        окно.Обновить();
    }

    void OnDisable()
    {
        if (предпросмотр != null) DestroyImmediate(предпросмотр);
    }

    void Обновить()
    {
        if (предпросмотр != null) DestroyImmediate(предпросмотр);
        предпросмотр = картинка != null ? УбратьФон(Прочитать(картинка), допуск, смягчение, толькоСнаружи, false) : null;
        if (предпросмотр != null) предпросмотр.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        картинка = (Texture2D)EditorGUILayout.ObjectField("Картинка", картинка, typeof(Texture2D), false);
        допуск = EditorGUILayout.Slider(new GUIContent("Допуск", "Насколько цвет может отличаться от фона, чтобы тоже стать прозрачным"), допуск, 0, 0.6f);
        смягчение = EditorGUILayout.Slider(new GUIContent("Смягчение краёв", "Полупрозрачный переход по краю, чтобы не было «лесенки»"), смягчение, 0, 0.3f);
        толькоСнаружи = EditorGUILayout.Toggle(new GUIContent("Только фон снаружи", "Убирается только фон, связанный с краями картинки. Такой же цвет внутри рисунка (например, белые глаза на белом фоне) останется"), толькоСнаружи);
        обрезатьКрая = EditorGUILayout.Toggle(new GUIContent("Обрезать пустые края", "Картинка обрезается по рисунку"), обрезатьКрая);
        if (EditorGUI.EndChangeCheck()) Обновить();

        if (картинка == null)
        {
            EditorGUILayout.HelpBox("Выберите картинку или нажмите на неё в окне Project правой кнопкой - Runity - Убрать фон картинки", MessageType.Info);
            return;
        }

        if (предпросмотр != null)
        {
            Rect область = GUILayoutUtility.GetAspectRect((float)предпросмотр.width / предпросмотр.height, GUILayout.MaxHeight(position.height - 200));
            // Клетчатый фон, чтобы было видно прозрачность
            for (float y = область.y; y < область.yMax; y += 12)
                for (float x = область.x; x < область.xMax; x += 12)
                    EditorGUI.DrawRect(new Rect(x, y, Mathf.Min(12, область.xMax - x), Mathf.Min(12, область.yMax - y)), ((int)((x - область.x) / 12) + (int)((y - область.y) / 12)) % 2 == 0 ? new Color(0.8f, 0.8f, 0.8f) : Color.white);
            GUI.DrawTexture(область, предпросмотр, ScaleMode.ScaleToFit, true);
        }

        if (GUILayout.Button("Сохранить картинку без фона", GUILayout.Height(28)))
        {
            string путь = Сохранить(картинка, допуск, смягчение, толькоСнаружи, обрезатьКрая);
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(путь);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }

    /// <summary>
    /// Читает пиксели картинки, даже если у неё выключено чтение
    /// </summary>
    public static Texture2D Прочитать(Texture2D картинка)
    {
        string путь = AssetDatabase.GetAssetPath(картинка);
        Texture2D копия = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (File.Exists(путь) && (путь.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || путь.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || путь.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            && копия.LoadImage(File.ReadAllBytes(путь))) return копия;

        DestroyImmediate(копия);
        RenderTexture rt = RenderTexture.GetTemporary(картинка.width, картинка.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(картинка, rt);
        RenderTexture было = RenderTexture.active;
        RenderTexture.active = rt;
        копия = new Texture2D(картинка.width, картинка.height, TextureFormat.RGBA32, false);
        копия.ReadPixels(new Rect(0, 0, картинка.width, картинка.height), 0, 0);
        копия.Apply();
        RenderTexture.active = было;
        RenderTexture.ReleaseTemporary(rt);
        return копия;
    }

    /// <summary>
    /// Делает фон прозрачным. Цвет фона - самый частый цвет по краям картинки. Даёт новую картинку
    /// </summary>
    public static Texture2D УбратьФон(Texture2D исходная, float допуск, float смягчение, bool толькоСнаружи, bool обрезатьКрая)
    {
        int ш = исходная.width, в = исходная.height;
        Color32[] пиксели = исходная.GetPixels32();
        Color фон = ЦветФона(пиксели, ш, в);

        float Отличие(Color32 c) => Mathf.Max(Mathf.Abs(c.r / 255f - фон.r), Mathf.Abs(c.g / 255f - фон.g), Mathf.Abs(c.b / 255f - фон.b));
        bool[] фоновый = new bool[пиксели.Length];
        if (толькоСнаружи)
        {
            // Заливка от краёв: фоном считается только то, до чего можно дойти от края по похожим цветам
            Queue<int> очередь = new Queue<int>();
            void Начать(int x, int y)
            {
                int i = y * ш + x;
                if (!фоновый[i] && Отличие(пиксели[i]) <= допуск + смягчение) { фоновый[i] = true; очередь.Enqueue(i); }
            }
            for (int x = 0; x < ш; x++) { Начать(x, 0); Начать(x, в - 1); }
            for (int y = 0; y < в; y++) { Начать(0, y); Начать(ш - 1, y); }
            while (очередь.Count > 0)
            {
                int i = очередь.Dequeue(), x = i % ш, y = i / ш;
                if (x > 0) Начать(x - 1, y);
                if (x < ш - 1) Начать(x + 1, y);
                if (y > 0) Начать(x, y - 1);
                if (y < в - 1) Начать(x, y + 1);
            }
        }
        else for (int i = 0; i < пиксели.Length; i++) фоновый[i] = Отличие(пиксели[i]) <= допуск + смягчение;

        for (int i = 0; i < пиксели.Length; i++)
        {
            if (!фоновый[i]) continue;
            float отличие = Отличие(пиксели[i]);
            float a = смягчение > 0 ? Mathf.Clamp01((отличие - допуск) / смягчение) : (отличие <= допуск ? 0 : 1);
            пиксели[i].a = (byte)Mathf.Min(пиксели[i].a, a * 255);
        }

        RectInt рамка = new RectInt(0, 0, ш, в);
        if (обрезатьКрая)
        {
            int minX = ш, minY = в, maxX = -1, maxY = -1;
            for (int y = 0; y < в; y++)
                for (int x = 0; x < ш; x++)
                    if (пиксели[y * ш + x].a > 8) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            if (maxX >= minX) рамка = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        Texture2D итог = new Texture2D(рамка.width, рамка.height, TextureFormat.RGBA32, false);
        Color32[] часть = new Color32[рамка.width * рамка.height];
        for (int y = 0; y < рамка.height; y++)
            Array.Copy(пиксели, (y + рамка.y) * ш + рамка.x, часть, y * рамка.width, рамка.width);
        итог.SetPixels32(часть);
        итог.Apply();
        return итог;
    }

    static Color ЦветФона(Color32[] пиксели, int ш, int в)
    {
        Dictionary<int, int> сколько = new Dictionary<int, int>();
        void Учесть(Color32 c)
        {
            int ключ = (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3);
            сколько[ключ] = сколько.TryGetValue(ключ, out int n) ? n + 1 : 1;
        }
        for (int x = 0; x < ш; x++) { Учесть(пиксели[x]); Учесть(пиксели[(в - 1) * ш + x]); }
        for (int y = 0; y < в; y++) { Учесть(пиксели[y * ш]); Учесть(пиксели[y * ш + ш - 1]); }
        int лучший = 0, наиб = -1;
        foreach (KeyValuePair<int, int> пара in сколько) if (пара.Value > наиб) { наиб = пара.Value; лучший = пара.Key; }

        // Точный цвет - среднее всех краевых пикселей этой группы
        Vector3 сумма = Vector3.zero;
        int n2 = 0;
        void Сложить(Color32 c)
        {
            if (((c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3)) != лучший) return;
            сумма += new Vector3(c.r, c.g, c.b) / 255f;
            n2++;
        }
        for (int x = 0; x < ш; x++) { Сложить(пиксели[x]); Сложить(пиксели[(в - 1) * ш + x]); }
        for (int y = 0; y < в; y++) { Сложить(пиксели[y * ш]); Сложить(пиксели[y * ш + ш - 1]); }
        сумма /= Mathf.Max(1, n2);
        return new Color(сумма.x, сумма.y, сумма.z);
    }

    /// <summary>
    /// Сохраняет картинку без фона рядом с исходной и даёт путь к новому файлу. Настройки импорта (спрайт, пиксели на единицу) переносятся
    /// </summary>
    public static string Сохранить(Texture2D картинка, float допуск = 0.12f, float смягчение = 0.06f, bool толькоСнаружи = true, bool обрезатьКрая = true)
    {
        Texture2D исходная = Прочитать(картинка);
        Texture2D итог = УбратьФон(исходная, допуск, смягчение, толькоСнаружи, обрезатьКрая);
        string путьИсходной = AssetDatabase.GetAssetPath(картинка);
        string путь = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(путьИсходной), Path.GetFileNameWithoutExtension(путьИсходной) + " без фона.png").Replace('\\', '/'));
        File.WriteAllBytes(путь, итог.EncodeToPNG());
        DestroyImmediate(исходная);
        DestroyImmediate(итог);
        AssetDatabase.ImportAsset(путь);

        if (AssetImporter.GetAtPath(путьИсходной) is TextureImporter было && AssetImporter.GetAtPath(путь) is TextureImporter стало)
        {
            стало.textureType = было.textureType;
            стало.spriteImportMode = было.spriteImportMode == SpriteImportMode.None ? SpriteImportMode.None : SpriteImportMode.Single;
            стало.spritePixelsPerUnit = было.spritePixelsPerUnit;
            стало.filterMode = было.filterMode;
            стало.alphaIsTransparency = true;
            стало.SaveAndReimport();
        }
        return путь;
    }
}
#endif
