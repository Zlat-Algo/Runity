using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Сам ставит пакеты Unity, без которых Runity не работает: систему ввода, навигацию, сетевую игру и другие.
// Если Runity установлен через Package Manager, Unity ставит их сам. А если папку Runity просто положили в Assets,
// это делает установщик. Он лежит в отдельной сборке без зависимостей, поэтому работает, даже когда остальная Runity ещё не компилируется
[InitializeOnLoad]
static class УстановщикЗависимостей
{
    const string ключВключено = "Runity.УстановщикЗависимостей";
    const string пунктМеню = "Runity/Сами ставить нужные пакеты Unity";

    static ListRequest список;
    static AddAndRemoveRequest установка;

    static УстановщикЗависимостей()
    {
        // Проверяем один раз за запуск редактора
        if (SessionState.GetBool(ключВключено, false)) return;
        SessionState.SetBool(ключВключено, true);
        EditorApplication.delayCall += Проверить;
    }

    static bool включено
    {
        get => EditorPrefs.GetBool(ключВключено, true);
        set => EditorPrefs.SetBool(ключВключено, value);
    }

    [MenuItem(пунктМеню, false, 52)]
    static void Переключить()
    {
        включено = !включено;
        if (включено) Проверить();
    }

    [MenuItem(пунктМеню, true)]
    static bool ПоказатьГалочку()
    {
        Menu.SetChecked(пунктМеню, включено);
        return true;
    }

    static void Проверить()
    {
        if (!включено || Application.isBatchMode || список != null || установка != null) return;
        список = Client.List(true, false);
        EditorApplication.update += ДождатьсяСписка;
    }

    static void ДождатьсяСписка()
    {
        if (!список.IsCompleted) return;
        EditorApplication.update -= ДождатьсяСписка;
        ListRequest готовый = список;
        список = null;
        if (готовый.Status != StatusCode.Success) return;

        HashSet<string> есть = new HashSet<string>(готовый.Result.Select(пакет => пакет.name));
        string[] нужно = Зависимости().Where(пара => !есть.Contains(пара.Key)).Select(пара => $"{пара.Key}@{пара.Value}").ToArray();
        if (нужно.Length == 0) return;

        Debug.Log("Runity ставит пакеты Unity, без которых он не работает: " + string.Join(", ", нужно) + ". Отключить это можно в меню " + пунктМеню);
        установка = Client.AddAndRemove(нужно, null);
        EditorApplication.update += ДождатьсяУстановки;
    }

    static void ДождатьсяУстановки()
    {
        if (!установка.IsCompleted) return;
        EditorApplication.update -= ДождатьсяУстановки;
        if (установка.Status != StatusCode.Success)
            Debug.LogWarning("Runity не смог поставить нужные пакеты: " + установка.Error?.message + ". Поставьте их через Window - Package Manager");
        установка = null;
    }

    // Зависимости берутся из package.json самой Runity
    static Dictionary<string, string> Зависимости()
    {
        Dictionary<string, string> зависимости = new Dictionary<string, string>();
        string файл = НайтиОписаниеПакета(ЭтотФайл());
        if (файл == null) return зависимости;

        string текст = File.ReadAllText(файл);
        Match блок = Regex.Match(текст, "\"dependencies\"\\s*:\\s*\\{(?<список>[^}]*)\\}");
        foreach (Match пакет in Regex.Matches(блок.Groups["список"].Value, "\"(?<имя>[^\"]+)\"\\s*:\\s*\"(?<версия>[^\"]+)\""))
            зависимости[пакет.Groups["имя"].Value] = пакет.Groups["версия"].Value;
        return зависимости;
    }

    static string ЭтотФайл([CallerFilePath] string путь = "") => путь;

    static string НайтиОписаниеПакета(string откуда)
    {
        for (DirectoryInfo папка = File.Exists(откуда) ? new FileInfo(откуда).Directory : null; папка != null; папка = папка.Parent)
        {
            string файл = Path.Combine(папка.FullName, "package.json");
            if (File.Exists(файл) && File.ReadAllText(файл).Contains("\"com.zlatalgo.runity\"")) return файл;
        }
        return null;
    }
}
