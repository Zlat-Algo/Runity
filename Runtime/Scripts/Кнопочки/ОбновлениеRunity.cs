#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Подгоняет проект под новую версию Runity, чтобы ничего не приходилось править руками.
/// Переименованные компоненты Unity исправляет в скриптах сам (атрибут MovedFrom), переименованные поля не теряются (FormerlySerializedAs),
/// а всё остальное делают шаги обновления ниже: они выполняются один раз, когда проект открывается с новой версией Runity
/// </summary>
[InitializeOnLoad]
public static class ОбновлениеRunity
{
    const string файлВерсии = "ProjectSettings/RunityВерсия.txt";

    // Шаги обновления: с какой версии шаг нужен, что он делает и сам шаг. Новые шаги добавляются в конец
    static readonly (string версия, string описание, Action шаг)[] шаги =
    {
        ("1.3.0", "модели glTF и .blend переимпортированы: у них новые настройки (матовая поверхность, скелет, анимации)", ПереимпортироватьМодели),
        ("1.3.0", "у сетевых образцов восстановлены Сетевой мост и синхронизация положения (раньше они терялись при сохранении образца)", ПочинитьСетевыеОбразцы),
    };

    static ОбновлениеRunity()
    {
        EditorApplication.delayCall += Проверить;
    }

    /// <summary>
    /// Версия Runity, которая сейчас стоит в проекте
    /// </summary>
    public static string текущаяВерсия
    {
        get
        {
            string файл = НайтиОписаниеПакета(ЭтотФайл());
            if (файл == null) return "0.0.0";
            Match версия = Regex.Match(File.ReadAllText(файл), "\"version\"\\s*:\\s*\"(?<v>[^\"]+)\"");
            return версия.Success ? версия.Groups["v"].Value : "0.0.0";
        }
    }

    static void Проверить()
    {
        string новая = текущаяВерсия;
        // Проект открыт с Runity впервые - обновлять нечего
        if (!File.Exists(файлВерсии))
        {
            File.WriteAllText(файлВерсии, новая);
            return;
        }

        string старая = File.ReadAllText(файлВерсии).Trim();
        if (Сравнить(старая, новая) >= 0) return;

        List<string> сделано = new List<string>();
        foreach ((string версия, string описание, Action шаг) in шаги.Where(ш => Сравнить(ш.версия, старая) > 0 && Сравнить(ш.версия, новая) <= 0))
        {
            try
            {
                шаг();
                сделано.Add(описание);
            }
            catch (Exception ошибка)
            {
                Debug.LogWarning($"Runity: шаг обновления «{описание}» не выполнен: {ошибка.Message}");
            }
        }

        File.WriteAllText(файлВерсии, новая);
        Debug.Log($"Runity обновлён с версии {старая} до {новая}." + (сделано.Count > 0 ? " Проект подогнан сам:\n- " + string.Join("\n- ", сделано) : ""));
        ПроверитьПотерянныеСкрипты();
    }

    /// <summary>
    /// Сравнивает версии вида 1.2.0: меньше нуля - первая старше, больше - новее
    /// </summary>
    public static int Сравнить(string первая, string вторая)
    {
        int[] Числа(string версия) => версия.Split('.').Select(часть => int.TryParse(new string(часть.TakeWhile(char.IsDigit).ToArray()), out int число) ? число : 0).ToArray();
        int[] a = Числа(первая), b = Числа(вторая);
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            int x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }

    static void ПереимпортироватьМодели()
    {
        foreach (string путь in AssetDatabase.GetAllAssetPaths().Where(п => п.StartsWith("Assets/") && (п.EndsWith(".glb") || п.EndsWith(".gltf") || п.EndsWith(".blend"))))
            AssetDatabase.ImportAsset(путь, ImportAssetOptions.ForceUpdate);
    }

    // Сетевой мост и синхронизация жили в чужом файле, и Unity не сохранял их в образцах: на их месте остались потерянные скрипты
    static void ПочинитьСетевыеОбразцы()
    {
        Type сетевойОбъект = Type.GetType("СетевойОбъект, com.zlatalgo.runity.network");
        if (сетевойОбъект == null) return;
        Type мост = Type.GetType("СетевойМост, com.zlatalgo.runity.network");
        Type синхронизация = Type.GetType("ВладельческаяСинхронизация, com.zlatalgo.runity.network");

        foreach (string путь in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            GameObject образец = AssetDatabase.LoadAssetAtPath<GameObject>(путь);
            if (образец == null || образец.GetComponentInChildren(сетевойОбъект, true) == null) continue;

            GameObject содержимое = PrefabUtility.LoadPrefabContents(путь);
            try
            {
                foreach (Component объект in содержимое.GetComponentsInChildren(сетевойОбъект, true))
                {
                    GameObject носитель = объект.gameObject;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(носитель);
                    if (мост != null && носитель.GetComponent(мост) == null) носитель.AddComponent(мост);
                    bool синхронизировать = new SerializedObject(объект).FindProperty("_синхронизироватьПоложение")?.FindPropertyRelative("значение")?.boolValue ?? true;
                    if (синхронизация != null && синхронизировать && носитель.GetComponent(синхронизация) == null) носитель.AddComponent(синхронизация);
                }
                PrefabUtility.SaveAsPrefabAsset(содержимое, путь);
            }
            finally { PrefabUtility.UnloadPrefabContents(содержимое); }
        }
    }

    // Если после обновления на открытых сценах есть объекты с потерянными скриптами - сообщаем, где они
    static void ПроверитьПотерянныеСкрипты()
    {
        int потеряно = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            foreach (GameObject корень in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects())
                foreach (Transform часть in корень.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(часть.gameObject) > 0)
                    {
                        потеряно++;
                        Debug.LogWarning($"У объекта «{часть.name}» потерян скрипт - возможно, компонент переименован или удалён", часть.gameObject);
                    }
        }
        if (потеряно > 0) Debug.LogWarning($"Runity: объектов с потерянными скриптами: {потеряно}. Щёлкните по сообщениям выше, чтобы найти их");
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
#endif
