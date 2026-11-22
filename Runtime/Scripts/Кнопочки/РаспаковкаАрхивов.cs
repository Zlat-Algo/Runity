#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

/// <summary>
/// Когда в проект закидывают архив, он сам превращается в папку с тем же названием, а архив удаляется
/// </summary>
public class РаспаковкаАрхивов : AssetPostprocessor
{
    const string ключВключено = "Runity.РаспаковкаАрхивов";
    const string пунктМеню = "Runity/Распаковывать архивы в проекте";

    static readonly string[] другиеАрхивы = { ".rar", ".7z", ".tar", ".gz", ".tgz", ".unitypackage" };

    /// <summary>
    /// Если "да", то архивы распаковываются сами
    /// </summary>
    public static bool включено
    {
        get => EditorPrefs.GetBool(ключВключено, true);
        set => EditorPrefs.SetBool(ключВключено, value);
    }

    [MenuItem(пунктМеню, false, 50)]
    static void Переключить() => включено = !включено;

    [MenuItem(пунктМеню, true)]
    static bool ПоказатьГалочку()
    {
        Menu.SetChecked(пунктМеню, включено);
        return true;
    }

    // Архивы, которые только что закинули в проект. Только у нового файла ещё нет .meta, поэтому архивы,
    // которые давно лежат в проекте, пришли вместе с ним из git или переимпортируются, не трогаются
    static readonly HashSet<string> новые = new HashSet<string>();

    void OnPreprocessAsset()
    {
        if (включено && assetImporter.importSettingsMissing && ЭтоАрхив(assetPath)) новые.Add(assetPath);
    }

    static void OnPostprocessAllAssets(string[] импортированные, string[] удалённые, string[] перенесённые, string[] откуда)
    {
        if (!включено || новые.Count == 0) return;

        List<string> архивы = импортированные.Where(путь => новые.Remove(путь) && File.Exists(путь)).ToList();
        if (архивы.Count == 0) return;

        // Внутри импорта нельзя создавать и удалять ассеты, поэтому распаковка ждёт его окончания
        EditorApplication.delayCall += () =>
        {
            bool чтоТоРаспаковано = false;
            foreach (string архив in архивы) чтоТоРаспаковано |= Распаковать(архив);
            if (чтоТоРаспаковано) AssetDatabase.Refresh();
        };
    }

    internal static bool ЭтоАрхив(string путь)
    {
        string расширение = Path.GetExtension(путь).ToLowerInvariant();
        return расширение == ".zip" || другиеАрхивы.Contains(расширение);
    }

    /// <summary>
    /// Распаковывает архив в папку рядом с ним и удаляет сам архив. Даёт "да", если всё получилось
    /// </summary>
    public static bool Распаковать(string архив)
    {
        if (!File.Exists(архив)) return false;

        string имя = Path.GetFileNameWithoutExtension(архив);
        if (имя.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)) имя = Path.GetFileNameWithoutExtension(имя);
        string папка = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(архив), имя).Replace('\\', '/'));
        string временная = Path.Combine(Path.GetTempPath(), "Runity_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(временная);

            string расширение = Path.GetExtension(архив).ToLowerInvariant();
            bool распаковано = расширение == ".zip" ? РаспаковатьZip(архив, временная)
                : расширение == ".unitypackage" ? ПакетUnity.Распаковать(архив, временная) >= 0
                : РаспаковатьПрограммой(архив, временная);

            if (!распаковано)
            {
                string как = UnityEngine.Application.platform switch
                {
                    UnityEngine.RuntimePlatform.OSXEditor => "командой «brew install sevenzip» в Терминале",
                    UnityEngine.RuntimePlatform.LinuxEditor => "командой «sudo apt install 7zip»",
                    _ => "с сайта 7-zip.org или командой «winget install 7zip.7zip»"
                };
                Программы.ПредложитьУстановку($"Не получилось распаковать {Path.GetFileName(архив)}. Архивы ZIP Runity распаковывает сам, а для RAR и 7z нужна бесплатная программа 7-Zip. Установите её {как}, перезапустите Unity и снова перетащите архив", "https://www.7-zip.org/");
                return false;
            }

            // Если всё содержимое архива лежит в одной папке, лишняя вложенность не нужна
            string корень = временная;
            string[] файлы = Directory.GetFiles(корень);
            string[] папки = Directory.GetDirectories(корень);
            if (файлы.Length == 0 && папки.Length == 1) корень = папки[0];

            ПеренестиСодержимое(корень, папка);
            File.Delete(архив);
            if (File.Exists(архив + ".meta")) File.Delete(архив + ".meta");

            Debug.Log($"Архив {Path.GetFileName(архив)} распакован в папку {папка}");
            return true;
        }
        catch (Exception ошибка)
        {
            Debug.LogError($"Не получилось распаковать {архив}: {ошибка.Message}");
            return false;
        }
        finally
        {
            try { if (Directory.Exists(временная)) Directory.Delete(временная, true); } catch { }
        }
    }

    static bool РаспаковатьZip(string архив, string куда)
    {
        // Проводник Windows записывает русские имена в кодировке 866, а не в UTF-8
        Encoding кодировка;
        try { кодировка = Encoding.GetEncoding(866); }
        catch { кодировка = Encoding.UTF8; }

        string полныйКуда = Path.GetFullPath(куда) + Path.DirectorySeparatorChar;

        using (FileStream поток = File.OpenRead(архив))
        using (ZipArchive zip = new ZipArchive(поток, ZipArchiveMode.Read, false, кодировка))
        {
            foreach (ZipArchiveEntry запись in zip.Entries)
            {
                string путь = Path.GetFullPath(Path.Combine(куда, запись.FullName));
                // Защита от архивов, которые пытаются записать файлы за пределы папки
                if (!путь.StartsWith(полныйКуда, StringComparison.OrdinalIgnoreCase)) continue;

                if (string.IsNullOrEmpty(запись.Name))
                {
                    Directory.CreateDirectory(путь);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(путь));
                using (Stream из = запись.Open())
                using (FileStream в = File.Create(путь))
                {
                    из.CopyTo(в);
                }
            }
        }
        return true;
    }

    // RAR, 7z и tar распаковывает 7-Zip, а если его нет - встроенная в систему программа tar.
    // В Windows 11 и macOS она умеет открывать и RAR, и 7z
    static bool РаспаковатьПрограммой(string архив, string куда)
    {
        string[] пути7zip =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
        };
        string семьZip = пути7zip.FirstOrDefault(File.Exists) ?? Программы.Найти("7z") ?? Программы.Найти("7zz");

        if (семьZip != null && Запустить(семьZip, $"x \"{Path.GetFullPath(архив)}\" -o\"{куда}\" -y")) return true;
        return Запустить("tar", $"-xf \"{Path.GetFullPath(архив)}\" -C \"{куда}\"") && Directory.EnumerateFileSystemEntries(куда).Any();
    }

    static bool Запустить(string программа, string аргументы)
    {
        try
        {
            using (Process процесс = Process.Start(new ProcessStartInfo(программа, аргументы) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            {
                процесс.StandardOutput.ReadToEnd();
                процесс.StandardError.ReadToEnd();
                процесс.WaitForExit(120000);
                return процесс.HasExited && процесс.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    static void ПеренестиСодержимое(string откуда, string куда)
    {
        Directory.CreateDirectory(куда);
        foreach (string файл in Directory.GetFiles(откуда))
        {
            File.Copy(файл, Path.Combine(куда, Path.GetFileName(файл)), true);
        }
        foreach (string папка in Directory.GetDirectories(откуда))
        {
            ПеренестиСодержимое(папка, Path.Combine(куда, Path.GetFileName(папка)));
        }
    }
}
#endif
