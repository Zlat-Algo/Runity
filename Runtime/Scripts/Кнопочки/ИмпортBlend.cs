#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Читает файлы Blender (.blend) без самой программы Blender: объекты, их расположение, модели, развёртку и цвета материалов.
/// Модификаторы, анимации и кости не переносятся - для них нужен установленный Blender, тогда Unity откроет файл сам.
/// Если Blender не установлен, Runity включает этот способ сам. Переключить способ можно в окне Inspector файла
/// </summary>
[ScriptedImporter(1, new string[0], new[] { "blend" })]
public class ИмпортBlend : ScriptedImporter
{
    [Подсказка("Во сколько раз изменить размер модели")]
    public float масштаб = 1;

    [Подсказка("Если \"да\", то поверхность сглаживается. Если \"нет\", видны грани")]
    public bool сглаживание = false;

    [Подсказка("Если \"да\", то к каждой части модели добавляется коллайдер по её форме")]
    public bool коллайдер = false;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        ФайлBlend файл;
        try
        {
            файл = new ФайлBlend(File.ReadAllBytes(ctx.assetPath));
        }
        catch (Exception ошибка)
        {
            ctx.LogImportError($"Не удалось прочитать {ctx.assetPath}: {ошибка.Message}");
            GameObject пустой = new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
            ctx.AddObjectToAsset("модель", пустой);
            ctx.SetMainObject(пустой);
            return;
        }

        GameObject корень = new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
        Dictionary<ulong, Mesh> сетки = new Dictionary<ulong, Mesh>();
        Dictionary<ulong, Material> материалы = new Dictionary<ulong, Material>();
        Dictionary<ulong, List<Material>> наборы = new Dictionary<ulong, List<Material>>();
        Dictionary<ulong, GameObject> объекты = new Dictionary<ulong, GameObject>();
        List<(ulong адрес, ulong родитель, Matrix4x4 матрица)> места = new List<(ulong, ulong, Matrix4x4)>();
        Material стандартный = new Material(Шейдер()) { name = "Стандартный" };
        ctx.AddObjectToAsset("стандартный", стандартный);
        int номер = 0;

        foreach (ФайлBlend.Блок блок in файл.Блоки("OB"))
        {
            ФайлBlend.Структура объект = файл.Прочитать(блок);
            short вид = объект.Короткое("type");
            GameObject созданный = new GameObject(объект.Имя());
            объекты[блок.адрес] = созданный;
            места.Add((блок.адрес, объект.Указатель("parent"), МатрицаОбъекта(объект)));

            if (вид != 1) continue; // 1 - сетка. Камеры, свет и прочее остаются пустыми объектами
            ulong адресСетки = объект.Указатель("data");
            if (адресСетки == 0) continue;

            if (!сетки.TryGetValue(адресСетки, out Mesh сетка))
            {
                List<int> материалыЧастей;
                сетка = ПрочитатьСетку(файл, файл.Прочитать(адресСетки), out материалыЧастей, out List<ulong> адресаМатериалов);
                if (сетка == null) continue;
                ctx.AddObjectToAsset("сетка " + номер++, сетка);
                сетки[адресСетки] = сетка;

                List<Material> набор = new List<Material>();
                foreach (int частьМатериала in материалыЧастей)
                {
                    ulong адрес = частьМатериала >= 0 && частьМатериала < адресаМатериалов.Count ? адресаМатериалов[частьМатериала] : 0;
                    набор.Add(адрес == 0 ? стандартный : Материал(файл, адрес, материалы, ctx));
                }
                наборы[адресСетки] = набор;
            }

            созданный.AddComponent<MeshFilter>().sharedMesh = сетка;
            созданный.AddComponent<MeshRenderer>().sharedMaterials = наборы[адресСетки].ToArray();
            созданный.AddComponent<Моделлер>();
            if (коллайдер)
            {
                созданный.AddComponent<MeshCollider>().sharedMesh = сетка;
                созданный.AddComponent<КоллайдерМеш>();
            }
        }

        foreach ((ulong адрес, ulong родитель, Matrix4x4 матрица) in места)
        {
            Transform часть = объекты[адрес].transform;
            часть.SetParent(родитель != 0 && объекты.TryGetValue(родитель, out GameObject р) ? р.transform : корень.transform, false);
            // Blender: верх - ось Z. Unity: верх - ось Y. Оси Y и Z меняются местами
            Matrix4x4 перестановка = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
            Matrix4x4 вUnity = перестановка * матрица * перестановка;
            часть.localPosition = вUnity.GetColumn(3);
            часть.localRotation = вUnity.ValidTRS() ? вUnity.rotation : Quaternion.identity;
            часть.localScale = вUnity.lossyScale;
        }
        корень.transform.localScale = Vector3.one * масштаб;

        if (сетки.Count == 0) ctx.LogImportWarning("В файле не нашлось моделей");
        ctx.AddObjectToAsset("модель", корень);
        ctx.SetMainObject(корень);
    }

    static Shader Шейдер()
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        return Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard") ?? Shader.Find("Standard");
    }

    static Material Материал(ФайлBlend файл, ulong адрес, Dictionary<ulong, Material> готовые, AssetImportContext ctx)
    {
        if (готовые.TryGetValue(адрес, out Material готовый)) return готовый;
        ФайлBlend.Структура описание = файл.Прочитать(адрес);
        Material материал = new Material(Шейдер()) { name = описание.Имя() };
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Color цвет = new Color(описание.Дробное("r", 0.8f), описание.Дробное("g", 0.8f), описание.Дробное("b", 0.8f), описание.Дробное("a", 1));
        материал.SetColor(urp ? "_BaseColor" : "_Color", цвет);
        материал.SetFloat("_Metallic", описание.Дробное("metallic", 0));
        материал.SetFloat(urp ? "_Smoothness" : "_Glossiness", 1 - описание.Дробное("roughness", 0.5f));
        ctx.AddObjectToAsset("материал " + готовые.Count, материал);
        готовые[адрес] = материал;
        return материал;
    }

    // Расположение объекта относительно родителя в осях Blender
    static Matrix4x4 МатрицаОбъекта(ФайлBlend.Структура объект)
    {
        float[] место = объект.Дробные("loc", 3);
        float[] размер = объект.ЕстьПоле("size") ? объект.Дробные("size", 3) : объект.Дробные("scale", 3);
        Quaternion поворот;
        if (объект.Короткое("rotmode") == 0)
        {
            float[] q = объект.Дробные("quat", 4);
            поворот = new Quaternion(q[1], q[2], q[3], q[0]);
        }
        else
        {
            // Поворот XYZ в Blender: сначала вокруг X, потом Y, потом Z
            float[] r = объект.Дробные("rot", 3);
            поворот = Quaternion.AngleAxis(r[2] * Mathf.Rad2Deg, Vector3.forward) * Quaternion.AngleAxis(r[1] * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(r[0] * Mathf.Rad2Deg, Vector3.right);
        }
        Matrix4x4 своё = Matrix4x4.TRS(new Vector3(место[0], место[1], место[2]), поворот, new Vector3(размер[0], размер[1], размер[2]));
        if (объект.Указатель("parent") == 0) return своё;

        float[] м = объект.Дробные("parentinv", 16);
        Matrix4x4 обратная = new Matrix4x4(new Vector4(м[0], м[1], м[2], м[3]), new Vector4(м[4], м[5], м[6], м[7]), new Vector4(м[8], м[9], м[10], м[11]), new Vector4(м[12], м[13], м[14], м[15]));
        return обратная * своё;
    }

    Mesh ПрочитатьСетку(ФайлBlend файл, ФайлBlend.Структура описание, out List<int> материалыЧастей, out List<ulong> адресаМатериалов)
    {
        материалыЧастей = new List<int>();
        адресаМатериалов = new List<ulong>();

        // Точки. В старых версиях - массив MVert, в новых - слой "position"
        Vector3[] точки;
        if (описание.ЕстьПоле("mvert") && описание.Указатель("mvert") != 0)
        {
            ФайлBlend.Массив mvert = файл.МассивСтруктур(описание.Указатель("mvert"));
            точки = new Vector3[mvert.количество];
            for (int i = 0; i < точки.Length; i++)
            {
                float[] co = mvert[i].Дробные("co", 3);
                точки[i] = new Vector3(co[0], co[1], co[2]);
            }
        }
        else
        {
            float[] позиции = файл.СлойДробных(описание, "vdata", "position", out _);
            if (позиции == null) return null;
            точки = new Vector3[позиции.Length / 3];
            for (int i = 0; i < точки.Length; i++) точки[i] = new Vector3(позиции[i * 3], позиции[i * 3 + 1], позиции[i * 3 + 2]);
        }

        // Углы граней: какая точка в каком углу
        int[] углы;
        if (описание.ЕстьПоле("mloop") && описание.Указатель("mloop") != 0)
        {
            ФайлBlend.Массив mloop = файл.МассивСтруктур(описание.Указатель("mloop"));
            углы = new int[mloop.количество];
            for (int i = 0; i < углы.Length; i++) углы[i] = mloop[i].Целое("v");
        }
        else углы = файл.СлойЦелых(описание, "ldata", ".corner_vert");
        if (углы == null) return null;

        // Грани: с какого угла начинаются и сколько у них углов
        List<(int начало, int сколько, int материал)> грани = new List<(int, int, int)>();
        if (описание.ЕстьПоле("mpoly") && описание.Указатель("mpoly") != 0)
        {
            ФайлBlend.Массив mpoly = файл.МассивСтруктур(описание.Указатель("mpoly"));
            for (int i = 0; i < mpoly.количество; i++) грани.Add((mpoly[i].Целое("loopstart"), mpoly[i].Целое("totloop"), mpoly[i].Короткое("mat_nr")));
        }
        else
        {
            int[] начала = файл.ЦелыеПоАдресу(описание.Указатель("face_offset_indices"));
            int[] номераМатериалов = файл.СлойЦелых(описание, "pdata", "material_index");
            if (начала == null) return null;
            for (int i = 0; i + 1 < начала.Length; i++)
                грани.Add((начала[i], начала[i + 1] - начала[i], номераМатериалов != null && i < номераМатериалов.Length ? номераМатериалов[i] : 0));
        }

        // Развёртка хранится для каждого угла
        float[] развёртка = null;
        if (описание.ЕстьПоле("mloopuv") && описание.Указатель("mloopuv") != 0)
        {
            ФайлBlend.Массив mloopuv = файл.МассивСтруктур(описание.Указатель("mloopuv"));
            развёртка = new float[mloopuv.количество * 2];
            for (int i = 0; i < mloopuv.количество; i++) { float[] uv = mloopuv[i].Дробные("uv", 2); развёртка[i * 2] = uv[0]; развёртка[i * 2 + 1] = uv[1]; }
        }
        else развёртка = файл.СлойРазвёртки(описание);

        // Материалы сетки
        ulong адресНабора = описание.ЕстьПоле("mat") ? описание.Указатель("mat") : 0;
        int сколькоМатериалов = описание.ЕстьПоле("totcol") ? описание.Короткое("totcol") : 0;
        if (адресНабора != 0) адресаМатериалов.AddRange(файл.УказателиПоАдресу(адресНабора, сколькоМатериалов));

        // Сглаженные нормали: сумма нормалей соседних граней в каждой точке
        Vector3[] гладкие = new Vector3[точки.Length];
        if (сглаживание)
        {
            foreach ((int начало, int сколько, int _) in грани)
            {
                if (сколько < 3) continue;
                Vector3 н = Vector3.Cross(точки[углы[начало + 1]] - точки[углы[начало]], точки[углы[начало + 2]] - точки[углы[начало]]);
                for (int k = 0; k < сколько; k++) гладкие[углы[начало + k]] += н;
            }
        }

        List<Vector3> вершины = new List<Vector3>(), нормали = new List<Vector3>();
        List<Vector2> uv0 = new List<Vector2>();
        SortedDictionary<int, List<int>> части = new SortedDictionary<int, List<int>>();
        foreach ((int начало, int сколько, int материал) in грани)
        {
            if (сколько < 3 || начало + сколько > углы.Length) continue;
            Vector3 нормальГрани = Vector3.Cross(точки[углы[начало + 1]] - точки[углы[начало]], точки[углы[начало + 2]] - точки[углы[начало]]).normalized;
            int база = вершины.Count;
            for (int k = 0; k < сколько; k++)
            {
                int угол = начало + k;
                Vector3 т = точки[углы[угол]];
                Vector3 н = сглаживание ? гладкие[углы[угол]].normalized : нормальГрани;
                // Меняем местами Y и Z: так верх Blender становится верхом Unity
                вершины.Add(new Vector3(т.x, т.z, т.y));
                нормали.Add(new Vector3(н.x, н.z, н.y));
                uv0.Add(развёртка != null && угол * 2 + 1 < развёртка.Length ? new Vector2(развёртка[угол * 2], развёртка[угол * 2 + 1]) : Vector2.zero);
            }
            if (!части.TryGetValue(материал, out List<int> треугольники)) части[материал] = треугольники = new List<int>();
            for (int k = 1; k + 1 < сколько; k++)
            {
                // После перестановки осей модель отражена, поэтому порядок углов разворачивается
                треугольники.Add(база); треугольники.Add(база + k + 1); треугольники.Add(база + k);
            }
        }
        if (вершины.Count == 0) return null;

        Mesh сетка = new Mesh { name = описание.Имя() };
        if (вершины.Count > 65000) сетка.indexFormat = IndexFormat.UInt32;
        сетка.SetVertices(вершины);
        сетка.SetNormals(нормали);
        if (развёртка != null) сетка.SetUVs(0, uv0);
        сетка.subMeshCount = части.Count;
        int номерЧасти = 0;
        foreach (KeyValuePair<int, List<int>> часть in части)
        {
            сетка.SetTriangles(часть.Value, номерЧасти++);
            материалыЧастей.Add(часть.Key);
        }
        сетка.RecalculateBounds();
        if (развёртка != null) сетка.RecalculateTangents();
        return сетка;
    }
}

// Чтение файла .blend. Файл - это набор блоков памяти Blender и описание их устройства (SDNA),
// поэтому поля находятся по имени, и чтение работает с разными версиями Blender
internal class ФайлBlend
{
    public class Блок
    {
        public string код;
        public int начало;
        public int длина;
        public ulong адрес;
        public int структура;
        public int количество;
    }

    public struct Структура
    {
        public ФайлBlend файл;
        public int тип;
        public int начало;

        public bool ЕстьПоле(string имя) => файл.Смещение(тип, имя, out _) >= 0;

        int Где(string имя)
        {
            int смещение = файл.Смещение(тип, имя, out _);
            if (смещение < 0) throw new Exception($"В {файл.типы[файл.структуры[тип].тип]} нет поля {имя}");
            return начало + смещение;
        }

        public int Целое(string имя) => BitConverter.ToInt32(файл.байты, Где(имя));
        public short Короткое(string имя) => ЕстьПоле(имя) ? BitConverter.ToInt16(файл.байты, Где(имя)) : (short)0;
        public float Дробное(string имя, float вместо) => ЕстьПоле(имя) ? BitConverter.ToSingle(файл.байты, Где(имя)) : вместо;
        public ulong Указатель(string имя) => ЕстьПоле(имя) ? файл.ПрочитатьУказатель(Где(имя)) : 0;

        public float[] Дробные(string имя, int сколько)
        {
            float[] результат = new float[сколько];
            if (!ЕстьПоле(имя)) return результат;
            int где = Где(имя);
            for (int i = 0; i < сколько; i++) результат[i] = BitConverter.ToSingle(файл.байты, где + i * 4);
            return результат;
        }

        public string Строка(string имя)
        {
            int где = Где(имя);
            файл.Смещение(тип, имя, out int размер);
            int конец = где;
            while (конец < где + размер && файл.байты[конец] != 0) конец++;
            return Encoding.UTF8.GetString(файл.байты, где, конец - где);
        }

        public Структура Вложенная(string имя) => new Структура { файл = файл, тип = файл.ТипПоля(тип, имя), начало = Где(имя) };

        // Имя из заголовка ID без двух букв вида: "OBКуб" -> "Куб"
        public string Имя()
        {
            string полное = Вложенная("id").Строка("name");
            return полное.Length > 2 ? полное.Substring(2) : полное;
        }
    }

    public struct Массив
    {
        public ФайлBlend файл;
        public Блок блок;
        public int количество;
        public Структура this[int номер] => new Структура { файл = файл, тип = блок.структура, начало = блок.начало + номер * файл.размерыТипов[файл.структуры[блок.структура].тип] };
    }

    class ОписаниеСтруктуры
    {
        public int тип;
        public List<(int тип, int имя)> поля = new List<(int, int)>();
    }

    public readonly byte[] байты;
    readonly int размерУказателя;
    readonly List<Блок> блоки = new List<Блок>();
    readonly Dictionary<ulong, Блок> поАдресу = new Dictionary<ulong, Блок>();
    string[] имена;
    string[] типы;
    short[] размерыТипов;
    List<ОписаниеСтруктуры> структуры = new List<ОписаниеСтруктуры>();
    readonly Dictionary<string, int> структураПоИмени = new Dictionary<string, int>();
    readonly Dictionary<(int, string), (int смещение, int размер, int тип)> поля = new Dictionary<(int, string), (int, int, int)>();

    public ФайлBlend(byte[] данные)
    {
        if (данные.Length > 2 && данные[0] == 0x1F && данные[1] == 0x8B)
        {
            // Старые сжатые файлы - это обычный gzip
            using (GZipStream поток = new GZipStream(new MemoryStream(данные), CompressionMode.Decompress))
            using (MemoryStream распакованное = new MemoryStream())
            {
                поток.CopyTo(распакованное);
                данные = распакованное.ToArray();
            }
        }
        else if (данные.Length > 4 && данные[0] == 0x28 && данные[1] == 0xB5 && данные[2] == 0x2F && данные[3] == 0xFD)
        {
            throw new Exception("файл сохранён со сжатием. Откройте его в Blender, снимите галочку «Compress» в окне сохранения и сохраните заново");
        }

        байты = данные;
        if (Encoding.ASCII.GetString(данные, 0, 7) != "BLENDER") throw new Exception("это не файл Blender");

        int место;
        bool большиеБлоки;
        if (char.IsDigit((char)данные[7]))
        {
            // Blender 5 и новее: "BLENDER17-01v0500" - размер заголовка, указатели, вид блоков, порядок байтов, версия
            int размерЗаголовка = int.Parse(Encoding.ASCII.GetString(данные, 7, 2));
            размерУказателя = данные[9] == '-' ? 8 : 4;
            большиеБлоки = true;
            if (данные[12] != 'v') throw new Exception("файлы с обратным порядком байтов не поддерживаются");
            место = размерЗаголовка;
        }
        else
        {
            размерУказателя = данные[7] == '-' ? 8 : 4;
            большиеБлоки = false;
            if (данные[8] != 'v') throw new Exception("файлы с обратным порядком байтов не поддерживаются");
            место = 12;
        }

        while (место + 16 <= данные.Length)
        {
            Блок блок = new Блок { код = Encoding.ASCII.GetString(данные, место, 4).TrimEnd('\0') };
            if (большиеБлоки)
            {
                блок.структура = BitConverter.ToInt32(данные, место + 4);
                блок.адрес = BitConverter.ToUInt64(данные, место + 8);
                блок.длина = (int)BitConverter.ToInt64(данные, место + 16);
                блок.количество = (int)BitConverter.ToInt64(данные, место + 24);
                место += 32;
            }
            else
            {
                блок.длина = BitConverter.ToInt32(данные, место + 4);
                блок.адрес = размерУказателя == 8 ? BitConverter.ToUInt64(данные, место + 8) : BitConverter.ToUInt32(данные, место + 8);
                блок.структура = BitConverter.ToInt32(данные, место + 8 + размерУказателя);
                блок.количество = BitConverter.ToInt32(данные, место + 12 + размерУказателя);
                место += 16 + размерУказателя;
            }
            блок.начало = место;
            место += блок.длина;
            if (блок.код == "ENDB") break;
            блоки.Add(блок);
            if (блок.адрес != 0 && !поАдресу.ContainsKey(блок.адрес)) поАдресу[блок.адрес] = блок;
            if (блок.код == "DNA1") ПрочитатьОписание(блок.начало);
        }
        if (имена == null) throw new Exception("в файле нет описания данных");
    }

    void ПрочитатьОписание(int место)
    {
        int Выровнять(int м) => (м + 3) & ~3;
        int Слово() { int з = BitConverter.ToInt32(байты, место); место += 4; return з; }
        string Строка() { int начало = место; while (байты[место] != 0) место++; string с = Encoding.UTF8.GetString(байты, начало, место - начало); место++; return с; }

        место += 4; // SDNA
        место += 4; // NAME
        имена = new string[Слово()];
        for (int i = 0; i < имена.Length; i++) имена[i] = Строка();
        место = Выровнять(место) + 4; // TYPE
        типы = new string[Слово()];
        for (int i = 0; i < типы.Length; i++) типы[i] = Строка();
        место = Выровнять(место) + 4; // TLEN
        размерыТипов = new short[типы.Length];
        for (int i = 0; i < типы.Length; i++) { размерыТипов[i] = BitConverter.ToInt16(байты, место); место += 2; }
        место = Выровнять(место) + 4; // STRC
        int сколько = Слово();
        for (int i = 0; i < сколько; i++)
        {
            ОписаниеСтруктуры с = new ОписаниеСтруктуры { тип = BitConverter.ToInt16(байты, место) };
            int полей = BitConverter.ToInt16(байты, место + 2);
            место += 4;
            for (int k = 0; k < полей; k++) { с.поля.Add((BitConverter.ToInt16(байты, место), BitConverter.ToInt16(байты, место + 2))); место += 4; }
            структуры.Add(с);
            структураПоИмени[типы[с.тип]] = i;
        }
    }

    static string ЧистоеИмя(string имя)
    {
        int скобка = имя.IndexOf('[');
        if (скобка >= 0) имя = имя.Substring(0, скобка);
        return имя.Replace("*", "").Replace("(", "").Replace(")", "");
    }

    int РазмерПоля(int тип, string имя)
    {
        int элементов = 1;
        for (int i = имя.IndexOf('['); i >= 0; i = имя.IndexOf('[', i + 1))
            элементов *= int.Parse(имя.Substring(i + 1, имя.IndexOf(']', i) - i - 1));
        return (имя.Contains("*") ? размерУказателя : размерыТипов[тип]) * элементов;
    }

    internal int Смещение(int структура, string поле, out int размер)
    {
        if (!поля.TryGetValue((структура, поле), out var найдено))
        {
            найдено = (-1, 0, -1);
            int смещение = 0;
            foreach ((int тип, int имя) in структуры[структура].поля)
            {
                int размерПоля = РазмерПоля(тип, имена[имя]);
                if (ЧистоеИмя(имена[имя]) == поле) { найдено = (смещение, размерПоля, тип); break; }
                смещение += размерПоля;
            }
            поля[(структура, поле)] = найдено;
        }
        размер = найдено.размер;
        return найдено.смещение;
    }

    internal int ТипПоля(int структура, string поле)
    {
        Смещение(структура, поле, out _);
        return структураПоИмени[типы[поля[(структура, поле)].тип]];
    }

    internal ulong ПрочитатьУказатель(int где) => размерУказателя == 8 ? BitConverter.ToUInt64(байты, где) : BitConverter.ToUInt32(байты, где);

    public IEnumerable<Блок> Блоки(string код)
    {
        foreach (Блок блок in блоки) if (блок.код == код) yield return блок;
    }

    public Структура Прочитать(Блок блок) => new Структура { файл = this, тип = блок.структура, начало = блок.начало };

    public Структура Прочитать(ulong адрес) => Прочитать(поАдресу[адрес]);

    public Массив МассивСтруктур(ulong адрес)
    {
        Блок блок = поАдресу[адрес];
        return new Массив { файл = this, блок = блок, количество = блок.количество };
    }

    public int[] ЦелыеПоАдресу(ulong адрес)
    {
        if (адрес == 0 || !поАдресу.TryGetValue(адрес, out Блок блок)) return null;
        int[] результат = new int[блок.длина / 4];
        Buffer.BlockCopy(байты, блок.начало, результат, 0, результат.Length * 4);
        return результат;
    }

    public List<ulong> УказателиПоАдресу(ulong адрес, int сколько)
    {
        List<ulong> результат = new List<ulong>();
        if (!поАдресу.TryGetValue(адрес, out Блок блок)) return результат;
        сколько = Mathf.Min(сколько > 0 ? сколько : блок.длина / размерУказателя, блок.длина / размерУказателя);
        for (int i = 0; i < сколько; i++) результат.Add(ПрочитатьУказатель(блок.начало + i * размерУказателя));
        return результат;
    }

    // Слои данных (CustomData) хранят точки, углы и развёртку в новых версиях Blender
    IEnumerable<(Структура слой, Блок данные)> Слои(Структура сетка, string группа)
    {
        if (!сетка.ЕстьПоле(группа)) yield break;
        Структура данные = сетка.Вложенная(группа);
        ulong адресСлоёв = данные.Указатель("layers");
        int сколько = данные.Целое("totlayer");
        if (адресСлоёв == 0 || !поАдресу.TryGetValue(адресСлоёв, out Блок блокСлоёв)) yield break;
        int размерСлоя = размерыТипов[структуры[блокСлоёв.структура].тип];
        for (int i = 0; i < сколько; i++)
        {
            Структура слой = new Структура { файл = this, тип = блокСлоёв.структура, начало = блокСлоёв.начало + i * размерСлоя };
            ulong адрес = слой.Указатель("data");
            if (адрес != 0 && поАдресу.TryGetValue(адрес, out Блок блок)) yield return (слой, блок);
        }
    }

    public float[] СлойДробных(Структура сетка, string группа, string имя, out int вид)
    {
        вид = 0;
        foreach ((Структура слой, Блок блок) in Слои(сетка, группа))
        {
            if (слой.Строка("name") != имя) continue;
            вид = слой.Целое("type");
            float[] результат = new float[блок.длина / 4];
            Buffer.BlockCopy(байты, блок.начало, результат, 0, результат.Length * 4);
            return результат;
        }
        return null;
    }

    public int[] СлойЦелых(Структура сетка, string группа, string имя)
    {
        foreach ((Структура слой, Блок блок) in Слои(сетка, группа))
        {
            if (слой.Строка("name") != имя) continue;
            int[] результат = new int[блок.длина / 4];
            Buffer.BlockCopy(байты, блок.начало, результат, 0, результат.Length * 4);
            return результат;
        }
        return null;
    }

    public float[] СлойРазвёртки(Структура сетка)
    {
        const int двеДроби = 49; // CD_PROP_FLOAT2
        foreach ((Структура слой, Блок блок) in Слои(сетка, "ldata"))
        {
            string имя = слой.Строка("name");
            if (слой.Целое("type") != двеДроби || имя.StartsWith(".")) continue;
            float[] результат = new float[блок.длина / 4];
            Buffer.BlockCopy(байты, блок.начало, результат, 0, результат.Length * 4);
            return результат;
        }
        return null;
    }
}

// Если Blender не установлен, файлы .blend читает Runity
internal class ВыборИмпортаBlend : AssetPostprocessor
{
    static bool? установлен;

    /// <summary>
    /// Даёт "да", если на компьютере есть программа Blender, через которую Unity открывает файлы .blend
    /// </summary>
    internal static bool BlenderУстановлен
    {
        get
        {
            if (установлен == null) установлен = НайтиBlender();
            return установлен.Value;
        }
    }

    static bool НайтиBlender()
    {
        try
        {
            if (Application.platform == RuntimePlatform.OSXEditor) return Directory.Exists("/Applications/Blender.app");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string связь = Команда("assoc .blend");
                int равно = связь.IndexOf('=');
                if (равно < 0) return false;
                string программа = Команда("ftype " + связь.Substring(равно + 1).Trim());
                равно = программа.IndexOf('=');
                if (равно < 0) return false;
                string путь = программа.Substring(равно + 1).Trim();
                путь = путь.StartsWith("\"") ? путь.Substring(1, путь.IndexOf('"', 1) - 1) : путь.Split(' ')[0];
                return File.Exists(путь);
            }
            foreach (string папка in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                if (File.Exists(Path.Combine(папка, "blender"))) return true;
            }
        }
        catch { }
        return false;
    }

    static string Команда(string текст)
    {
        System.Diagnostics.ProcessStartInfo запуск = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + текст)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (System.Diagnostics.Process процесс = System.Diagnostics.Process.Start(запуск))
        {
            string вывод = процесс.StandardOutput.ReadToEnd();
            процесс.WaitForExit(3000);
            return вывод;
        }
    }

    static void OnPostprocessAllAssets(string[] импортированные, string[] удалённые, string[] перемещённые, string[] откуда)
    {
        if (BlenderУстановлен) return;
        foreach (string путь in импортированные)
        {
            if (!путь.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)) continue;
            if (AssetDatabase.GetImporterOverride(путь) != null) continue;
            // Unity не умеет открывать .blend без Blender, поэтому файл переходит к Runity
            AssetDatabase.SetImporterOverride<ИмпортBlend>(путь);
            Debug.Log($"Blender не установлен, поэтому {путь} открыт средствами Runity. Модификаторы и анимации при этом не переносятся");
        }
    }
}
/// <summary>
/// Помогает Unity с файлами Blender (.blend). Unity сам открывает их через установленный Blender,
/// а Runity поворачивает модель правильно и объясняет, что делать, если Blender не установлен
/// </summary>
internal class ИмпортBlender : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith(".blend", StringComparison.OrdinalIgnoreCase) || !assetImporter.importSettingsMissing) return;

        // В Blender верх - ось Z, в Unity - ось Y. Без этого модель лежит повёрнутой на 90 градусов
        ModelImporter импорт = (ModelImporter)assetImporter;
        импорт.bakeAxisConversion = true;
        импорт.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPostprocessModel(GameObject модель)
    {
        if (!assetPath.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)) return;
        if (модель.GetComponentsInChildren<Renderer>(true).Length > 0)
        {
            // У каждой части модели сразу есть русский Моделлер
            foreach (MeshRenderer отрисовщик in модель.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (отрисовщик.GetComponent<Моделлер>() == null) отрисовщик.gameObject.AddComponent<Моделлер>();
            }
            return;
        }

        // Без Blender файл сразу переходит к Runity, ругаться не нужно
        if (!ВыборИмпортаBlend.BlenderУстановлен) return;
        Debug.LogError($"Не удалось открыть {assetPath}. Unity читает файлы .blend с помощью самой программы Blender: установите её с сайта blender.org " +
                       "и перезапустите Unity. Если Blender уже установлен, откройте файл в нём и выберите в окне Inspector этого файла способ «ИмпортBlend» - Runity прочитает его без Blender");
    }
}
#endif
