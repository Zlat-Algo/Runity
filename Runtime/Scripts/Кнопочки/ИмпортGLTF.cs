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
/// Переносит объекты, их расположение, модели, материалы, текстуры, скелет, морфы, анимации и коллайдеры
/// (из физических расширений glTF и по именам вроде UCX_ или -col).
/// Если в проект установлен официальный пакет glTFast, модели читает он
/// </summary>
[ScriptedImporter(3, new[] { "gltf", "glb" })]
public class ИмпортGLTF : ScriptedImporter
{
    [Подсказка("Во сколько раз изменить размер модели. В glTF размеры в метрах, как и в Unity")]
    public float масштаб = 1;

    [Подсказка("Если \"да\", то к каждой части модели добавляется коллайдер по её форме")]
    public bool коллайдер = false;

    [Подсказка("Если \"нет\", то вместо материалов из файла ставится стандартный")]
    public bool материалы = true;

    [Подсказка("Если \"нет\", поверхность матовая: без гладкости, металлического блеска и бликов. Многие модели из интернета без этого выглядят как пластик или лакированные. Если \"да\", гладкость и металл берутся из файла")]
    public bool гладкостьИзФайла = false;

    [Подсказка("Если \"да\", лишние пустые обёртки вроде Sketchfab_model, root и GLTF_SceneRootNode убираются, а модель называется как файл")]
    public bool сократитьИерархию = true;

    [Подсказка("Если \"да\", переносятся анимации: на модель ставится Аниматор, и первая анимация играет сама")]
    public bool анимации = true;

    Dictionary<string, object> json;
    byte[][] буферы;
    string папка;
    AssetImportContext контекст;
    Texture2D[] текстуры;
    Material[] готовыеМатериалы;
    Material стандартный;
    Mesh[] сетки;
    List<int>[] материалыСеток;
    Transform[] узлы;
    Transform корень;

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
        GameObject объектКорня = new GameObject(имя);
        корень = объектКорня.transform;
        List<object> всеУзлы = Список("nodes");
        узлы = new Transform[всеУзлы.Count];

        List<object> сцены = Список("scenes");
        int номерСцены = json.TryGetValue("scene", out object сцена) ? Convert.ToInt32(сцена) : 0;
        List<int> корни = new List<int>();
        if (сцены.Count > 0 && ((Dictionary<string, object>)сцены[Mathf.Clamp(номерСцены, 0, сцены.Count - 1)]).TryGetValue("nodes", out object узлыСцены))
            foreach (object узел in (List<object>)узлыСцены) корни.Add(Convert.ToInt32(узел));
        else
        {
            HashSet<int> дети = new HashSet<int>();
            foreach (object узел in всеУзлы)
                if (((Dictionary<string, object>)узел).TryGetValue("children", out object список)) foreach (object ребёнок in (List<object>)список) дети.Add(Convert.ToInt32(ребёнок));
            for (int i = 0; i < всеУзлы.Count; i++) if (!дети.Contains(i)) корни.Add(i);
        }

        // Сначала все объекты и их расположение, потом модели: скелету нужны уже готовые кости
        foreach (int узел in корни) СоздатьУзел(узел, корень);
        for (int i = 0; i < узлы.Length; i++) if (узлы[i] != null) ДобавитьСодержимое(i);

        HashSet<int> анимированные = new HashSet<int>();
        foreach (object анимация in Список("animations"))
            foreach (object канал in (List<object>)((Dictionary<string, object>)анимация)["channels"])
                if (((Dictionary<string, object>)((Dictionary<string, object>)канал)["target"]).TryGetValue("node", out object цель)) анимированные.Add(Convert.ToInt32(цель));
        if (сократитьИерархию) СократитьИерархию(анимированные);
        if (анимации) СоздатьАнимации();

        корень.localScale = Vector3.one * масштаб;
        GameObject корень_ = объектКорня;

        ctx.AddObjectToAsset("модель", корень_);
        ctx.SetMainObject(корень_);
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
            string видПрозрачности = описание.TryGetValue("alphaMode", out object режим) ? (string)режим : "OPAQUE";
            bool прозрачный = видПрозрачности == "BLEND";
            // Без своей текстуры металличности вся поверхность считалась бы металлом
            if (pbr.ContainsKey("metallicRoughnessTexture") && !pbr.ContainsKey("metallicFactor")) металл = 0;
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
            if (!гладкостьИзФайла) МатовыеМатериалы.Сделать(материал, urp);
            if (прозрачный) СделатьПрозрачным(материал, urp);
            if (сОбеихСторон) материал.SetFloat(urp ? "_Cull" : "_CullMode", 0);
            if (видПрозрачности == "MASK")
            {
                // Вырезание по прозрачности: листья, решётки, трава
                материал.SetFloat(urp ? "_AlphaClip" : "_Mode", urp ? 1 : 1);
                материал.SetFloat("_Cutoff", Дробь(описание, "alphaCutoff", 0.5f));
                материал.EnableKeyword(urp ? "_ALPHATEST_ON" : "_ALPHATEST_ON");
                материал.renderQueue = (int)RenderQueue.AlphaTest;
            }
            // Preserve Specular Lighting почти всегда не нужен: из-за него прозрачные детали выглядят слишком яркими
            if (urp) материал.SetFloat("_BlendModePreserveSpecular", 0);

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
            List<BoneWeight> веса = new List<BoneWeight>();
            List<List<Vector3>> морфы = new List<List<Vector3>>(), морфыНормалей = new List<List<Vector3>>();
            bool всеСНормалями = true, естьРазвёртка = false, естьЦвета = false, естьКости = false;

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

                // Скелет: к каким костям (до четырёх) привязана каждая точка и с какой силой
                if (атрибуты.TryGetValue("JOINTS_0", out object кости) && атрибуты.TryGetValue("WEIGHTS_0", out object силы))
                {
                    естьКости = true;
                    float[] номера = Прочитать(Convert.ToInt32(кости), out _);
                    float[] доли = Прочитать(Convert.ToInt32(силы), out _);
                    for (int i = 0; i < сколько; i++)
                    {
                        float сумма = доли[i * 4] + доли[i * 4 + 1] + доли[i * 4 + 2] + доли[i * 4 + 3];
                        if (сумма <= 0) сумма = 1;
                        веса.Add(new BoneWeight
                        {
                            boneIndex0 = (int)номера[i * 4], weight0 = доли[i * 4] / сумма,
                            boneIndex1 = (int)номера[i * 4 + 1], weight1 = доли[i * 4 + 1] / сумма,
                            boneIndex2 = (int)номера[i * 4 + 2], weight2 = доли[i * 4 + 2] / сумма,
                            boneIndex3 = (int)номера[i * 4 + 3], weight3 = доли[i * 4 + 3] / сумма,
                        });
                    }
                }
                else for (int i = 0; i < сколько; i++) веса.Add(new BoneWeight { weight0 = 1 });

                // Морфы: сдвиги точек для мимики и плавных изменений формы
                List<object> цели = примитив.TryGetValue("targets", out object t) ? (List<object>)t : new List<object>();
                for (int k = 0; k < цели.Count; k++)
                {
                    while (морфы.Count <= k)
                    {
                        морфы.Add(new List<Vector3>(new Vector3[сдвиг]));
                        морфыНормалей.Add(new List<Vector3>(new Vector3[сдвиг]));
                    }
                    Dictionary<string, object> цель = (Dictionary<string, object>)цели[k];
                    float[] сдвиги = цель.TryGetValue("POSITION", out object sp) ? Прочитать(Convert.ToInt32(sp), out _) : new float[сколько * 3];
                    float[] нормалиСдвиг = цель.TryGetValue("NORMAL", out object sn) ? Прочитать(Convert.ToInt32(sn), out _) : new float[сколько * 3];
                    for (int i = 0; i < сколько; i++)
                    {
                        морфы[k].Add(new Vector3(-сдвиги[i * 3], сдвиги[i * 3 + 1], сдвиги[i * 3 + 2]));
                        морфыНормалей[k].Add(new Vector3(-нормалиСдвиг[i * 3], нормалиСдвиг[i * 3 + 1], нормалиСдвиг[i * 3 + 2]));
                    }
                }
                for (int k = цели.Count; k < морфы.Count; k++)
                {
                    морфы[k].AddRange(new Vector3[сколько]);
                    морфыНормалей[k].AddRange(new Vector3[сколько]);
                }

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
            if (естьКости) сетка.boneWeights = веса.ToArray();

            List<object> именаМорфов = описание.TryGetValue("extras", out object доп) && ((Dictionary<string, object>)доп).TryGetValue("targetNames", out object тн) ? (List<object>)тн : new List<object>();
            for (int k = 0; k < морфы.Count; k++)
            {
                if (морфы[k].Count != точки.Count) continue;
                string названиеМорфа = k < именаМорфов.Count ? (string)именаМорфов[k] : "Морф " + k;
                сетка.AddBlendShapeFrame(названиеМорфа, 100, морфы[k].ToArray(), морфыНормалей[k].ToArray(), null);
            }

            контекст.AddObjectToAsset("сетка " + m, сетка);
            сетки[m] = сетка;
            материалыСеток[m] = материалыЧастей;
        }
    }

    void СоздатьУзел(int номер, Transform родитель)
    {
        Dictionary<string, object> узел = (Dictionary<string, object>)Список("nodes")[номер];
        // У каждого узла должно быть имя: по нему анимации находят свои кости
        GameObject объект = new GameObject(узел.TryGetValue("name", out object имя) && !string.IsNullOrEmpty((string)имя) ? (string)имя : "Узел " + номер);
        объект.transform.SetParent(родитель, false);
        узлы[номер] = объект.transform;

        if (узел.TryGetValue("matrix", out _))
        {
            float[] м = Числа(узел, "matrix");
            Matrix4x4 матрица = ВUnity(м, 0);
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

        if (узел.TryGetValue("children", out object дети))
        {
            foreach (object ребёнок in (List<object>)дети) СоздатьУзел(Convert.ToInt32(ребёнок), объект.transform);
        }
    }

    // Матрица glTF (по столбцам) в матрицу Unity: ось X отражается с обеих сторон
    static Matrix4x4 ВUnity(float[] м, int с)
    {
        Matrix4x4 матрица = new Matrix4x4(
            new Vector4(м[с], м[с + 1], м[с + 2], м[с + 3]), new Vector4(м[с + 4], м[с + 5], м[с + 6], м[с + 7]),
            new Vector4(м[с + 8], м[с + 9], м[с + 10], м[с + 11]), new Vector4(м[с + 12], м[с + 13], м[с + 14], м[с + 15]));
        Matrix4x4 отражение = Matrix4x4.Scale(new Vector3(-1, 1, 1));
        return отражение * матрица * отражение;
    }

    void ДобавитьСодержимое(int номер)
    {
        Dictionary<string, object> узел = (Dictionary<string, object>)Список("nodes")[номер];
        GameObject объект = узлы[номер].gameObject;

        if (узел.TryGetValue("mesh", out object сетка))
        {
            int номерСетки = Convert.ToInt32(сетка);
            Mesh модель = сетки[номерСетки];
            List<int> номера = материалыСеток[номерСетки];
            Material[] набор = new Material[номера.Count];
            for (int i = 0; i < номера.Count; i++)
                набор[i] = материалы && номера[i] >= 0 && номера[i] < готовыеМатериалы.Length && готовыеМатериалы[номера[i]] != null ? готовыеМатериалы[номера[i]] : стандартный;

            if (ЭтоКоллайдер(объект.name, out string вид))
            {
                // Невидимая модель-коллайдер, как в Unreal (UCX_) и Godot (-col)
                ДобавитьКоллайдерПоФорме(объект, модель, вид);
            }
            else if (узел.TryGetValue("skin", out object скин) || модель.blendShapeCount > 0)
            {
                SkinnedMeshRenderer кожа = объект.AddComponent<SkinnedMeshRenderer>();
                кожа.sharedMesh = модель;
                кожа.sharedMaterials = набор;
                if (скин != null) НастроитьСкелет(кожа, Convert.ToInt32(скин));
                if (узел.TryGetValue("weights", out _))
                {
                    float[] силы = Числа(узел, "weights");
                    for (int k = 0; k < силы.Length && k < модель.blendShapeCount; k++) кожа.SetBlendShapeWeight(k, силы[k] * 100);
                }
            }
            else
            {
                объект.AddComponent<MeshFilter>().sharedMesh = модель;
                объект.AddComponent<MeshRenderer>().sharedMaterials = набор;
                объект.AddComponent<Моделлер>();
                if (коллайдер)
                {
                    объект.AddComponent<MeshCollider>().sharedMesh = модель;
                    объект.AddComponent<КоллайдерМодели>();
                }
            }
        }

        ДобавитьФизику(объект, узел);
    }

    void НастроитьСкелет(SkinnedMeshRenderer кожа, int номерСкина)
    {
        Dictionary<string, object> скин = (Dictionary<string, object>)Список("skins")[номерСкина];
        List<object> суставы = (List<object>)скин["joints"];
        Transform[] кости = new Transform[суставы.Count];
        for (int i = 0; i < кости.Length; i++) кости[i] = узлы[Convert.ToInt32(суставы[i])];

        Matrix4x4[] позы = new Matrix4x4[кости.Length];
        if (скин.TryGetValue("inverseBindMatrices", out object матрицы))
        {
            float[] м = Прочитать(Convert.ToInt32(матрицы), out _);
            for (int i = 0; i < позы.Length; i++) позы[i] = ВUnity(м, i * 16);
        }
        else for (int i = 0; i < позы.Length; i++) позы[i] = Matrix4x4.identity;

        кожа.sharedMesh.bindposes = позы;
        кожа.bones = кости;
        кожа.rootBone = скин.TryGetValue("skeleton", out object скелет) ? узлы[Convert.ToInt32(скелет)] : кости.Length > 0 ? кости[0] : null;
        кожа.updateWhenOffscreen = false;
    }

    #region Коллайдеры

    // Соглашения об именах коллайдеров из других движков
    static bool ЭтоКоллайдер(string имя, out string вид)
    {
        вид = null;
        foreach (string приставка in new[] { "UCX_", "UBX_", "USP_", "UCP_" })
            if (имя.StartsWith(приставка, StringComparison.OrdinalIgnoreCase)) { вид = приставка.Substring(0, 3).ToUpperInvariant(); return true; }
        string нижнее = имя.ToLowerInvariant();
        if (нижнее.EndsWith("-col") || нижнее.EndsWith("_col") || нижнее.EndsWith("-colonly") || нижнее.EndsWith("-convcol") || нижнее.EndsWith("_collider") || нижнее == "collider")
        {
            вид = нижнее.EndsWith("-convcol") ? "UCX" : "MESH";
            return true;
        }
        return false;
    }

    void ДобавитьКоллайдерПоФорме(GameObject объект, Mesh модель, string вид)
    {
        Bounds рамка = модель.bounds;
        switch (вид)
        {
            case "UBX":
                BoxCollider куб = объект.AddComponent<BoxCollider>();
                куб.center = рамка.center; куб.size = рамка.size;
                объект.AddComponent<КоллайдерКуб>();
                break;
            case "USP":
                SphereCollider сфера = объект.AddComponent<SphereCollider>();
                сфера.center = рамка.center; сфера.radius = Mathf.Max(рамка.extents.x, рамка.extents.y, рамка.extents.z);
                объект.AddComponent<КоллайдерСфера>();
                break;
            case "UCP":
                CapsuleCollider капсула = объект.AddComponent<CapsuleCollider>();
                капсула.center = рамка.center; капсула.height = рамка.size.y; капсула.radius = Mathf.Max(рамка.extents.x, рамка.extents.z);
                объект.AddComponent<КоллайдерКапсула>();
                break;
            default:
                MeshCollider сетка = объект.AddComponent<MeshCollider>();
                сетка.sharedMesh = модель;
                сетка.convex = вид == "UCX";
                объект.AddComponent<КоллайдерМодели>();
                break;
        }
    }

    // Физические расширения glTF: KHR_physics_rigid_bodies (с формами из KHR_implicit_shapes) и OMI_physics_body (с формами из OMI_physics_shape)
    void ДобавитьФизику(GameObject объект, Dictionary<string, object> узел)
    {
        if (!узел.TryGetValue("extensions", out object р)) return;
        Dictionary<string, object> расширения = (Dictionary<string, object>)р;

        Dictionary<string, object> тело = null, форма = null;
        bool триггер = false;
        if (расширения.TryGetValue("KHR_physics_rigid_bodies", out object khr))
        {
            тело = (Dictionary<string, object>)khr;
            Dictionary<string, object> коллайдерУзла = тело.TryGetValue("collider", out object c) ? (Dictionary<string, object>)c : тело.TryGetValue("trigger", out object tr) ? (Dictionary<string, object>)tr : null;
            триггер = тело.ContainsKey("trigger");
            if (коллайдерУзла != null && коллайдерУзла.TryGetValue("geometry", out object g)) форма = Форма((Dictionary<string, object>)g, "KHR_implicit_shapes");
            тело = тело.TryGetValue("motion", out object движение) ? (Dictionary<string, object>)движение : null;
        }
        else if (расширения.TryGetValue("OMI_physics_body", out object omi))
        {
            Dictionary<string, object> описание = (Dictionary<string, object>)omi;
            foreach (string ключ in new[] { "collider", "trigger" })
                if (описание.TryGetValue(ключ, out object часть) && ((Dictionary<string, object>)часть).TryGetValue("shape", out object номер))
                {
                    форма = ФормаПоНомеру(Convert.ToInt32(номер), "OMI_physics_shape");
                    триггер = ключ == "trigger";
                }
            тело = описание.TryGetValue("motion", out object движение) ? (Dictionary<string, object>)движение : null;
        }
        else return;

        if (форма != null)
        {
            Collider коллайдерФормы = ДобавитьФорму(объект, форма);
            if (коллайдерФормы != null) коллайдерФормы.isTrigger = триггер;
        }

        if (тело != null)
        {
            Rigidbody физика = объект.AddComponent<Rigidbody>();
            физика.mass = Дробь(тело, "mass", 1);
            string видДвижения = тело.TryGetValue("type", out object вд) ? (string)вд : "dynamic";
            физика.isKinematic = (тело.TryGetValue("isKinematic", out object кин) && (bool)кин) || видДвижения == "kinematic";
            объект.AddComponent<ФизическоеТело>();
        }
    }

    Dictionary<string, object> Форма(Dictionary<string, object> геометрия, string расширение)
    {
        if (геометрия.TryGetValue("shape", out object номер)) return ФормаПоНомеру(Convert.ToInt32(номер), расширение);
        if (геометрия.TryGetValue("mesh", out object сетка)) return new Dictionary<string, object> { ["type"] = "mesh", ["mesh"] = сетка, ["convexHull"] = геометрия.TryGetValue("convexHull", out object выпуклый) && (bool)выпуклый };
        return null;
    }

    Dictionary<string, object> ФормаПоНомеру(int номер, string расширение)
    {
        if (!json.TryGetValue("extensions", out object р) || !((Dictionary<string, object>)р).TryGetValue(расширение, out object формы)) return null;
        List<object> список = (List<object>)((Dictionary<string, object>)формы)["shapes"];
        return номер >= 0 && номер < список.Count ? (Dictionary<string, object>)список[номер] : null;
    }

    Collider ДобавитьФорму(GameObject объект, Dictionary<string, object> форма)
    {
        string вид = форма.TryGetValue("type", out object т) ? (string)т : "";
        Dictionary<string, object> данные = форма.TryGetValue(вид, out object д) ? (Dictionary<string, object>)д : форма;
        switch (вид)
        {
            case "box":
                float[] размер = Числа(данные, "size", 1, 1, 1);
                BoxCollider куб = объект.AddComponent<BoxCollider>();
                куб.size = new Vector3(размер[0], размер[1], размер[2]);
                объект.AddComponent<КоллайдерКуб>();
                return куб;
            case "sphere":
                SphereCollider сфера = объект.AddComponent<SphereCollider>();
                сфера.radius = Дробь(данные, "radius", 0.5f);
                объект.AddComponent<КоллайдерСфера>();
                return сфера;
            case "capsule":
            case "cylinder":
                CapsuleCollider капсула = объект.AddComponent<CapsuleCollider>();
                float радиус = Дробь(данные, "radius", Дробь(данные, "radiusTop", 0.5f));
                капсула.radius = радиус;
                капсула.height = Дробь(данные, "height", 2) + (вид == "capsule" && !данные.ContainsKey("mid") ? радиус * 2 : 0);
                объект.AddComponent<КоллайдерКапсула>();
                return капсула;
            case "mesh":
            case "convex":
            case "trimesh":
                int номерСетки = данные.TryGetValue("mesh", out object м) ? Convert.ToInt32(м) : -1;
                if (номерСетки < 0 || номерСетки >= сетки.Length) return null;
                MeshCollider коллайдерСетки = объект.AddComponent<MeshCollider>();
                коллайдерСетки.sharedMesh = сетки[номерСетки];
                коллайдерСетки.convex = вид == "convex" || (данные.TryGetValue("convexHull", out object выпуклый) && (bool)выпуклый);
                объект.AddComponent<КоллайдерМодели>();
                return коллайдерСетки;
        }
        return null;
    }

    #endregion

    #region Иерархия

    // Убирает пустые обёртки с одним ребёнком (Sketchfab_model, root, GLTF_SceneRootNode). Их расположение переносится на детей,
    // поэтому модель выглядит так же. Обёртку над анимированными узлами со своим поворотом или размером оставляем: иначе анимация съехала бы
    void СократитьИерархию(HashSet<int> анимированные)
    {
        HashSet<Transform> анимируемые = new HashSet<Transform>();
        foreach (int номер in анимированные) if (номер >= 0 && номер < узлы.Length && узлы[номер] != null) анимируемые.Add(узлы[номер]);

        while (корень.childCount == 1)
        {
            Transform обёртка = корень.GetChild(0);
            if (обёртка.GetComponents<Component>().Length > 1 || анимируемые.Contains(обёртка) || обёртка.childCount == 0) break;

            bool своёРасположение = обёртка.localPosition != Vector3.zero || обёртка.localRotation != Quaternion.identity || обёртка.localScale != Vector3.one;
            bool естьАнимацияНиже = false;
            foreach (Transform часть in обёртка.GetComponentsInChildren<Transform>(true)) естьАнимацияНиже |= анимируемые.Contains(часть);
            if (своёРасположение && естьАнимацияНиже) break;

            Matrix4x4 матрица = Matrix4x4.TRS(обёртка.localPosition, обёртка.localRotation, обёртка.localScale);
            List<Transform> дети = new List<Transform>();
            foreach (Transform ребёнок in обёртка) дети.Add(ребёнок);
            foreach (Transform ребёнок in дети)
            {
                Matrix4x4 итог = матрица * Matrix4x4.TRS(ребёнок.localPosition, ребёнок.localRotation, ребёнок.localScale);
                ребёнок.SetParent(корень, false);
                ребёнок.localPosition = итог.GetColumn(3);
                ребёнок.localRotation = итог.rotation;
                ребёнок.localScale = итог.lossyScale;
            }
            DestroyImmediate(обёртка.gameObject);
        }
    }

    #endregion

    #region Анимации

    void СоздатьАнимации()
    {
        List<object> анимации = Список("animations");
        if (анимации.Count == 0) return;

        List<AnimationClip> клипы = new List<AnimationClip>();
        for (int a = 0; a < анимации.Count; a++)
        {
            Dictionary<string, object> анимация = (Dictionary<string, object>)анимации[a];
            AnimationClip клип = new AnimationClip { name = анимация.TryGetValue("name", out object имя) && !string.IsNullOrEmpty((string)имя) ? (string)имя : "Анимация " + (a + 1), frameRate = 30 };
            List<object> выборки = (List<object>)анимация["samplers"];

            foreach (object к in (List<object>)анимация["channels"])
            {
                Dictionary<string, object> канал = (Dictionary<string, object>)к;
                Dictionary<string, object> цель = (Dictionary<string, object>)канал["target"];
                if (!цель.TryGetValue("node", out object н)) continue;
                Transform узел = узлы[Convert.ToInt32(н)];
                if (узел == null) continue;
                string путь = AnimationUtility.CalculateTransformPath(узел, корень);
                Dictionary<string, object> выборка = (Dictionary<string, object>)выборки[Convert.ToInt32(канал["sampler"])];
                float[] время = Прочитать(Convert.ToInt32(выборка["input"]), out _);
                float[] значения = Прочитать(Convert.ToInt32(выборка["output"]), out int размер);
                string способ = выборка.TryGetValue("interpolation", out object и) ? (string)и : "LINEAR";
                string что = (string)цель["path"];

                if (что == "translation") ЗаписатьКривые(клип, путь, typeof(Transform), new[] { "localPosition.x", "localPosition.y", "localPosition.z" }, время, значения, 3, способ, new[] { -1f, 1, 1 });
                else if (что == "scale") ЗаписатьКривые(клип, путь, typeof(Transform), new[] { "localScale.x", "localScale.y", "localScale.z" }, время, значения, 3, способ, new[] { 1f, 1, 1 });
                else if (что == "rotation")
                {
                    // Повороты подряд держим в одном «полушарии»: иначе модель крутилась бы длинным путём
                    int шаг = способ == "CUBICSPLINE" ? 12 : 4, середина = способ == "CUBICSPLINE" ? 4 : 0;
                    for (int i = 1; i < время.Length; i++)
                    {
                        int п = (i - 1) * шаг + середина, т = i * шаг + середина;
                        float скалярное = значения[п] * значения[т] + значения[п + 1] * значения[т + 1] + значения[п + 2] * значения[т + 2] + значения[п + 3] * значения[т + 3];
                        if (скалярное < 0) for (int c = 0; c < шаг; c++) значения[i * шаг + c] = -значения[i * шаг + c];
                    }
                    ЗаписатьКривые(клип, путь, typeof(Transform), new[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" }, время, значения, 4, способ, new[] { 1f, -1, -1, 1 });
                }
                else if (что == "weights" && узел.TryGetComponent(out SkinnedMeshRenderer кожа) && кожа.sharedMesh != null)
                {
                    int морфов = кожа.sharedMesh.blendShapeCount;
                    if (морфов == 0) continue;
                    string[] свойства = new string[морфов];
                    float[] множители = new float[морфов];
                    for (int m = 0; m < морфов; m++) { свойства[m] = "blendShape." + кожа.sharedMesh.GetBlendShapeName(m); множители[m] = 100; }
                    ЗаписатьКривые(клип, путь, typeof(SkinnedMeshRenderer), свойства, время, значения, морфов, способ, множители);
                }
            }

            клип.EnsureQuaternionContinuity();
            AnimationClipSettings настройки = AnimationUtility.GetAnimationClipSettings(клип);
            настройки.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(клип, настройки);
            контекст.AddObjectToAsset("анимация " + a, клип);
            клипы.Add(клип);
        }

        // Аниматор с готовым контроллером: каждая анимация - отдельное состояние, первая играет сама
        UnityEditor.Animations.AnimatorController контроллер = new UnityEditor.Animations.AnimatorController { name = корень.name + " (анимации)" };
        контроллер.AddLayer("Основной слой");
        UnityEditor.Animations.AnimatorStateMachine машина = контроллер.layers[0].stateMachine;
        машина.hideFlags = HideFlags.HideInHierarchy;
        контекст.AddObjectToAsset("контроллер", контроллер);
        контекст.AddObjectToAsset("машина состояний", машина);
        for (int i = 0; i < клипы.Count; i++)
        {
            UnityEditor.Animations.AnimatorState состояние = машина.AddState(клипы[i].name);
            состояние.motion = клипы[i];
            состояние.hideFlags = HideFlags.HideInHierarchy;
            контекст.AddObjectToAsset("состояние " + i, состояние);
        }

        Animator аниматор = корень.gameObject.AddComponent<Animator>();
        аниматор.runtimeAnimatorController = контроллер;
        корень.gameObject.AddComponent<Аниматор>();
    }

    static void ЗаписатьКривые(AnimationClip клип, string путь, Type тип, string[] свойства, float[] время, float[] значения, int размер, string способ, float[] множители)
    {
        bool кубическая = способ == "CUBICSPLINE", ступенька = способ == "STEP";
        int шаг = кубическая ? размер * 3 : размер;
        for (int c = 0; c < размер; c++)
        {
            Keyframe[] ключи = new Keyframe[время.Length];
            for (int i = 0; i < время.Length; i++)
            {
                float множитель = множители[c];
                float значение = значения[i * шаг + (кубическая ? размер : 0) + c] * множитель;
                ключи[i] = new Keyframe(время[i], значение);
                if (кубическая)
                {
                    // Касательные в glTF - скорость изменения за секунду, как и в Unity
                    ключи[i].inTangent = значения[i * шаг + c] * множитель;
                    ключи[i].outTangent = значения[i * шаг + размер * 2 + c] * множитель;
                }
            }
            for (int i = 0; i < ключи.Length && !кубическая; i++)
            {
                if (ступенька)
                {
                    ключи[i].inTangent = float.PositiveInfinity;
                    ключи[i].outTangent = float.PositiveInfinity;
                    continue;
                }
                // Прямые отрезки между ключами
                ключи[i].inTangent = i > 0 ? (ключи[i].value - ключи[i - 1].value) / Mathf.Max(0.0001f, ключи[i].time - ключи[i - 1].time) : 0;
                ключи[i].outTangent = i + 1 < ключи.Length ? (ключи[i + 1].value - ключи[i].value) / Mathf.Max(0.0001f, ключи[i + 1].time - ключи[i].time) : 0;
            }
            клип.SetCurve(путь, тип, свойства[c], new AnimationCurve(ключи));
        }
    }

    #endregion
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
