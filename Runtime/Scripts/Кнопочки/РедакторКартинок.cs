#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Инструмент редактора картинок
/// </summary>
public enum ИнструментКартинки { Кисть, Карандаш, Ластик, Заливка, Пипетка, Линия, Прямоугольник, Овал, Выделение, Лассо, Перо, Перемещение, Рука }

/// <summary>
/// Редактор картинок прямо в Unity: кисть, карандаш, ластик, заливка, пипетка, линии и фигуры, выделение прямоугольником, лассо и пером,
/// перемещение выделенного, отражение, поворот, изменение размера, удаление фона. Отмена - Ctrl+Z, повтор - Ctrl+Y, сохранить - Ctrl+S
/// </summary>
public class РедакторКартинок : EditorWindow
{
    // Картинка
    Texture2D исходная;
    string путь;
    int ш, в;
    Color32[] пиксели;
    Texture2D холст;
    bool изменено, нужноОбновитьХолст;

    // Выделение: null - выделено всё
    bool[] выделение;
    Texture2D видВыделения;
    bool нужноОбновитьВыделение;

    // Инструменты
    ИнструментКартинки инструмент = ИнструментКартинки.Кисть;
    Color основной = Color.black, второй = Color.white;
    float размер = 8, жёсткость = 0.8f, непрозрачность = 1, допуск = 0.1f;
    bool залитыеФигуры, заливкаВезде;

    // Вид
    float масштаб = 1;
    Vector2 сдвиг;
    Rect областьХолста;
    Vector2 мышьВКартинке;
    bool мышьНадХолстом;

    // Отмена и повтор
    readonly List<Color32[]> отмена = new List<Color32[]>();
    readonly List<Color32[]> повтор = new List<Color32[]>();
    const int глубинаОтмены = 40;

    // Текущее действие мышью
    bool рисуем;
    Color32[] доДействия;
    float[] покрытие;
    Vector2 начало, прошлая;
    Color цветДействия;
    readonly List<Vector2> точкиКонтура = new List<Vector2>();
    bool[] выделениеДоДействия;
    Color32[] плавающие;
    bool[] плавающаяМаска;
    int режимВыделения; // 0 - заменить, 1 - добавить (Shift), 2 - убрать (Alt)

    // Новая картинка и размер
    int новаяШирина = 256, новаяВысота = 256;
    bool показатьРазмер;
    int размерШирина, размерВысота;

    [MenuItem("Assets/Runity/Редакторы/Редактировать картинку", false, 199)]
    static void ОткрытьДляВыбранной() => Открыть(Selection.activeObject as Texture2D ?? (Selection.activeObject as Sprite)?.texture);

    [MenuItem("Assets/Runity/Редакторы/Редактировать картинку", true)]
    static bool МожноОткрыть() => Selection.activeObject is Texture2D || Selection.activeObject is Sprite;

    [MenuItem("Runity/Редакторы/Редактор картинок", false, 49)]
    static void ОткрытьПустой() => Открыть(null);

    /// <summary>
    /// Открывает картинку в редакторе
    /// </summary>
    public static void Открыть(Texture2D картинка)
    {
        РедакторКартинок окно = GetWindow<РедакторКартинок>("Редактор картинок");
        окно.minSize = new Vector2(620, 460);
        if (картинка != null) окно.Загрузить(картинка);
    }

    void OnDisable()
    {
        if (холст != null) DestroyImmediate(холст);
        if (видВыделения != null) DestroyImmediate(видВыделения);
    }

    void Загрузить(Texture2D картинка)
    {
        if (изменено && !EditorUtility.DisplayDialog("Редактор картинок", "Изменения в текущей картинке не сохранены. Открыть другую?", "Открыть", "Отмена")) return;
        Texture2D копия = УдалениеФона.Прочитать(картинка);
        исходная = картинка;
        путь = AssetDatabase.GetAssetPath(картинка);
        Установить(копия.width, копия.height, копия.GetPixels32());
        DestroyImmediate(копия);
        отмена.Clear();
        повтор.Clear();
        изменено = false;
        ВписатьВОкно();
    }

    /// <summary>
    /// Создаёт пустую прозрачную картинку
    /// </summary>
    public void Новая(int ширина, int высота)
    {
        исходная = null;
        путь = null;
        Установить(ширина, высота, new Color32[ширина * высота]);
        отмена.Clear();
        повтор.Clear();
        изменено = true;
        ВписатьВОкно();
    }

    void Установить(int ширина, int высота, Color32[] новые)
    {
        ш = ширина;
        в = высота;
        пиксели = новые;
        выделение = null;
        if (холст != null) DestroyImmediate(холст);
        холст = new Texture2D(ш, в, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
        нужноОбновитьХолст = true;
        нужноОбновитьВыделение = true;
        размерШирина = ш;
        размерВысота = в;
    }

    void ВписатьВОкно()
    {
        if (ш == 0) return;
        Rect место = new Rect(0, 0, position.width - 20, position.height - 150);
        масштаб = Mathf.Max(0.05f, Mathf.Min(место.width / ш, место.height / в));
        if (масштаб > 1) масштаб = Mathf.Floor(масштаб);
        сдвиг = Vector2.zero;
    }

    #region Окно

    void OnGUI()
    {
        Клавиши();
        ВерхняяПанель();
        if (пиксели == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Откройте картинку: перетащите её в поле «Картинка» или нажмите на неё в окне Project правой кнопкой - Runity - Редакторы - Редактировать картинку. Или создайте новую", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            новаяШирина = EditorGUILayout.IntField("Ширина", новаяШирина);
            новаяВысота = EditorGUILayout.IntField("Высота", новаяВысота);
            if (GUILayout.Button("Новая картинка")) Новая(Mathf.Clamp(новаяШирина, 1, 8192), Mathf.Clamp(новаяВысота, 1, 8192));
            EditorGUILayout.EndHorizontal();
            return;
        }
        ПанельИнструментов();
        ПанельНастроек();
        Rect место = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        Холст(место);
        СтрокаСостояния();
    }

    void ВерхняяПанель()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        Texture2D выбрана = (Texture2D)EditorGUILayout.ObjectField(исходная, typeof(Texture2D), false, GUILayout.Width(200));
        if (выбрана != исходная && выбрана != null) Загрузить(выбрана);
        if (GUILayout.Button("Новая", EditorStyles.toolbarButton)) { if (!изменено || EditorUtility.DisplayDialog("Редактор картинок", "Изменения не сохранены. Создать новую картинку?", "Создать", "Отмена")) Новая(Mathf.Clamp(новаяШирина, 1, 8192), Mathf.Clamp(новаяВысота, 1, 8192)); }
        GUI.enabled = пиксели != null;
        if (GUILayout.Button(new GUIContent("Сохранить", "Ctrl+S"), EditorStyles.toolbarButton)) Сохранить(false);
        if (GUILayout.Button("Сохранить как...", EditorStyles.toolbarButton)) Сохранить(true);
        GUILayout.Space(8);
        GUI.enabled = отмена.Count > 0;
        if (GUILayout.Button(new GUIContent("↶", "Отменить (Ctrl+Z)"), EditorStyles.toolbarButton, GUILayout.Width(26))) Отменить();
        GUI.enabled = повтор.Count > 0;
        if (GUILayout.Button(new GUIContent("↷", "Повторить (Ctrl+Y)"), EditorStyles.toolbarButton, GUILayout.Width(26))) Повторить();
        GUI.enabled = пиксели != null;
        GUILayout.Space(8);
        if (GUILayout.Button(new GUIContent("Картинка ▾", "Отражение, поворот, размер, удаление фона"), EditorStyles.toolbarDropDown)) МенюКартинки();
        if (GUILayout.Button(new GUIContent("Выделение ▾", "Выделить всё, снять, обратить, удалить, обрезать"), EditorStyles.toolbarDropDown)) МенюВыделения();
        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        if (пиксели != null && GUILayout.Button(new GUIContent("По размеру окна", "Показать картинку целиком"), EditorStyles.toolbarButton)) ВписатьВОкно();
        EditorGUILayout.EndHorizontal();

        if (показатьРазмер && пиксели != null)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            размерШирина = EditorGUILayout.IntField("Новая ширина", размерШирина);
            размерВысота = EditorGUILayout.IntField("Новая высота", размерВысота);
            if (GUILayout.Button("Растянуть")) { ИзменитьРазмер(размерШирина, размерВысота, true); показатьРазмер = false; }
            if (GUILayout.Button("Холст")) { ИзменитьРазмер(размерШирина, размерВысота, false); показатьРазмер = false; }
            if (GUILayout.Button("×", GUILayout.Width(22))) показатьРазмер = false;
            EditorGUILayout.EndHorizontal();
        }
    }

    static readonly (ИнструментКартинки инструмент, string надпись, string подсказка)[] кнопки =
    {
        (ИнструментКартинки.Кисть, "Кисть", "Мягкая кисть (B). Правая кнопка мыши рисует вторым цветом"),
        (ИнструментКартинки.Карандаш, "Карандаш", "Чёткие пиксели без сглаживания (P) - для пиксель-арта"),
        (ИнструментКартинки.Ластик, "Ластик", "Стирает до прозрачности (E)"),
        (ИнструментКартинки.Заливка, "Заливка", "Заливает область похожего цвета (G). С галочкой «Везде» - все такие пиксели картинки"),
        (ИнструментКартинки.Пипетка, "Пипетка", "Берёт цвет с картинки (I). Правая кнопка - второй цвет. Alt с любым рисующим инструментом - тоже пипетка"),
        (ИнструментКартинки.Линия, "Линия", "Прямая линия (U). Shift - под углом, кратным 45°"),
        (ИнструментКартинки.Прямоугольник, "Прямоуг.", "Прямоугольник (R). Shift - квадрат"),
        (ИнструментКартинки.Овал, "Овал", "Овал (O). Shift - круг"),
        (ИнструментКартинки.Выделение, "Выделение", "Прямоугольное выделение (M). Shift - добавить, Alt - убрать"),
        (ИнструментКартинки.Лассо, "Лассо", "Выделение от руки (L). Shift - добавить, Alt - убрать"),
        (ИнструментКартинки.Перо, "Перо", "Выделение многоугольником (N): щелчки ставят точки, двойной щелчок, Enter или щелчок по первой точке замыкают"),
        (ИнструментКартинки.Перемещение, "Двигать", "Двигает выделенное (V). Без выделения двигает всю картинку"),
        (ИнструментКартинки.Рука, "Рука", "Двигает вид (H или зажатое колёсико). Колёсико - приближение"),
    };

    void ПанельИнструментов()
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var кнопка in кнопки)
        {
            bool выбран = инструмент == кнопка.инструмент;
            if (GUILayout.Toggle(выбран, new GUIContent(кнопка.надпись, кнопка.подсказка), EditorStyles.miniButton) && !выбран) ВыбратьИнструмент(кнопка.инструмент);
        }
        EditorGUILayout.EndHorizontal();
    }

    void ВыбратьИнструмент(ИнструментКартинки новый)
    {
        if (инструмент == ИнструментКартинки.Перо && точкиКонтура.Count > 0) точкиКонтура.Clear();
        инструмент = новый;
    }

    void ПанельНастроек()
    {
        EditorGUILayout.BeginHorizontal();
        float ширинаПодписи = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70;
        основной = EditorGUILayout.ColorField(new GUIContent("Цвет", "Основной цвет (левая кнопка мыши)"), основной, GUILayout.Width(130));
        второй = EditorGUILayout.ColorField(new GUIContent("Второй", "Второй цвет (правая кнопка мыши)"), второй, GUILayout.Width(130));
        if (GUILayout.Button(new GUIContent("⇄", "Поменять цвета местами (X)"), EditorStyles.miniButton, GUILayout.Width(24))) (основной, второй) = (второй, основной);
        bool кистевой = инструмент == ИнструментКартинки.Кисть || инструмент == ИнструментКартинки.Ластик || инструмент == ИнструментКартинки.Карандаш
            || инструмент == ИнструментКартинки.Линия || инструмент == ИнструментКартинки.Прямоугольник || инструмент == ИнструментКартинки.Овал;
        if (кистевой)
        {
            размер = EditorGUILayout.Slider(new GUIContent("Размер", "Толщина в пикселях. [ и ] - меньше и больше"), размер, 1, 300, GUILayout.Width(230));
            if (инструмент != ИнструментКартинки.Карандаш)
                жёсткость = EditorGUILayout.Slider(new GUIContent("Жёсткость", "0 - мягкий край, 1 - чёткий"), жёсткость, 0, 1, GUILayout.Width(200));
            непрозрачность = EditorGUILayout.Slider(new GUIContent("Сила", "Непрозрачность мазка"), непрозрачность, 0.01f, 1, GUILayout.Width(200));
            if (инструмент == ИнструментКартинки.Прямоугольник || инструмент == ИнструментКартинки.Овал)
                залитыеФигуры = EditorGUILayout.ToggleLeft(new GUIContent("Залить", "Фигура залита цветом, а не только контур"), залитыеФигуры, GUILayout.Width(60));
        }
        else if (инструмент == ИнструментКартинки.Заливка)
        {
            допуск = EditorGUILayout.Slider(new GUIContent("Допуск", "Насколько цвет может отличаться, чтобы тоже залиться"), допуск, 0, 1, GUILayout.Width(220));
            непрозрачность = EditorGUILayout.Slider(new GUIContent("Сила", "Непрозрачность заливки"), непрозрачность, 0.01f, 1, GUILayout.Width(200));
            заливкаВезде = EditorGUILayout.ToggleLeft(new GUIContent("Везде", "Залить все пиксели такого цвета, а не только соседние"), заливкаВезде, GUILayout.Width(70));
        }
        EditorGUIUtility.labelWidth = ширинаПодписи;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void СтрокаСостояния()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{ш} × {в}", EditorStyles.miniLabel, GUILayout.Width(90));
        GUILayout.Label($"Масштаб {масштаб * 100:0}%", EditorStyles.miniLabel, GUILayout.Width(110));
        if (мышьНадХолстом) GUILayout.Label($"x {(int)мышьВКартинке.x}, y {(int)мышьВКартинке.y}", EditorStyles.miniLabel, GUILayout.Width(110));
        if (выделение != null) GUILayout.Label("Есть выделение - рисуется только внутри", EditorStyles.miniLabel);
        if (инструмент == ИнструментКартинки.Перо && точкиКонтура.Count > 0) GUILayout.Label("Перо: двойной щелчок или Enter - замкнуть, Esc - отменить", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (изменено) GUILayout.Label("не сохранено", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    void МенюКартинки()
    {
        GenericMenu меню = new GenericMenu();
        меню.AddItem(new GUIContent("Отразить по горизонтали"), false, () => Преобразовать((x, y) => (ш - 1 - x, y), ш, в));
        меню.AddItem(new GUIContent("Отразить по вертикали"), false, () => Преобразовать((x, y) => (x, в - 1 - y), ш, в));
        меню.AddItem(new GUIContent("Повернуть по часовой"), false, () => Повернуть(true));
        меню.AddItem(new GUIContent("Повернуть против часовой"), false, () => Повернуть(false));
        меню.AddSeparator("");
        меню.AddItem(new GUIContent("Изменить размер..."), false, () => { показатьРазмер = true; размерШирина = ш; размерВысота = в; });
        меню.AddItem(new GUIContent("Обрезать пустые края"), false, ОбрезатьПустое);
        меню.AddSeparator("");
        меню.AddItem(new GUIContent("Убрать фон (как в окне Удаление фона)"), false, УбратьФон);
        меню.AddItem(new GUIContent("Обесцветить"), false, () => ИзменитьЦвета(c => { float g = c.grayscale; return new Color(g, g, g, c.a); }));
        меню.AddItem(new GUIContent("Обратить цвета"), false, () => ИзменитьЦвета(c => new Color(1 - c.r, 1 - c.g, 1 - c.b, c.a)));
        меню.AddItem(new GUIContent("Перекрасить в основной цвет"), false, () => ИзменитьЦвета(c => new Color(основной.r * c.grayscale * 1.5f, основной.g * c.grayscale * 1.5f, основной.b * c.grayscale * 1.5f, c.a)));
        меню.ShowAsContext();
    }

    void МенюВыделения()
    {
        GenericMenu меню = new GenericMenu();
        меню.AddItem(new GUIContent("Выделить всё	Ctrl+A"), false, ВыделитьВсё);
        меню.AddItem(new GUIContent("Снять выделение	Ctrl+D"), false, СнятьВыделение);
        меню.AddItem(new GUIContent("Обратить выделение	Ctrl+I"), false, ОбратитьВыделение);
        меню.AddItem(new GUIContent("Выделить по цвету под пипеткой"), false, () => ВыделитьПоЦвету(основной));
        меню.AddSeparator("");
        меню.AddItem(new GUIContent("Удалить выделенное	Delete"), false, УдалитьВыделенное);
        меню.AddItem(new GUIContent("Залить выделенное основным цветом	Alt+Backspace"), false, ЗалитьВыделенное);
        меню.AddItem(new GUIContent("Обрезать картинку по выделению"), false, ОбрезатьПоВыделению);
        меню.ShowAsContext();
    }

    #endregion

    #region Холст и мышь

    void Холст(Rect место)
    {
        областьХолста = место;
        EditorGUI.DrawRect(место, new Color(0.17f, 0.17f, 0.17f));
        Rect картинка = new Rect(место.center.x - ш * масштаб / 2 + сдвиг.x, место.center.y - в * масштаб / 2 + сдвиг.y, ш * масштаб, в * масштаб);

        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Keyboard);
        мышьВКартинке = new Vector2((e.mousePosition.x - картинка.x) / масштаб, в - (e.mousePosition.y - картинка.y) / масштаб);
        мышьНадХолстом = место.Contains(e.mousePosition);

        switch (e.GetTypeForControl(id))
        {
            case EventType.ScrollWheel when мышьНадХолстом:
                float было = масштаб;
                масштаб = Mathf.Clamp(масштаб * (e.delta.y < 0 ? 1.25f : 0.8f), 0.05f, 64);
                // Приближаем к точке под мышью
                Vector2 отЦентра = e.mousePosition - (место.center + сдвиг);
                сдвиг -= отЦентра * (масштаб / было - 1);
                e.Use();
                break;
            case EventType.MouseDown when мышьНадХолстом:
                GUIUtility.hotControl = id;
                GUIUtility.keyboardControl = id;
                НачатьДействие(e);
                e.Use();
                break;
            case EventType.MouseDrag when GUIUtility.hotControl == id:
                if (e.button == 2 || инструмент == ИнструментКартинки.Рука) сдвиг += e.delta;
                else ПродолжитьДействие(e);
                e.Use();
                break;
            case EventType.MouseUp when GUIUtility.hotControl == id:
                GUIUtility.hotControl = 0;
                ЗакончитьДействие(e);
                e.Use();
                break;
            case EventType.MouseMove:
                if (мышьНадХолстом) Repaint();
                break;
            case EventType.Repaint:
                if (нужноОбновитьХолст) { холст.SetPixels32(пиксели); холст.Apply(false); нужноОбновитьХолст = false; }
                GUI.BeginClip(место);
                Rect вОкне = new Rect(картинка.x - место.x, картинка.y - место.y, картинка.width, картинка.height);
                УдалениеФона.Шахматка(Обрезать(вОкне, new Rect(0, 0, место.width, место.height)), 8);
                GUI.DrawTexture(вОкне, холст, ScaleMode.StretchToFill, true);
                if (масштаб >= 10) Сетка(вОкне, место);
                НарисоватьВыделение(вОкне);
                НарисоватьКурсор(вОкне, e.mousePosition - место.position);
                GUI.EndClip();
                break;
        }

        if (мышьНадХолстом)
            EditorGUIUtility.AddCursorRect(место, инструмент == ИнструментКартинки.Рука ? MouseCursor.Pan : инструмент == ИнструментКартинки.Перемещение ? MouseCursor.MoveArrow : MouseCursor.Arrow);
    }

    static Rect Обрезать(Rect r, Rect границы) => Rect.MinMaxRect(Mathf.Max(r.xMin, границы.xMin), Mathf.Max(r.yMin, границы.yMin), Mathf.Min(r.xMax, границы.xMax), Mathf.Min(r.yMax, границы.yMax));

    void Сетка(Rect вОкне, Rect место)
    {
        Color линия = new Color(0, 0, 0, 0.15f);
        int от = Mathf.Max(0, (int)(-вОкне.x / масштаб)), до = Mathf.Min(ш, (int)((место.width - вОкне.x) / масштаб) + 1);
        for (int x = от; x <= до; x++) EditorGUI.DrawRect(new Rect(вОкне.x + x * масштаб, Mathf.Max(0, вОкне.y), 1, Mathf.Min(вОкне.height, место.height)), линия);
        от = Mathf.Max(0, (int)(-вОкне.y / масштаб)); до = Mathf.Min(в, (int)((место.height - вОкне.y) / масштаб) + 1);
        for (int y = от; y <= до; y++) EditorGUI.DrawRect(new Rect(Mathf.Max(0, вОкне.x), вОкне.y + y * масштаб, Mathf.Min(вОкне.width, место.width), 1), линия);
    }

    void НарисоватьВыделение(Rect вОкне)
    {
        if (выделение != null)
        {
            if (видВыделения == null || видВыделения.width != ш || видВыделения.height != в)
            {
                if (видВыделения != null) DestroyImmediate(видВыделения);
                видВыделения = new Texture2D(ш, в, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                нужноОбновитьВыделение = true;
            }
            if (нужноОбновитьВыделение)
            {
                // Невыделенное слегка затемняется, а край выделения обводится
                Color32[] вид = new Color32[ш * в];
                Color32 тень = new Color32(0, 0, 0, 90), край = new Color32(80, 170, 255, 255);
                for (int y = 0; y < в; y++)
                    for (int x = 0; x < ш; x++)
                    {
                        int i = y * ш + x;
                        if (!выделение[i]) { вид[i] = тень; continue; }
                        bool граница = x == 0 || y == 0 || x == ш - 1 || y == в - 1 || !выделение[i - 1] || !выделение[i + 1] || !выделение[i - ш] || !выделение[i + ш];
                        if (граница) вид[i] = край;
                    }
                видВыделения.SetPixels32(вид);
                видВыделения.Apply(false);
                нужноОбновитьВыделение = false;
            }
            GUI.DrawTexture(вОкне, видВыделения, ScaleMode.StretchToFill, true);
        }

        // Контур лассо, пера или прямоугольника, который сейчас рисуется
        if (точкиКонтура.Count > 0)
        {
            Handles.color = new Color(0.3f, 0.7f, 1f);
            Vector3[] точки = new Vector3[точкиКонтура.Count + (инструмент == ИнструментКартинки.Перо ? 1 : 0)];
            for (int i = 0; i < точкиКонтура.Count; i++) точки[i] = ВОкно(вОкне, точкиКонтура[i]);
            if (инструмент == ИнструментКартинки.Перо) точки[точки.Length - 1] = ВОкно(вОкне, мышьВКартинке);
            Handles.DrawAAPolyLine(2, точки);
            if (инструмент == ИнструментКартинки.Перо) foreach (Vector2 т in точкиКонтура) EditorGUI.DrawRect(new Rect(ВОкно(вОкне, т).x - 2, ВОкно(вОкне, т).y - 2, 4, 4), Color.white);
        }
        if (рисуем && инструмент == ИнструментКартинки.Выделение)
        {
            Vector3 а = ВОкно(вОкне, начало), б = ВОкно(вОкне, прошлая);
            Handles.color = new Color(0.3f, 0.7f, 1f);
            Handles.DrawAAPolyLine(2, а, new Vector3(б.x, а.y), б, new Vector3(а.x, б.y), а);
        }
    }

    Vector3 ВОкно(Rect вОкне, Vector2 точка) => new Vector3(вОкне.x + точка.x * масштаб, вОкне.y + (в - точка.y) * масштаб);

    void НарисоватьКурсор(Rect вОкне, Vector2 мышь)
    {
        if (!мышьНадХолстом) return;
        bool кистевой = инструмент == ИнструментКартинки.Кисть || инструмент == ИнструментКартинки.Ластик || инструмент == ИнструментКартинки.Карандаш;
        if (кистевой)
        {
            float радиус = Mathf.Max(1, размер * масштаб / 2);
            Handles.color = new Color(1, 1, 1, 0.8f);
            Handles.DrawWireDisc(мышь, Vector3.forward, радиус);
            Handles.color = new Color(0, 0, 0, 0.6f);
            Handles.DrawWireDisc(мышь, Vector3.forward, радиус + 1);
        }
        else
        {
            EditorGUI.DrawRect(new Rect(мышь.x - 6, мышь.y, 13, 1), Color.white);
            EditorGUI.DrawRect(new Rect(мышь.x, мышь.y - 6, 1, 13), Color.white);
        }
    }

    void НачатьДействие(Event e)
    {
        if (e.button == 2 || инструмент == ИнструментКартинки.Рука) return;
        Vector2 точка = мышьВКартинке;
        bool правая = e.button == 1;
        цветДействия = правая ? второй : основной;
        начало = прошлая = точка;
        режимВыделения = e.shift ? 1 : e.alt ? 2 : 0;

        // Alt с рисующим инструментом - быстрая пипетка
        if (инструмент == ИнструментКартинки.Пипетка || (e.alt && (инструмент == ИнструментКартинки.Кисть || инструмент == ИнструментКартинки.Карандаш || инструмент == ИнструментКартинки.Заливка)))
        {
            Взять(точка, правая);
            return;
        }

        switch (инструмент)
        {
            case ИнструментКартинки.Заливка:
                Запомнить();
                Залить((int)точка.x, (int)точка.y, цветДействия, e.shift || заливкаВезде);
                return;
            case ИнструментКартинки.Перо:
                if (точкиКонтура.Count > 2 && (e.clickCount >= 2 || Vector2.Distance(точка, точкиКонтура[0]) * масштаб < 8)) { ЗамкнутьКонтур(); return; }
                if (точкиКонтура.Count == 0) выделениеДоДействия = выделение != null ? (bool[])выделение.Clone() : null;
                точкиКонтура.Add(точка);
                return;
            case ИнструментКартинки.Лассо:
                выделениеДоДействия = выделение != null ? (bool[])выделение.Clone() : null;
                точкиКонтура.Clear();
                точкиКонтура.Add(точка);
                рисуем = true;
                return;
            case ИнструментКартинки.Выделение:
                выделениеДоДействия = выделение != null ? (bool[])выделение.Clone() : null;
                рисуем = true;
                return;
            case ИнструментКартинки.Перемещение:
                Запомнить();
                НачатьПеремещение();
                рисуем = true;
                return;
        }

        // Кисть, карандаш, ластик и фигуры
        Запомнить();
        доДействия = (Color32[])пиксели.Clone();
        покрытие = new float[ш * в];
        рисуем = true;
        if (инструмент == ИнструментКартинки.Кисть || инструмент == ИнструментКартинки.Ластик || инструмент == ИнструментКартинки.Карандаш)
        {
            if (e.shift && отмена.Count > 0) Мазок(прошлаяТочкаМазка, точка); // Shift - прямая от прошлого мазка
            else Штамп(точка);
            ПрименитьПокрытие();
        }
    }

    Vector2 прошлаяТочкаМазка;

    void ПродолжитьДействие(Event e)
    {
        if (!рисуем) return;
        Vector2 точка = мышьВКартинке;
        switch (инструмент)
        {
            case ИнструментКартинки.Кисть:
            case ИнструментКартинки.Карандаш:
            case ИнструментКартинки.Ластик:
                Мазок(прошлая, точка);
                ПрименитьПокрытие();
                break;
            case ИнструментКартинки.Линия:
            case ИнструментКартинки.Прямоугольник:
            case ИнструментКартинки.Овал:
                Фигура(начало, e.shift ? Ровно(начало, точка) : точка);
                break;
            case ИнструментКартинки.Лассо:
                if (Vector2.Distance(точка, точкиКонтура[точкиКонтура.Count - 1]) * масштаб > 2) точкиКонтура.Add(точка);
                break;
            case ИнструментКартинки.Перемещение:
                Переместить(точка - начало);
                break;
        }
        прошлая = точка;
        Repaint();
    }

    void ЗакончитьДействие(Event e)
    {
        if (!рисуем) return;
        рисуем = false;
        switch (инструмент)
        {
            case ИнструментКартинки.Выделение:
                Vector2 а = начало, б = мышьВКартинке;
                RectInt рамка = new RectInt(Mathf.FloorToInt(Mathf.Min(а.x, б.x)), Mathf.FloorToInt(Mathf.Min(а.y, б.y)), Mathf.CeilToInt(Mathf.Abs(б.x - а.x)), Mathf.CeilToInt(Mathf.Abs(б.y - а.y)));
                if (рамка.width < 1 || рамка.height < 1) { if (режимВыделения == 0) СнятьВыделение(); break; }
                bool[] маска = new bool[ш * в];
                for (int y = Mathf.Max(0, рамка.yMin); y < Mathf.Min(в, рамка.yMax); y++)
                    for (int x = Mathf.Max(0, рамка.xMin); x < Mathf.Min(ш, рамка.xMax); x++) маска[y * ш + x] = true;
                ПрименитьВыделение(маска);
                break;
            case ИнструментКартинки.Лассо:
                if (точкиКонтура.Count > 2) ПрименитьВыделение(Многоугольник(точкиКонтура));
                else if (режимВыделения == 0) СнятьВыделение();
                точкиКонтура.Clear();
                break;
            case ИнструментКартинки.Перемещение:
                плавающие = null;
                плавающаяМаска = null;
                break;
            default:
                прошлаяТочкаМазка = мышьВКартинке;
                break;
        }
        доДействия = null;
        покрытие = null;
        Repaint();
    }

    void Клавиши()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown || пиксели == null) return;
        bool ctrl = e.control || e.command;
        bool обработано = true;
        if (ctrl && e.keyCode == KeyCode.Z) { if (e.shift) Повторить(); else Отменить(); }
        else if (ctrl && e.keyCode == KeyCode.Y) Повторить();
        else if (ctrl && e.keyCode == KeyCode.S) Сохранить(false);
        else if (ctrl && e.keyCode == KeyCode.A) ВыделитьВсё();
        else if (ctrl && e.keyCode == KeyCode.D) СнятьВыделение();
        else if (ctrl && e.keyCode == KeyCode.I) ОбратитьВыделение();
        else if (e.keyCode == KeyCode.Delete) УдалитьВыделенное();
        else if (e.alt && e.keyCode == KeyCode.Backspace) ЗалитьВыделенное();
        else if (ctrl) обработано = false;
        else if (EditorGUIUtility.editingTextField) обработано = false;
        else switch (e.keyCode)
        {
            case KeyCode.B: ВыбратьИнструмент(ИнструментКартинки.Кисть); break;
            case KeyCode.P: ВыбратьИнструмент(ИнструментКартинки.Карандаш); break;
            case KeyCode.E: ВыбратьИнструмент(ИнструментКартинки.Ластик); break;
            case KeyCode.G: ВыбратьИнструмент(ИнструментКартинки.Заливка); break;
            case KeyCode.I: ВыбратьИнструмент(ИнструментКартинки.Пипетка); break;
            case KeyCode.U: ВыбратьИнструмент(ИнструментКартинки.Линия); break;
            case KeyCode.R: ВыбратьИнструмент(ИнструментКартинки.Прямоугольник); break;
            case KeyCode.O: ВыбратьИнструмент(ИнструментКартинки.Овал); break;
            case KeyCode.M: ВыбратьИнструмент(ИнструментКартинки.Выделение); break;
            case KeyCode.L: ВыбратьИнструмент(ИнструментКартинки.Лассо); break;
            case KeyCode.N: ВыбратьИнструмент(ИнструментКартинки.Перо); break;
            case KeyCode.V: ВыбратьИнструмент(ИнструментКартинки.Перемещение); break;
            case KeyCode.H: ВыбратьИнструмент(ИнструментКартинки.Рука); break;
            case KeyCode.X: (основной, второй) = (второй, основной); break;
            case KeyCode.LeftBracket: размер = Mathf.Max(1, размер - Mathf.Max(1, размер * 0.15f)); break;
            case KeyCode.RightBracket: размер = Mathf.Min(300, размер + Mathf.Max(1, размер * 0.15f)); break;
            case KeyCode.Return: case KeyCode.KeypadEnter: if (инструмент == ИнструментКартинки.Перо && точкиКонтура.Count > 2) ЗамкнутьКонтур(); break;
            case KeyCode.Escape: точкиКонтура.Clear(); break;
            default: обработано = false; break;
        }
        if (обработано) { e.Use(); Repaint(); }
    }

    // Shift: линия под углом, кратным 45°, а фигура - квадрат или круг
    Vector2 Ровно(Vector2 а, Vector2 б)
    {
        Vector2 д = б - а;
        if (д.sqrMagnitude < 1e-6f) return б;
        if (инструмент == ИнструментКартинки.Линия)
        {
            float угол = Mathf.Round(Mathf.Atan2(д.y, д.x) / (Mathf.PI / 4)) * (Mathf.PI / 4);
            return а + new Vector2(Mathf.Cos(угол), Mathf.Sin(угол)) * д.magnitude;
        }
        float сторона = Mathf.Max(Mathf.Abs(д.x), Mathf.Abs(д.y));
        return а + new Vector2(д.x < 0 ? -сторона : сторона, д.y < 0 ? -сторона : сторона);
    }

    #endregion

    #region Рисование

    bool Можно(int i) => выделение == null || выделение[i];

    // Мягкая круглая кисть: покрытие пикселя запоминается наибольшее за мазок, чтобы мазок не темнел, проходя по себе
    void Штамп(Vector2 центр)
    {
        if (инструмент == ИнструментКартинки.Карандаш)
        {
            int р = Mathf.Max(1, Mathf.RoundToInt(размер));
            int x0 = Mathf.FloorToInt(центр.x) - (р - 1) / 2, y0 = Mathf.FloorToInt(центр.y) - (р - 1) / 2;
            for (int y = y0; y < y0 + р; y++)
                for (int x = x0; x < x0 + р; x++)
                {
                    if (x < 0 || y < 0 || x >= ш || y >= в) continue;
                    if (р > 2)
                    {
                        float dx = x + 0.5f - (x0 + р / 2f), dy = y + 0.5f - (y0 + р / 2f);
                        if (dx * dx + dy * dy > р * р / 4f + 0.25f) continue;
                    }
                    int i = y * ш + x;
                    покрытие[i] = Mathf.Max(покрытие[i], непрозрачность);
                }
            return;
        }

        float радиус = Mathf.Max(0.5f, размер / 2);
        int xMin = Mathf.Max(0, Mathf.FloorToInt(центр.x - радиус)), xMax = Mathf.Min(ш - 1, Mathf.CeilToInt(центр.x + радиус));
        int yMin = Mathf.Max(0, Mathf.FloorToInt(центр.y - радиус)), yMax = Mathf.Min(в - 1, Mathf.CeilToInt(центр.y + радиус));
        float твёрдо = радиус * Mathf.Clamp(жёсткость, 0, 0.999f);
        for (int y = yMin; y <= yMax; y++)
            for (int x = xMin; x <= xMax; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), центр);
                if (d > радиус + 0.5f) continue;
                float сила = d <= твёрдо ? 1 : 1 - Mathf.Clamp01((d - твёрдо) / (радиус - твёрдо + 0.5f));
                сила = сила * сила * (3 - 2 * сила) * непрозрачность;
                int i = y * ш + x;
                if (сила > покрытие[i]) покрытие[i] = сила;
            }
    }

    void Мазок(Vector2 от, Vector2 до)
    {
        float шаг = инструмент == ИнструментКартинки.Карандаш ? 0.5f : Mathf.Max(0.5f, размер * 0.15f);
        float длина = Vector2.Distance(от, до);
        int шагов = Mathf.Max(1, Mathf.CeilToInt(длина / шаг));
        for (int k = 1; k <= шагов; k++) Штамп(Vector2.Lerp(от, до, (float)k / шагов));
    }

    // Пиксели = как было до мазка + цвет мазка с силой покрытия
    void ПрименитьПокрытие()
    {
        Color32 цвет = цветДействия;
        bool ластик = инструмент == ИнструментКартинки.Ластик;
        for (int i = 0; i < пиксели.Length; i++)
        {
            float c = покрытие[i];
            if (c <= 0 || !Можно(i)) { пиксели[i] = доДействия[i]; continue; }
            Color32 было = доДействия[i];
            if (ластик) { было.a = (byte)Mathf.RoundToInt(было.a * (1 - c)); пиксели[i] = было; continue; }
            пиксели[i] = Смешать(было, цвет, c * цветДействия.a);
        }
        нужноОбновитьХолст = true;
        изменено = true;
    }

    // Наложение цвета поверх с учётом прозрачности обоих
    static Color32 Смешать(Color32 низ, Color32 верх, float сила)
    {
        float аН = низ.a / 255f, аВ = Mathf.Clamp01(сила);
        float а = аВ + аН * (1 - аВ);
        if (а <= 0) return new Color32(0, 0, 0, 0);
        byte С(byte н, byte в2) => (byte)Mathf.RoundToInt((в2 * аВ + н * аН * (1 - аВ)) / а);
        return new Color32(С(низ.r, верх.r), С(низ.g, верх.g), С(низ.b, верх.b), (byte)Mathf.RoundToInt(а * 255));
    }

    void Фигура(Vector2 а, Vector2 б)
    {
        Array.Clear(покрытие, 0, покрытие.Length);
        if (инструмент == ИнструментКартинки.Линия)
        {
            Мазок(а, б);
            Штамп(а);
        }
        else
        {
            Vector2 мин = Vector2.Min(а, б), макс = Vector2.Max(а, б);
            if (залитыеФигуры)
            {
                for (int y = Mathf.Max(0, (int)мин.y); y < Mathf.Min(в, Mathf.CeilToInt(макс.y)); y++)
                    for (int x = Mathf.Max(0, (int)мин.x); x < Mathf.Min(ш, Mathf.CeilToInt(макс.x)); x++)
                    {
                        if (инструмент == ИнструментКартинки.Овал)
                        {
                            Vector2 ц = (мин + макс) / 2, р = (макс - мин) / 2;
                            float dx = (x + 0.5f - ц.x) / Mathf.Max(0.5f, р.x), dy = (y + 0.5f - ц.y) / Mathf.Max(0.5f, р.y);
                            if (dx * dx + dy * dy > 1) continue;
                        }
                        покрытие[y * ш + x] = непрозрачность;
                    }
            }
            else if (инструмент == ИнструментКартинки.Прямоугольник)
            {
                Мазок(мин, new Vector2(макс.x, мин.y)); Мазок(new Vector2(макс.x, мин.y), макс);
                Мазок(макс, new Vector2(мин.x, макс.y)); Мазок(new Vector2(мин.x, макс.y), мин);
            }
            else
            {
                Vector2 ц = (мин + макс) / 2, р = (макс - мин) / 2;
                int шагов = Mathf.Clamp(Mathf.CeilToInt((р.x + р.y) * 2), 16, 2000);
                Vector2 прош = ц + new Vector2(р.x, 0);
                for (int k = 1; k <= шагов; k++)
                {
                    float у = 2 * Mathf.PI * k / шагов;
                    Vector2 т = ц + new Vector2(Mathf.Cos(у) * р.x, Mathf.Sin(у) * р.y);
                    Мазок(прош, т);
                    прош = т;
                }
            }
        }
        ПрименитьПокрытие();
    }

    void Взять(Vector2 точка, bool второйЦвет)
    {
        int x = Mathf.Clamp((int)точка.x, 0, ш - 1), y = Mathf.Clamp((int)точка.y, 0, в - 1);
        Color цвет = пиксели[y * ш + x];
        if (второйЦвет) второй = цвет; else основной = цвет;
        Repaint();
    }

    static float Разница(Color32 a, Color32 b) =>
        Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b), Mathf.Abs(a.a - b.a)) / 255f;

    void Залить(int x0, int y0, Color цвет, bool везде)
    {
        if (x0 < 0 || y0 < 0 || x0 >= ш || y0 >= в) return;
        Color32 образец = пиксели[y0 * ш + x0];
        bool[] залито = new bool[ш * в];
        if (везде)
        {
            for (int i = 0; i < залито.Length; i++) залито[i] = Можно(i) && Разница(пиксели[i], образец) <= допуск;
        }
        else
        {
            Stack<int> стек = new Stack<int>();
            стек.Push(y0 * ш + x0);
            while (стек.Count > 0)
            {
                int i = стек.Pop();
                if (залито[i] || !Можно(i) || Разница(пиксели[i], образец) > допуск) continue;
                залито[i] = true;
                int x = i % ш, y = i / ш;
                if (x > 0) стек.Push(i - 1);
                if (x < ш - 1) стек.Push(i + 1);
                if (y > 0) стек.Push(i - ш);
                if (y < в - 1) стек.Push(i + ш);
            }
        }
        Color32 c = цвет;
        for (int i = 0; i < залито.Length; i++)
            if (залито[i]) пиксели[i] = непрозрачность >= 0.999f && цвет.a >= 0.999f ? c : Смешать(пиксели[i], c, непрозрачность * цвет.a);
        нужноОбновитьХолст = true;
        изменено = true;
        Repaint();
    }

    #endregion

    #region Выделение

    void ПрименитьВыделение(bool[] маска)
    {
        if (режимВыделения == 0 || выделениеДоДействия == null)
        {
            выделение = режимВыделения == 2 ? null : маска;
            if (режимВыделения == 2 && выделениеДоДействия == null)
            {
                выделение = new bool[ш * в];
                for (int i = 0; i < выделение.Length; i++) выделение[i] = !маска[i];
            }
        }
        else
        {
            выделение = (bool[])выделениеДоДействия.Clone();
            for (int i = 0; i < выделение.Length; i++) выделение[i] = режимВыделения == 1 ? выделение[i] || маска[i] : выделение[i] && !маска[i];
        }
        нужноОбновитьВыделение = true;
        Repaint();
    }

    void ЗамкнутьКонтур()
    {
        if (точкиКонтура.Count > 2) ПрименитьВыделение(Многоугольник(точкиКонтура));
        точкиКонтура.Clear();
    }

    // Пиксели внутри многоугольника: построчно ищем пересечения со сторонами
    bool[] Многоугольник(List<Vector2> точки)
    {
        bool[] маска = new bool[ш * в];
        List<float> пересечения = new List<float>();
        for (int y = 0; y < в; y++)
        {
            float cy = y + 0.5f;
            пересечения.Clear();
            for (int i = 0, j = точки.Count - 1; i < точки.Count; j = i++)
            {
                Vector2 а = точки[i], б = точки[j];
                if ((а.y <= cy && б.y > cy) || (б.y <= cy && а.y > cy))
                    пересечения.Add(а.x + (cy - а.y) / (б.y - а.y) * (б.x - а.x));
            }
            пересечения.Sort();
            for (int k = 0; k + 1 < пересечения.Count; k += 2)
                for (int x = Mathf.Max(0, Mathf.CeilToInt(пересечения[k] - 0.5f)); x <= Mathf.Min(ш - 1, Mathf.FloorToInt(пересечения[k + 1] - 0.5f)); x++)
                    маска[y * ш + x] = true;
        }
        return маска;
    }

    void ВыделитьВсё() { выделение = null; нужноОбновитьВыделение = true; Repaint(); }
    void СнятьВыделение() { выделение = null; нужноОбновитьВыделение = true; Repaint(); }

    void ОбратитьВыделение()
    {
        if (выделение == null) return;
        for (int i = 0; i < выделение.Length; i++) выделение[i] = !выделение[i];
        нужноОбновитьВыделение = true;
        Repaint();
    }

    void ВыделитьПоЦвету(Color цвет)
    {
        Color32 образец = цвет;
        выделение = new bool[ш * в];
        for (int i = 0; i < выделение.Length; i++) выделение[i] = Разница(пиксели[i], образец) <= Mathf.Max(допуск, 0.02f);
        нужноОбновитьВыделение = true;
        Repaint();
    }

    void УдалитьВыделенное()
    {
        if (выделение == null) return;
        Запомнить();
        for (int i = 0; i < пиксели.Length; i++) if (выделение[i]) пиксели[i] = new Color32(0, 0, 0, 0);
        нужноОбновитьХолст = true;
        изменено = true;
        Repaint();
    }

    void ЗалитьВыделенное()
    {
        Запомнить();
        Color32 c = основной;
        for (int i = 0; i < пиксели.Length; i++) if (Можно(i)) пиксели[i] = c;
        нужноОбновитьХолст = true;
        изменено = true;
        Repaint();
    }

    void ОбрезатьПоВыделению()
    {
        if (выделение == null) return;
        RectInt рамка = Рамка(i => выделение[i]);
        if (рамка.width <= 0) return;
        Обрезать(рамка);
    }

    void ОбрезатьПустое()
    {
        RectInt рамка = Рамка(i => пиксели[i].a > 0);
        if (рамка.width <= 0 || (рамка.width == ш && рамка.height == в)) return;
        Обрезать(рамка);
    }

    RectInt Рамка(Func<int, bool> внутри)
    {
        int minX = ш, minY = в, maxX = -1, maxY = -1;
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
                if (внутри(y * ш + x)) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        return maxX < minX ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    void Обрезать(RectInt рамка)
    {
        Запомнить();
        Color32[] новые = new Color32[рамка.width * рамка.height];
        for (int y = 0; y < рамка.height; y++) Array.Copy(пиксели, (y + рамка.y) * ш + рамка.x, новые, y * рамка.width, рамка.width);
        Установить(рамка.width, рамка.height, новые);
        изменено = true;
        Repaint();
    }

    // Перемещение: выделенные пиксели «отрываются» и едут за мышью, а на старом месте остаётся прозрачность
    void НачатьПеремещение()
    {
        доДействия = (Color32[])пиксели.Clone();
        плавающаяМаска = выделение != null ? (bool[])выделение.Clone() : null;
        выделениеДоДействия = плавающаяМаска;
    }

    void Переместить(Vector2 смещение)
    {
        int dx = Mathf.RoundToInt(смещение.x), dy = Mathf.RoundToInt(смещение.y);
        bool[] маска = плавающаяМаска;
        for (int i = 0; i < пиксели.Length; i++) пиксели[i] = маска == null ? new Color32(0, 0, 0, 0) : маска[i] ? new Color32(0, 0, 0, 0) : доДействия[i];
        bool[] новоеВыделение = маска != null ? new bool[ш * в] : null;
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
            {
                int i = y * ш + x;
                if (маска != null && !маска[i]) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= ш || ny >= в) continue;
                int j = ny * ш + nx;
                пиксели[j] = маска == null ? доДействия[i] : Смешать(пиксели[j], доДействия[i], доДействия[i].a / 255f);
                if (новоеВыделение != null) новоеВыделение[j] = true;
            }
        if (новоеВыделение != null) { выделение = новоеВыделение; нужноОбновитьВыделение = true; }
        нужноОбновитьХолст = true;
        изменено = true;
    }

    #endregion

    #region Картинка целиком

    void Преобразовать(Func<int, int, (int, int)> куда, int новаяШ, int новаяВ)
    {
        Запомнить();
        Color32[] новые = new Color32[новаяШ * новаяВ];
        bool[] новоеВыделение = выделение != null ? new bool[новаяШ * новаяВ] : null;
        for (int y = 0; y < в; y++)
            for (int x = 0; x < ш; x++)
            {
                (int nx, int ny) = куда(x, y);
                новые[ny * новаяШ + nx] = пиксели[y * ш + x];
                if (новоеВыделение != null) новоеВыделение[ny * новаяШ + nx] = выделение[y * ш + x];
            }
        Установить(новаяШ, новаяВ, новые);
        выделение = новоеВыделение;
        изменено = true;
        Repaint();
    }

    void Повернуть(bool поЧасовой)
    {
        int старШ = ш, старВ = в;
        if (поЧасовой) Преобразовать((x, y) => (y, старШ - 1 - x), старВ, старШ);
        else Преобразовать((x, y) => (старВ - 1 - y, x), старВ, старШ);
    }

    void ИзменитьРазмер(int новаяШ, int новаяВ, bool растянуть)
    {
        новаяШ = Mathf.Clamp(новаяШ, 1, 8192);
        новаяВ = Mathf.Clamp(новаяВ, 1, 8192);
        Запомнить();
        Color32[] новые = new Color32[новаяШ * новаяВ];
        if (растянуть)
        {
            // Плавное растяжение через видеокарту, а для пиксель-арта при увеличении в целое число раз - чёткое
            bool чётко = новаяШ % ш == 0 && новаяВ % в == 0 && новаяШ / ш == новаяВ / в;
            холст.SetPixels32(пиксели);
            холст.Apply(false);
            холст.filterMode = чётко ? FilterMode.Point : FilterMode.Bilinear;
            RenderTexture rt = RenderTexture.GetTemporary(новаяШ, новаяВ, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(холст, rt);
            RenderTexture было = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D итог = new Texture2D(новаяШ, новаяВ, TextureFormat.RGBA32, false);
            итог.ReadPixels(new Rect(0, 0, новаяШ, новаяВ), 0, 0);
            итог.Apply();
            RenderTexture.active = было;
            RenderTexture.ReleaseTemporary(rt);
            новые = итог.GetPixels32();
            DestroyImmediate(итог);
        }
        else
        {
            // Холст: картинка остаётся по центру, края добавляются прозрачными или обрезаются
            int ox = (новаяШ - ш) / 2, oy = (новаяВ - в) / 2;
            for (int y = 0; y < в; y++)
                for (int x = 0; x < ш; x++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx >= 0 && ny >= 0 && nx < новаяШ && ny < новаяВ) новые[ny * новаяШ + nx] = пиксели[y * ш + x];
                }
        }
        Установить(новаяШ, новаяВ, новые);
        изменено = true;
        ВписатьВОкно();
        Repaint();
    }

    void ИзменитьЦвета(Func<Color, Color> как)
    {
        Запомнить();
        for (int i = 0; i < пиксели.Length; i++) if (Можно(i)) пиксели[i] = как(пиксели[i]);
        нужноОбновитьХолст = true;
        изменено = true;
        Repaint();
    }

    void УбратьФон()
    {
        Запомнить();
        холст.SetPixels32(пиксели);
        холст.Apply(false);
        Texture2D без = УдалениеФона.УбратьФон(холст, new НастройкиФона { обрезатьКрая = false });
        Color32[] итог = без.GetPixels32();
        DestroyImmediate(без);
        for (int i = 0; i < пиксели.Length; i++) if (Можно(i)) пиксели[i] = итог[i];
        нужноОбновитьХолст = true;
        изменено = true;
        Repaint();
    }

    #endregion

    #region Отмена и сохранение

    void Запомнить()
    {
        отмена.Add((Color32[])пиксели.Clone());
        размерыОтмены.Add(new Vector2Int(ш, в));
        if (отмена.Count > глубинаОтмены) { отмена.RemoveAt(0); размерыОтмены.RemoveAt(0); }
        повтор.Clear();
        размерыПовтора.Clear();
    }

    readonly List<Vector2Int> размерыОтмены = new List<Vector2Int>();
    readonly List<Vector2Int> размерыПовтора = new List<Vector2Int>();

    void Отменить()
    {
        if (отмена.Count == 0) return;
        повтор.Add(пиксели);
        размерыПовтора.Add(new Vector2Int(ш, в));
        Вернуть(отмена, размерыОтмены);
    }

    void Повторить()
    {
        if (повтор.Count == 0) return;
        отмена.Add(пиксели);
        размерыОтмены.Add(new Vector2Int(ш, в));
        Вернуть(повтор, размерыПовтора);
    }

    void Вернуть(List<Color32[]> откуда, List<Vector2Int> размеры)
    {
        Color32[] было = откуда[откуда.Count - 1];
        Vector2Int размерБыл = размеры[размеры.Count - 1];
        откуда.RemoveAt(откуда.Count - 1);
        размеры.RemoveAt(размеры.Count - 1);
        if (размерБыл.x != ш || размерБыл.y != в) Установить(размерБыл.x, размерБыл.y, было);
        else { пиксели = было; нужноОбновитьХолст = true; }
        изменено = true;
        Repaint();
    }

    void Сохранить(bool как)
    {
        if (пиксели == null) return;
        string куда = путь;
        bool можноПерезаписать = !string.IsNullOrEmpty(куда) && куда.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        if (как || !можноПерезаписать)
        {
            string папка = string.IsNullOrEmpty(путь) ? "Assets" : Path.GetDirectoryName(путь);
            string имя = string.IsNullOrEmpty(путь) ? "Новая картинка" : Path.GetFileNameWithoutExtension(путь) + (можноПерезаписать ? " копия" : "");
            куда = EditorUtility.SaveFilePanelInProject("Сохранить картинку", имя, "png", "Картинка сохранится в формате PNG", папка);
            if (string.IsNullOrEmpty(куда)) return;
        }

        Texture2D итог = new Texture2D(ш, в, TextureFormat.RGBA32, false);
        итог.SetPixels32(пиксели);
        итог.Apply();
        File.WriteAllBytes(куда, итог.EncodeToPNG());
        DestroyImmediate(итог);
        bool новый = AssetImporter.GetAtPath(куда) == null;
        AssetDatabase.ImportAsset(куда, ImportAssetOptions.ForceUpdate);
        if (новый && !string.IsNullOrEmpty(путь)) ПеренестиИмпорт(путь, куда);
        путь = куда;
        исходная = AssetDatabase.LoadAssetAtPath<Texture2D>(куда);
        изменено = false;
        ShowNotification(new GUIContent("Сохранено: " + Path.GetFileName(куда)));
    }

    /// <summary>
    /// Переносит настройки импорта картинки (спрайт, пиксели на единицу, фильтр) на новую картинку
    /// </summary>
    internal static void ПеренестиИмпорт(string откуда, string куда)
    {
        if (!(AssetImporter.GetAtPath(откуда) is TextureImporter было) || !(AssetImporter.GetAtPath(куда) is TextureImporter стало)) return;
        стало.textureType = было.textureType;
        стало.spriteImportMode = было.spriteImportMode == SpriteImportMode.None ? SpriteImportMode.None : SpriteImportMode.Single;
        стало.spritePixelsPerUnit = было.spritePixelsPerUnit;
        стало.filterMode = было.filterMode;
        стало.textureCompression = было.textureCompression;
        стало.alphaIsTransparency = true;
        стало.SaveAndReimport();
    }

    #endregion
}
#endif
