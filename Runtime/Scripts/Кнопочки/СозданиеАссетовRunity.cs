#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Всё, что Runity создаёт в окне Project, лежит в одном месте: правый клик - Runity
internal static class СозданиеАссетовRunity
{
    static void Создать<T>(string имя) where T : ScriptableObject =>
        ProjectWindowUtil.CreateAsset(ScriptableObject.CreateInstance<T>(), имя + ".asset");

    [MenuItem("Assets/Runity/Материал", false, 20)]
    static void Материал() => Создать<Материал>("Новый материал");

    [MenuItem("Assets/Runity/Физический материал", false, 21)]
    static void ФизическийМатериал() => Создать<ФизическийМатериал>("Новый физический материал");

    [MenuItem("Assets/Runity/Кадровая анимация", false, 22)]
    static void КадроваяАнимация() => Создать<КадроваяАнимация>("Новая анимация");

    [MenuItem("Assets/Runity/Вещь", false, 24)]
    static void Вещь() => Создать<Вещь>("Новая вещь");

    [MenuItem("Assets/Runity/Диалог", false, 25)]
    static void Диалог() => Создать<Диалог>("Новый диалог");

    [MenuItem("Assets/Runity/Задание", false, 26)]
    static void Задание() => Создать<Задание>("Новое задание");

    [MenuItem("Assets/Runity/Список сцен", false, 30)]
    static void СписокСцен() => Создать<СписокСцен>("Список сцен");

    [MenuItem("Assets/Runity/Настройки графики", false, 33)]
    static void НастройкиГрафики() => global::НастройкиГрафики.ОткрытьВРедакторе();

    [MenuItem("Assets/Runity/Настройки проекта", false, 34)]
    static void НастройкиПроекта() => global::НастройкиПроекта.ОткрытьВРедакторе();
}
#endif
