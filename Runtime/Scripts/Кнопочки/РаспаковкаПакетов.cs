#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Пакет Unity (.unitypackage) - это сжатый архив tar, где каждый ассет лежит в папке со своим GUID:
// сам файл (asset), его настройки (asset.meta) и путь, куда его положить (pathname)
internal static class ПакетUnity
{
    /// <summary>
    /// Раскладывает пакет в папку: каждый ассет - по своему пути внутри неё, вместе с .meta. Так ссылки между ассетами пакета не рвутся.
    /// Ассеты, которые уже есть в проекте (с тем же GUID), пропускаются. Даёт, сколько ассетов разложено
    /// </summary>
    public static int Распаковать(string пакет, string куда)
    {
        Dictionary<string, Dictionary<string, byte[]>> ассеты = new Dictionary<string, Dictionary<string, byte[]>>();
        using (FileStream файл = File.OpenRead(пакет))
        using (GZipStream распаковка = new GZipStream(файл, CompressionMode.Decompress))
        {
            foreach ((string имя, byte[] данные) in ПрочитатьTar(распаковка))
            {
                string[] части = имя.Replace('\\', '/').TrimStart('.', '/').Split('/');
                if (части.Length != 2 || данные == null) continue;
                if (!ассеты.TryGetValue(части[0], out Dictionary<string, byte[]> ассет)) ассеты[части[0]] = ассет = new Dictionary<string, byte[]>();
                ассет[части[1]] = данные;
            }
        }

        int разложено = 0;
        string полныйКуда = Path.GetFullPath(куда) + Path.DirectorySeparatorChar;
        foreach (KeyValuePair<string, Dictionary<string, byte[]>> пара in ассеты)
        {
            if (!пара.Value.TryGetValue("pathname", out byte[] имяФайла)) continue;
            string путь = Encoding.UTF8.GetString(имяФайла).Split('\n')[0].Trim().Replace('\\', '/');
            if (путь.StartsWith("Assets/")) путь = путь.Substring("Assets/".Length);
            if (string.IsNullOrEmpty(путь)) continue;

            // Этот ассет уже есть в проекте: второй с тем же GUID сломал бы ссылки на первый
            if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(пара.Key)))
            {
                Debug.Log($"Пропущен {путь}: он уже есть в проекте");
                continue;
            }

            string полный = Path.GetFullPath(Path.Combine(куда, путь));
            if (!полный.StartsWith(полныйКуда, StringComparison.OrdinalIgnoreCase)) continue;

            if (пара.Value.TryGetValue("asset", out byte[] содержимое))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(полный));
                File.WriteAllBytes(полный, содержимое);
            }
            else Directory.CreateDirectory(полный);
            if (пара.Value.TryGetValue("asset.meta", out byte[] настройки)) File.WriteAllBytes(полный + ".meta", настройки);
            разложено++;
        }
        return разложено;
    }

    // Простое чтение tar: заголовки по 512 байт, за каждым - содержимое, дополненное до 512
    static IEnumerable<(string имя, byte[] данные)> ПрочитатьTar(Stream поток)
    {
        byte[] заголовок = new byte[512];
        string длинноеИмя = null;
        while (ПрочитатьРовно(поток, заголовок, 512))
        {
            if (заголовок.All(б => б == 0)) yield break;

            string имя = Строка(заголовок, 0, 100);
            string префикс = Строка(заголовок, 345, 155);
            if (!string.IsNullOrEmpty(префикс)) имя = префикс + "/" + имя;
            long размер = Convert.ToInt64(Строка(заголовок, 124, 12).Trim().TrimEnd('\0').Trim() is string восьмеричное && восьмеричное.Length > 0 ? восьмеричное : "0", 8);
            char вид = (char)заголовок[156];

            byte[] данные = new byte[размер];
            ПрочитатьРовно(поток, данные, (int)размер);
            int добавка = (int)((512 - размер % 512) % 512);
            if (добавка > 0) ПрочитатьРовно(поток, new byte[добавка], добавка);

            if (вид == 'L') { длинноеИмя = Encoding.UTF8.GetString(данные).TrimEnd('\0'); continue; }
            if (длинноеИмя != null) { имя = длинноеИмя; длинноеИмя = null; }
            if (вид == '0' || вид == '\0') yield return (имя, данные);
            else yield return (имя, null);
        }
    }

    static string Строка(byte[] данные, int откуда, int длина)
    {
        int конец = откуда;
        while (конец < откуда + длина && данные[конец] != 0) конец++;
        return Encoding.UTF8.GetString(данные, откуда, конец - откуда);
    }

    static bool ПрочитатьРовно(Stream поток, byte[] куда, int сколько)
    {
        int прочитано = 0;
        while (прочитано < сколько)
        {
            int часть = поток.Read(куда, прочитано, сколько - прочитано);
            if (часть <= 0) return false;
            прочитано += часть;
        }
        return true;
    }
}

// Раскладывает модель (fbx, glb, blend и другие) на обычные ассеты в папке: текстуры, материалы, сетки, анимации, схему анимаций и префаб.
// Материалы по возможности становятся Материалами Runity, а у компонентов префаба появляются аналоги Runity.
// Картинки, которые уже лежат в проекте рядом с моделью, не копируются. Пустые обёртки сверху (RootNode и подобные) убираются
internal static class РаспаковкаМодели
{
    public static bool МожноРаспаковать(string путь) => AssetDatabase.LoadMainAssetAtPath(путь) is GameObject && !путь.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

    static readonly string[] картинки = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".bmp", ".exr", ".hdr" };

    public static string Распаковать(string путь)
    {
        GameObject модель = AssetDatabase.LoadAssetAtPath<GameObject>(путь);
        string имя = ИмпортGLTF.ЧистоеИмя(модель.name.Equals(Path.GetFileNameWithoutExtension(путь), StringComparison.OrdinalIgnoreCase) ? ИмпортGLTF.ИмяМодели(путь, null) : модель.name);
        if (string.IsNullOrWhiteSpace(имя)) имя = Path.GetFileNameWithoutExtension(путь);
        string папка = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(путь), имя).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(папка).Replace('\\', '/'), Path.GetFileName(папка));

        Dictionary<Object, Object> замены = new Dictionary<Object, Object>();
        Object[] части = AssetDatabase.LoadAllAssetsAtPath(путь);

        // Встроенные текстуры: если такая картинка уже лежит рядом в проекте, берём её, иначе сохраняем картинкой PNG
        Dictionary<Texture2D, string> новые = new Dictionary<Texture2D, string>();
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (Texture2D текстура in части.OfType<Texture2D>())
            {
                Texture2D рядом = НайтиКартинкуРядом(путь, текстура.name);
                if (рядом != null) { замены[текстура] = рядом; continue; }
                string файл = Уникальный(папка, текстура.name, ".png");
                File.WriteAllBytes(файл, ВКартинку(текстура));
                новые[текстура] = файл;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        foreach (KeyValuePair<Texture2D, string> картинка in новые)
        {
            AssetDatabase.ImportAsset(картинка.Value);
            замены[картинка.Key] = AssetDatabase.LoadAssetAtPath<Texture2D>(картинка.Value);
        }

        foreach (Mesh сетка in части.OfType<Mesh>())
        {
            Mesh копия = Object.Instantiate(сетка);
            копия.name = сетка.name;
            AssetDatabase.CreateAsset(копия, Уникальный(папка, сетка.name, ".asset"));
            замены[сетка] = копия;
        }

        foreach (Material материал in части.OfType<Material>())
        {
            замены[материал] = СделатьМатериал(материал, папка, замены);
        }
        ПометитьКартыНеровностей(части.OfType<Material>(), замены);

        // Префаб: та же модель, но с распакованными ассетами и без лишних обёрток сверху
        GameObject экземпляр = Object.Instantiate(модель);
        экземпляр.name = имя;
        List<AnimationClip> клипы = части.OfType<AnimationClip>().Where(к => !к.name.StartsWith("__preview__")).ToList();
        List<AnimationClip> копии = клипы.Select(к => { AnimationClip копия = Object.Instantiate(к); копия.name = к.name; return копия; }).ToList();
        СократитьОбёртки(экземпляр.transform, копии);
        for (int i = 0; i < копии.Count; i++)
        {
            AssetDatabase.CreateAsset(копии[i], Уникальный(папка, копии[i].name, ".anim"));
            замены[клипы[i]] = копии[i];
        }

        foreach (MeshFilter фильтр in экземпляр.GetComponentsInChildren<MeshFilter>(true))
            if (фильтр.sharedMesh != null && замены.TryGetValue(фильтр.sharedMesh, out Object сетка)) фильтр.sharedMesh = (Mesh)сетка;
        foreach (SkinnedMeshRenderer кожа in экземпляр.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (кожа.sharedMesh != null && замены.TryGetValue(кожа.sharedMesh, out Object сетка)) кожа.sharedMesh = (Mesh)сетка;
        foreach (MeshCollider коллайдер in экземпляр.GetComponentsInChildren<MeshCollider>(true))
            if (коллайдер.sharedMesh != null && замены.TryGetValue(коллайдер.sharedMesh, out Object сетка)) коллайдер.sharedMesh = (Mesh)сетка;
        foreach (Renderer отрисовщик in экземпляр.GetComponentsInChildren<Renderer>(true))
            отрисовщик.sharedMaterials = отрисовщик.sharedMaterials.Select(м => м != null && замены.TryGetValue(м, out Object новый) ? (Material)(новый is Материал русский ? (Material)русский : новый as Material) : м).ToArray();

        // Своя схема анимаций из распакованных клипов: каждая анимация - состояние, первая играет сама
        if (копии.Count > 0)
        {
            AnimatorController схема = AnimatorController.CreateAnimatorControllerAtPath(Уникальный(папка, имя + " (анимации)", ".controller"));
            foreach (AnimationClip клип in копии) схема.AddMotion(клип);
            Animator аниматор = экземпляр.GetComponent<Animator>();
            if (аниматор == null) аниматор = экземпляр.AddComponent<Animator>();
            аниматор.runtimeAnimatorController = схема;
        }

        АналогиRunity.Добавить(экземпляр);
        string префаб = Уникальный(папка, имя, ".prefab");
        PrefabUtility.SaveAsPrefabAsset(экземпляр, префаб);
        Object.DestroyImmediate(экземпляр);

        AssetDatabase.SaveAssets();
        Debug.Log($"Модель {путь} распакована в папку {папка}", AssetDatabase.LoadAssetAtPath<GameObject>(префаб));
        return папка;
    }

    // Картинка с тем же именем в папке модели, её подпапках или папке выше (Sketchfab кладёт текстуры в соседнюю папку textures)
    static Texture2D НайтиКартинкуРядом(string путьМодели, string имя)
    {
        if (string.IsNullOrEmpty(имя)) return null;
        string папкаМодели = Path.GetDirectoryName(путьМодели).Replace('\\', '/');
        string выше = Path.GetDirectoryName(папкаМодели)?.Replace('\\', '/');
        foreach (string где in new[] { папкаМодели, выше })
        {
            if (string.IsNullOrEmpty(где) || где == "Assets" && где != папкаМодели) continue;
            foreach (string guid in AssetDatabase.FindAssets(имя + " t:Texture2D", new[] { где }))
            {
                string найденный = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(найденный).Equals(имя, StringComparison.OrdinalIgnoreCase) && картинки.Contains(Path.GetExtension(найденный).ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(найденный);
            }
        }
        return null;
    }

    // Картинка, которая служит картой неровностей, должна быть импортирована как Normal Map, иначе свет на модели ложится неправильно
    static void ПометитьКартыНеровностей(IEnumerable<Material> материалы, Dictionary<Object, Object> замены)
    {
        foreach (Material материал in материалы)
        {
            if (!материал.HasProperty("_BumpMap") || материал.GetTexture("_BumpMap") == null) continue;
            Texture карта = материал.GetTexture("_BumpMap");
            if (замены.TryGetValue(карта, out Object новая)) карта = (Texture)новая;
            string путь = AssetDatabase.GetAssetPath(карта);
            if (AssetImporter.GetAtPath(путь) is TextureImporter импорт && импорт.textureType != TextureImporterType.NormalMap)
            {
                импорт.textureType = TextureImporterType.NormalMap;
                импорт.SaveAndReimport();
            }
        }
    }

    // Убирает пустые обёртки с одним ребёнком сверху модели, сохраняя расположение частей. Пути в анимациях исправляются.
    // Обёртку над анимированной частью со своим расположением оставляем: иначе анимация съехала бы
    internal static void СократитьОбёртки(Transform корень, List<AnimationClip> клипы)
    {
        HashSet<Transform> кости = new HashSet<Transform>();
        foreach (SkinnedMeshRenderer кожа in корень.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (Transform кость in кожа.bones) if (кость != null) кости.Add(кость);
        HashSet<string> анимированные = new HashSet<string>();
        foreach (AnimationClip клип in клипы)
        {
            foreach (EditorCurveBinding привязка in AnimationUtility.GetCurveBindings(клип)) анимированные.Add(привязка.path);
            foreach (EditorCurveBinding привязка in AnimationUtility.GetObjectReferenceCurveBindings(клип)) анимированные.Add(привязка.path);
        }

        while (корень.childCount == 1)
        {
            Transform обёртка = корень.GetChild(0);
            if (обёртка.GetComponents<Component>().Length > 1 || обёртка.childCount == 0 || кости.Contains(обёртка) || анимированные.Contains(обёртка.name)) break;
            bool своёРасположение = обёртка.localPosition.sqrMagnitude > 1e-10f || Quaternion.Angle(обёртка.localRotation, Quaternion.identity) > 0.01f || (обёртка.localScale - Vector3.one).sqrMagnitude > 1e-10f;
            bool анимированныйРебёнок = false;
            foreach (Transform ребёнок in обёртка) анимированныйРебёнок |= анимированные.Contains(обёртка.name + "/" + ребёнок.name);
            if (своёРасположение && анимированныйРебёнок) break;

            string приставка = обёртка.name + "/";
            List<Transform> дети = new List<Transform>();
            foreach (Transform ребёнок in обёртка) дети.Add(ребёнок);
            foreach (Transform ребёнок in дети) ребёнок.SetParent(корень, true);
            Object.DestroyImmediate(обёртка.gameObject);

            foreach (AnimationClip клип in клипы)
            {
                foreach (EditorCurveBinding привязка in AnimationUtility.GetCurveBindings(клип))
                {
                    if (!привязка.path.StartsWith(приставка)) continue;
                    AnimationCurve кривая = AnimationUtility.GetEditorCurve(клип, привязка);
                    AnimationUtility.SetEditorCurve(клип, привязка, null);
                    EditorCurveBinding новая = привязка;
                    новая.path = привязка.path.Substring(приставка.Length);
                    AnimationUtility.SetEditorCurve(клип, новая, кривая);
                }
                foreach (EditorCurveBinding привязка in AnimationUtility.GetObjectReferenceCurveBindings(клип))
                {
                    if (!привязка.path.StartsWith(приставка)) continue;
                    ObjectReferenceKeyframe[] ключи = AnimationUtility.GetObjectReferenceCurve(клип, привязка);
                    AnimationUtility.SetObjectReferenceCurve(клип, привязка, null);
                    EditorCurveBinding новая = привязка;
                    новая.path = привязка.path.Substring(приставка.Length);
                    AnimationUtility.SetObjectReferenceCurve(клип, новая, ключи);
                }
            }
            анимированные = new HashSet<string>(анимированные.Select(п => п.StartsWith(приставка) ? п.Substring(приставка.Length) : п));
        }
    }

    static string Уникальный(string папка, string имя, string расширение)
    {
        foreach (char запрещённый in Path.GetInvalidFileNameChars()) имя = имя.Replace(запрещённый, '_');
        if (string.IsNullOrWhiteSpace(имя)) имя = "Без имени";
        return AssetDatabase.GenerateUniqueAssetPath($"{папка}/{имя}{расширение}");
    }

    // Текстура внутри модели может быть нечитаемой и сжатой, поэтому она перерисовывается через видеокарту
    static byte[] ВКартинку(Texture2D текстура)
    {
        RenderTexture холст = RenderTexture.GetTemporary(текстура.width, текстура.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(текстура, холст);
        RenderTexture было = RenderTexture.active;
        RenderTexture.active = холст;
        Texture2D копия = new Texture2D(текстура.width, текстура.height, TextureFormat.RGBA32, false);
        копия.ReadPixels(new Rect(0, 0, текстура.width, текстура.height), 0, 0);
        копия.Apply();
        RenderTexture.active = было;
        RenderTexture.ReleaseTemporary(холст);
        byte[] png = копия.EncodeToPNG();
        Object.DestroyImmediate(копия);
        return png;
    }

    // Обычные материалы (Lit, Standard) становятся Материалами Runity, особые - копируются как есть
    static Object СделатьМатериал(Material материал, string папка, Dictionary<Object, Object> замены)
    {
        Texture Текстура(string свойство)
        {
            if (!материал.HasProperty(свойство)) return null;
            Texture текстура = материал.GetTexture(свойство);
            return текстура != null && замены.TryGetValue(текстура, out Object новая) ? (Texture)новая : текстура;
        }

        string шейдер = материал.shader != null ? материал.shader.name : "";
        bool обычный = шейдер == "Universal Render Pipeline/Lit" || шейдер == "Standard" || шейдер == "Universal Render Pipeline/Simple Lit";
        if (!обычный)
        {
            Material копия = new Material(материал) { name = материал.name };
            foreach (string свойство in копия.GetTexturePropertyNames())
            {
                Texture новая = Текстура(свойство);
                if (новая != null) копия.SetTexture(свойство, новая);
            }
            AssetDatabase.CreateAsset(копия, Уникальный(папка, материал.name, ".mat"));
            return копия;
        }

        Материал русский = ScriptableObject.CreateInstance<Материал>();
        AssetDatabase.CreateAsset(русский, Уникальный(папка, материал.name, ".asset"));
        typeof(Материал).GetMethod("СоздатьВнутреннийМатериал", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(русский, null);
        Color цвет = материал.HasProperty("_BaseColor") ? материал.GetColor("_BaseColor") : материал.HasProperty("_Color") ? материал.GetColor("_Color") : Color.white;
        русский.цвет = цвет;
        русский.текстура = (Текстура("_BaseMap") ?? Текстура("_MainTex")) as Texture2D;
        русский.картаНеровностей = Текстура("_BumpMap") as Texture2D;
        if (материал.HasProperty("_Metallic")) русский.металличность = материал.GetFloat("_Metallic");
        if (материал.HasProperty("_Smoothness")) русский.гладкость = материал.GetFloat("_Smoothness");
        else if (материал.HasProperty("_Glossiness")) русский.гладкость = материал.GetFloat("_Glossiness");
        if (материал.IsKeywordEnabled("_EMISSION") && материал.HasProperty("_EmissionColor")) русский.свечение = материал.GetColor("_EmissionColor");
        bool прозрачный = (материал.HasProperty("_Surface") && материал.GetFloat("_Surface") > 0.5f) || материал.renderQueue >= 3000;
        русский.прозрачный = прозрачный;
        EditorUtility.SetDirty(русский);
        return русский;
    }
}
#endif
