#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Действия в меню редактора: выделение объектов, распаковка архивов и моделей, уборка пустых коллайдеров
/// </summary>
public static class ДействияRunity
{
    #region Выделение

    // В меню шестерёнки (и по правому щелчку на заголовке) любого компонента
    [MenuItem("CONTEXT/Component/Выбрать все объекты с этим компонентом", false, 1000)]
    static void ВыбратьСЭтимКомпонентом(MenuCommand команда)
    {
        Selection.objects = ОбъектыСКомпонентом(команда.context.GetType()).Cast<Object>().ToArray();
    }

    /// <summary>
    /// Все объекты открытых сцен, на которых есть компонент этого типа
    /// </summary>
    public static List<GameObject> ОбъектыСКомпонентом(System.Type тип)
    {
        List<GameObject> найденные = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene сцена = SceneManager.GetSceneAt(i);
            if (!сцена.isLoaded) continue;
            foreach (GameObject корень in сцена.GetRootGameObjects())
                foreach (Component компонент in корень.GetComponentsInChildren(тип, true))
                    if (компонент.GetType() == тип && !найденные.Contains(компонент.gameObject)) найденные.Add(компонент.gameObject);
        }
        return найденные;
    }

    // В правом щелчке по объектам в окне Hierarchy
    [MenuItem("GameObject/Выбрать всё внутри", false, -1000)]
    static void ВыбратьВсёВнутри(MenuCommand команда)
    {
        // Пункт вызывается для каждого выделенного объекта - делаем работу один раз
        if (команда.context != null && команда.context != Selection.activeGameObject) return;
        Selection.objects = ВсёВнутри(Selection.gameObjects).Cast<Object>().ToArray();
    }

    [MenuItem("GameObject/Выбрать всё внутри", true)]
    static bool МожноВыбратьВсёВнутри() => Selection.gameObjects.Length > 0;

    /// <summary>
    /// Выбранные объекты вместе со всеми объектами внутри них
    /// </summary>
    public static List<GameObject> ВсёВнутри(IEnumerable<GameObject> объекты)
    {
        List<GameObject> все = new List<GameObject>();
        foreach (GameObject объект in объекты)
            foreach (Transform часть in объект.GetComponentsInChildren<Transform>(true))
                if (!все.Contains(часть.gameObject)) все.Add(часть.gameObject);
        return все;
    }

    #endregion

    #region Распаковка

    [MenuItem("Assets/Runity/Распаковать", false, 0)]
    static void Распаковать()
    {
        foreach (Object выбранный in Selection.objects)
        {
            string путь = AssetDatabase.GetAssetPath(выбранный);
            if (РаспаковкаАрхивов.ЭтоАрхив(путь)) РаспаковкаАрхивов.Распаковать(путь);
            else if (РаспаковкаМодели.МожноРаспаковать(путь)) РаспаковкаМодели.Распаковать(путь);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Runity/Распаковать", true)]
    static bool МожноРаспаковать() => Selection.objects.Any(о =>
    {
        string путь = AssetDatabase.GetAssetPath(о);
        return !string.IsNullOrEmpty(путь) && (РаспаковкаАрхивов.ЭтоАрхив(путь) || РаспаковкаМодели.МожноРаспаковать(путь));
    });

    #endregion

    #region Коллайдеры модели

    [MenuItem("CONTEXT/КоллайдерМодели/Удалить пустые Коллайдеры Модели")]
    static void УдалитьПустыеИзМеню() => УдалитьПустыеКоллайдерыМодели();

    /// <summary>
    /// Удаляет со всех открытых сцен Коллайдеры Модели, у которых не выбрана модель. Даёт, сколько удалено
    /// </summary>
    public static int УдалитьПустыеКоллайдерыМодели()
    {
        int удалено = 0;
        foreach (GameObject объект in ОбъектыСКомпонентом(typeof(КоллайдерМодели)))
        {
            MeshCollider коллайдер = объект.GetComponent<MeshCollider>();
            if (коллайдер == null || коллайдер.sharedMesh != null) continue;
            Undo.DestroyObjectImmediate(объект.GetComponent<КоллайдерМодели>());
            Undo.DestroyObjectImmediate(коллайдер);
            удалено++;
        }
        Debug.Log($"Удалено пустых Коллайдеров Модели: {удалено}");
        return удалено;
    }

    #endregion
}
#endif
