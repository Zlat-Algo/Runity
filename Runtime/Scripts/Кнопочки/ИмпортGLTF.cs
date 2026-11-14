#if UNITY_EDITOR && !RUNITY_GLTFAST
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Читает модели glTF (.gltf и .glb) - открытый формат, в который сохраняют Blender, Sketchfab и многие другие программы.
/// Переносит объекты, их расположение, модели, материалы и текстуры. Анимации и кости пока не переносятся.
/// Если в проект установлен официальный пакет glTFast, модели читает он
/// </summary>
[ScriptedImporter(1, new[] { "gltf", "glb" })]
public class ИмпортGLTF : ScriptedImporter
{
    [Подсказка("Во сколько раз изменить размер модели. В glTF размеры в метрах, как и в Unity")]
    public float масштаб = 1;

    [Подсказка("Если \"да\", то к каждой части модели добавляется коллайдер по её форме")]
    public bool коллайдер = false;

    [Подсказка("Если \"нет\", то вместо материалов из файла ставится стандартный")]
    public bool материалы = true;

    Dictionary<string, object> json;
    byte[][] буферы;
    string папка;
    AssetImportContext контекст;
    Texture2D[] текстуры;
    Material[] готовыеМатериалы;
    Material стандартный;
    Mesh[] сетки;
    List<int>[] материалыСеток;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        контекст = ctx;
        папка = Path.GetDirectoryName(ctx.assetPath);
        byte[] файл = File.ReadAllBytes(ctx.assetPath);
        byte[] бинарный = null;

        if (файл.Length >= 12 && BitConverter.ToUInt32(файл, 0) == 0x46546C67)
        {
            // .glb: заголовок, затем куски JSON и двоичных данных
            int место = 12;
            string текст = null;
            while (место + 8 <= файл.Length)
            {
                int длина = BitConverter.ToInt32(файл, место);
                uint вид = BitConverter.ToUInt32(файл, место + 4);
                if (вид == 0x4E4F534A) текст = Encoding.UTF8.GetString(файл, место + 8, длина);
                else if (вид == 0x004E4942) { бинарный = new byte[длина]; Buffer.BlockCopy(файл, место + 8, бинарный, 0, длина); }
                место += 8 + длина;
            }
            json = (Dictionary<string, object>)ПростойJSON.Разобрать(текст);
        }
        else
        {
            json = (Dictionary<string, object>)ПростойJSON.Разобрать(Encoding.UTF8.GetString(файл));
        }

        List<object> списокБуферов = Список("buffers");
        буферы = new byte[списокБуферов.Count][];
        for (int i = 0; i < списокБуферов.Count; i++)
        {
            Dictionary<string, object> буфер = (Dictionary<string, object>)списокБуферов[i];
            буферы[i] = буфер.TryGetValue("uri", out object uri) ? ПрочитатьАдрес((string)uri) : бинарный;
        }

        стандартный = new Material(Шейдер()) { name = "Стандартный" };
        ctx.AddObjectToAsset("стандартный материал", стандартный);
        ЗагрузитьТекстуры();
        ЗагрузитьМатериалы();
        ЗагрузитьСетки();

        string имя = Path.GetFileNameWithoutExtension(ctx.assetPath);
        GameObject корень = new GameObject(имя);
        List<object> сцены = Список("scenes");
        int номерСцены = json.TryGetValue("scene", out object сцена) ? Convert.ToInt32(сцена) : 0;
        if (сцены.Count > 0)
        {
            Dictionary<string, object> выбранная = (Dictionary<string, object>)сцены[Mathf.Clamp(номерСцены, 0, сцены.Count - 1)];
            if (выбранная.TryGetValue("nodes", out object узлы))
            {
                foreach (object узел in (List<object>)узлы) СоздатьУзел(Convert.ToInt32(узел), корень.transform);
            }
        }
        else
        {
            for (int i = 0; i < Список("nodes").Count; i++) СоздатьУзел(i, корень.transform);
        }
        корень.transform.localScale = Vector3.one * масштаб;

        ctx.AddObjectToAsset("модель", корень);
        ctx.SetMainObject(корень);
    }

    List<object> Список(string ключ) => json.TryGetValue(ключ, out object значение) ? (List<object>)значение : new List<object>();

    byte[] ПрочитатьАдрес(string адрес)
    {
        if (адрес.StartsWith("data:"))
        {
            int запятая = адрес.IndexOf(',');
            return Convert.FromBase64String(адрес.Substring(запятая + 1));
        }
        string путь = Path.Combine(папка, Uri.UnescapeDataString(адрес));
        if (!File.Exists(путь))
        {
            контекст.LogImportError($"Не найден файл {путь}, на который ссылается модель");
            return new byte[0];
        }
        контекст.DependsOnSourceAsset(путь.Replace('\\', '/'));
        return File.ReadAllBytes(путь);
    }

    byte[] ДанныеВида(int номерВида, out int начало, out int длина, out int шаг)
    {
        Dictionary<string, object> вид = (Dictionary<string, object>)Список("bufferViews")[номерВида];
        начало = Целое(вид, "byteOffset");
        длина = Целое(вид, "byteLength");
        шаг = Целое(вид, "byteStride");
        return буферы[Целое(вид, "buffer")];
    }

    static int Целое(Dictionary<string, object> объект, string ключ, int вместо = 0) => объект.TryGetValue(ключ, out object значение) ? Convert.ToInt32(значение) : вместо;
    static float Дробь(Dictionary<string, object> объект, string ключ, float вместо) => объект.TryGetValue(ключ, out object значение) ? Convert.ToSingle(значение) : вместо;

    static float[] Числа(Dictionary<string, object> объект, string ключ, params float[] вместо)
    {
        if (!объект.TryGetValue(ключ, out object значение)) return вместо;
        List<object> список = (List<object>)значение;
        float[] результат = new float[список.Count];
        for (int i = 0; i < список.Count; i++) результат[i] = Convert.ToSingle(список[i]);
        return результат;
    }

    // Читает данные доступа (accessor) как дробные числа: по несколько чисел на каждый элемент
    float[] Прочитать(int номер, out int размерЭлемента)
    {
        Dictionary<string, object> доступ = (Dictionary<string, object>)Список("accessors")[номер];
        int сколько = Целое(доступ, "count");
        размерЭлемента = (string)доступ["type"] switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT2" => 4, "MAT3" => 9, "MAT4" => 16, _ => 1 };
        int вид = Целое(доступ, "componentType");
        bool нормализовано = доступ.TryGetValue("normalized", out object н) && (bool)н;
        float[] результат = new float[сколько * размерЭлемента];
        if (!доступ.ContainsKey("bufferView")) return результат;

        byte[] данные = ДанныеВида(Целое(доступ, "bufferView"), out int начало, out _, out int шаг);
        int размерЧисла = вид switch { 5120 => 1, 5121 => 1, 5122 => 2, 5123 => 2, _ => 4 };
        if (шаг == 0) шаг = размерЧисла * размерЭлемента;
        начало += Целое(доступ, "byteOffset");

        for (int i = 0; i < сколько; i++)
        {
            for (int k = 0; k < размерЭлемента; k++)
            {
                int место = начало + i * шаг + k * размерЧисла;
                float число = вид switch
                {
                    5120 => нормализовано ? Mathf.Max((sbyte)данные[место] / 127f, -1) : (sbyte)данные[место],
                    5121 => нормализовано ? данные[место] / 255f : данные[место],
                    5122 => нормализовано ? Mathf.Max(BitConverter.ToInt16(данные, место) / 32767f, -1) : BitConverter.ToInt16(данные, место),
                    5123 => нормализовано ? BitConverter.ToUInt16(данные, место) / 65535f : BitConverter.ToUInt16(данные, место),
                    5125 => BitConverter.ToUInt32(данные, место),
                    _ => BitConverter.ToSingle(данные, место)
                };
                результат[i * размерЭлемента + k] = число;
            }
        }
        return результат;
    }

    void ЗагрузитьТекстуры()
    {
        List<object> картинки = Список("images");
        Texture2D[] загруженные = new Texture2D[картинки.Count];
        HashSet<int> линейные = new HashSet<int>();

        // Карты неровностей хранят направления, а не цвета, поэтому их нельзя переводить в другое цветовое пространство
        foreach (object материал in Список("materials"))
        {
            if (((Dictionary<string, object>)материал).TryGetValue("normalTexture", out object карта))
                линейные.Add(ИсточникТекстуры(Целое((Dictionary<string, object>)карта, "index")));
        }

        for (int i = 0; i < картинки.Count; i++)
        {
            Dictionary<string, object> картинка = (Dictionary<string, object>)картинки[i];
            byte[] байты;
            if (картинка.TryGetValue("bufferView", out object вид))
            {
                byte[] данные = ДанныеВида(Convert.ToInt32(вид), out int начало, out int длина, out _);
                байты = new byte[длина];
                Buffer.BlockCopy(данные, начало, байты, 0, длина);
            }
            else байты = ПрочитатьАдрес((string)картинка["uri"]);

            Texture2D текстура = new Texture2D(2, 2, TextureFormat.RGBA32, true, линейные.Contains(i));
            if (!текстура.LoadImage(байты))
            {
                контекст.LogImportWarning($"Картинку {i} не удалось прочитать: подходят только PNG и JPG");
                continue;
            }
            текстура.name = картинка.TryGetValue("name", out object имя) ? (string)имя : "Текстура " + i;
            текстура.wrapMode = TextureWrapMode.Repeat;
            текстура.Compress(true);
            контекст.AddObjectToAsset("текстура " + i, текстура);
            загруженные[i] = текстура;
        }

        List<object> списокТекстур = Список("textures");
        текстуры = new Texture2D[списокТекстур.Count];
        for (int i = 0; i < списокТекстур.Count; i++)
        {
            int источник = ИсточникТекстуры(i);
            if (источник >= 0 && источник < загруженные.Length) текстуры[i] = загруженные[источник];
        }
    }

    int ИсточникТекстуры(int номер)
    {
        List<object> список = Список("textures");
        if (номер < 0 || номер >= список.Count) return -1;
        return Целое((Dictionary<string, object>)список[номер], "source", -1);
    }

    Texture2D Текстура(Dictionary<string, object> объект, string ключ)
    {
        if (!объект.TryGetValue(ключ, out object ссылка)) return null;
        int номер = Целое((Dictionary<string, object>)ссылка, "index", -1);
        return номер >= 0 && номер < текстуры.Length ? текстуры[номер] : null;
    }

    static Shader Шейдер()
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        return Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard") ?? Shader.Find("Standard");
    }

    void ЗагрузитьМатериалы()
    {
        List<object> список = Список("materials");
        готовыеМатериалы = new Material[список.Count];
        bool urp = GraphicsSettings.currentRenderPipeline != null;

        for (int i = 0; i < список.Count && материалы; i++)
        {
            Dictionary<string, object> описание = (Dictionary<string, object>)список[i];
            Material материал = new Material(Шейдер()) { name = описание.TryGetValue("name", out object имя) ? (string)имя : "Материал " + i };

            Dictionary<string, object> pbr = описание.TryGetValue("pbrMetallicRoughness", out object p) ? (Dictionary<string, object>)p : new Dictionary<string, object>();
            float[] цвет = Числа(pbr, "baseColorFactor", 1, 1, 1, 1);
            Color основной = new Color(цвет[0], цвет[1], цвет[2], цвет[3]).gamma;
            Texture2D основная = Текстура(pbr, "baseColorTexture");
            float металл = Дробь(pbr, "metallicFactor", 1);
            float шероховатость = Дробь(pbr, "roughnessFactor", 1);
            float[] свечение = Числа(описание, "emissiveFactor", 0, 0, 0);
            Texture2D картаСвечения = Текстура(описание, "emissiveTexture");
            Texture2D неровности = Текстура(описание, "normalTexture");
            bool прозрачный = описание.TryGetValue("alphaMode", out object режим) && (string)режим == "BLEND";
            bool сОбеихСторон = описание.TryGetValue("doubleSided", out object стороны) && (bool)стороны;

            материал.SetColor(urp ? "_BaseColor" : "_Color", основной);
            if (основная != null) материал.SetTexture(urp ? "_BaseMap" : "_MainTex", основная);
            материал.SetFloat("_Metallic", металл);
            материал.SetFloat(urp ? "_Smoothness" : "_Glossiness", 1 - шероховатость);
            if (неровности != null)
            {
                материал.SetTexture("_BumpMap", неровности);
                материал.EnableKeyword("_NORMALMAP");
            }
            Color цветСвечения = new Color(свечение[0], свечение[1], свечение[2]);
            if (цветСвечения.maxColorComponent > 0 || картаСвечения != null)
            {
                материал.EnableKeyword("_EMISSION");
                материал.SetColor("_EmissionColor", картаСвечения != null && цветСвечения.maxColorComponent == 0 ? Color.white : цветСвечения);
                if (картаСвечения != null) материал.SetTexture("_EmissionMap", картаСвечения);
                материал.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (прозрачный) СделатьПрозрачным(материал, urp);
            if (сОбеихСторон) материал.SetFloat(urp ? "_Cull" : "_CullMode", 0);

            контекст.AddObjectToAsset("материал " + i, материал);
            готовыеМатериалы[i] = материал;
        }
    }

    static void СделатьПрозрачным(Material материал, bool urp)
    {
        if (urp)
        {
            материал.SetFloat("_Surface", 1);
            материал.SetFloat("_Blend", 0);
            материал.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            материал.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            материал.SetFloat("_ZWrite", 0);
            материал.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            материал.SetFloat("_Mode", 3);
            материал.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            материал.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            материал.SetInt("_ZWrite", 0);
            материал.EnableKeyword("_ALPHABLEND_ON");
        }
        материал.renderQueue = (int)RenderQueue.Transparent;
    }

    void ЗагрузитьСетки()
    {
        List<object> список = Список("meshes");
        сетки = new Mesh[список.Count];
        материалыСеток = new List<int>[список.Count];

        for (int m = 0; m < список.Count; m++)
        {
            Dictionary<string, object> описание = (Dictionary<string, object>)список[m];
            List<Vector3> точки = new List<Vector3>();
            List<Vector3> нормали = new List<Vector3>();
            List<Vector2> развёртка = new List<Vector2>();
            List<Color> цвета = new List<Color>();
            List<List<int>> части = new List<List<int>>();
            List<int> материалыЧастей = new List<int>();
            bool всеСНормалями = true, естьРазвёртка = false, естьЦвета = false;

            foreach (object часть in (List<object>)описание["primitives"])
            {
                Dictionary<string, object> примитив = (Dictionary<string, object>)часть;
                if (Целое(примитив, "mode", 4) != 4)
                {
                    контекст.LogImportWarning("Часть модели состоит из линий или точек и пропущена: читаются только треугольники");
                    continue;
                }

                Dictionary<string, object> атрибуты = (Dictionary<string, object>)примитив["attributes"];
                int сдвиг = точки.Count;
                float[] позиции = Прочитать(Convert.ToInt32(атрибуты["POSITION"]), out _);
                int сколько = позиции.Length / 3;

                // В glTF ось X смотрит в другую сторону, чем в Unity, поэтому модель отражается по X
                for (int i = 0; i < сколько; i++) точки.Add(new Vector3(-позиции[i * 3], позиции[i * 3 + 1], позиции[i * 3 + 2]));

                if (атрибуты.TryGetValue("NORMAL", out object н))
                {
                    float[] значения = Прочитать(Convert.ToInt32(н), out _);
                    for (int i = 0; i < сколько; i++) нормали.Add(new Vector3(-значения[i * 3], значения[i * 3 + 1], значения[i * 3 + 2]));
                }
                else
                {
                    всеСНормалями = false;
                    for (int i = 0; i < сколько; i++) нормали.Add(Vector3.up);
                }

                if (атрибуты.TryGetValue("TEXCOORD_0", out object uv))
                {
                    естьРазвёртка = true;
                    float[] значения = Прочитать(Convert.ToInt32(uv), out _);
                    for (int i = 0; i < сколько; i++) развёртка.Add(new Vector2(значения[i * 2], 1 - значения[i * 2 + 1]));
                }
                else for (int i = 0; i < сколько; i++) развёртка.Add(Vector2.zero);

                if (атрибуты.TryGetValue("COLOR_0", out object c))
                {
                    естьЦвета = true;
                    float[] значения = Прочитать(Convert.ToInt32(c), out int компонентов);
                    for (int i = 0; i < сколько; i++)
                        цвета.Add(new Color(значения[i * компонентов], значения[i * компонентов + 1], значения[i * компонентов + 2], компонентов == 4 ? значения[i * компонентов + 3] : 1).gamma);
                }
                else for (int i = 0; i < сколько; i++) цвета.Add(Color.white);

                List<int> треугольники = new List<int>();
                if (примитив.TryGetValue("indices", out object индексы))
                {
                    float[] номера = Прочитать(Convert.ToInt32(индексы), out _);
                    for (int i = 0; i + 2 < номера.Length; i += 3)
                    {
                        // После отражения по X треугольники нужно развернуть, иначе модель будет видна изнанкой
                        треугольники.Add(сдвиг + (int)номера[i]);
                        треугольники.Add(сдвиг + (int)номера[i + 2]);
                        треугольники.Add(сдвиг + (int)номера[i + 1]);
                    }
                }
                else
                {
                    for (int i = 0; i + 2 < сколько; i += 3)
                    {
                        треугольники.Add(сдвиг + i);
                        треугольники.Add(сдвиг + i + 2);
                        треугольники.Add(сдвиг + i + 1);
                    }
                }
                части.Add(треугольники);
                материалыЧастей.Add(Целое(примитив, "material", -1));
            }

            Mesh сетка = new Mesh { name = описание.TryGetValue("name", out object имя) ? (string)имя : "Модель " + m };
            if (точки.Count > 65000) сетка.indexFormat = IndexFormat.UInt32;
            сетка.SetVertices(точки);
            сетка.subMeshCount = части.Count;
            for (int i = 0; i < части.Count; i++) сетка.SetTriangles(части[i], i);
            if (естьРазвёртка) сетка.SetUVs(0, развёртка);
            if (естьЦвета) сетка.SetColors(цвета);
            if (всеСНормалями) сетка.SetNormals(нормали);
            else сетка.RecalculateNormals();
            сетка.RecalculateBounds();
            if (естьРазвёртка) сетка.RecalculateTangents();

            контекст.AddObjectToAsset("сетка " + m, сетка);
            сетки[m] = сетка;
            материалыСеток[m] = материалыЧастей;
        }
    }

    void СоздатьУзел(int номер, Transform родитель)
    {
        Dictionary<string, object> узел = (Dictionary<string, object>)Список("nodes")[номер];
        GameObject объект = new GameObject(узел.TryGetValue("name", out object имя) ? (string)имя : "Узел " + номер);
        объект.transform.SetParent(родитель, false);

        if (узел.TryGetValue("matrix", out _))
        {
            float[] м = Числа(узел, "matrix");
            Matrix4x4 матрица = new Matrix4x4(
                new Vector4(м[0], м[1], м[2], м[3]), new Vector4(м[4], м[5], м[6], м[7]),
                new Vector4(м[8], м[9], м[10], м[11]), new Vector4(м[12], м[13], м[14], м[15]));
            Matrix4x4 отражение = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            матрица = отражение * матрица * отражение;
            объект.transform.localPosition = матрица.GetColumn(3);
            объект.transform.localRotation = матрица.rotation;
            объект.transform.localScale = матрица.lossyScale;
        }
        else
        {
            float[] t = Числа(узел, "translation", 0, 0, 0);
            float[] r = Числа(узел, "rotation", 0, 0, 0, 1);
            float[] s = Числа(узел, "scale", 1, 1, 1);
            объект.transform.localPosition = new Vector3(-t[0], t[1], t[2]);
            объект.transform.localRotation = new Quaternion(r[0], -r[1], -r[2], r[3]);
            объект.transform.localScale = new Vector3(s[0], s[1], s[2]);
        }

        if (узел.TryGetValue("mesh", out object сетка))
        {
            int номерСетки = Convert.ToInt32(сетка);
            Mesh модель = сетки[номерСетки];
            объект.AddComponent<MeshFilter>().sharedMesh = модель;
            MeshRenderer отрисовщик = объект.AddComponent<MeshRenderer>();
            List<int> номера = материалыСеток[номерСетки];
            Material[] набор = new Material[номера.Count];
            for (int i = 0; i < номера.Count; i++)
                набор[i] = материалы && номера[i] >= 0 && номера[i] < готовыеМатериалы.Length && готовыеМатериалы[номера[i]] != null ? готовыеМатериалы[номера[i]] : стандартный;
            отрисовщик.sharedMaterials = набор;
            объект.AddComponent<Моделлер>();
            if (коллайдер)
            {
                объект.AddComponent<MeshCollider>().sharedMesh = модель;
                объект.AddComponent<КоллайдерМеш>();
            }
        }

        if (узел.TryGetValue("children", out object дети))
        {
            foreach (object ребёнок in (List<object>)дети) СоздатьУзел(Convert.ToInt32(ребёнок), объект.transform);
        }
    }
}

// Маленький разборщик JSON: JsonUtility Unity не умеет читать произвольные объекты
internal static class ПростойJSON
{
    public static object Разобрать(string текст)
    {
        int место = 0;
        return Значение(текст, ref место);
    }

    static void Пропустить(string т, ref int м)
    {
        while (м < т.Length && char.IsWhiteSpace(т[м])) м++;
    }

    static object Значение(string т, ref int м)
    {
        Пропустить(т, ref м);
        char с = т[м];
        if (с == '{')
        {
            Dictionary<string, object> объект = new Dictionary<string, object>();
            м++;
            Пропустить(т, ref м);
            if (т[м] == '}') { м++; return объект; }
            while (true)
            {
                Пропустить(т, ref м);
                string ключ = СтрокаJSON(т, ref м);
                Пропустить(т, ref м);
                м++; // двоеточие
                объект[ключ] = Значение(т, ref м);
                Пропустить(т, ref м);
                if (т[м++] == '}') return объект;
            }
        }
        if (с == '[')
        {
            List<object> список = new List<object>();
            м++;
            Пропустить(т, ref м);
            if (т[м] == ']') { м++; return список; }
            while (true)
            {
                список.Add(Значение(т, ref м));
                Пропустить(т, ref м);
                if (т[м++] == ']') return список;
            }
        }
        if (с == '"') return СтрокаJSON(т, ref м);
        if (т.Length - м >= 4 && т.Substring(м, 4) == "true") { м += 4; return true; }
        if (т.Length - м >= 5 && т.Substring(м, 5) == "false") { м += 5; return false; }
        if (т.Length - м >= 4 && т.Substring(м, 4) == "null") { м += 4; return null; }

        int начало = м;
        while (м < т.Length && "+-0123456789.eE".IndexOf(т[м]) >= 0) м++;
        return double.Parse(т.Substring(начало, м - начало), CultureInfo.InvariantCulture);
    }

    static string СтрокаJSON(string т, ref int м)
    {
        StringBuilder результат = new StringBuilder();
        м++;
        while (т[м] != '"')
        {
            char с = т[м++];
            if (с != '\\') { результат.Append(с); continue; }
            char знак = т[м++];
            switch (знак)
            {
                case 'n': результат.Append('\n'); break;
                case 't': результат.Append('\t'); break;
                case 'r': результат.Append('\r'); break;
                case 'b': результат.Append('\b'); break;
                case 'f': результат.Append('\f'); break;
                case 'u': результат.Append((char)Convert.ToInt32(т.Substring(м, 4), 16)); м += 4; break;
                default: результат.Append(знак); break;
            }
        }
        м++;
        return результат.ToString();
    }
}

#endif
