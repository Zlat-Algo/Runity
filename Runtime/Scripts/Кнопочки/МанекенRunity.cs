#if UNITY_EDITOR
using System.Collections.Generic;
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
        // Свои анимации проекта (например, из Asset Store) рядом с манекеном не лежат - их можно добавить кнопкой в Аниматоре игрока
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

    // Какие слова в названии анимации подходят какому состоянию. Русские и английские, как называют в Mixamo и Asset Store
    static readonly (string состояние, string[] слова)[] словаСостояний =
    {
        ("стоит", new[] { "idle", "stand", "breath", "стоит", "покой", "стойка" }),
        ("идёт", new[] { "walk", "идёт", "ходьба", "шаг" }),
        ("бежит", new[] { "run", "sprint", "jog", "бежит", "бег" }),
        ("прыгает", new[] { "jump", "прыг" }),
        ("падает", new[] { "fall", "inair", "air", "падает", "падение", "полёт" }),
        ("приземляется", new[] { "land", "приземл" }),
        ("получаетУрон", new[] { "hit", "damage", "hurt", "урон", "удар получ" }),
        ("атакует", new[] { "attack", "slash", "punch", "kick", "атак", "удар" }),
        ("умирает", new[] { "death", "die", "dead", "умира", "смерть" }),
    };

    // Слова, из-за которых анимация хуже подходит для простого состояния: ходьба боком, с оружием и т.п.
    static readonly string[] особые = { "back", "left", "right", "strafe", "turn", "crouch", "combat", "injured", "sneak", "swim", "ladder", "sit", "carry", "bow", "rifle", "gun", "sword", "shield", "torch", "spell", "aim", "lean", "begin", "start", "end", "stop", "loop", "назад", "вбок" };

    static int Оценка(string имя, string[] слова, string состояние)
    {
        string н = имя.ToLowerInvariant();
        if (!слова.Any(н.Contains)) return int.MinValue;
        // «Прыжок - приземление» - это приземление, а не прыжок; «Бег» не должен находить «Breath»
        if (состояние == "прыгает" && (н.Contains("land") || н.Contains("fall"))) return int.MinValue;
        if (состояние == "стоит" && (н.Contains("turn") || н.Contains("walk") || н.Contains("run"))) return int.MinValue;
        int оценка = 100 - н.Length;
        оценка -= особые.Count(н.Contains) * 30;
        if (н.Contains("01") || н.Contains("forward") || н.Contains("вперёд")) оценка += 20;
        if ((состояние == "прыгает" && (н.Contains("begin") || н.Contains("start"))) || (состояние == "падает" && н.Contains("loop"))) оценка += 40;
        return оценка;
    }

    /// <summary>
    /// Раскладывает анимации из папки по состояниям Аниматора игрока: Idle - «Стоит», Walk - «Идёт», Run - «Бежит», Jump - «Прыгает» и так далее.
    /// Подходят и русские названия. Для человека (Humanoid) берутся только человеческие анимации, их можно брать от других моделей. Даёт, сколько состояний заполнено
    /// </summary>
    public static int НайтиАнимации(АниматорИгрока аниматор, string папка, bool заменять = true)
    {
        Animator основа = аниматор.GetComponentInChildren<Animator>();
        bool человек = основа != null && основа.avatar != null && основа.avatar.isHuman;
        List<AnimationClip> клипы = AssetDatabase.FindAssets("t:AnimationClip", new[] { папка })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct()
            .SelectMany(путь => AssetDatabase.LoadAllAssetsAtPath(путь).OfType<AnimationClip>())
            .Where(к => !к.name.StartsWith("__preview__") && (!человек || к.humanMotion))
            .Distinct().ToList();

        SerializedObject данные = new SerializedObject(аниматор);
        int заполнено = 0;
        foreach (var (состояние, слова) in словаСостояний)
        {
            AnimationClip лучший = клипы.Select(к => (к, оценка: Оценка(к.name, слова, состояние))).Where(п => п.оценка > int.MinValue).OrderByDescending(п => п.оценка).Select(п => п.к).FirstOrDefault();
            if (лучший == null) continue;
            SerializedProperty поле = данные.FindProperty("_" + состояние);
            if (поле == null) continue;
            if (!заменять && ЕстьАнимация(поле)) continue;
            поле.FindPropertyRelative("вид").enumValueIndex = (int)ВидАнимации.Клип;
            поле.FindPropertyRelative("клип").objectReferenceValue = лучший;
            заполнено++;
        }
        данные.ApplyModifiedProperties();
        return заполнено;
    }

    static bool ЕстьАнимация(SerializedProperty поле) =>
        поле.FindPropertyRelative("вид").enumValueIndex == (int)ВидАнимации.Клип ? поле.FindPropertyRelative("клип").objectReferenceValue != null : поле.FindPropertyRelative("кадры").objectReferenceValue != null;

    /// <summary>
    /// Анимации манекена Runity подходят любому человеку (Humanoid): ставит их в пустые состояния
    /// </summary>
    public static int АнимацииМанекена(АниматорИгрока аниматор, bool заменять = false)
    {
        GameObject модельМанекена = модель;
        return модельМанекена == null ? 0 : НайтиАнимации(аниматор, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(модельМанекена)).Replace('\\', '/'), заменять);
    }

    /// <summary>
    /// Ставит любую модель на сцену как персонажа: анимации ищутся рядом с моделью, а если их не хватает и модель - человек, добавляются анимации манекена Runity
    /// </summary>
    public static GameObject ПоставитьПерсонажа(GameObject образец, Transform родитель = null)
    {
        GameObject персонаж = PrefabUtility.IsPartOfPrefabAsset(образец) || AssetDatabase.Contains(образец) ? (GameObject)PrefabUtility.InstantiatePrefab(образец) : UnityEngine.Object.Instantiate(образец);
        if (родитель != null) персонаж.transform.SetParent(родитель, false);
        Animator основа = персонаж.GetComponentInChildren<Animator>();
        if (основа == null) основа = персонаж.AddComponent<Animator>();
        основа.runtimeAnimatorController = null;
        if (основа.avatar == null)
        {
            // Аватар модели лежит в её файле (fbx, glTF)
            string путь = AssetDatabase.GetAssetPath(образец);
            основа.avatar = AssetDatabase.LoadAllAssetsAtPath(путь).OfType<Avatar>().FirstOrDefault();
        }

        АниматорИгрока анимации = основа.GetComponent<АниматорИгрока>();
        if (анимации == null) анимации = основа.gameObject.AddComponent<АниматорИгрока>();
        string папка = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(образец))?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(папка)) НайтиАнимации(анимации, папка);
        if (основа.avatar != null && основа.avatar.isHuman) АнимацииМанекена(анимации);
        EditorUtility.SetDirty(анимации);
        return персонаж;
    }

    [MenuItem("Assets/Runity/Поставить на сцену как персонажа", false, 103)]
    static void ПоставитьИзМеню()
    {
        GameObject персонаж = ПоставитьПерсонажа((GameObject)Selection.activeObject);
        Undo.RegisterCreatedObjectUndo(персонаж, "Персонаж");
        Selection.activeObject = персонаж;
    }

    [MenuItem("Assets/Runity/Поставить на сцену как персонажа", true)]
    static bool МожноПоставить() => Selection.activeObject is GameObject объект && AssetDatabase.Contains(объект) && объект.GetComponentInChildren<Renderer>(true) != null;

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
