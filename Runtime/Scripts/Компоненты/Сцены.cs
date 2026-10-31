#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/Сцены")]
public class Сцены : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    public static void Запустить(Число номерСцены)
    {
        if (номерСцены < количествоСцен)
        {
            SceneManager.LoadScene((int)номерСцены);
        }
        else
        {
            Консоль.НапечататьОшибку($"Сцены под номером {номерСцены} нет среди зарегистрированных. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через File - Build Profiles - Scene List");
        }
    }
    public static void Запустить(Строка имяСцены) //=> SceneManager.LoadScene((string)имяСцены);
    {
        if (ПроверитьНаличиеСцены(имяСцены))
        {
            SceneManager.LoadScene(имяСцены);
        }
        else
        {
            Консоль.НапечататьОшибку($"Сцены {имяСцены} нет среди зарегистрированных. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через File - Build Profiles - Scene List");
        }
    }
    public static void Перезапустить() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public static Флажок ЗапуститьСледующую()
    {
        if (номерАктивнойСцены + 1 < количествоСцен)
        {
            SceneManager.LoadScene(номерАктивнойСцены + 1);
            return true;
        }
        else
        {
            Консоль.НапечататьОшибку($"Следующая сцена не существует!. Всего зарегистрировано {количествоСцен} сцен. Вы можете зарегистрировать новые сцены через File - Build Profiles - Scene List");
            return false;
        }
    }
    public static Флажок ЗапуститьПредыдущую()
    {
        if (номерАктивнойСцены > 0)
        {
            SceneManager.LoadScene(номерАктивнойСцены - 1);
            return true;
        }
        else
        {
            Консоль.НапечататьОшибку("Предыдущая сцена не существует!");
            return false;
        }
    }
    public int кря { get; set; }
    public Число крякря { get; set; }
    public static Число номерАктивнойСцены
    {
        get => SceneManager.GetActiveScene().buildIndex;
        set => Запустить(value);
    }
    public static Строка имяАктивнойСцены => SceneManager.GetActiveScene().name;
    public static Число количествоСцен => SceneManager.sceneCountInBuildSettings;
    public static Флажок ПроверитьНаличиеСцены(Строка имяСцены)
    {
        return SceneManager.GetSceneByName(имяСцены).IsValid();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Сцены))]
internal class СценыEditor : RunityEditor<Сцены>
{
    public override void OnInspectorGUI()
    {
        Текст($"Этот объект может управлять сценами\n\nАктивная сцена: {Сцены.имяАктивнойСцены} ({Сцены.номерАктивнойСцены} из {Сцены.количествоСцен})");

        Пробел();

        Поле("Запустить сцену", x => Сцены.номерАктивнойСцены,
            (title, value) => EditorGUILayout.IntField(title, value));

        Кнопка("Запустить предыдущую", () =>
        {
            Сцены.ЗапуститьПредыдущую();
        });

        Кнопка("Перезапустить текущую", () =>
        {
            Сцены.Перезапустить();
        });

        Кнопка("Запустить следующую", () =>
        {
            Сцены.ЗапуститьСледующую();
        });
    }
}
#endif
