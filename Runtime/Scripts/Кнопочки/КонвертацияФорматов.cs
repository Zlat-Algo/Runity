#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Переводит в форматы, которые понимает Unity, то, что он сам не читает:
/// картинки .webp - в .png (анимированные - в Кадровую анимацию, как гифки),
/// видео .webm со сжатием VP9 или AV1 - в .mp4. Видео .webm со сжатием VP8 Unity читает сам, его Runity не трогает.
/// Картинки читаются средствами самой системы (Windows, macOS), видео - средствами Windows или программой ffmpeg. Если чего-то не хватает, Runity подскажет, что поставить
/// </summary>
public class КонвертацияФорматов : AssetPostprocessor
{
    const string ключВключено = "Runity.КонвертацияФорматов";
    const string пунктМеню = "Runity/Переводить webp и webm в понятные Unity форматы";

    /// <summary>
    /// Если "да", новые файлы webp и webm переводятся сами, как только попадают в проект
    /// </summary>
    public static bool включено
    {
        get => EditorPrefs.GetBool(ключВключено, true);
        set => EditorPrefs.SetBool(ключВключено, value);
    }

    [MenuItem(пунктМеню, false, 51)]
    static void Переключить() => включено = !включено;

    [MenuItem(пунктМеню, true)]
    static bool ПоказатьГалочку()
    {
        Menu.SetChecked(пунктМеню, включено);
        return true;
    }

    static readonly HashSet<string> новые = new HashSet<string>();

    void OnPreprocessAsset()
    {
        if (включено && assetImporter.importSettingsMissing && Подходит(assetPath)) новые.Add(assetPath);
    }

    static void OnPostprocessAllAssets(string[] импортированные, string[] удалённые, string[] перенесённые, string[] откуда)
    {
        if (!включено || новые.Count == 0) return;
        List<string> файлы = импортированные.Where(путь => новые.Remove(путь) && File.Exists(путь)).ToList();
        if (файлы.Count == 0) return;

        // Менять ассеты прямо во время импорта нельзя, поэтому перевод идёт сразу после него
        EditorApplication.delayCall += () =>
        {
            // Файл могли уже перевести или удалить, пока шёл импорт
            foreach (string файл in файлы) if (File.Exists(файл)) Перевести(файл);
        };
    }

    static bool Подходит(string путь)
    {
        string расширение = Path.GetExtension(путь).ToLowerInvariant();
        return расширение == ".webp" || расширение == ".webm";
    }

    /// <summary>
    /// Переводит файл webp или webm в понятный Unity формат. Даёт путь к новому файлу или пусто, если переводить не понадобилось или не получилось
    /// </summary>
    public static string Перевести(string путь)
    {
        try
        {
            string расширение = Path.GetExtension(путь).ToLowerInvariant();
            if (расширение == ".webp") return ПеревестиКартинку(путь);
            if (расширение == ".webm") return ПеревестиВидео(путь);
        }
        catch (Exception ошибка)
        {
            Debug.LogError($"Не получилось перевести {путь}: {ошибка.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        return null;
    }

    #region Картинки WebP

    static string ПеревестиКартинку(string путь)
    {
        byte[] данные = File.ReadAllBytes(путь);
        if (данные.Length < 12 || Encoding.ASCII.GetString(данные, 0, 4) != "RIFF" || Encoding.ASCII.GetString(данные, 8, 4) != "WEBP")
            throw new Exception("это не картинка WebP");

        List<(string код, int начало, int длина)> куски = Куски(данные, 12, данные.Length);
        bool анимация = куски.Any(к => к.код == "ANMF");
        string временная = Path.Combine(Path.GetTempPath(), "Runity webp " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(временная);

        try
        {
            if (!анимация)
            {
                string png = Path.ChangeExtension(путь, ".png");
                string файл = Path.Combine(временная, "кадр.png");
                Раскодировать(new List<(string, string)> { (Path.GetFullPath(путь), файл) });
                File.Copy(файл, png, true);
                AssetDatabase.DeleteAsset(путь);
                AssetDatabase.ImportAsset(png);
                Debug.Log($"Runity перевёл {путь} в {png}");
                return png;
            }

            return ПеревестиАнимацию(путь, данные, куски, временная);
        }
        finally
        {
            try { Directory.Delete(временная, true); } catch { }
        }
    }

    // Анимированный WebP: кадры лежат внутри по отдельности, каждый - маленькая картинка WebP со смещением
    static string ПеревестиАнимацию(string путь, byte[] данные, List<(string код, int начало, int длина)> куски, string временная)
    {
        (string _, int началоЗаголовка, int _) = куски.First(к => к.код == "VP8X");
        int ширина = Число24(данные, началоЗаголовка + 4) + 1;
        int высота = Число24(данные, началоЗаголовка + 7) + 1;

        List<(int x, int y, int ш, int в, int задержка, bool очистить, bool наложить)> описания = new List<(int, int, int, int, int, bool, bool)>();
        List<(string, string)> задания = new List<(string, string)>();
        foreach ((string код, int начало, int длина) in куски.Where(к => к.код == "ANMF"))
        {
            int x = Число24(данные, начало) * 2, y = Число24(данные, начало + 3) * 2;
            int ш = Число24(данные, начало + 6) + 1, в = Число24(данные, начало + 9) + 1;
            int задержка = Число24(данные, начало + 12);
            byte флаги = данные[начало + 15];

            string файл = Path.Combine(временная, $"кадр_{описания.Count:000}.webp");
            File.WriteAllBytes(файл, ОтдельнаяКартинка(данные, начало + 16, начало + длина, ш, в));
            задания.Add((файл, Path.ChangeExtension(файл, ".png")));
            описания.Add((x, y, ш, в, задержка, (флаги & 1) != 0, (флаги & 2) == 0));
        }

        Раскодировать(задания);

        // Кадры накладываются друг на друга, как задумал автор анимации
        ДекодерГифок.Результат результат = new ДекодерГифок.Результат { ширина = ширина, высота = высота };
        Color32[] холст = new Color32[ширина * высота];
        Texture2D картинка = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        for (int i = 0; i < описания.Count; i++)
        {
            (int x, int y, int ш, int в, int задержка, bool очистить, bool наложить) = описания[i];
            картинка.LoadImage(File.ReadAllBytes(задания[i].Item2));
            Color32[] пиксели = картинка.GetPixels32();
            for (int r = 0; r < в; r++)
            {
                int строка = высота - 1 - (y + в - 1 - r);
                if (строка < 0 || строка >= высота) continue;
                for (int c = 0; c < ш && x + c < ширина; c++)
                {
                    Color32 новый = пиксели[r * ш + c];
                    int место = строка * ширина + x + c;
                    холст[место] = наложить ? Наложить(холст[место], новый) : новый;
                }
            }
            результат.кадры.Add((Color32[])холст.Clone());
            результат.задержки.Add(Mathf.Max(задержка, 10));

            if (очистить)
            {
                for (int r = 0; r < в; r++)
                {
                    int строка = высота - 1 - (y + r);
                    if (строка < 0 || строка >= высота) continue;
                    for (int c = 0; c < ш && x + c < ширина; c++) холст[строка * ширина + x + c] = default;
                }
            }
        }
        UnityEngine.Object.DestroyImmediate(картинка);

        string путьАнимации = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(путь, ".asset"));
        КадроваяАнимация анимация = ScriptableObject.CreateInstance<КадроваяАнимация>();
        AssetDatabase.CreateAsset(анимация, путьАнимации);
        ИмпортГифки.ЗаполнитьКадрами(анимация, результат);
        AssetDatabase.DeleteAsset(путь);
        Debug.Log($"Runity перевёл анимированный {путь} в Кадровую анимацию {путьАнимации}");
        return путьАнимации;
    }

    static Color32 Наложить(Color32 низ, Color32 верх)
    {
        if (верх.a == 255 || низ.a == 0) return верх;
        if (верх.a == 0) return низ;
        float а = верх.a / 255f, б = низ.a / 255f * (1 - а), итог = а + б;
        return new Color32(
            (byte)((верх.r * а + низ.r * б) / итог),
            (byte)((верх.g * а + низ.g * б) / итог),
            (byte)((верх.b * а + низ.b * б) / итог),
            (byte)(итог * 255));
    }

    static List<(string код, int начало, int длина)> Куски(byte[] данные, int откуда, int докуда)
    {
        List<(string, int, int)> куски = new List<(string, int, int)>();
        for (int место = откуда; место + 8 <= докуда;)
        {
            string код = Encoding.ASCII.GetString(данные, место, 4);
            int длина = BitConverter.ToInt32(данные, место + 4);
            куски.Add((код, место + 8, длина));
            место += 8 + длина + (длина & 1);
        }
        return куски;
    }

    static int Число24(byte[] д, int место) => д[место] | д[место + 1] << 8 | д[место + 2] << 16;

    // Собирает из данных кадра самостоятельный файл WebP, который умеет открыть Windows
    static byte[] ОтдельнаяКартинка(byte[] данные, int откуда, int докуда, int ширина, int высота)
    {
        List<(string код, int начало, int длина)> куски = Куски(данные, откуда, докуда);
        bool прозрачность = куски.Any(к => к.код == "ALPH");
        using (MemoryStream поток = new MemoryStream())
        using (BinaryWriter запись = new BinaryWriter(поток))
        {
            запись.Write(Encoding.ASCII.GetBytes("RIFF"));
            запись.Write(0);
            запись.Write(Encoding.ASCII.GetBytes("WEBP"));
            if (прозрачность)
            {
                запись.Write(Encoding.ASCII.GetBytes("VP8X"));
                запись.Write(10);
                запись.Write(new byte[] { 0x10, 0, 0, 0 });
                запись.Write(new[] { (byte)(ширина - 1), (byte)((ширина - 1) >> 8), (byte)((ширина - 1) >> 16) });
                запись.Write(new[] { (byte)(высота - 1), (byte)((высота - 1) >> 8), (byte)((высота - 1) >> 16) });
            }
            foreach ((string код, int начало, int длина) in куски.Where(к => к.код == "ALPH" || к.код == "VP8 " || к.код == "VP8L"))
            {
                запись.Write(Encoding.ASCII.GetBytes(код));
                запись.Write(длина);
                запись.Write(данные, начало, длина);
                if ((длина & 1) != 0) запись.Write((byte)0);
            }
            запись.Seek(4, SeekOrigin.Begin);
            запись.Write((int)поток.Length - 8);
            return поток.ToArray();
        }
    }

    // Переводит картинки WebP в PNG. Сначала пробуются средства самой системы: в Windows - встроенный декодер,
    // в macOS - утилита sips. Если не вышло - программы ffmpeg, dwebp или ImageMagick, если они есть
    static void Раскодировать(List<(string откуда, string куда)> задания)
    {
        EditorUtility.DisplayProgressBar("Runity", "Перевод картинок WebP...", 0.5f);
        string ошибкаСистемы = "";
        if (Application.platform == RuntimePlatform.WindowsEditor && РаскодироватьСредствамиWindows(задания, out ошибкаСистемы)) return;
        if (Application.platform == RuntimePlatform.OSXEditor && ВсеПолучились(задания, з => Программы.Запустить("sips", $"-s format png \"{з.откуда}\" --out \"{з.куда}\"", out _))) return;

        string ffmpeg = Программы.Найти("ffmpeg"), dwebp = Программы.Найти("dwebp"), magick = Программы.Найти("magick") ?? Программы.Найти("convert");
        if (ffmpeg != null && ВсеПолучились(задания, з => Программы.Запустить(ffmpeg, $"-y -loglevel error -i \"{з.откуда}\" \"{з.куда}\"", out _))) return;
        if (dwebp != null && ВсеПолучились(задания, з => Программы.Запустить(dwebp, $"\"{з.откуда}\" -o \"{з.куда}\"", out _))) return;
        if (magick != null && Application.platform != RuntimePlatform.WindowsEditor && ВсеПолучились(задания, з => Программы.Запустить(magick, $"\"{з.откуда}\" \"{з.куда}\"", out _))) return;

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            Программы.ПредложитьУстановку("Чтобы переводить картинки WebP, Windows нужно бесплатное дополнение «Расширения для изображений WebP» из Microsoft Store. Установите его и снова перетащите файл в проект",
                "ms-windows-store://pdp/?ProductId=9PG2DK419DRG");
            throw new Exception("в Windows нет декодера WebP. " + ошибкаСистемы);
        }
        Программы.ПредложитьУстановку("Чтобы переводить картинки WebP, нужна программа ffmpeg. " + Программы.КакПоставитьFfmpeg, "https://ffmpeg.org/download.html");
        throw new Exception("не нашлось программы, которая умеет открывать WebP");
    }

    static bool ВсеПолучились(List<(string откуда, string куда)> задания, Func<(string откуда, string куда), bool> перевести)
    {
        foreach ((string откуда, string куда) задание in задания)
            if (!перевести(задание) || !File.Exists(задание.куда)) return false;
        return true;
    }

    static bool РаскодироватьСредствамиWindows(List<(string откуда, string куда)> задания, out string ошибка)
    {
        string список = Path.Combine(Path.GetTempPath(), "Runity webp " + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllLines(список, задания.Select(з => з.откуда + "|" + з.куда), Encoding.UTF8);
        try
        {
            string скрипт =
                "$ErrorActionPreference = 'Stop'\n" +
                "Add-Type -AssemblyName PresentationCore\n" +
                $"foreach ($line in [IO.File]::ReadAllLines('{список.Replace("'", "''")}', [Text.Encoding]::UTF8)) {{\n" +
                "  $p = $line.Split('|'); $s = [IO.File]::OpenRead($p[0])\n" +
                "  try {\n" +
                "    $d = [Windows.Media.Imaging.BitmapDecoder]::Create($s, [Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat, [Windows.Media.Imaging.BitmapCacheOption]::OnLoad)\n" +
                "    $e = New-Object Windows.Media.Imaging.PngBitmapEncoder\n" +
                "    $e.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($d.Frames[0]))\n" +
                "    $o = [IO.File]::Create($p[1]); try { $e.Save($o) } finally { $o.Close() }\n" +
                "  } finally { $s.Close() }\n" +
                "}\n";
            return Программы.PowerShell(скрипт, out ошибка) && задания.All(з => File.Exists(з.куда));
        }
        finally
        {
            File.Delete(список);
        }
    }

    #endregion

    #region Видео WebM

    /// <summary>
    /// Каким способом сжато видео WebM: VP8, VP9 или AV1. Пусто, если не удалось понять
    /// </summary>
    public static string СжатиеВидео(string путь)
    {
        byte[] начало = new byte[Math.Min(512 * 1024, new FileInfo(путь).Length)];
        using (FileStream поток = File.OpenRead(путь)) поток.Read(начало, 0, начало.Length);
        string текст = Encoding.ASCII.GetString(начало);
        if (текст.Contains("V_VP9")) return "VP9";
        if (текст.Contains("V_AV1")) return "AV1";
        if (текст.Contains("V_VP8")) return "VP8";
        return null;
    }

    static string ПеревестиВидео(string путь)
    {
        // Видео со сжатием VP8 Unity читает сам
        if (СжатиеВидео(путь) == "VP8") return null;

        string mp4 = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(путь, ".mp4"));
        string откуда = Path.GetFullPath(путь), куда = Path.GetFullPath(mp4);
        EditorUtility.DisplayProgressBar("Runity", $"Перевод видео {Path.GetFileName(путь)} в mp4. Это может занять время...", 0.5f);

        // Сначала ffmpeg, если он есть, а в Windows - ещё и встроенный перекодировщик видео
        string ошибка = "";
        string ffmpeg = Программы.Найти("ffmpeg");
        bool получилось = ffmpeg != null && Программы.Запустить(ffmpeg, $"-y -loglevel error -i \"{откуда}\" -c:v libx264 -profile:v baseline -pix_fmt yuv420p -color_primaries bt709 -color_trc bt709 -colorspace bt709 -c:a aac \"{куда}\"", out ошибка) && Готово(куда);
        if (!получилось && Application.platform == RuntimePlatform.WindowsEditor)
        {
            получилось = ПеревестиСредствамиWindows(откуда, куда, out ошибка) && Готово(куда);
            // Перекодировщик Windows не пишет в видео метки цвета, и Unity потом предупреждает о возможном сдвиге цвета
            if (получилось) МеткиЦветаMp4.Дописать(куда);
        }

        if (!получилось)
        {
            if (File.Exists(куда)) File.Delete(куда);
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Программы.ПредложитьУстановку("Чтобы переводить видео webm, Windows нужны бесплатные дополнения из Microsoft Store: «Расширения для видео VP9» и «Расширения для веб-медиа». Установите оба и снова перетащите файл в проект",
                    "ms-windows-store://pdp/?ProductId=9N4D0MSMP0PT", "ms-windows-store://pdp/?ProductId=9N5TDP8VCMHS");
            }
            else
            {
                Программы.ПредложитьУстановку("Чтобы переводить видео webm со сжатием VP9 и AV1, нужна программа ffmpeg. " + Программы.КакПоставитьFfmpeg, "https://ffmpeg.org/download.html");
            }
            throw new Exception("видео не перевелось. " + ошибка);
        }

        AssetDatabase.DeleteAsset(путь);
        AssetDatabase.ImportAsset(mp4);
        Debug.Log($"Runity перевёл {путь} в {mp4}: Unity не умеет показывать webm со сжатием VP9 и AV1");
        return mp4;
    }

    static bool Готово(string путь) => File.Exists(путь) && new FileInfo(путь).Length > 0;

    // Встроенный в Windows перекодировщик видео
    static bool ПеревестиСредствамиWindows(string откуда, string куда, out string ошибка)
    {
        string скрипт =
            "$ErrorActionPreference = 'Stop'\n" +
            "Add-Type -AssemblyName System.Runtime.WindowsRuntime\n" +
            "$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]\n" +
            "$null = [Windows.Media.Transcoding.MediaTranscoder, Windows.Media.Transcoding, ContentType = WindowsRuntime]\n" +
            "$null = [Windows.Media.MediaProperties.MediaEncodingProfile, Windows.Media.MediaProperties, ContentType = WindowsRuntime]\n" +
            "$m = [System.WindowsRuntimeSystemExtensions].GetMethods()\n" +
            "$asOp = $m | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1\n" +
            "$asAct = $m | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncActionWithProgress`1' } | Select-Object -First 1\n" +
            "function Wait-Op($op, [Type]$t) { $task = $asOp.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait(-1) | Out-Null; $task.Result }\n" +
            $"$in = Wait-Op ([Windows.Storage.StorageFile]::GetFileFromPathAsync('{откуда.Replace("'", "''")}')) ([Windows.Storage.StorageFile])\n" +
            $"$folder = Wait-Op ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync('{Path.GetDirectoryName(куда).Replace("'", "''")}')) ([Windows.Storage.StorageFolder])\n" +
            $"$out = Wait-Op ($folder.CreateFileAsync('{Path.GetFileName(куда).Replace("'", "''")}', [Windows.Storage.CreationCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])\n" +
            "$profile = [Windows.Media.MediaProperties.MediaEncodingProfile]::CreateMp4([Windows.Media.MediaProperties.VideoEncodingQuality]::Auto)\n" +
            // Профиль Baseline: с другими Unity предупреждает о неправильных отметках времени
            "$profile.Video.ProfileId = [Windows.Media.MediaProperties.H264ProfileIds]::Baseline\n" +
            "$tr = New-Object Windows.Media.Transcoding.MediaTranscoder\n" +
            "$prep = Wait-Op ($tr.PrepareFileTranscodeAsync($in, $out, $profile)) ([Windows.Media.Transcoding.PrepareTranscodeResult])\n" +
            "if (-not $prep.CanTranscode) { throw \"Windows не может открыть это видео: $($prep.FailureReason)\" }\n" +
            "$task = $asAct.MakeGenericMethod([double]).Invoke($null, @($prep.TranscodeAsync())); $task.Wait(-1) | Out-Null\n";
        return Программы.PowerShell(скрипт, out ошибка);
    }

    #endregion
}

// Запуск внешних программ: PowerShell, ffmpeg, 7-Zip
internal static class Программы
{
    static readonly Dictionary<string, string> найденные = new Dictionary<string, string>();

    /// <summary>
    /// Ищет установленную программу по имени в обычных местах. Пусто, если её нет
    /// </summary>
    public static string Найти(string имя)
    {
        if (найденные.TryGetValue(имя, out string путь)) return путь;
        string файл = Application.platform == RuntimePlatform.WindowsEditor ? имя + ".exe" : имя;
        IEnumerable<string> папки = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Concat(new[] { $@"C:\{имя}\bin", $@"C:\Program Files\{имя}\bin", "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin" });
        путь = папки.Where(п => !string.IsNullOrWhiteSpace(п)).Select(п => Path.Combine(п.Trim(), файл)).FirstOrDefault(File.Exists);
        найденные[имя] = путь;
        return путь;
    }

    /// <summary>
    /// Как поставить ffmpeg на этой системе
    /// </summary>
    public static string КакПоставитьFfmpeg => Application.platform switch
    {
        RuntimePlatform.OSXEditor => "Установите её командой «brew install ffmpeg» в Терминале (нужен Homebrew с сайта brew.sh) и перезапустите Unity",
        RuntimePlatform.LinuxEditor => "Установите её командой «sudo apt install ffmpeg» (или менеджером пакетов вашей системы) и перезапустите Unity",
        _ => "Установите её командой «winget install ffmpeg» в командной строке и перезапустите Unity"
    };

    // Предлагает открыть страницу, где ставится недостающее. В пакетном режиме просто пишет в консоль
    public static void ПредложитьУстановку(string текст, params string[] ссылки)
    {
        Debug.LogWarning(текст);
        if (Application.isBatchMode) return;
        if (EditorUtility.DisplayDialog("Runity", текст, "Открыть страницу установки", "Не сейчас"))
            foreach (string ссылка in ссылки) Application.OpenURL(ссылка);
    }

    public static bool Запустить(string программа, string аргументы, out string ошибка)
    {
        ошибка = "";
        try
        {
            System.Diagnostics.ProcessStartInfo запуск = new System.Diagnostics.ProcessStartInfo(программа, аргументы)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };
            using (System.Diagnostics.Process процесс = System.Diagnostics.Process.Start(запуск))
            {
                System.Threading.Tasks.Task<string> вывод = процесс.StandardOutput.ReadToEndAsync();
                ошибка = процесс.StandardError.ReadToEnd();
                вывод.Wait();
                процесс.WaitForExit();
                return процесс.ExitCode == 0;
            }
        }
        catch (Exception исключение)
        {
            ошибка = исключение.Message;
            return false;
        }
    }

    // Скрипт передаётся закодированным, поэтому русские буквы и кавычки в путях не мешают
    public static bool PowerShell(string скрипт, out string ошибка)
    {
        string закодировано = Convert.ToBase64String(Encoding.Unicode.GetBytes("[Console]::OutputEncoding = [Text.Encoding]::UTF8\n" + скрипт));
        return Запустить("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + закодировано, out ошибка);
    }
}
// Дописывает в видео mp4 (H.264) метки цвета BT.709 - то, что обычно пишут видеоредакторы.
// Без них Windows при показе видео в Unity предупреждает: «Color primaries 0 is unknown»
internal static class МеткиЦветаMp4
{
    public static bool Дописать(string путь)
    {
        try
        {
            byte[] файл = File.ReadAllBytes(путь);
            List<int> цепочка = Найти(файл, 0, файл.Length, new[] { "moov", "trak", "mdia", "minf", "stbl", "stsd", "avc1", "avcC" });
            if (цепочка == null) return false;

            int место = цепочка[цепочка.Count - 1] + 8 + 5;
            if ((файл[место] & 0x1f) != 1) return false;
            int длина = файл[место + 1] << 8 | файл[место + 2];
            byte[] старый = new byte[длина];
            Buffer.BlockCopy(файл, место + 3, старый, 0, длина);
            byte[] новый = ДобавитьЦвет(старый);
            if (новый == null) return false;

            // moov должен лежать после данных: иначе пришлось бы сдвигать ссылки на кадры
            int moov = цепочка[0];
            foreach ((int начало, int размер, string вид) in Блоки(файл, 0, файл.Length))
                if (вид == "mdat" && начало > moov) return false;

            int разница = новый.Length - старый.Length;
            byte[] итог = new byte[файл.Length + разница];
            Buffer.BlockCopy(файл, 0, итог, 0, место + 1);
            итог[место + 1] = (byte)(новый.Length >> 8);
            итог[место + 2] = (byte)новый.Length;
            Buffer.BlockCopy(новый, 0, итог, место + 3, новый.Length);
            Buffer.BlockCopy(файл, место + 3 + длина, итог, место + 3 + новый.Length, файл.Length - место - 3 - длина);
            foreach (int блок in цепочка)
            {
                int размер = (итог[блок] << 24 | итог[блок + 1] << 16 | итог[блок + 2] << 8 | итог[блок + 3]) + разница;
                итог[блок] = (byte)(размер >> 24); итог[блок + 1] = (byte)(размер >> 16); итог[блок + 2] = (byte)(размер >> 8); итог[блок + 3] = (byte)размер;
            }
            File.WriteAllBytes(путь, итог);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static IEnumerable<(int начало, int размер, string вид)> Блоки(byte[] д, int откуда, int докуда)
    {
        for (int i = откуда; i + 8 <= докуда;)
        {
            int размер = д[i] << 24 | д[i + 1] << 16 | д[i + 2] << 8 | д[i + 3];
            if (размер < 8) yield break;
            yield return (i, размер, Encoding.ASCII.GetString(д, i + 4, 4));
            i += размер;
        }
    }

    static List<int> Найти(byte[] д, int откуда, int докуда, string[] путь)
    {
        foreach ((int начало, int размер, string вид) in Блоки(д, откуда, докуда))
        {
            if (вид != путь[0]) continue;
            if (путь.Length == 1) return new List<int> { начало };
            int внутри = вид == "stsd" ? начало + 16 : вид == "avc1" ? начало + 8 + 78 : начало + 8;
            List<int> дальше = Найти(д, внутри, начало + размер, путь.Skip(1).ToArray());
            if (дальше != null) { дальше.Insert(0, начало); return дальше; }
        }
        return null;
    }

    // Разбирает заголовок потока (SPS) и вставляет в его описание (VUI) метки цвета BT.709
    static byte[] ДобавитьЦвет(byte[] sps)
    {
        List<bool> биты = new List<bool>();
        int нули = 0;
        foreach (byte б in sps)
        {
            if (нули >= 2 && б == 3) { нули = 0; continue; }
            for (int k = 7; k >= 0; k--) биты.Add((б >> k & 1) == 1);
            нули = б == 0 ? нули + 1 : 0;
        }

        int п = 0;
        int Ч(int n) { int v = 0; for (int k = 0; k < n; k++) v = v << 1 | (биты[п++] ? 1 : 0); return v; }
        int ЧЭГ() { int z = 0; while (!биты[п]) { z++; п++; } п++; return (1 << z) - 1 + Ч(z); }

        Ч(8);
        int профиль = Ч(8); Ч(8); Ч(8); ЧЭГ();
        if (профиль != 66 && профиль != 77 && профиль != 88) return null;
        ЧЭГ();
        int poc = ЧЭГ();
        if (poc == 0) ЧЭГ();
        else if (poc == 1) { Ч(1); ЧЭГ(); ЧЭГ(); int n = ЧЭГ(); for (int k = 0; k < n; k++) ЧЭГ(); }
        ЧЭГ(); Ч(1); ЧЭГ(); ЧЭГ();
        if (Ч(1) == 0) Ч(1);
        Ч(1);
        if (Ч(1) == 1) { ЧЭГ(); ЧЭГ(); ЧЭГ(); ЧЭГ(); }
        if (Ч(1) == 0) return null;
        if (Ч(1) == 1 && Ч(8) == 255) { Ч(16); Ч(16); }
        if (Ч(1) == 1) Ч(1);
        if (биты[п]) return null;

        int стоп = биты.LastIndexOf(true);
        List<bool> новые = биты.GetRange(0, п);
        void Записать(int значение, int n) { for (int k = n - 1; k >= 0; k--) новые.Add((значение >> k & 1) == 1); }
        Записать(1, 1); Записать(5, 3); Записать(0, 1); Записать(1, 1);
        Записать(1, 8); Записать(1, 8); Записать(1, 8);
        новые.AddRange(биты.GetRange(п + 1, стоп - п - 1));
        новые.Add(true);
        while (новые.Count % 8 != 0) новые.Add(false);

        List<byte> итог = new List<byte>();
        нули = 0;
        for (int i = 0; i < новые.Count; i += 8)
        {
            int б = 0;
            for (int k = 0; k < 8; k++) б = б << 1 | (новые[i + k] ? 1 : 0);
            if (нули >= 2 && б <= 3) { итог.Add(3); нули = 0; }
            итог.Add((byte)б);
            нули = б == 0 ? нули + 1 : 0;
        }
        return итог.ToArray();
    }
}
#endif
