#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Как искать фон картинки
/// </summary>
public enum СпособУдаленияФона
{
    /// <summary>Фон - это цвет (или несколько цветов), похожие на него пиксели становятся прозрачными. Для однотонного фона</summary>
    ЦветФона,
    /// <summary>Фон растёт от краёв картинки, пока соседние пиксели похожи друг на друга. Для фона с плавными переходами и тенями: стена, небо, студийный фон</summary>
    ПлавныйФон,
    /// <summary>Убирается всё снаружи обводки выбранного цвета. Для наклеек и стикеров на пёстром фоне</summary>
    СнаружиОбводки,
}

/// <summary>
/// Настройки удаления фона
/// </summary>
[Serializable]
public class НастройкиФона
{
    public СпособУдаленияФона способ = СпособУдаленияФона.ЦветФона;
    /// <summary>Цвета фона. Если пусто - берётся самый частый цвет по краям картинки</summary>
    public List<Color> цветаФона = new List<Color>();
    /// <summary>Цвет обводки для способа «Снаружи обводки»</summary>
    public Color цветОбводки = Color.white;
    /// <summary>Насколько цвет может отличаться, чтобы считаться фоном (или обводкой)</summary>
    public float допуск = 0.12f;
    /// <summary>Полупрозрачный переход по краю</summary>
    public float смягчение = 0.05f;
    /// <summary>Только фон, связанный с краями картинки. Такой же цвет внутри рисунка останется</summary>
    public bool толькоСнаружи = true;
    /// <summary>Убрать и саму обводку</summary>
    public bool убратьОбводку;
    /// <summary>Убрать непрозрачные пятнышки меньше стольких пикселей</summary>
    public int убратьПятна = 20;
    /// <summary>Закрасить прозрачные дырки внутри рисунка меньше стольких пикселей</summary>
    public int заполнитьДыры;
    /// <summary>На сколько пикселей ужать край рисунка - убирает остатки фона по контуру</summary>
    public int сжатьКрай;
    /// <summary>Убрать цветной ореол фона на полупрозрачных краях</summary>
    public bool убратьОреол = true;
    /// <summary>Обрезать картинку по рисунку</summary>
    public bool обрезатьКрая = true;

    public НастройкиФона Копия()
    {
        НастройкиФона копия = (НастройкиФона)MemberwiseClone();
        копия.цветаФона = new List<Color>(цветаФона);
        return копия;
    }
}

/// <summary>
/// Удаление фона у картинки. Три способа: однотонный фон, плавный фон (стены, небо) и всё снаружи обводки (наклейки).
/// Пипеткой можно указать цвета фона прямо на картинке. Исходная картинка не меняется, рядом сохраняется новая .png
/// </summary>
public class УдалениеФона : EditorWindow
{
    Texture2D картинка;
    Texture2D исходная;
    НастройкиФона настройки = new НастройкиФона();
    Texture2D предпросмотр;
    bool показатьИсходную;
    bool пипеткаОбводки;
    bool нужноОбновить;
    Vector2 прокрутка;

    [MenuItem("Assets/Runity/Редакторы/Убрать фон картинки", false, 201)]
    static void ОткрытьДляВыбранной() => Открыть(Selection.activeObject as Texture2D ?? (Selection.activeObject as Sprite)?.texture);

    [MenuItem("Assets/Runity/Редакторы/Убрать фон картинки", true)]
    static bool МожноОткрыть() => Selection.activeObject is Texture2D || Selection.activeObject is Sprite;

    [MenuItem("Runity/Редакторы/Удаление фона", false, 51)]
    static void ОткрытьПустое() => Открыть(null);

    /// <summary>
    /// Открывает удаление фона для картинки
    /// </summary>
    public static void Открыть(Texture2D какая)
    {
        УдалениеФона окно = GetWindow<УдалениеФона>("Удаление фона");
        окно.minSize = new Vector2(380, 520);
        окно.Выбрать(какая);
    }

    void Выбрать(Texture2D какая)
    {
        картинка = какая;
        if (исходная != null) DestroyImmediate(исходная);
        исходная = какая != null ? Прочитать(какая) : null;
        if (исходная != null) исходная.hideFlags = HideFlags.HideAndDontSave;
        настройки.цветаФона.Clear();
        Обновить();
    }

    void OnDisable()
    {
        if (предпросмотр != null) DestroyImmediate(предпросмотр);
        if (исходная != null) DestroyImmediate(исходная);
    }

    void Обновить()
    {
        нужноОбновить = false;
        if (предпросмотр != null) DestroyImmediate(предпросмотр);
        if (исходная == null) return;
        НастройкиФона безОбрезки = настройки.Копия();
        безОбрезки.обрезатьКрая = false;
        предпросмотр = УбратьФон(исходная, безОбрезки);
        предпросмотр.hideFlags = HideFlags.HideAndDontSave;
        Repaint();
    }

    void OnGUI()
    {
        Texture2D выбранная = (Texture2D)EditorGUILayout.ObjectField("Картинка", картинка, typeof(Texture2D), false);
        if (выбранная != картинка) Выбрать(выбранная);
        if (картинка == null || исходная == null)
        {
            EditorGUILayout.HelpBox("Выберите картинку или нажмите на неё в окне Project правой кнопкой - Runity - Редакторы - Убрать фон картинки", MessageType.Info);
            return;
        }

        прокрутка = EditorGUILayout.BeginScrollView(прокрутка);
        EditorGUI.BeginChangeCheck();
        настройки.способ = (СпособУдаленияФона)EditorGUILayout.EnumPopup(new GUIContent("Способ", "Цвет фона - однотонный фон. Плавный фон - стены, небо, тени. Снаружи обводки - наклейки на пёстром фоне"), настройки.способ);
        EditorGUILayout.HelpBox(настройки.способ switch
        {
            СпособУдаленияФона.ЦветФона => "Щёлкните по картинке, чтобы добавить цвет фона. Без щелчков берётся самый частый цвет по краям",
            СпособУдаленияФона.ПлавныйФон => "Фон растёт от краёв, пока соседние пиксели похожи. Если фон «затекает» в рисунок - уменьшите допуск. Щелчок по картинке добавляет цвет фона",
            _ => "Щёлкните по обводке, чтобы выбрать её цвет. Всё снаружи обводки станет прозрачным, каким бы пёстрым ни был фон",
        }, MessageType.None);

        if (настройки.способ == СпособУдаленияФона.СнаружиОбводки)
        {
            настройки.цветОбводки = EditorGUILayout.ColorField("Цвет обводки", настройки.цветОбводки);
            настройки.убратьОбводку = EditorGUILayout.Toggle(new GUIContent("Убрать и обводку", "Иначе обводка остаётся частью рисунка"), настройки.убратьОбводку);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Цвета фона", "Щёлкните по картинке, чтобы добавить"));
            if (настройки.цветаФона.Count == 0) GUILayout.Label("самый частый по краям", EditorStyles.miniLabel);
            for (int i = 0; i < настройки.цветаФона.Count; i++)
            {
                Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18));
                EditorGUI.DrawRect(r, настройки.цветаФона[i]);
            }
            if (настройки.цветаФона.Count > 0 && GUILayout.Button("Сбросить", EditorStyles.miniButton, GUILayout.Width(70))) настройки.цветаФона.Clear();
            EditorGUILayout.EndHorizontal();
            if (настройки.способ == СпособУдаленияФона.ЦветФона)
                настройки.толькоСнаружи = EditorGUILayout.Toggle(new GUIContent("Только фон снаружи", "Такой же цвет внутри рисунка (например, белые глаза на белом фоне) останется"), настройки.толькоСнаружи);
        }

        настройки.допуск = EditorGUILayout.Slider(new GUIContent("Допуск", "Насколько цвет может отличаться и всё равно считаться фоном"), настройки.допуск, 0, 0.6f);
        настройки.смягчение = EditorGUILayout.Slider(new GUIContent("Смягчение краёв", "Полупрозрачный переход по краю, чтобы не было «лесенки»"), настройки.смягчение, 0, 0.3f);
        настройки.сжатьКрай = EditorGUILayout.IntSlider(new GUIContent("Ужать край", "Срезает столько пикселей по контуру - убирает остатки фона"), настройки.сжатьКрай, 0, 6);
        настройки.убратьПятна = EditorGUILayout.IntSlider(new GUIContent("Убрать пятна до", "Отдельные кусочки рисунка меньше стольких пикселей исчезнут - убирает мусор от фона"), настройки.убратьПятна, 0, 2000);
        настройки.заполнитьДыры = EditorGUILayout.IntSlider(new GUIContent("Заполнить дыры до", "Прозрачные дырки внутри рисунка меньше стольких пикселей закрасятся обратно"), настройки.заполнитьДыры, 0, 5000);
        настройки.убратьОреол = EditorGUILayout.Toggle(new GUIContent("Убрать ореол", "Убирает цвет фона с полупрозрачных краёв"), настройки.убратьОреол);
        настройки.обрезатьКрая = EditorGUILayout.Toggle(new GUIContent("Обрезать пустые края", "Картинка обрезается по рисунку"), настройки.обрезатьКрая);
        if (EditorGUI.EndChangeCheck()) ОтложитьОбновление();

        показатьИсходную = GUILayout.Toggle(показатьИсходную, "Показать исходную картинку", EditorStyles.miniButton);

        Texture2D показ = показатьИсходную ? исходная : предпросмотр;
        if (показ != null)
        {
            Rect область = GUILayoutUtility.GetAspectRect((float)показ.width / показ.height, GUILayout.MaxHeight(Mathf.Max(200, position.height - 380)));
            область = Вписать(область, (float)показ.width / показ.height);
            Шахматка(область);
            GUI.DrawTexture(область, показ, ScaleMode.StretchToFill, true);
            EditorGUIUtility.AddCursorRect(область, MouseCursor.Arrow);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && область.Contains(e.mousePosition))
            {
                int x = Mathf.Clamp((int)((e.mousePosition.x - область.x) / область.width * исходная.width), 0, исходная.width - 1);
                int y = Mathf.Clamp((int)((область.yMax - e.mousePosition.y) / область.height * исходная.height), 0, исходная.height - 1);
                Color цвет = исходная.GetPixel(x, y);
                if (настройки.способ == СпособУдаленияФона.СнаружиОбводки) настройки.цветОбводки = цвет;
                else настройки.цветаФона.Add(цвет);
                e.Use();
                Обновить();
            }
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Сохранить картинку без фона", GUILayout.Height(28)))
        {
            string путь = Сохранить(картинка, настройки);
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(путь);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }

    // Пересчёт идёт после того, как ползунок отпустили или мышь остановилась, а не на каждый пиксель движения
    void ОтложитьОбновление()
    {
        if (нужноОбновить) return;
        нужноОбновить = true;
        EditorApplication.delayCall += () => { if (this != null && нужноОбновить) Обновить(); };
    }

    static Rect Вписать(Rect область, float пропорция)
    {
        float ширина = Mathf.Min(область.width, область.height * пропорция);
        float высота = ширина / пропорция;
        return new Rect(область.x + (область.width - ширина) / 2, область.y, ширина, высота);
    }

    /// <summary>
    /// Клетчатый фон, на котором видно прозрачность
    /// </summary>
    internal static void Шахматка(Rect область, float клетка = 10)
    {
        EditorGUI.DrawRect(область, Color.white);
        Color серый = new Color(0.8f, 0.8f, 0.8f);
        for (float y = область.y; y < область.yMax; y += клетка)
            for (float x = область.x + (((int)((y - область.y) / клетка)) % 2) * клетка; x < область.xMax; x += клетка * 2)
                EditorGUI.DrawRect(new Rect(x, y, Mathf.Min(клетка, область.xMax - x), Mathf.Min(клетка, область.yMax - y)), серый);
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
    /// Делает фон прозрачным старым простым способом: цвет фона - самый частый по краям
    /// </summary>
    public static Texture2D УбратьФон(Texture2D исходная, float допуск, float смягчение, bool толькоСнаружи, bool обрезатьКрая) =>
        УбратьФон(исходная, new НастройкиФона { допуск = допуск, смягчение = смягчение, толькоСнаружи = толькоСнаружи, обрезатьКрая = обрезатьКрая, убратьПятна = 0, убратьОреол = false });

    // Разница цветов: яркость и оттенок отдельно, оттенок весит вдвое больше. Так светлая кожа не путается со светло-серой стеной,
    // а тень на однотонном фоне (тот же оттенок, но темнее) остаётся близкой к фону
    static float Разница(Color32 a, Color b)
    {
        float r = a.r / 255f - b.r, g = a.g / 255f - b.g, bl = a.b / 255f - b.b;
        float яркость = 0.299f * r + 0.587f * g + 0.114f * bl;
        float синева = -0.169f * r - 0.331f * g + 0.5f * bl;
        float краснота = 0.5f * r - 0.419f * g - 0.081f * bl;
        return Mathf.Sqrt(яркость * яркость + 4 * (синева * синева + краснота * краснота));
    }

    static float Разница(Color32 a, Color32 b) => Разница(a, (Color)b);

    /// <summary>
    /// Делает фон прозрачным по настройкам. Даёт новую картинку
    /// </summary>
    public static Texture2D УбратьФон(Texture2D исходная, НастройкиФона н)
    {
        int ш = исходная.width, в = исходная.height, всего = ш * в;
        Color32[] пиксели = исходная.GetPixels32();
        List<Color> фон = н.цветаФона.Count > 0 ? н.цветаФона : new List<Color> { ЦветФона(пиксели, ш, в) };
        float БлижеКФону(Color32 c)
        {
            float лучшее = float.MaxValue;
            foreach (Color f in фон) лучшее = Mathf.Min(лучшее, Разница(c, f));
            return лучшее;
        }

        // Для каждого пикселя - насколько он «фон»: отличие от фона (0 - точно фон). Не фон - бесконечность
        float[] отличие = new float[всего];
        for (int i = 0; i < всего; i++) отличие[i] = float.MaxValue;
        float граница = н.допуск + н.смягчение;

        Queue<int> очередь = new Queue<int>();
        void ОтКраёв(Func<int, int, bool> можно)
        {
            void Начать(int i, int откуда)
            {
                if (отличие[i] != float.MaxValue || !можно(i, откуда)) return;
                отличие[i] = н.способ == СпособУдаленияФона.СнаружиОбводки ? 0 : БлижеКФону(пиксели[i]);
                очередь.Enqueue(i);
            }
            for (int x = 0; x < ш; x++) { Начать(x, -1); Начать((в - 1) * ш + x, -1); }
            for (int y = 0; y < в; y++) { Начать(y * ш, -1); Начать(y * ш + ш - 1, -1); }
            while (очередь.Count > 0)
            {
                int i = очередь.Dequeue(), x = i % ш, y = i / ш;
                if (x > 0) Начать(i - 1, i);
                if (x < ш - 1) Начать(i + 1, i);
                if (y > 0) Начать(i - ш, i);
                if (y < в - 1) Начать(i + ш, i);
            }
        }

        switch (н.способ)
        {
            case СпособУдаленияФона.ЦветФона:
                if (н.толькоСнаружи) ОтКраёв((i, _) => БлижеКФону(пиксели[i]) <= граница);
                else for (int i = 0; i < всего; i++) { float d = БлижеКФону(пиксели[i]); if (d <= граница) отличие[i] = d; }
                break;

            case СпособУдаленияФона.ПлавныйФон:
                // Шаг к соседу разрешён, если соседи похожи, а сам пиксель не слишком далёк от цветов фона
                float шаг = Mathf.Max(0.015f, н.допуск * 0.4f);
                float предел = граница * 1.5f;
                ОтКраёв((i, откуда) => (откуда < 0 ? БлижеКФону(пиксели[i]) <= граница * 2 : Разница(пиксели[i], пиксели[откуда]) <= шаг) && БлижеКФону(пиксели[i]) <= предел);
                // Плавный фон весь прозрачный, а мягкость - только по контуру
                for (int i = 0; i < всего; i++) if (отличие[i] != float.MaxValue) отличие[i] = 0;
                break;

            case СпособУдаленияФона.СнаружиОбводки:
                Color обводка = н.цветОбводки;
                ОтКраёв((i, _) => Разница(пиксели[i], обводка) > н.допуск);
                if (н.убратьОбводку)
                {
                    // Обводка, примыкающая к фону, тоже уходит
                    bool[] фон2 = new bool[всего];
                    for (int i = 0; i < всего; i++) фон2[i] = отличие[i] != float.MaxValue;
                    for (int i = 0; i < всего; i++)
                    {
                        if (!фон2[i]) continue;
                        int x = i % ш, y = i / ш;
                        foreach (int j in new[] { x > 0 ? i - 1 : -1, x < ш - 1 ? i + 1 : -1, y > 0 ? i - ш : -1, y < в - 1 ? i + ш : -1 })
                            if (j >= 0 && отличие[j] == float.MaxValue && Разница(пиксели[j], обводка) <= н.допуск) { отличие[j] = 0; очередь.Enqueue(j); }
                    }
                    while (очередь.Count > 0)
                    {
                        int i = очередь.Dequeue(), x = i % ш, y = i / ш;
                        foreach (int j in new[] { x > 0 ? i - 1 : -1, x < ш - 1 ? i + 1 : -1, y > 0 ? i - ш : -1, y < в - 1 ? i + ш : -1 })
                            if (j >= 0 && отличие[j] == float.MaxValue && Разница(пиксели[j], обводка) <= н.допуск) { отличие[j] = 0; очередь.Enqueue(j); }
                    }
                }
                break;
        }

        // Прозрачность: фон - 0, переходная зона - частично
        float[] a = new float[всего];
        for (int i = 0; i < всего; i++)
        {
            if (отличие[i] == float.MaxValue) { a[i] = 1; continue; }
            a[i] = н.смягчение > 0 && н.способ == СпособУдаленияФона.ЦветФона ? Mathf.Clamp01((отличие[i] - н.допуск) / н.смягчение) : 0;
        }

        if (н.заполнитьДыры > 0) Острова(a, ш, в, false, н.заполнитьДыры);
        if (н.убратьПятна > 0) Острова(a, ш, в, true, н.убратьПятна);
        for (int шагСжатия = 0; шагСжатия < н.сжатьКрай; шагСжатия++) Сжать(a, ш, в);
        if (н.смягчение > 0 && н.способ != СпособУдаленияФона.ЦветФона) Размыть(a, ш, в, Mathf.CeilToInt(н.смягчение * 10));

        Color средФона = фон[0];
        for (int i = 0; i < всего; i++)
        {
            float прозрачность = Mathf.Min(a[i], пиксели[i].a / 255f);
            if (н.убратьОреол && прозрачность > 0.02f && прозрачность < 0.98f)
            {
                // Пиксель края = рисунок * a + фон * (1 - a): вычитаем фон
                Color c = пиксели[i];
                Color чистый = new Color((c.r - средФона.r * (1 - прозрачность)) / прозрачность, (c.g - средФона.g * (1 - прозрачность)) / прозрачность, (c.b - средФона.b * (1 - прозрачность)) / прозрачность);
                пиксели[i] = new Color(Mathf.Clamp01(чистый.r), Mathf.Clamp01(чистый.g), Mathf.Clamp01(чистый.b));
            }
            пиксели[i].a = (byte)Mathf.RoundToInt(прозрачность * 255);
        }

        RectInt рамка = new RectInt(0, 0, ш, в);
        if (н.обрезатьКрая)
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

    // Связные кусочки: непрозрачные (пятна) или прозрачные внутри рисунка (дыры). Маленькие перекрашиваются в противоположное
    static void Острова(float[] a, int ш, int в, bool непрозрачные, int наибольший)
    {
        int всего = ш * в;
        bool[] посещён = new bool[всего];
        List<int> кусок = new List<int>();
        Stack<int> стек = new Stack<int>();
        bool Наш(int i) => непрозрачные ? a[i] > 0.5f : a[i] <= 0.5f;
        for (int старт = 0; старт < всего; старт++)
        {
            if (посещён[старт] || !Наш(старт)) continue;
            кусок.Clear();
            bool касаетсяКрая = false;
            стек.Push(старт);
            посещён[старт] = true;
            while (стек.Count > 0)
            {
                int i = стек.Pop(), x = i % ш, y = i / ш;
                кусок.Add(i);
                if (x == 0 || y == 0 || x == ш - 1 || y == в - 1) касаетсяКрая = true;
                if (x > 0 && !посещён[i - 1] && Наш(i - 1)) { посещён[i - 1] = true; стек.Push(i - 1); }
                if (x < ш - 1 && !посещён[i + 1] && Наш(i + 1)) { посещён[i + 1] = true; стек.Push(i + 1); }
                if (y > 0 && !посещён[i - ш] && Наш(i - ш)) { посещён[i - ш] = true; стек.Push(i - ш); }
                if (y < в - 1 && !посещён[i + ш] && Наш(i + ш)) { посещён[i + ш] = true; стек.Push(i + ш); }
            }
            // Прозрачный кусок у края - это внешний фон, а не дыра
            if (кусок.Count > наибольший || (!непрозрачные && касаетсяКрая)) continue;
            foreach (int i in кусок) a[i] = непрозрачные ? 0 : 1;
        }
    }

    static void Сжать(float[] a, int ш, int в)
    {
        float[] было = (float[])a.Clone();
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
            {
                int i = y * ш + x;
                float наим = было[i];
                if (x > 0) наим = Mathf.Min(наим, было[i - 1]);
                if (x < ш - 1) наим = Mathf.Min(наим, было[i + 1]);
                if (y > 0) наим = Mathf.Min(наим, было[i - ш]);
                if (y < в - 1) наим = Mathf.Min(наим, было[i + ш]);
                a[i] = наим;
            }
    }

    // Размытие только прозрачности, чтобы край стал мягким
    static void Размыть(float[] a, int ш, int в, int радиус)
    {
        if (радиус <= 0) return;
        float[] б = new float[a.Length];
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
            {
                float сумма = 0; int n = 0;
                for (int dx = -радиус; dx <= радиус; dx++) { int xx = Mathf.Clamp(x + dx, 0, ш - 1); сумма += a[y * ш + xx]; n++; }
                б[y * ш + x] = сумма / n;
            }
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
            {
                float сумма = 0; int n = 0;
                for (int dy = -радиус; dy <= радиус; dy++) { int yy = Mathf.Clamp(y + dy, 0, в - 1); сумма += б[yy * ш + x]; n++; }
                a[y * ш + x] = сумма / n;
            }
    }

    static Color ЦветФона(Color32[] пиксели, int ш, int в)
    {
        Dictionary<int, int> сколько = new Dictionary<int, int>();
        int Ключ(Color32 c) => (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3);
        void Учесть(Color32 c) => сколько[Ключ(c)] = сколько.TryGetValue(Ключ(c), out int n) ? n + 1 : 1;
        for (int x = 0; x < ш; x++) { Учесть(пиксели[x]); Учесть(пиксели[(в - 1) * ш + x]); }
        for (int y = 0; y < в; y++) { Учесть(пиксели[y * ш]); Учесть(пиксели[y * ш + ш - 1]); }
        int лучший = 0, наиб = -1;
        foreach (KeyValuePair<int, int> пара in сколько) if (пара.Value > наиб) { наиб = пара.Value; лучший = пара.Key; }

        Vector3 сумма = Vector3.zero;
        int count = 0;
        void Сложить(Color32 c)
        {
            if (Ключ(c) != лучший) return;
            сумма += new Vector3(c.r, c.g, c.b) / 255f;
            count++;
        }
        for (int x = 0; x < ш; x++) { Сложить(пиксели[x]); Сложить(пиксели[(в - 1) * ш + x]); }
        for (int y = 0; y < в; y++) { Сложить(пиксели[y * ш]); Сложить(пиксели[y * ш + ш - 1]); }
        сумма /= Mathf.Max(1, count);
        return new Color(сумма.x, сумма.y, сумма.z);
    }

    /// <summary>
    /// Сохраняет картинку без фона рядом с исходной (старый простой способ)
    /// </summary>
    public static string Сохранить(Texture2D картинка, float допуск = 0.12f, float смягчение = 0.06f, bool толькоСнаружи = true, bool обрезатьКрая = true) =>
        Сохранить(картинка, new НастройкиФона { допуск = допуск, смягчение = смягчение, толькоСнаружи = толькоСнаружи, обрезатьКрая = обрезатьКрая, убратьПятна = 0, убратьОреол = false });

    /// <summary>
    /// Сохраняет картинку без фона рядом с исходной и даёт путь к новому файлу. Настройки импорта (спрайт, пиксели на единицу) переносятся
    /// </summary>
    public static string Сохранить(Texture2D картинка, НастройкиФона настройки)
    {
        Texture2D исходная = Прочитать(картинка);
        Texture2D итог = УбратьФон(исходная, настройки);
        string путьИсходной = AssetDatabase.GetAssetPath(картинка);
        string путь = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(путьИсходной), Path.GetFileNameWithoutExtension(путьИсходной) + " без фона.png").Replace('\\', '/'));
        File.WriteAllBytes(путь, итог.EncodeToPNG());
        DestroyImmediate(исходная);
        DestroyImmediate(итог);
        AssetDatabase.ImportAsset(путь);
        РедакторКартинок.ПеренестиИмпорт(путьИсходной, путь);
        return путь;
    }
}
#endif
