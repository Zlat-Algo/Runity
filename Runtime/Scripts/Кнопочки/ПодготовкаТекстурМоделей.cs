#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

// Картинки, которые лежат рядом с моделью glTF, сразу импортируются правильно: карта неровностей - как Normal Map,
// а карты металла, шероховатости и затенения - без перевода цвета. Тогда модель использует их напрямую, без копий
public class ПодготовкаТекстурМоделей : AssetPostprocessor
{
    enum Роль { Цвет, Неровности, Данные }

    void OnPreprocessTexture()
    {
        if (!assetImporter.importSettingsMissing || !(assetImporter is TextureImporter импорт)) return;
        Роль роль = РольКартинки(assetPath);
        if (роль == Роль.Неровности) импорт.textureType = TextureImporterType.NormalMap;
        else if (роль == Роль.Данные) импорт.sRGBTexture = false;
    }

    // Ищем модели glTF в папке картинки и двух папках выше
    static Роль РольКартинки(string путьКартинки)
    {
        string полный = Path.GetFullPath(путьКартинки);
        string папка = Path.GetDirectoryName(путьКартинки);
        for (int уровень = 0; уровень < 3 && !string.IsNullOrEmpty(папка); уровень++, папка = Path.GetDirectoryName(папка))
        {
            foreach (string модель in Directory.GetFiles(папка, "*.gltf"))
            {
                Dictionary<string, Роль> роли = Роли(модель);
                if (роли.TryGetValue(полный, out Роль роль)) return роль;
            }
        }
        return Роль.Цвет;
    }

    static readonly Dictionary<string, (DateTime когда, Dictionary<string, Роль> роли)> кэш = new Dictionary<string, (DateTime, Dictionary<string, Роль>)>();

    static Dictionary<string, Роль> Роли(string модель)
    {
        DateTime изменён = File.GetLastWriteTimeUtc(модель);
        if (кэш.TryGetValue(модель, out var был) && был.когда == изменён) return был.роли;

        Dictionary<string, Роль> роли = new Dictionary<string, Роль>(StringComparer.OrdinalIgnoreCase);
        try
        {
            Dictionary<string, object> json = (Dictionary<string, object>)ПростойJSON.Разобрать(File.ReadAllText(модель));
            List<object> Список(Dictionary<string, object> где, string ключ) => где.TryGetValue(ключ, out object з) && з is List<object> с ? с : new List<object>();
            List<object> картинки = Список(json, "images"), текстуры = Список(json, "textures");

            void Отметить(object ссылка, Роль роль)
            {
                if (!(ссылка is Dictionary<string, object> с) || !с.TryGetValue("index", out object н)) return;
                int номер = Convert.ToInt32(н);
                if (номер < 0 || номер >= текстуры.Count || !((Dictionary<string, object>)текстуры[номер]).TryGetValue("source", out object и)) return;
                int источник = Convert.ToInt32(и);
                if (источник < 0 || источник >= картинки.Count || !((Dictionary<string, object>)картинки[источник]).TryGetValue("uri", out object адрес)) return;
                string строка = (string)адрес;
                if (строка.StartsWith("data:")) return;
                string путь = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(модель), Uri.UnescapeDataString(строка)));
                // Если одна картинка служит и цветом, и данными, оставляем как цвет
                if (!роли.TryGetValue(путь, out Роль была) || была == Роль.Данные) роли[путь] = роль;
            }

            foreach (object м in Список(json, "materials"))
            {
                Dictionary<string, object> материал = (Dictionary<string, object>)м;
                if (материал.TryGetValue("normalTexture", out object n)) Отметить(n, Роль.Неровности);
                if (материал.TryGetValue("occlusionTexture", out object o)) Отметить(o, Роль.Данные);
                if (материал.TryGetValue("pbrMetallicRoughness", out object pbr))
                {
                    Dictionary<string, object> основа = (Dictionary<string, object>)pbr;
                    if (основа.TryGetValue("metallicRoughnessTexture", out object mr)) Отметить(mr, Роль.Данные);
                    if (основа.TryGetValue("baseColorTexture", out object bc)) Отметить(bc, Роль.Цвет);
                }
                if (материал.TryGetValue("emissiveTexture", out object e)) Отметить(e, Роль.Цвет);
            }
        }
        catch { }
        кэш[модель] = (изменён, роли);
        return роли;
    }
}
#endif
