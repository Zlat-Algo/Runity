#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Добавляет к компонентам Unity их русские аналоги из Runity: к Mesh Renderer - Моделлер, к Image - Картинку и так далее
/// </summary>
public static class АналогиRunity
{
    // Какой компонент Runity показывает какой компонент Unity
    static readonly (Type unity, Type runity)[] пары =
    {
        (typeof(MeshRenderer), typeof(Моделлер)),
        (typeof(SpriteRenderer), typeof(Спрайтер)),
        (typeof(BoxCollider), typeof(КоллайдерКуб)),
        (typeof(SphereCollider), typeof(КоллайдерСфера)),
        (typeof(CapsuleCollider), typeof(КоллайдерКапсула)),
        (typeof(MeshCollider), typeof(КоллайдерМеш)),
        (typeof(BoxCollider2D), typeof(КоллайдерПрямоугольник2D)),
        (typeof(CircleCollider2D), typeof(КоллайдерКруг2D)),
        (typeof(CapsuleCollider2D), typeof(КоллайдерКапсула2D)),
        (typeof(EdgeCollider2D), typeof(КоллайдерЛиния2D)),
        (typeof(PolygonCollider2D), typeof(КоллайдерПолигональный2D)),
        (typeof(Rigidbody), typeof(ФизическоеТело)),
        (typeof(Rigidbody2D), typeof(ФизическоеТело2D)),
        (typeof(HingeJoint), typeof(Шарнир)),
        (typeof(SpringJoint), typeof(Пружина)),
        (typeof(FixedJoint), typeof(ЖёсткоеСоединение)),
        (typeof(HingeJoint2D), typeof(Шарнир2D)),
        (typeof(SpringJoint2D), typeof(Пружина2D)),
        (typeof(DistanceJoint2D), typeof(Верёвка2D)),
        (typeof(AreaEffector2D), typeof(ЗонаСилы)),
        (typeof(BuoyancyEffector2D), typeof(Вода2D)),
        (typeof(SurfaceEffector2D), typeof(Конвейер)),
        (typeof(PointEffector2D), typeof(Притяжение2D)),
        (typeof(PlatformEffector2D), typeof(ОдносторонняяПлатформа)),
        (typeof(Light), typeof(Свет)),
        (typeof(Light2D), typeof(Свет2D)),
        (typeof(Camera), typeof(Камера)),
        (typeof(AudioSource), typeof(ИсточникЗвука)),
        (typeof(AudioListener), typeof(СлушательЗвука)),
        (typeof(ParticleSystem), typeof(Частицы)),
        (typeof(LineRenderer), typeof(Линия)),
        (typeof(TrailRenderer), typeof(След)),
        (typeof(VideoPlayer), typeof(Видео)),
        (typeof(Animator), typeof(Аниматор)),
        (typeof(Tilemap), typeof(Тайлер)),
        (typeof(NavMeshAgent), typeof(НавигационныйАгент)),
        (typeof(NavMeshObstacle), typeof(ПрепятствиеНавигации)),
        (typeof(NavMeshSurface), typeof(НавигационнаяКарта)),
        (typeof(Canvas), typeof(Интерфейс)),
        (typeof(RectTransform), typeof(Расположение)),
        (typeof(Image), typeof(Картинка)),
        (typeof(Text), typeof(Текст)),
        (typeof(Button), typeof(КнопкаИнтерфейса)),
        (typeof(Toggle), typeof(Галочка)),
        (typeof(Slider), typeof(Слайдер)),
        (typeof(InputField), typeof(ВводТекста)),
        (typeof(Dropdown), typeof(ВыпадающийСписок)),
        (typeof(ScrollRect), typeof(Прокрутка)),
        (typeof(CanvasGroup), typeof(ГруппаИнтерфейса)),
        (typeof(LayoutGroup), typeof(ГруппаЭлементов)),
    };

    /// <summary>
    /// Русский аналог компонента Unity. Пусто, если аналога нет
    /// </summary>
    public static Type Аналог(Type типUnity)
    {
        foreach ((Type unity, Type runity) in пары)
        {
            if (unity.IsAssignableFrom(типUnity)) return runity;
        }
        return null;
    }

    /// <summary>
    /// Добавляет аналоги Runity объекту и всем его детям. Даёт, сколько компонентов добавлено
    /// </summary>
    public static int Добавить(GameObject корень, bool сОтменой = false)
    {
        int добавлено = 0;
        foreach (Transform часть in корень.GetComponentsInChildren<Transform>(true))
        {
            добавлено += ДобавитьНаОбъект(часть.gameObject, сОтменой);
        }
        return добавлено;
    }

    static int ДобавитьНаОбъект(GameObject объект, bool сОтменой)
    {
        int добавлено = 0;
        foreach (Component компонент in объект.GetComponents<Component>())
        {
            if (компонент == null) continue;
            Type аналог = Аналог(компонент.GetType());
            if (аналог == null || объект.GetComponent(аналог) != null || !Подходит(компонент)) continue;

            // Некоторые аналоги при добавлении ставят свои стандартные настройки. Настройки, которые уже были, важнее
            List<(Component, string)> было = new List<(Component, string)>();
            foreach (Component старый in объект.GetComponents<Component>())
            {
                if (старый != null && !(старый is Компонент) && !(старый is Transform)) было.Add((старый, EditorJsonUtility.ToJson(старый)));
            }

            Component новый = сОтменой ? Undo.AddComponent(объект, аналог) : объект.AddComponent(аналог);
            if (новый == null) continue;

            foreach ((Component старый, string json) in было)
            {
                if (старый != null) EditorJsonUtility.FromJsonOverwrite(json, старый);
            }
            добавлено++;
        }
        return добавлено;
    }

    static bool Подходит(Component компонент)
    {
        switch (компонент)
        {
            // Моделлеру нужна сама модель, а у трёхмерного текста её нет
            case MeshRenderer _: return компонент.GetComponent<MeshFilter>() != null && компонент.GetComponent<TextMesh>() == null;
            // Вложенные холсты - часть основного Интерфейса
            case Canvas холст: return холст.transform.parent == null || холст.transform.parent.GetComponentInParent<Canvas>() == null;
            // Эти компоненты сами управляют прозрачностью своего объекта
            case CanvasGroup _: return компонент.GetComponent<ЭкранЗагрузки>() == null && компонент.GetComponent<ОкноДиалога>() == null;
        }
        return true;
    }

    [MenuItem("GameObject/Runity/Добавить аналоги Runity", false, 1000)]
    static void ДобавитьВыделенным(MenuCommand команда)
    {
        // Пункт вызывается для каждого выделенного объекта, поэтому работаем один раз
        if (команда.context != null && команда.context != Selection.activeGameObject) return;
        GameObject[] выделенные = Selection.gameObjects;
        if (выделенные.Length == 0)
        {
            ДобавитьНаСцену();
            return;
        }

        int всего = 0;
        foreach (GameObject объект in выделенные) всего += Добавить(объект, true);
        Консоль.Напечатать($"Добавлено компонентов Runity: {всего}");
    }

    [MenuItem("Runity/Добавить аналоги Runity на всю сцену", false, 200)]
    static void ДобавитьНаСцену()
    {
        int всего = 0;
        foreach (GameObject корень in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            всего += Добавить(корень, true);
        }
        Консоль.Напечатать($"Добавлено компонентов Runity: {всего}");
    }
}
#endif
