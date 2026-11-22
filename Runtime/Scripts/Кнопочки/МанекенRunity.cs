#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Манекен Runity: человечек со скелетом и готовыми анимациями (стоит, идёт, бежит, прыгает, машет).
/// Анимации включаются сами: персонаж стоит - «Стоит», двигается - «Идёт», с ускорением - «Бежит», в воздухе - «Прыгает»
/// </summary>
public static class МанекенRunity
{
    const string имяФайла = "Манекен.glb";

    /// <summary>
    /// Модель манекена из пакета Runity
    /// </summary>
    public static GameObject модель
    {
        get
        {
            string путь = AssetDatabase.FindAssets("Манекен")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(п => п.EndsWith("/Манекен/" + имяФайла));
            return путь != null ? AssetDatabase.LoadAssetAtPath<GameObject>(путь) : null;
        }
    }

    /// <summary>
    /// Анимация манекена по названию: Стоит, Идёт, Бежит, Прыгает, Машет
    /// </summary>
    public static AnimationClip Анимация(string название)
    {
        GameObject модельМанекена = модель;
        if (модельМанекена == null) return null;
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(модельМанекена)).OfType<AnimationClip>().FirstOrDefault(к => к.name == название);
    }

    /// <summary>
    /// Ставит манекена на сцену. Анимации переключаются сами по движению
    /// </summary>
    public static GameObject Создать(Transform родитель = null, string имя = "Манекен")
    {
        GameObject образец = модель;
        if (образец == null)
        {
            Debug.LogWarning("Runity: не найдена модель манекена «" + имяФайла + "». Переустановите Runity через менеджер пакетов");
            return null;
        }

        GameObject манекен = (GameObject)PrefabUtility.InstantiatePrefab(образец);
        манекен.name = имя;
        if (родитель != null) манекен.transform.SetParent(родитель, false);

        // Анимациями управляет Аниматор Игрока, схема из файла не нужна
        if (манекен.TryGetComponent(out Animator аниматор)) аниматор.runtimeAnimatorController = null;

        АниматорИгрока анимации = манекен.GetComponent<АниматорИгрока>();
        if (анимации == null) анимации = манекен.AddComponent<АниматорИгрока>();
        Назначить(анимации.стоит, "Стоит");
        Назначить(анимации.идёт, "Идёт");
        Назначить(анимации.бежит, "Бежит");
        Назначить(анимации.прыгает, "Прыгает");
        Назначить(анимации.падает, "Прыгает");
        EditorUtility.SetDirty(анимации);
        return манекен;
    }

    static void Назначить(АнимацияСостояния состояние, string клип)
    {
        AnimationClip анимация = Анимация(клип);
        if (анимация == null) return;
        состояние.вид = ВидАнимации.Клип;
        состояние.клип = анимация;
    }

    [MenuItem("GameObject/Runity/3D/Манекен", false, -99)]
    static void СоздатьИзМеню(MenuCommand команда)
    {
        GameObject манекен = Создать((команда.context as GameObject)?.transform);
        if (манекен == null) return;
        Undo.RegisterCreatedObjectUndo(манекен, "Манекен");
        Selection.activeObject = манекен;
    }
}
#endif
