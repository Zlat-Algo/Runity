#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Запускает сцены и рассказывает про них. Список сцен настраивается в меню Runity - Список сцен
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/Сцены")]
public class Сцены : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Запускает указанную сцену
    /// </summary>
    public static void Запустить(Число номерСцены)
    {
        if (номерСцены < количествоСцен)
        {
            ЗагрузитьСцену(номерСцены);
        }
        else
        {
            Консоль.НапечататьОшибку($"Сцены под номером {номерСцены} нет среди зарегистрированных. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через меню Runity - Список сцен");
        }
    }
    /// <summary>
    /// Запускает указанную сцену. В редакторе её можно запустить, даже если её нет в списке сцен
    /// </summary>
    public static void Запустить(Строка имяСцены) //=> SceneManager.LoadScene((string)имяСцены);
    {
        if (ПроверитьНаличиеСцены(имяСцены))
        {
            ЗагрузитьСцену(имяСцены);
        }
        else
        {
            Консоль.НапечататьОшибку($"Сцены {имяСцены} нет среди зарегистрированных. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через меню Runity - Список сцен");
        }
    }
    // Если в Resources есть образец Экрана загрузки, переход идёт через него, иначе - сразу
    static void ЗагрузитьСцену(Число номер)
    {
        int n = номер;
        if (!ЭкранЗагрузки.Начать(() => SceneManager.LoadSceneAsync(n))) SceneManager.LoadScene(n);
    }

    static void ЗагрузитьСцену(Строка имя)
    {
        string n = имя;
        if (!ЭкранЗагрузки.Начать(() => ЗагрузитьФоном(n))) ЗагрузитьФоном(n);
    }

    /// <summary>
    /// Запускает текущую сцену заново
    /// </summary>
    public static void Перезапустить()
    {
        Scene сцена = SceneManager.GetActiveScene();
        if (!ЭкранЗагрузки.Начать(() => ЗагрузитьФоном(сцена))) ЗагрузитьФоном(сцена);
    }

    // Загружает сцену по имени. В редакторе находит её в проекте, даже если её нет в списке сцен
    internal static AsyncOperation ЗагрузитьФоном(string имя)
    {
#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(имя))
        {
            string путь = ПутьВПроекте(имя);
            if (путь != null) return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(путь, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif
        return SceneManager.LoadSceneAsync(имя);
    }

    internal static AsyncOperation ЗагрузитьФоном(Scene сцена)
    {
#if UNITY_EDITOR
        if (сцена.buildIndex < 0) return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(сцена.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        return SceneManager.LoadSceneAsync(сцена.buildIndex);
    }

#if UNITY_EDITOR
    static string ПутьВПроекте(string имя)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene " + имя))
        {
            string путь = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(путь) == имя) return путь;
        }
        return null;
    }
#endif

    /// <summary>
    /// Запускает сцену со следующим номером
    /// </summary>
    public static Флажок ЗапуститьСледующую()
    {
        if (номерАктивнойСцены + 1 < количествоСцен)
        {
            ЗагрузитьСцену((int)номерАктивнойСцены + 1);
            return true;
        }
        else
        {
            Консоль.НапечататьОшибку($"Следующая сцена не существует!. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через меню Runity - Список сцен");
            return false;
        }
    }
    /// <summary>
    /// Запускает сцену с предыдущим номером
    /// </summary>
    public static Флажок ЗапуститьПредыдущую()
    {
        if (номерАктивнойСцены > 0)
        {
            ЗагрузитьСцену((int)номерАктивнойСцены - 1);
            return true;
        }
        else
        {
            Консоль.НапечататьОшибку("Предыдущая сцена не существует!");
            return false;
        }
    }
    /// <summary>
    /// Номер текущей сцены. Если записать сюда другой номер, запустится другая сцена
    /// </summary>
    public static Число номерАктивнойСцены
    {
        get => SceneManager.GetActiveScene().buildIndex;
        set => Запустить(value);
    }
    /// <summary>
    /// Имя текущей сцены
    /// </summary>
    public static Строка имяАктивнойСцены => SceneManager.GetActiveScene().name;
    /// <summary>
    /// Сколько сцен зарегистрировано в игре
    /// </summary>
    public static Число количествоСцен => SceneManager.sceneCountInBuildSettings;
    /// <summary>
    /// Даёт "да", если сцена с таким именем зарегистрирована
    /// </summary>
    public static Флажок ПроверитьНаличиеСцены(Строка имяСцены)
    {
#if UNITY_EDITOR
        // В редакторе можно запускать любые сцены проекта
        if (ПутьВПроекте(имяСцены) != null) return true;
#endif
        return Application.CanStreamedLevelBeLoaded(имяСцены);
    }
    /// <summary>
    /// Даёт имя зарегистрированной сцены по её номеру
    /// </summary>
    public static Строка ИмяСцены(Число номерСцены)
    {
        return System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(номерСцены));
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Сцены))]
internal class СценыEditor : RunityEditor<Сцены>
{
    protected override void Отрисовать()
    {
        Текст($"Этот объект может управлять сценами\n\nАктивная сцена: {Сцены.имяАктивнойСцены} ({Сцены.номерАктивнойСцены} из {Сцены.количествоСцен})");

        Пробел();

        Кнопка("Настроить список сцен", СписокСцен.ОткрытьВРедакторе);

        Пробел();

        if (!Application.isPlaying)
        {
            Текст("Переключать сцены можно во время игры");
            return;
        }

        КнопкаМетода("Запустить предыдущую", nameof(Сцены.ЗапуститьПредыдущую), () =>
        {
            Сцены.ЗапуститьПредыдущую();
        });

        КнопкаМетода("Перезапустить текущую", nameof(Сцены.Перезапустить), () =>
        {
            Сцены.Перезапустить();
        });

        КнопкаМетода("Запустить следующую", nameof(Сцены.ЗапуститьСледующую), () =>
        {
            Сцены.ЗапуститьСледующую();
        });
    }
}
#endif
