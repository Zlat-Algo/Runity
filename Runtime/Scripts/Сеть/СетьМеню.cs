#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Готовые сетевые объекты в меню Hierarchy - Runity - Сеть
internal static class СетьМеню
{
    [MenuItem("GameObject/Runity/Сеть/Сеть с меню", false, 0)]
    static void СетьСМеню(MenuCommand команда)
    {
        GameObject объект = new GameObject("Сеть");
        Undo.RegisterCreatedObjectUndo(объект, "Create Сеть");
        Сеть сеть = объект.AddComponent<Сеть>();

        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Основа/Панель");
        GameObject панель = Selection.activeGameObject;
        панель.name = "Меню сети";
        RectTransform область = (RectTransform)панель.transform;
        область.anchorMin = область.anchorMax = new Vector2(0, 1);
        область.pivot = new Vector2(0, 1);
        область.anchoredPosition = new Vector2(20, -20);
        область.sizeDelta = new Vector2(420, 400);

        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Элементы/Текст");
        GameObject состояние = Selection.activeGameObject;
        состояние.name = "Состояние сети";
        состояние.transform.SetParent(панель.transform, false);
        состояние.GetComponent<Text>().fontSize = 20;
        ((RectTransform)состояние.transform).sizeDelta = new Vector2(380, 80);
        ВыводЗначения вывод = состояние.AddComponent<ВыводЗначения>();
        вывод.объект = объект;
        вывод.поле = "Сеть/состояние";

        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Элементы/Ввод текста");
        GameObject ввод = Selection.activeGameObject;
        ввод.name = "Адрес хозяина";
        ввод.transform.SetParent(панель.transform, false);
        ВводТекста полеВвода = ввод.GetComponent<ВводТекста>();
        полеВвода.подсказка = "Адрес хозяина, например 26.12.34.56";
        сеть.полеАдреса = полеВвода;

        Кнопка(панель.transform, "Создать игру", объект, "Сеть/ СоздатьИгру ()");
        Кнопка(панель.transform, "Подключиться по адресу", объект, "Сеть/ Подключиться ()");
        Кнопка(панель.transform, "Найти игру", объект, "Сеть/ НайтиИгры ()");
        Кнопка(панель.transform, "Отключиться", объект, "Сеть/ Отключиться ()");

        // Найденная игра сразу подключается: так проще всего начать
        ДобавитьСобытие(сеть, "_приНахожденииИгры", объект, "Сеть/ ПодключитьсяКПервойНайденной ()");
        // Пока идёт игра, меню не нужно
        ДобавитьСобытие(сеть, "_приСозданииИгры", панель, "Объект/ Выключить ()");
        ДобавитьСобытие(сеть, "_приПодключении", панель, "Объект/ Выключить ()");
        ДобавитьСобытие(сеть, "_приОтключении", панель, "Объект/ Включить ()");

        Selection.activeObject = объект;
    }

    static void Кнопка(Transform родитель, string надпись, GameObject цель, string действие)
    {
        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Элементы/Кнопка");
        GameObject кнопка = Selection.activeGameObject;
        кнопка.name = надпись;
        кнопка.transform.SetParent(родитель, false);
        кнопка.GetComponentInChildren<Text>().text = надпись;
        ((RectTransform)кнопка.transform).sizeDelta = new Vector2(380, 50);
        ДобавитьСобытие(кнопка.GetComponent<КнопкаИнтерфейса>(), "_приНажатии", цель, действие);
    }

    internal static void ДобавитьСобытие(Component компонент, string поле, GameObject объект, string действие)
    {
        SerializedObject данные = new SerializedObject(компонент);
        SerializedProperty список = данные.FindProperty(поле).FindPropertyRelative("события");
        список.arraySize++;
        SerializedProperty событие = список.GetArrayElementAtIndex(список.arraySize - 1);
        событие.FindPropertyRelative("объект").objectReferenceValue = объект;
        событие.FindPropertyRelative("компонент").stringValue = действие;
        данные.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("GameObject/Runity/Сеть/Сетевой игрок (3D)", false, 1)]
    static void СетевойИгрок(MenuCommand команда)
    {
        GameObject игрок = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        игрок.name = "Сетевой игрок";
        Object.DestroyImmediate(игрок.GetComponent<Collider>());
        GameObjectUtility.SetParentAndAlign(игрок, команда.context as GameObject);
        Undo.RegisterCreatedObjectUndo(игрок, "Create Сетевой игрок");
        игрок.AddComponent<Персонаж>();
        игрок.AddComponent<ВладельческаяСинхронизация>();
        СетевойОбъект сетевой = игрок.AddComponent<СетевойОбъект>();
        сетевой.НайтиУправление();
        АналогиRunity.Добавить(игрок);
        Selection.activeObject = игрок;
        EditorUtility.DisplayDialog("Сетевой игрок", "Перетащите этого игрока в окно Project, чтобы получился образец (префаб), удалите его со сцены и укажите образец в поле «Игрок» у Сети", "Понятно");
    }

    [MenuItem("GameObject/Runity/Сеть/Чат", false, 2)]
    static void Чат(MenuCommand команда)
    {
        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Основа/Панель");
        GameObject панель = Selection.activeGameObject;
        панель.name = "Чат";
        RectTransform область = (RectTransform)панель.transform;
        область.anchorMin = область.anchorMax = new Vector2(0, 0);
        область.pivot = new Vector2(0, 0);
        область.anchoredPosition = new Vector2(20, 20);
        область.sizeDelta = new Vector2(460, 320);
        СетевойЧат чат = панель.AddComponent<СетевойЧат>();

        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Элементы/Текст");
        GameObject сообщения = Selection.activeGameObject;
        сообщения.name = "Сообщения";
        сообщения.transform.SetParent(панель.transform, false);
        Text текст = сообщения.GetComponent<Text>();
        текст.fontSize = 18;
        текст.alignment = TextAnchor.LowerLeft;
        текст.supportRichText = true;
        ((RectTransform)сообщения.transform).sizeDelta = new Vector2(420, 200);
        чат.текстЧата = сообщения.GetComponent<Текст>();

        EditorApplication.ExecuteMenuItem("GameObject/Runity/Интерфейс/Элементы/Ввод текста");
        GameObject ввод = Selection.activeGameObject;
        ввод.name = "Сообщение";
        ввод.transform.SetParent(панель.transform, false);
        ВводТекста поле = ввод.GetComponent<ВводТекста>();
        поле.подсказка = "Напишите сообщение и нажмите Enter";
        чат.полеВвода = поле;

        Кнопка(панель.transform, "Отправить", панель, "СетевойЧат/ ОтправитьИзПоля ()");
        Selection.activeObject = панель;
    }
}
#endif
