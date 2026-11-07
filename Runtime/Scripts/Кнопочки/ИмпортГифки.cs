#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Превращает файл .gif в Кадровую анимацию. Unity сама умеет читать только первый кадр гифки,
/// поэтому кадры разбираются здесь и сохраняются отдельными картинками рядом с анимацией
/// </summary>
public static class ИмпортГифки
{
    [MenuItem("Assets/Create/Runity/Анимация из гифки...", false, -98)]
    static void СоздатьАнимацию()
    {
        string файл = ВыбранныйФайлГифки();
        if (файл != null)
        {
            Импортировать(Path.GetFullPath(файл), Path.ChangeExtension(файл, ".asset"));
            return;
        }

        string путь = EditorUtility.OpenFilePanel("Выберите гифку", "", "gif");
        if (string.IsNullOrEmpty(путь)) return;

        string папка = "Assets";
        string выбранное = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (AssetDatabase.IsValidFolder(выбранное)) папка = выбранное;
        else if (!string.IsNullOrEmpty(выбранное)) папка = Path.GetDirectoryName(выбранное).Replace("\\", "/");

        Импортировать(путь, $"{папка}/{Path.GetFileNameWithoutExtension(путь)}.asset");
    }

    static string ВыбранныйФайлГифки()
    {
        string путь = AssetDatabase.GetAssetPath(Selection.activeObject);
        return путь.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? путь : null;
    }

    /// <summary>
    /// Спрашивает файл гифки и заменяет ею кадры анимации
    /// </summary>
    public static void ВыбратьИИмпортировать(КадроваяАнимация анимация)
    {
        string путь = EditorUtility.OpenFilePanel("Выберите гифку", "", "gif");
        if (string.IsNullOrEmpty(путь)) return;

        Заполнить(анимация, путь);
    }

    /// <summary>
    /// Создаёт анимацию из файла гифки и возвращает её
    /// </summary>
    public static КадроваяАнимация Импортировать(string путьКГифке, string путьКАнимации)
    {
        КадроваяАнимация анимация = ScriptableObject.CreateInstance<КадроваяАнимация>();
        AssetDatabase.CreateAsset(анимация, AssetDatabase.GenerateUniqueAssetPath(путьКАнимации));

        Заполнить(анимация, путьКГифке);

        Selection.activeObject = анимация;
        return анимация;
    }

    static void Заполнить(КадроваяАнимация анимация, string путьКГифке)
    {
        ДекодерГифок.Результат гифка;

        try
        {
            гифка = ДекодерГифок.Прочитать(File.ReadAllBytes(путьКГифке));
        }
        catch (Exception ошибка) when (ошибка is FormatException || ошибка is IndexOutOfRangeException || ошибка is IOException)
        {
            EditorUtility.DisplayDialog("Runity", $"Не получилось прочитать гифку: {ошибка.Message}", "Понятно");
            return;
        }

        string путьАнимации = AssetDatabase.GetAssetPath(анимация);
        string папка = Path.GetDirectoryName(путьАнимации).Replace("\\", "/");
        string папкаКадров = $"{папка}/{анимация.name} кадры";

        if (!AssetDatabase.IsValidFolder(папкаКадров))
        {
            AssetDatabase.CreateFolder(папка, $"{анимация.name} кадры");
        }

        List<string> пути = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < гифка.кадры.Count; i++)
            {
                string путь = $"{папкаКадров}/кадр_{i:000}.png";
                СохранитьКадр(гифка, i, путь);
                пути.Add(путь);
                AssetDatabase.ImportAsset(путь);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Undo.RecordObject(анимация, "Импорт гифки");
        анимация.картинки.Clear();
        анимация.спрайты.Clear();

        foreach (string путь in пути)
        {
            TextureImporter импортёр = (TextureImporter)AssetImporter.GetAtPath(путь);
            импортёр.textureType = TextureImporterType.Sprite;
            импортёр.spriteImportMode = SpriteImportMode.Single;
            импортёр.mipmapEnabled = false;
            импортёр.textureCompression = TextureImporterCompression.Uncompressed;
            импортёр.SaveAndReimport();

            анимация.спрайты.Add(AssetDatabase.LoadAssetAtPath<Sprite>(путь));
        }

        анимация.кадровВСекунду = гифка.кадровВСекунду;
        EditorUtility.SetDirty(анимация);
        AssetDatabase.SaveAssets();
    }

    static void СохранитьКадр(ДекодерГифок.Результат гифка, int номер, string путь)
    {
        Texture2D картинка = new Texture2D(гифка.ширина, гифка.высота, TextureFormat.RGBA32, false);
        картинка.SetPixels32(гифка.кадры[номер]);
        картинка.Apply();
        File.WriteAllBytes(путь, картинка.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(картинка);
    }

    /// <summary>
    /// Даёт все спрайты, нарезанные из одного файла, в порядке слева направо и сверху вниз
    /// </summary>
    public static List<Sprite> СпрайтыАтласа(Texture2D атлас)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(атлас))
            .OfType<Sprite>()
            .OrderByDescending(спрайт => Mathf.Round(спрайт.rect.yMax))
            .ThenBy(спрайт => спрайт.rect.x)
            .ToList();
    }
}

/// <summary>
/// Читает формат GIF: заголовок, палитры, кадры со сжатием LZW и наложение кадров друг на друга
/// </summary>
internal static class ДекодерГифок
{
    internal class Результат
    {
        public int ширина;
        public int высота;
        /// <summary>
        /// Готовые кадры целиком: строки идут снизу вверх, как в Texture2D
        /// </summary>
        public List<Color32[]> кадры = new List<Color32[]>();
        /// <summary>
        /// Сколько миллисекунд показывается каждый кадр
        /// </summary>
        public List<int> задержки = new List<int>();

        public float кадровВСекунду
        {
            get
            {
                double среднее = задержки.Count > 0 ? задержки.Average() : 100;
                return (float)(1000.0 / Math.Max(среднее, 1));
            }
        }
    }

    public static Результат Прочитать(byte[] данные)
    {
        if (данные.Length < 13 || данные[0] != 'G' || данные[1] != 'I' || данные[2] != 'F')
        {
            throw new FormatException("это не файл GIF");
        }

        int п = 6;
        Результат результат = new Результат { ширина = Слово(данные, ref п), высота = Слово(данные, ref п) };
        int флаги = данные[п++];
        п += 2;

        Color32[] общаяПалитра = (флаги & 0x80) != 0 ? Палитра(данные, ref п, 2 << (флаги & 7)) : null;

        int ш = результат.ширина;
        int в = результат.высота;
        Color32[] холст = new Color32[ш * в];
        int задержка = 0;
        int прозрачный = -1;
        int удаление = 0;

        while (п < данные.Length)
        {
            int блок = данные[п++];

            if (блок == 0x3B) break;

            if (блок == 0x21)
            {
                int метка = данные[п++];
                if (метка == 0xF9)
                {
                    п++;
                    int настройки = данные[п++];
                    задержка = Слово(данные, ref п) * 10;
                    int индексПрозрачности = данные[п++];
                    прозрачный = (настройки & 1) != 0 ? индексПрозрачности : -1;
                    удаление = (настройки >> 2) & 7;
                    п++;
                }
                else
                {
                    ПропуститьБлоки(данные, ref п);
                }
            }
            else if (блок == 0x2C)
            {
                int x = Слово(данные, ref п);
                int y = Слово(данные, ref п);
                int кадрШ = Слово(данные, ref п);
                int кадрВ = Слово(данные, ref п);
                int флагиКадра = данные[п++];

                Color32[] палитра = (флагиКадра & 0x80) != 0 ? Палитра(данные, ref п, 2 << (флагиКадра & 7)) : общаяПалитра;
                if (палитра == null) throw new FormatException("у кадра нет палитры");

                byte[] индексы = Распаковать(данные, ref п, кадрШ * кадрВ);
                Color32[] доКадра = удаление == 3 ? (Color32[])холст.Clone() : null;

                НаложитьКадр(холст, ш, в, индексы, палитра, x, y, кадрШ, кадрВ, (флагиКадра & 0x40) != 0, прозрачный);

                результат.кадры.Add(ПеревернутьСтроки(холст, ш, в));
                // Многие гифки указывают нулевую задержку, а браузеры показывают такие кадры по 100 мс
                результат.задержки.Add(задержка >= 20 ? задержка : 100);

                if (удаление == 2)
                {
                    for (int строка = y; строка < Math.Min(y + кадрВ, в); строка++)
                    {
                        Array.Clear(холст, строка * ш + x, Math.Min(кадрШ, ш - x));
                    }
                }
                else if (удаление == 3)
                {
                    холст = доКадра;
                }

                прозрачный = -1;
                задержка = 0;
                удаление = 0;
            }
            else
            {
                throw new FormatException($"неизвестный блок {блок}");
            }
        }

        if (результат.кадры.Count == 0) throw new FormatException("в файле нет кадров");

        return результат;
    }

    static int Слово(byte[] данные, ref int п)
    {
        int значение = данные[п] | (данные[п + 1] << 8);
        п += 2;
        return значение;
    }

    static Color32[] Палитра(byte[] данные, ref int п, int размер)
    {
        Color32[] палитра = new Color32[размер];
        for (int i = 0; i < размер; i++)
        {
            палитра[i] = new Color32(данные[п], данные[п + 1], данные[п + 2], 255);
            п += 3;
        }
        return палитра;
    }

    static void ПропуститьБлоки(byte[] данные, ref int п)
    {
        while (данные[п] != 0)
        {
            п += данные[п] + 1;
        }
        п++;
    }

    static byte[] Распаковать(byte[] данные, ref int п, int количество)
    {
        const int максимумКодов = 4096;

        int размерМинимальный = данные[п++];
        int кодОчистки = 1 << размерМинимальный;
        int кодКонца = кодОчистки + 1;
        int следующий = кодКонца + 1;
        int размерКода = размерМинимальный + 1;
        int прошлый = -1;
        int первый = 0;

        short[] префикс = new short[максимумКодов];
        byte[] суффикс = new byte[максимумКодов];
        byte[] стек = new byte[максимумКодов + 1];
        byte[] результат = new byte[количество];

        for (int код = 0; код < кодОчистки; код++)
        {
            суффикс[код] = (byte)код;
        }

        int записано = 0;
        int биты = 0;
        int битовВБуфере = 0;
        int осталосьВБлоке = 0;
        bool конец = false;

        while (!конец)
        {
            while (битовВБуфере < размерКода)
            {
                if (осталосьВБлоке == 0)
                {
                    осталосьВБлоке = данные[п++];
                    if (осталосьВБлоке == 0)
                    {
                        return результат;
                    }
                }
                биты |= данные[п++] << битовВБуфере;
                битовВБуфере += 8;
                осталосьВБлоке--;
            }

            int код = биты & ((1 << размерКода) - 1);
            биты >>= размерКода;
            битовВБуфере -= размерКода;

            if (код == кодОчистки)
            {
                размерКода = размерМинимальный + 1;
                следующий = кодКонца + 1;
                прошлый = -1;
                continue;
            }

            if (код == кодКонца)
            {
                конец = true;
                continue;
            }

            if (код > следующий) throw new FormatException("повреждены данные кадра");

            if (прошлый == -1)
            {
                первый = код;
                прошлый = код;
                if (записано < количество) результат[записано++] = (byte)код;
                continue;
            }

            int текущий = код;
            int вершина = 0;

            if (текущий >= следующий)
            {
                стек[вершина++] = (byte)первый;
                текущий = прошлый;
            }

            while (текущий >= кодОчистки)
            {
                стек[вершина++] = суффикс[текущий];
                текущий = префикс[текущий];
            }

            первый = суффикс[текущий];
            стек[вершина++] = (byte)первый;

            if (следующий < максимумКодов)
            {
                префикс[следующий] = (short)прошлый;
                суффикс[следующий] = (byte)первый;
                следующий++;

                if (следующий == 1 << размерКода && размерКода < 12)
                {
                    размерКода++;
                }
            }

            прошлый = код;

            while (вершина > 0 && записано < количество)
            {
                результат[записано++] = стек[--вершина];
            }
        }

        // После кода конца могут остаться пустые блоки
        п += осталосьВБлоке;
        ПропуститьБлоки(данные, ref п);
        return результат;
    }

    static void НаложитьКадр(Color32[] холст, int ш, int в, byte[] индексы, Color32[] палитра, int x, int y, int кадрШ, int кадрВ, bool чересстрочно, int прозрачный)
    {
        int[] начала = { 0, 4, 2, 1 };
        int[] шаги = { 8, 8, 4, 2 };
        int проходов = чересстрочно ? 4 : 1;
        int п = 0;

        for (int проход = 0; проход < проходов; проход++)
        {
            int шаг = чересстрочно ? шаги[проход] : 1;

            for (int строка = чересстрочно ? начала[проход] : 0; строка < кадрВ; строка += шаг)
            {
                for (int столбец = 0; столбец < кадрШ; столбец++)
                {
                    int индекс = индексы[п++];
                    int целевойX = x + столбец;
                    int целевойY = y + строка;

                    if (индекс == прозрачный || индекс >= палитра.Length || целевойX >= ш || целевойY >= в) continue;

                    холст[целевойY * ш + целевойX] = палитра[индекс];
                }
            }
        }
    }

    static Color32[] ПеревернутьСтроки(Color32[] холст, int ш, int в)
    {
        Color32[] результат = new Color32[холст.Length];
        for (int строка = 0; строка < в; строка++)
        {
            Array.Copy(холст, строка * ш, результат, (в - 1 - строка) * ш, ш);
        }
        return результат;
    }
}
#endif
