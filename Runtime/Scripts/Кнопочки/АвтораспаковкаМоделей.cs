#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Новая модель, которую закинули в проект (fbx, glTF, blend и другие), сама раскладывается на обычные ассеты: префаб, материалы Runity,
// сетки, анимации и схему анимаций. Модели из пакетов и файлы только с анимациями (Персонаж@Бег.fbx) не трогаются. Выключается в меню Runity
public class АвтораспаковкаМоделей : AssetPostprocessor
{
    const string ключВключено = "Runity.РаспаковкаМоделей";
    const string пунктМеню = "Runity/Распаковывать модели в проекте";

    static readonly string[] модели = { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".glb", ".gltf", ".blend", ".stl", ".ply" };

    /// <summary>
    /// Распаковывать ли новые модели сами
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

    // Пока идёт импорт пакета (.unitypackage, Asset Store), его модели не распаковываем: у пакета свои префабы
    static bool идётИмпортПакета;

    [InitializeOnLoadMethod]
    static void Подписаться()
    {
        AssetDatabase.importPackageStarted += _ => идётИмпортПакета = true;
        AssetDatabase.importPackageCompleted += _ => идётИмпортПакета = false;
        AssetDatabase.importPackageCancelled += _ => идётИмпортПакета = false;
        AssetDatabase.importPackageFailed += (_, __) => идётИмпортПакета = false;
    }

    // Модели Unity импортирует и в отдельных процессах, поэтому «новизну» узнаём по файлу .meta: у новой модели он появился в этой сессии Unity.
    // У проекта, скачанного из git, .meta старше запуска Unity - его модели не трогаются
    static readonly DateTime началоСессии = DateTime.UtcNow - TimeSpan.FromSeconds(EditorApplication.timeSinceStartup);
    static readonly HashSet<string> уже = new HashSet<string>();

    /// <summary>
    /// Пути, которые не нужно распаковывать (например, модели из пакета Unity, разложенного Runity)
    /// </summary>
    internal static readonly HashSet<string> пропустить = new HashSet<string>();

    static bool Новая(string путь)
    {
        string мета = путь + ".meta";
        if (!File.Exists(мета)) return false;
        DateTime создан = File.GetCreationTimeUtc(мета);
        return создан > началоСессии && создан > DateTime.UtcNow.AddMinutes(-10);
    }

    // Если рядом уже есть распакованная папка с префабом, второй раз не распаковываем
    static bool УжеРаспакована(string путь, GameObject модель)
    {
        string папка = Path.GetDirectoryName(путь).Replace('\\', '/');
        foreach (string имя in new[] { модель.name, Path.GetFileNameWithoutExtension(путь), ИмпортGLTF.ЧистоеИмя(модель.name) })
            if (AssetDatabase.IsValidFolder(папка + "/" + имя) && AssetDatabase.FindAssets("t:Prefab", new[] { папка + "/" + имя }).Length > 0) return true;
        return false;
    }

    internal static bool МожноРаспаковать(string путь) =>
        путь.StartsWith("Assets/") && модели.Contains(Path.GetExtension(путь).ToLowerInvariant()) && !Path.GetFileName(путь).Contains("@");

    static void OnPostprocessAllAssets(string[] импортированные, string[] удалённые, string[] перенесённые, string[] откуда)
    {
        if (!включено || идётИмпортПакета) return;
        List<string> готовые = импортированные.Where(п => МожноРаспаковать(п) && !уже.Contains(п) && !пропустить.Contains(п) && Новая(п)).ToList();
        if (готовые.Count == 0) return;

        // Внутри импорта создавать ассеты нельзя, поэтому распаковка ждёт его окончания
        EditorApplication.delayCall += () =>
        {
            foreach (string путь in готовые)
            {
                GameObject модель = AssetDatabase.LoadAssetAtPath<GameObject>(путь);
                // Без видимых частей это файл только с анимациями - распаковывать нечего
                if (модель == null || модель.GetComponentInChildren<Renderer>(true) == null || УжеРаспакована(путь, модель)) continue;
                уже.Add(путь);
                try { РаспаковкаМодели.Распаковать(путь); }
                catch (Exception ошибка) { Debug.LogWarning($"Runity: модель {путь} не распакована: {ошибка.Message}"); }
            }
        };
    }
}
#endif
