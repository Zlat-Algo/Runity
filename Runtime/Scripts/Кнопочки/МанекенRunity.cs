#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Готовые персонажи Runity со скелетом человека (Humanoid) и анимациями
/// </summary>
public enum ВидПерсонажа
{
    /// <summary>Манекен-мужчина</summary>
    Манекен,
    /// <summary>Манекен-девушка</summary>
    МанекенДевушка,
    /// <summary>Гном</summary>
    Гном,
    /// <summary>Великан</summary>
    Великан,
    /// <summary>Скелет</summary>
    Скелет,
    /// <summary>Зомби</summary>
    Зомби,
    /// <summary>Простой человечек Runity</summary>
    Простой,
}

/// <summary>
/// Манекен и другие персонажи Runity: модели и анимации Кевина Иглесиаса (Kevin Iglesias, keviniglesias.com).
/// Анимации включаются сами: персонаж стоит - «Стоит», двигается - «Идёт», с ускорением - «Бежит», в воздухе - «Прыгает»
/// </summary>
public static class МанекенRunity
{
    const string имяФайла = "Манекен.glb";

    // Папка персонажа в Runtime/Персонажи и файл модели
    static readonly Dictionary<ВидПерсонажа, (string папка, string модель)> персонажи = new Dictionary<ВидПерсонажа, (string, string)>
    {
        { ВидПерсонажа.Манекен, ("Человек", "HumanM_Model.fbx") },
        { ВидПерсонажа.МанекенДевушка, ("Человек", "HumanF_Model.fbx") },
        { ВидПерсонажа.Гном, ("Гном", "DwarfM_Model.fbx") },
        { ВидПерсонажа.Великан, ("Великан", "GiantDummy.fbx") },
        { ВидПерсонажа.Скелет, ("Скелет", "Skeleton.fbx") },
        { ВидПерсонажа.Зомби, ("Зомби", "ZombieModel.fbx") },
    };

    /// <summary>
    /// Папка с персонажами Runity (в пакете или в Assets)
    /// </summary>
    public static string папкаПерсонажей
    {
        get
        {
            string путь = AssetDatabase.FindAssets("HumanM_Model")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(п => п.EndsWith("/Персонажи/Человек/HumanM_Model.fbx"));
            return путь == null ? null : путь.Substring(0, путь.Length - "/Человек/HumanM_Model.fbx".Length);
        }
    }

    /// <summary>
    /// Модель персонажа Runity
    /// </summary>
    public static GameObject Модель(ВидПерсонажа вид)
    {
        if (вид == ВидПерсонажа.Простой)
        {
            string простой = AssetDatabase.FindAssets("Манекен")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(п => п.EndsWith("/Манекен/" + имяФайла));
            return простой != null ? AssetDatabase.LoadAssetAtPath<GameObject>(простой) : null;
        }
        string папка = папкаПерсонажей;
        if (папка == null) return null;
        var (своя, файл) = персонажи[вид];
        return AssetDatabase.LoadAssetAtPath<GameObject>(папка + "/" + своя + "/" + файл);
    }

    /// <summary>
    /// Модель манекена Runity
    /// </summary>
    public static GameObject модель => Модель(ВидПерсонажа.Манекен) ?? Модель(ВидПерсонажа.Простой);

    // Русские названия анимаций манекена и то, как они называются у Кевина
    static readonly Dictionary<string, string> названия = new Dictionary<string, string>
    {
        { "Стоит", "HumanM@Idle01" }, { "Идёт", "HumanM@Walk01_Forward" }, { "Бежит", "HumanM@Run01_Forward" },
        { "Прыгает", "HumanM@Jump01 - Begin" }, { "Падает", "HumanM@Fall01" }, { "Приземляется", "HumanM@Jump01 - Land" },
        { "Машет", "HumanM@Talk01" }, { "Говорит", "HumanM@Talk01" }, { "Спринт", "HumanM@Sprint01_Forward" },
        { "Урон", "HumanM@Damage01" }, { "Умирает", "HumanM@Death01" },
    };

    /// <summary>
    /// Анимация манекена по названию: Стоит, Идёт, Бежит, Спринт, Прыгает, Падает, Приземляется, Говорит, Урон, Умирает (или название из файла)
    /// </summary>
    public static AnimationClip Анимация(string название)
    {
        string папка = папкаПерсонажей;
        if (папка != null)
        {
            string имя = названия.TryGetValue(название, out string своё) ? своё : название;
            AnimationClip клип = AssetDatabase.FindAssets("t:AnimationClip", new[] { папка + "/Человек" })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .SelectMany(путь => AssetDatabase.LoadAllAssetsAtPath(путь).OfType<AnimationClip>())
                .FirstOrDefault(к => к.name == имя);
            if (клип != null) return клип;
        }
        GameObject простой = Модель(ВидПерсонажа.Простой);
        if (простой == null) return null;
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(простой)).OfType<AnimationClip>().FirstOrDefault(к => к.name == название);
    }

    /// <summary>
    /// Ставит манекена на сцену. Анимации переключаются сами по движению
    /// </summary>
    public static GameObject Создать(Transform родитель = null, string имя = "Манекен") => Создать(ВидПерсонажа.Манекен, родитель, имя);

    /// <summary>
    /// Ставит персонажа Runity на сцену: манекена, гнома, великана, скелета или зомби. Анимации переключаются сами по движению
    /// </summary>
    public static GameObject Создать(ВидПерсонажа вид, Transform родитель = null, string имя = null)
    {
        GameObject образец = Модель(вид);
        if (образец == null && вид != ВидПерсонажа.Простой) { вид = ВидПерсонажа.Простой; образец = Модель(вид); }
        if (образец == null)
        {
            Debug.LogWarning("Runity: не найдены модели персонажей. Переустановите Runity через менеджер пакетов");
            return null;
        }

        GameObject персонаж = (GameObject)PrefabUtility.InstantiatePrefab(образец);
        персонаж.name = имя ?? ИмяПерсонажа(вид);
        if (родитель != null) персонаж.transform.SetParent(родитель, false);

        // Анимациями управляет Аниматор Игрока, схема из файла не нужна
        Animator аниматор = персонаж.GetComponent<Animator>();
        if (аниматор == null) аниматор = персонаж.AddComponent<Animator>();
        аниматор.runtimeAnimatorController = null;
        аниматор.applyRootMotion = false;

        АниматорИгрока анимации = персонаж.GetComponent<АниматорИгрока>();
        if (анимации == null) анимации = персонаж.AddComponent<АниматорИгрока>();
        if (вид == ВидПерсонажа.Простой)
        {
            Назначить(анимации.стоит, "Стоит");
            Назначить(анимации.идёт, "Идёт");
            Назначить(анимации.бежит, "Бежит");
            Назначить(анимации.прыгает, "Прыгает");
            Назначить(анимации.падает, "Прыгает");
        }
        else
        {
            // Свои анимации персонажа, а чего не хватает (например, прыжка у скелета) - от манекена: скелет у всех человеческий
            НайтиАнимации(анимации, папкаПерсонажей + "/" + персонажи[вид].папка);
            if (вид != ВидПерсонажа.Манекен && вид != ВидПерсонажа.МанекенДевушка) АнимацииМанекена(анимации);
        }
        EditorUtility.SetDirty(анимации);
        return персонаж;
    }

    static string ИмяПерсонажа(ВидПерсонажа вид) => вид switch
    {
        ВидПерсонажа.МанекенДевушка => "Манекен",
        ВидПерсонажа.Простой => "Манекен",
        _ => вид.ToString(),
    };

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
        string папка = папкаПерсонажей;
        if (папка != null) return НайтиАнимации(аниматор, папка + "/Человек", заменять);
        GameObject простой = Модель(ВидПерсонажа.Простой);
        return простой == null ? 0 : НайтиАнимации(аниматор, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(простой)).Replace('\\', '/'), заменять);
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

    static void СоздатьИзМеню(MenuCommand команда, ВидПерсонажа вид)
    {
        GameObject персонаж = Создать(вид, (команда.context as GameObject)?.transform);
        if (персонаж == null) return;
        Undo.RegisterCreatedObjectUndo(персонаж, персонаж.name);
        Selection.activeObject = персонаж;
    }

    [MenuItem("GameObject/Runity/3D/Манекен", false, -99)]
    static void СоздатьМанекена(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Манекен);
    [MenuItem("GameObject/Runity/3D/Персонажи/Манекен", false, -98)]
    static void СоздатьМанекена2(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Манекен);
    [MenuItem("GameObject/Runity/3D/Персонажи/Манекен-девушка", false, -97)]
    static void СоздатьДевушку(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.МанекенДевушка);
    [MenuItem("GameObject/Runity/3D/Персонажи/Гном", false, -96)]
    static void СоздатьГнома(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Гном);
    [MenuItem("GameObject/Runity/3D/Персонажи/Великан", false, -95)]
    static void СоздатьВеликана(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Великан);
    [MenuItem("GameObject/Runity/3D/Персонажи/Скелет", false, -94)]
    static void СоздатьСкелета(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Скелет);
    [MenuItem("GameObject/Runity/3D/Персонажи/Зомби", false, -93)]
    static void СоздатьЗомби(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Зомби);
    [MenuItem("GameObject/Runity/3D/Персонажи/Простой манекен", false, -92)]
    static void СоздатьПростого(MenuCommand команда) => СоздатьИзМеню(команда, ВидПерсонажа.Простой);
}
#endif
