#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

// Общие настройки и сборка модели для форматов, которые Unity сам не читает
public abstract class ИмпортМодели : ScriptedImporter
{
    [Подсказка("Во сколько раз изменить размер. Модели для 3D-печати обычно в миллиметрах - для них подойдёт 0.001")]
    public float масштаб = 1;

    [Подсказка("Большинство программ считают верхом ось Z, а Unity - ось Y. Если модель лежит на боку, переключите эту галочку")]
    public bool осьZВверх = true;

    [Подсказка("Если \"да\", то поверхность сглаживается. Если \"нет\", видны грани - так лучше для угловатых деталей")]
    public bool сглаживание = false;

    [Подсказка("Если \"да\", то к модели добавляется коллайдер по её форме")]
    public bool коллайдер = false;

    protected Vector3 Точка(float x, float y, float z) => (осьZВверх ? new Vector3(x, z, y) : new Vector3(x, y, z)) * масштаб;

    // При перестановке осей Y и Z модель отражается, поэтому треугольники нужно развернуть
    protected bool развернуть => осьZВверх;

    protected void Собрать(AssetImportContext контекст, List<Vector3> точки, List<int> треугольники, List<Color32> цвета, List<Vector2> развёртка, List<Vector3> нормали)
    {
        string имя = Path.GetFileNameWithoutExtension(контекст.assetPath);
        Mesh меш = new Mesh { name = имя };
        if (точки.Count > 65000) меш.indexFormat = IndexFormat.UInt32;

        if (!сглаживание && нормали == null)
        {
            // Чтобы были видны грани, у каждого треугольника свои вершины
            List<Vector3> отдельные = new List<Vector3>(треугольники.Count);
            List<Color32> отдельныеЦвета = цвета != null ? new List<Color32>(треугольники.Count) : null;
            List<Vector2> отдельнаяРазвёртка = развёртка != null ? new List<Vector2>(треугольники.Count) : null;
            foreach (int номер in треугольники)
            {
                отдельные.Add(точки[номер]);
                отдельныеЦвета?.Add(цвета[номер]);
                отдельнаяРазвёртка?.Add(развёртка[номер]);
            }
            точки = отдельные;
            цвета = отдельныеЦвета;
            развёртка = отдельнаяРазвёртка;
            треугольники = new List<int>(треугольники.Count);
            for (int i = 0; i < точки.Count; i++) треугольники.Add(i);
            if (точки.Count > 65000) меш.indexFormat = IndexFormat.UInt32;
        }

        меш.SetVertices(точки);
        меш.SetTriangles(треугольники, 0);
        if (цвета != null) меш.SetColors(цвета);
        if (развёртка != null) меш.SetUVs(0, развёртка);
        if (нормали != null && нормали.Count == точки.Count) меш.SetNormals(нормали);
        else меш.RecalculateNormals();
        меш.RecalculateBounds();
        меш.RecalculateTangents();

        GameObject объект = new GameObject(имя);
        объект.AddComponent<MeshFilter>().sharedMesh = меш;
        MeshRenderer отрисовщик = объект.AddComponent<MeshRenderer>();
        Material материал = new Material(Шейдер(цвета != null)) { name = имя };
        отрисовщик.sharedMaterial = материал;
        объект.AddComponent<Моделлер>();
        if (коллайдер)
        {
            объект.AddComponent<MeshCollider>().sharedMesh = меш;
            объект.AddComponent<КоллайдерМеш>();
        }

        контекст.AddObjectToAsset("меш", меш);
        контекст.AddObjectToAsset("материал", материал);
        контекст.AddObjectToAsset("модель", объект);
        контекст.SetMainObject(объект);
    }

    // Обычный материал URP не показывает цвета вершин, а материал частиц показывает
    static Shader Шейдер(bool естьЦвета)
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        string имя = естьЦвета
            ? (urp ? "Universal Render Pipeline/Particles/Lit" : "Particles/Standard Surface")
            : (urp ? "Universal Render Pipeline/Lit" : "Standard");
        return Shader.Find(имя) ?? Shader.Find("Standard");
    }

    protected void Треугольник(List<int> треугольники, int a, int b, int c)
    {
        треугольники.Add(a);
        if (развернуть) { треугольники.Add(c); треугольники.Add(b); }
        else { треугольники.Add(b); треугольники.Add(c); }
    }
}

/// <summary>
/// Читает модели STL, в которых обычно хранят детали для 3D-печати
/// </summary>
[ScriptedImporter(1, "stl")]
public class ИмпортSTL : ИмпортМодели
{
    public override void OnImportAsset(AssetImportContext контекст)
    {
        byte[] данные = File.ReadAllBytes(контекст.assetPath);
        List<Vector3> точки = new List<Vector3>();
        List<int> треугольники = new List<int>();

        if (Двоичный(данные)) ЧитатьДвоичный(данные, точки, треугольники);
        else ЧитатьТекстовый(Encoding.ASCII.GetString(данные), точки, треугольники);

        if (сглаживание) Склеить(ref точки, треугольники);
        Собрать(контекст, точки, треугольники, null, null, null);
    }

    // Текстовый файл тоже может начинаться со слова solid, поэтому надёжнее проверить размер
    static bool Двоичный(byte[] данные)
    {
        if (данные.Length < 84) return false;
        uint количество = BitConverter.ToUInt32(данные, 80);
        return 84 + количество * 50L == данные.Length;
    }

    void ЧитатьДвоичный(byte[] данные, List<Vector3> точки, List<int> треугольники)
    {
        uint количество = BitConverter.ToUInt32(данные, 80);
        int место = 84;
        for (uint т = 0; т < количество; т++)
        {
            место += 12;
            int начало = точки.Count;
            for (int в = 0; в < 3; в++)
            {
                точки.Add(Точка(BitConverter.ToSingle(данные, место), BitConverter.ToSingle(данные, место + 4), BitConverter.ToSingle(данные, место + 8)));
                место += 12;
            }
            место += 2;
            Треугольник(треугольники, начало, начало + 1, начало + 2);
        }
    }

    void ЧитатьТекстовый(string текст, List<Vector3> точки, List<int> треугольники)
    {
        int вТреугольнике = 0;
        foreach (string строка in текст.Split('\n'))
        {
            string[] части = строка.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (части.Length < 4 || части[0] != "vertex") continue;
            точки.Add(Точка(Число(части[1]), Число(части[2]), Число(части[3])));
            if (++вТреугольнике == 3)
            {
                вТреугольнике = 0;
                Треугольник(треугольники, точки.Count - 3, точки.Count - 2, точки.Count - 1);
            }
        }
    }

    static float Число(string текст) => float.Parse(текст, NumberStyles.Float, CultureInfo.InvariantCulture);

    // В STL у каждого треугольника свои вершины. Для сглаживания одинаковые вершины объединяются
    static void Склеить(ref List<Vector3> точки, List<int> треугольники)
    {
        Dictionary<Vector3, int> номера = new Dictionary<Vector3, int>();
        List<Vector3> общие = new List<Vector3>();
        for (int i = 0; i < треугольники.Count; i++)
        {
            Vector3 точка = точки[треугольники[i]];
            if (!номера.TryGetValue(точка, out int номер))
            {
                номер = общие.Count;
                номера[точка] = номер;
                общие.Add(точка);
            }
            треугольники[i] = номер;
        }
        точки = общие;
    }
}

/// <summary>
/// Читает модели PLY. В них часто хранят 3D-сканы, иногда с цветом каждой вершины
/// </summary>
[ScriptedImporter(1, "ply")]
public class ИмпортPLY : ИмпортМодели
{
    class Свойство
    {
        public string имя, тип, типДлины;
        public bool список;
    }

    class Элемент
    {
        public string имя;
        public int количество;
        public readonly List<Свойство> свойства = new List<Свойство>();
    }

    public override void OnImportAsset(AssetImportContext контекст)
    {
        byte[] данные = File.ReadAllBytes(контекст.assetPath);
        int конецШапки = НайтиКонецШапки(данные);
        if (конецШапки < 0)
        {
            контекст.LogImportError("Это не файл PLY: нет строки end_header");
            return;
        }

        string формат = "ascii";
        List<Элемент> элементы = new List<Элемент>();
        foreach (string строка in Encoding.ASCII.GetString(данные, 0, конецШапки).Split('\n'))
        {
            string[] части = строка.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (части.Length == 0) continue;
            if (части[0] == "format") формат = части[1];
            else if (части[0] == "element") элементы.Add(new Элемент { имя = части[1], количество = int.Parse(части[2], CultureInfo.InvariantCulture) });
            else if (части[0] == "property" && элементы.Count > 0)
            {
                Элемент последний = элементы[элементы.Count - 1];
                if (части[1] == "list") последний.свойства.Add(new Свойство { список = true, типДлины = части[2], тип = части[3], имя = части[4] });
                else последний.свойства.Add(new Свойство { тип = части[1], имя = части[2] });
            }
        }

        Читатель читатель = new Читатель(данные, конецШапки, формат);
        List<Vector3> точки = new List<Vector3>();
        List<Vector3> нормали = null;
        List<Color32> цвета = null;
        List<Vector2> развёртка = null;
        List<int> треугольники = new List<int>();

        foreach (Элемент элемент in элементы)
        {
            bool вершины = элемент.имя == "vertex";
            bool грани = элемент.имя == "face";
            if (вершины)
            {
                if (Есть(элемент, "nx")) нормали = new List<Vector3>(элемент.количество);
                if (Есть(элемент, "red") || Есть(элемент, "r")) цвета = new List<Color32>(элемент.количество);
                if (Есть(элемент, "u") || Есть(элемент, "s") || Есть(элемент, "texture_u")) развёртка = new List<Vector2>(элемент.количество);
            }

            for (int i = 0; i < элемент.количество; i++)
            {
                Dictionary<string, double> значения = new Dictionary<string, double>();
                List<int> список = null;
                foreach (Свойство свойство in элемент.свойства)
                {
                    if (свойство.список)
                    {
                        int длина = (int)читатель.Прочитать(свойство.типДлины);
                        List<int> индексы = new List<int>(длина);
                        for (int к = 0; к < длина; к++) индексы.Add((int)читатель.Прочитать(свойство.тип));
                        if (свойство.имя == "vertex_indices" || свойство.имя == "vertex_index") список = индексы;
                    }
                    else
                    {
                        double значение = читатель.Прочитать(свойство.тип);
                        // Цвета бывают и числами от 0 до 255, и дробными от 0 до 1
                        if ((свойство.тип == "float" || свойство.тип == "double" || свойство.тип == "float32") && (свойство.имя == "red" || свойство.имя == "green" || свойство.имя == "blue" || свойство.имя == "alpha"))
                            значение *= 255;
                        значения[свойство.имя] = значение;
                    }
                }

                if (вершины)
                {
                    точки.Add(Точка((float)Значение(значения, "x"), (float)Значение(значения, "y"), (float)Значение(значения, "z")));
                    нормали?.Add(Точка((float)Значение(значения, "nx"), (float)Значение(значения, "ny"), (float)Значение(значения, "nz")).normalized);
                    цвета?.Add(new Color32(
                        (byte)Mathf.Clamp((float)Значение(значения, "red", "r"), 0, 255),
                        (byte)Mathf.Clamp((float)Значение(значения, "green", "g"), 0, 255),
                        (byte)Mathf.Clamp((float)Значение(значения, "blue", "b"), 0, 255),
                        (byte)Mathf.Clamp(значения.ContainsKey("alpha") || значения.ContainsKey("a") ? (float)Значение(значения, "alpha", "a") : 255, 0, 255)));
                    развёртка?.Add(new Vector2((float)Значение(значения, "u", "s", "texture_u"), (float)Значение(значения, "v", "t", "texture_v")));
                }
                else if (грани && список != null)
                {
                    // Многоугольник разрезается на треугольники веером
                    for (int к = 1; к + 1 < список.Count; к++) Треугольник(треугольники, список[0], список[к], список[к + 1]);
                }
            }
        }

        if (сглаживание) нормали = нормали != null && нормали.Count == точки.Count ? нормали : null;
        else нормали = null;
        Собрать(контекст, точки, треугольники, цвета, развёртка, сглаживание ? нормали : null);
    }

    static bool Есть(Элемент элемент, string имя) => элемент.свойства.Exists(с => с.имя == имя);

    static double Значение(Dictionary<string, double> значения, params string[] имена)
    {
        foreach (string имя in имена) if (значения.TryGetValue(имя, out double значение)) return значение;
        return 0;
    }

    static int НайтиКонецШапки(byte[] данные)
    {
        byte[] метка = Encoding.ASCII.GetBytes("end_header");
        for (int i = 0; i + метка.Length < данные.Length && i < 65536; i++)
        {
            bool совпало = true;
            for (int к = 0; к < метка.Length && совпало; к++) совпало = данные[i + к] == метка[к];
            if (!совпало) continue;

            int конец = i + метка.Length;
            while (конец < данные.Length && данные[конец] != '\n') конец++;
            return конец + 1;
        }
        return -1;
    }

    // Читает значения из текстового или двоичного PLY
    class Читатель
    {
        readonly byte[] данные;
        readonly bool текстовый, обратныйПорядок;
        int место;
        readonly Queue<string> слова = new Queue<string>();

        public Читатель(byte[] данные, int начало, string формат)
        {
            this.данные = данные;
            место = начало;
            текстовый = формат == "ascii";
            обратныйПорядок = формат == "binary_big_endian" == BitConverter.IsLittleEndian;
            if (текстовый)
            {
                foreach (string слово in Encoding.ASCII.GetString(данные, начало, данные.Length - начало).Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                    слова.Enqueue(слово);
            }
        }

        public double Прочитать(string тип)
        {
            if (текстовый) return слова.Count > 0 ? double.Parse(слова.Dequeue(), NumberStyles.Float, CultureInfo.InvariantCulture) : 0;

            switch (тип)
            {
                case "char": case "int8": return (sbyte)данные[место++];
                case "uchar": case "uint8": return данные[место++];
                case "short": case "int16": return BitConverter.ToInt16(Байты(2), 0);
                case "ushort": case "uint16": return BitConverter.ToUInt16(Байты(2), 0);
                case "int": case "int32": return BitConverter.ToInt32(Байты(4), 0);
                case "uint": case "uint32": return BitConverter.ToUInt32(Байты(4), 0);
                case "float": case "float32": return BitConverter.ToSingle(Байты(4), 0);
                case "double": case "float64": return BitConverter.ToDouble(Байты(8), 0);
                default: throw new FormatException("Неизвестный тип в PLY: " + тип);
            }
        }

        byte[] Байты(int сколько)
        {
            byte[] байты = new byte[сколько];
            Array.Copy(данные, место, байты, 0, сколько);
            место += сколько;
            if (обратныйПорядок) Array.Reverse(байты);
            return байты;
        }
    }
}

[CustomEditor(typeof(ИмпортМодели), true)]
internal class ИмпортМоделиEditor : ScriptedImporterEditor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Runity читает эту модель сам: Unity такой формат не поддерживает. Перетащите модель в сцену, как обычную", MessageType.None);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("масштаб"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("осьZВверх"), new GUIContent("Ось Z вверх", "Большинство программ считают верхом ось Z, а Unity - ось Y. Если модель лежит на боку, переключите эту галочку"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("сглаживание"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("коллайдер"));
        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}
#endif
