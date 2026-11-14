#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RunityCustomObjects
{
    static GameObject CreateObject(MenuCommand menuCommand, string name)
    {
        // 1. Создаем новый игровой объект
        GameObject newObj = new GameObject(name);

        // 2. Настраиваем родительский объект и выравнивание (важно для клика в Hierarchy)
        GameObjectUtility.SetParentAndAlign(newObj, menuCommand.context as GameObject);

        // 3. Регистрируем создание для возможности отмены (Ctrl+Z)
        Undo.RegisterCreatedObjectUndo(newObj, "Create " + newObj.name);

        // 4. Делаем созданный объект выделенным в редакторе
        Selection.activeObject = newObj;

        return newObj;
    }

    [MenuItem("GameObject/Runity/Пустышка", false, -151)]
    private static void Пустой(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Пустышка");
    }

    [MenuItem("GameObject/Runity/2D/Спрайтер", false, -101)]
    private static void Спрайт(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спрайтер");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
    }

    [MenuItem("GameObject/Runity/2D/Спрайтер с физикой", false, -101)]
    private static void СпрайтСФизикой(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спрайтер");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
        newObj.AddComponent<ФизическоеТело2D>();
        newObj.AddComponent<КоллайдерПрямоугольник2D>();
    }

    [MenuItem("GameObject/Runity/Спавнер", false, 0)]
    private static void Спавнер(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спавнер");

        newObj.AddComponent<Спавнер>();
    }

    [MenuItem("GameObject/Runity/Управление", false, -51)]
    private static void Управление(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Управление");

        newObj.AddComponent<Управление>().УстановитьСтандартныеНастройки();
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигация/Навигационная карта", false, -51)]
    private static void НавигационнаяКарта(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Навигационная карта");

        newObj.AddComponent<НавигационнаяКарта>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигация/Навигационный агент", false, -51)]
    private static void НавигационныйАгент(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Навигационный агент");

        newObj.AddComponent<НавигационныйАгент>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигация/Бродячий агент", false, -50)]
    private static void БродячийАгент(MenuCommand menuCommand)
    {
        GameObject newObj = СоздатьФорму<КоллайдерКапсула>(menuCommand, "Бродячий агент", PrimitiveType.Capsule);
        newObj.AddComponent<НавигационныйАгент>();
        newObj.AddComponent<НавигационноеБрождение>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигация/Агент-патрульный", false, -50)]
    private static void АгентПатрульный(MenuCommand menuCommand)
    {
        GameObject newObj = СоздатьФорму<КоллайдерКапсула>(menuCommand, "Агент-патрульный", PrimitiveType.Capsule);
        newObj.AddComponent<НавигационныйАгент>();
        НавигационноеПатрулирование патруль = newObj.AddComponent<НавигационноеПатрулирование>();

        for (int i = 0; i < 2; i++)
        {
            GameObject точка = new GameObject($"Точка {i + 1}");
            Undo.RegisterCreatedObjectUndo(точка, "Create " + точка.name);
            точка.transform.position = newObj.transform.position + new Vector3(i == 0 ? -4 : 4, 0, 0);
            патруль.точки.Add(точка.transform);
        }
    }

    [MenuItem("GameObject/Runity/Передвижение/Игроки/Игрок (для платформера)", false, -51)]
    private static void ИгрокДляПлатформера(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Игрок");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
        newObj.AddComponent<КоллайдерПрямоугольник2D>();
        Передвижение2D передвижение2D = newObj.AddComponent<Передвижение2D>();
        передвижение2D.вверх = false;
        передвижение2D.вниз = false;
        передвижение2D.влево = true;
        передвижение2D.вправо = true;
        Прыжок прыжок = newObj.AddComponent<Прыжок>();
        прыжок.наВперёд = true;
        прыжок.наПробел = false;
        прыжок.наЛКМ = false;
        if (newObj.TryGetComponent(out ФизическоеТело2D физическоеТело2D))
        {
            физическоеТело2D.позволитьВращаться = false;
        }
    }

    [MenuItem("GameObject/Runity/Передвижение/Игроки/Игрок (для вида сверху)", false, -51)]
    private static void ИгрокДляВидаСверху(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Игрок");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
        newObj.AddComponent<КоллайдерПрямоугольник2D>();
        Передвижение2D передвижение2D = newObj.AddComponent<Передвижение2D>();
        передвижение2D.вверх = true;
        передвижение2D.вниз = true;
        передвижение2D.влево = true;
        передвижение2D.вправо = true;
    }

    [MenuItem("GameObject/Runity/Камера 2D", false, 0)]
    private static void Камера2D(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Камера");

        Камера камера = newObj.AddComponent<Камера>();
        камера.перспектива = false;
        ДобавитьСлушателя(newObj);
    }

    [MenuItem("GameObject/Runity/Камера 3D", false, 0)]
    private static void Камера3D(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Камера");

        Камера камера = newObj.AddComponent<Камера>();
        камера.перспектива = true;
        ДобавитьСлушателя(newObj);
    }

    // На сцене должен быть ровно один слушатель звука
    static void ДобавитьСлушателя(GameObject камера)
    {
        if (Object.FindAnyObjectByType<AudioListener>() == null) камера.AddComponent<СлушательЗвука>();
    }

    [MenuItem("GameObject/Runity/3D/Моделлер", false, -101)]
    private static void Моделлер(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Моделлер");

        newObj.AddComponent<Моделлер>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Игроки/Игрок (от первого лица)", false, -51)]
    private static void ИгрокОтПервогоЛица(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Игрок");

        newObj.AddComponent<Персонаж>();

        GameObject камера = new GameObject("Камера");
        камера.transform.SetParent(newObj.transform, false);
        камера.transform.localPosition = new Vector3(0, 0.6f, 0);
        камера.tag = "MainCamera";
        камера.AddComponent<Камера>().перспектива = true;
        ДобавитьСлушателя(камера);

        newObj.AddComponent<ОбзорМышкой>().камера = камера.transform;
    }

    [MenuItem("GameObject/Runity/Передвижение/Самостоятельное/Брождение", false, -51)]
    private static void СоздатьБрождение(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Бродяга");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
        newObj.AddComponent<Брождение>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Самостоятельное/Патрулирование", false, -51)]
    private static void СоздатьПатрулирование(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Патрульный");

        newObj.AddComponent<Спрайтер>().СпрайтБелогоКвадрата();
        Патрулирование патрулирование = newObj.AddComponent<Патрулирование>();

        for (int i = 0; i < 2; i++)
        {
            GameObject точка = new GameObject($"Точка {i + 1}");
            Undo.RegisterCreatedObjectUndo(точка, "Create " + точка.name);
            точка.transform.position = newObj.transform.position + new Vector3(i == 0 ? -3 : 3, 0, 0);
            патрулирование.точки.Add(точка.transform);
        }
    }

    [MenuItem("GameObject/Runity/2D/Тайлер", false, -101)]
    private static void СоздатьТайлер(MenuCommand menuCommand)
    {
        GameObject сетка = CreateObject(menuCommand, "Сетка");
        сетка.AddComponent<Grid>();

        GameObject newObj = new GameObject("Тайлер");
        newObj.transform.SetParent(сетка.transform, false);
        newObj.AddComponent<Тайлер>();

        Selection.activeObject = newObj;
    }

    [MenuItem("GameObject/Runity/Передвижение/Игроки/Игрок (от третьего лица)", false, -51)]
    private static void ИгрокОтТретьегоЛица(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Игрок");

        Персонаж персонаж = newObj.AddComponent<Персонаж>();
        персонаж.относительноЧего = ОтносительноЧегоДвижение.ОтносительноКамеры;
        персонаж.поворачиватьПоДвижению = true;

        GameObject модель = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        модель.name = "Модель";
        Object.DestroyImmediate(модель.GetComponent<Collider>());
        модель.transform.SetParent(newObj.transform, false);
        модель.AddComponent<Моделлер>();

        GameObject камера = new GameObject("Камера");
        Undo.RegisterCreatedObjectUndo(камера, "Create " + камера.name);
        камера.transform.position = newObj.transform.position + new Vector3(0, 4, -7);
        камера.transform.LookAt(newObj.transform);
        камера.tag = "MainCamera";
        камера.AddComponent<Камера>().перспектива = true;
        камера.AddComponent<Следование>().цель = newObj.transform;
        ДобавитьСлушателя(камера);
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигация/Препятствие навигации", false, -51)]
    private static void СоздатьПрепятствие(MenuCommand menuCommand)
    {
        GameObject newObj = СоздатьФорму<КоллайдерКуб>(menuCommand, "Препятствие", PrimitiveType.Cube);
        newObj.AddComponent<ПрепятствиеНавигации>().вырезатьВКарте = true;
    }

    [MenuItem("GameObject/Runity/3D/Формы/Куб", false, -100)]
    private static void ФормаКуб(MenuCommand menuCommand) => СоздатьФорму<КоллайдерКуб>(menuCommand, "Куб", PrimitiveType.Cube);

    [MenuItem("GameObject/Runity/3D/Формы/Сфера", false, -100)]
    private static void ФормаСфера(MenuCommand menuCommand) => СоздатьФорму<КоллайдерСфера>(menuCommand, "Сфера", PrimitiveType.Sphere);

    [MenuItem("GameObject/Runity/3D/Формы/Капсула", false, -100)]
    private static void ФормаКапсула(MenuCommand menuCommand) => СоздатьФорму<КоллайдерКапсула>(menuCommand, "Капсула", PrimitiveType.Capsule);

    [MenuItem("GameObject/Runity/3D/Формы/Цилиндр", false, -100)]
    private static void ФормаЦилиндр(MenuCommand menuCommand) => СоздатьФорму<КоллайдерКапсула>(menuCommand, "Цилиндр", PrimitiveType.Cylinder);

    [MenuItem("GameObject/Runity/3D/Формы/Плоскость", false, -100)]
    private static void ФормаПлоскость(MenuCommand menuCommand) => СоздатьФорму<КоллайдерМеш>(menuCommand, "Плоскость", PrimitiveType.Plane);

    static GameObject СоздатьФорму<TКоллайдер>(MenuCommand menuCommand, string имя, PrimitiveType тип) where TКоллайдер : Коллайдер3D
    {
        GameObject newObj = GameObject.CreatePrimitive(тип);
        newObj.name = имя;
        GameObjectUtility.SetParentAndAlign(newObj, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(newObj, "Create " + имя);
        Selection.activeObject = newObj;

        newObj.AddComponent<Моделлер>();
        newObj.AddComponent<TКоллайдер>();
        return newObj;
    }

    [MenuItem("GameObject/Runity/3D/Свет/Точечный", false, -90)]
    private static void СветТочечный(MenuCommand menuCommand) => СоздатьСвет(menuCommand, "Лампочка", ТипСвета.Точечный);

    [MenuItem("GameObject/Runity/3D/Свет/Направленный (солнце)", false, -90)]
    private static void СветНаправленный(MenuCommand menuCommand) => СоздатьСвет(menuCommand, "Солнце", ТипСвета.Направленный).transform.rotation = Quaternion.Euler(50, -30, 0);

    [MenuItem("GameObject/Runity/3D/Свет/Прожектор", false, -90)]
    private static void СветПрожектор(MenuCommand menuCommand) => СоздатьСвет(menuCommand, "Прожектор", ТипСвета.Прожектор).transform.rotation = Quaternion.Euler(90, 0, 0);

    static GameObject СоздатьСвет(MenuCommand menuCommand, string имя, ТипСвета тип)
    {
        GameObject newObj = CreateObject(menuCommand, имя);
        Свет свет = newObj.AddComponent<Свет>();
        свет.тип = тип;
        свет.тени = тип == ТипСвета.Направленный;
        return newObj;
    }

    [MenuItem("GameObject/Runity/2D/Свет/Точечный", false, -100)]
    private static void СоздатьСвет2D(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Свет 2D").AddComponent<Свет2D>().тип = ТипСвета2D.Точечный;
    }

    [MenuItem("GameObject/Runity/2D/Свет/Глобальный", false, -100)]
    private static void СоздатьГлобальныйСвет2D(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Глобальный свет 2D").AddComponent<Свет2D>().тип = ТипСвета2D.Глобальный;
    }

    [MenuItem("GameObject/Runity/2D/Односторонняя платформа", false, -100)]
    private static void СоздатьОдностороннююПлатформу(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Платформа");
        Спрайтер спрайтер = newObj.AddComponent<Спрайтер>();
        спрайтер.СпрайтБелогоКвадрата();
        newObj.transform.localScale = new Vector3(3, 0.3f, 1);
        newObj.AddComponent<КоллайдерПрямоугольник2D>();
        newObj.AddComponent<ОдносторонняяПлатформа>();
    }

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Огонь", false, 0)]
    private static void ЧастицыОгонь(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Огонь);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Дым", false, 0)]
    private static void ЧастицыДым(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Дым);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Искры", false, 0)]
    private static void ЧастицыИскры(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Искры);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Взрыв", false, 0)]
    private static void ЧастицыВзрыв(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Взрыв);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Снег", false, 0)]
    private static void ЧастицыСнег(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Снег);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Дождь", false, 0)]
    private static void ЧастицыДождь(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Дождь);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Пыль", false, 0)]
    private static void ЧастицыПыль(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Пыль);

    [MenuItem("GameObject/Runity/Эффекты/Частицы/Волшебство", false, 0)]
    private static void ЧастицыВолшебство(MenuCommand menuCommand) => СоздатьЧастицы(menuCommand, ПресетЧастиц.Волшебство);

    static void СоздатьЧастицы(MenuCommand menuCommand, ПресетЧастиц пресет)
    {
        GameObject newObj = CreateObject(menuCommand, пресет.ToString());
        newObj.AddComponent<Частицы>().ПрименитьПресет(пресет);
    }

    [MenuItem("GameObject/Runity/Эффекты/Линия", false, 0)]
    private static void СоздатьЛинию(MenuCommand menuCommand)
    {
        Линия линия = CreateObject(menuCommand, "Линия").AddComponent<Линия>();
        линия.толщина = 0.1;
        линия.Провести(new Vector3(-1, 0, 0), new Vector3(1, 0, 0));
    }

    [MenuItem("GameObject/Runity/Эффекты/Видео", false, 0)]
    private static void СоздатьВидео(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Видео").AddComponent<Видео>();
    }

    [MenuItem("GameObject/Runity/Звук/Источник звука", false, 0)]
    private static void СоздатьИсточникЗвука(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Источник звука").AddComponent<ИсточникЗвука>();
    }

    [MenuItem("GameObject/Runity/Ввод/Читер", false, 0)]
    private static void СоздатьЧитера(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Читер").AddComponent<Читер>();
    }

    #region Интерфейс

    const string guidБелогоКвадрата = "1c08e50202374ce42bca6cb30aa4bbbf";

    static DefaultControls.Resources РесурсыИнтерфейса() => new DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
        mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
    };

    static Интерфейс СоздатьОсновуИнтерфейса()
    {
        GameObject newObj = new GameObject("Интерфейс");
        Undo.RegisterCreatedObjectUndo(newObj, "Create " + newObj.name);
        Интерфейс интерфейс = newObj.AddComponent<Интерфейс>();
        интерфейс.СтандартныеНастройки();
        АналогиRunity.Добавить(newObj);
        return интерфейс;
    }

    // Элемент должен лежать внутри Интерфейса, иначе он не будет виден
    static void ПоместитьВИнтерфейс(MenuCommand menuCommand, GameObject элемент)
    {
        GameObject выбранный = menuCommand.context as GameObject;
        Transform родитель = выбранный != null && выбранный.GetComponentInParent<Canvas>() != null ? выбранный.transform : null;

        if (родитель == null)
        {
            Canvas основа = Object.FindAnyObjectByType<Canvas>();
            родитель = основа != null ? основа.transform : СоздатьОсновуИнтерфейса().transform;
        }

        GameObjectUtility.SetParentAndAlign(элемент, родитель.gameObject);
        Undo.RegisterCreatedObjectUndo(элемент, "Create " + элемент.name);
        Selection.activeObject = элемент;

        foreach (Text текст in элемент.GetComponentsInChildren<Text>(true))
        {
            if (текст.font == null) текст.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // У каждой части элемента должен быть русский аналог: у картинок - Картинка, у текстов - Текст
        АналогиRunity.Добавить(элемент);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Основа/Интерфейс", false, 10)]
    private static void СоздатьИнтерфейс(MenuCommand menuCommand)
    {
        Selection.activeObject = СоздатьОсновуИнтерфейса().gameObject;
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Текст", false, 11)]
    private static void СоздатьТекст(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateText(РесурсыИнтерфейса());
        newObj.name = "Текст";
        Текст текст = newObj.AddComponent<Текст>();
        текст.содержимое = "Новый текст";
        текст.размер = 36;
        текст.цвет = Цвет.белый;
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Кнопка", false, 12)]
    private static void СоздатьКнопку(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateButton(РесурсыИнтерфейса());
        newObj.name = "Кнопка";
        newObj.AddComponent<КнопкаИнтерфейса>();
        newObj.GetComponentInChildren<Text>().text = "Кнопка";
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 60);
        newObj.GetComponentInChildren<Text>().fontSize = 28;
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Ввод текста", false, 13)]
    private static void СоздатьВводТекста(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateInputField(РесурсыИнтерфейса());
        newObj.name = "Ввод текста";
        newObj.AddComponent<ВводТекста>().подсказка = "Введите текст...";
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        foreach (Text текст in newObj.GetComponentsInChildren<Text>(true)) текст.fontSize = 28;
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Слайдер", false, 14)]
    private static void СоздатьСлайдер(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateSlider(РесурсыИнтерфейса());
        newObj.name = "Слайдер";
        newObj.AddComponent<Слайдер>();
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Прогрессбар", false, 15)]
    private static void СоздатьПрогрессбар(MenuCommand menuCommand)
    {
        Sprite белыйКвадрат = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guidБелогоКвадрата));

        GameObject newObj = new GameObject("Прогрессбар", typeof(RectTransform));
        Image фон = newObj.AddComponent<Image>();
        фон.sprite = белыйКвадрат;
        фон.color = new Color(0.15f, 0.15f, 0.15f);
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

        newObj.AddComponent<Картинка>();

        GameObject заполнение = new GameObject("Полоска", typeof(RectTransform));
        заполнение.transform.SetParent(newObj.transform, false);
        заполнение.AddComponent<Image>().sprite = белыйКвадрат;

        RectTransform область = заполнение.GetComponent<RectTransform>();
        область.anchorMin = Vector2.zero;
        область.anchorMax = Vector2.one;
        область.offsetMin = new Vector2(4, 4);
        область.offsetMax = new Vector2(-4, -4);

        Картинка полоска = заполнение.AddComponent<Картинка>();
        полоска.режим = РежимКартинки.Заполнение;
        полоска.окраска = ОкраскаКартинки.ПоЗаполненности;

        ПоместитьВИнтерфейс(menuCommand, newObj);
        Selection.activeObject = заполнение;
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Картинка", false, 11)]
    private static void СоздатьКартинку(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateImage(РесурсыИнтерфейса());
        newObj.name = "Картинка";
        newObj.AddComponent<Картинка>();
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Галочка", false, 13)]
    private static void СоздатьГалочку(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateToggle(РесурсыИнтерфейса());
        newObj.name = "Галочка";
        newObj.AddComponent<Галочка>().надпись = "Галочка";
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Выпадающий список", false, 13)]
    private static void СоздатьВыпадающийСписок(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateDropdown(РесурсыИнтерфейса());
        newObj.name = "Выпадающий список";
        newObj.AddComponent<ВыпадающийСписок>().варианты = new System.Collections.Generic.List<string> { "Первый", "Второй", "Третий" };
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Основа/Прокрутка", false, 16)]
    private static void СоздатьПрокрутку(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateScrollView(РесурсыИнтерфейса());
        newObj.name = "Прокрутка";
        newObj.AddComponent<Прокрутка>().горизонтальная = false;

        // Чтобы элементы внутри сразу вставали друг под другом, а содержимое росло вместе с ними
        GameObject содержимое = newObj.GetComponent<ScrollRect>().content.gameObject;
        ГруппаЭлементов раскладка = содержимое.AddComponent<ГруппаЭлементов>();
        раскладка.режим = РежимГруппы.Вертикально;
        раскладка.отступМежду = 10;
        ContentSizeFitter подгонка = содержимое.AddComponent<ContentSizeFitter>();
        подгонка.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Основа/Панель", false, 16)]
    private static void СоздатьПанель(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreatePanel(РесурсыИнтерфейса());
        newObj.name = "Панель";
        newObj.AddComponent<ГруппаИнтерфейса>();
        ГруппаЭлементов раскладка = newObj.AddComponent<ГруппаЭлементов>();
        раскладка.режим = РежимГруппы.Вертикально;
        раскладка.поля = 40;
        раскладка.отступМежду = 20;
        раскладка.выравнивание = Выравнивание.ПоЦентру;
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Основа/Экран загрузки", false, 17)]
    [MenuItem("Assets/Create/Runity/Экран загрузки", false, -80)]
    private static void СоздатьЭкранЗагрузки()
    {
        string путь = "Assets/Resources/" + ЭкранЗагрузки.имяОбразца + ".prefab";
        if (ЭкранЗагрузки.образец != null)
        {
            путь = AssetDatabase.GetAssetPath(ЭкранЗагрузки.образец);
            EditorUtility.DisplayDialog("Экран загрузки", $"Образец экрана загрузки уже есть: {путь}. Он появляется сам при переходах между сценами", "Понятно");
        }
        else
        {
            СоздатьОбразецЭкранаЗагрузки("Assets/Resources");
            EditorUtility.DisplayDialog("Экран загрузки", $"Создан образец экрана загрузки: {путь}. Класть его на сцену не нужно: он появится сам при каждом переходе через Сцены и исчезнет после загрузки. Чтобы изменить его вид, откройте образец двойным щелчком", "Понятно");
        }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(путь);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    /// <summary>
    /// Создаёт образец экрана загрузки в указанной папке Resources и даёт путь к нему
    /// </summary>
    public static string СоздатьОбразецЭкранаЗагрузки(string папкаResources)
    {
        Sprite белыйКвадрат = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guidБелогоКвадрата));

        Интерфейс основа = СоздатьОсновуИнтерфейса();
        основа.name = "Экран загрузки";
        основа.порядокОтрисовки = 1000;

        GameObject фон = new GameObject("Фон", typeof(RectTransform));
        фон.transform.SetParent(основа.transform, false);
        Image картинкаФона = фон.AddComponent<Image>();
        картинкаФона.sprite = белыйКвадрат;
        картинкаФона.color = new Color(0.08f, 0.08f, 0.1f);
        Растянуть(фон.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject надпись = DefaultControls.CreateText(РесурсыИнтерфейса());
        надпись.name = "Текст";
        надпись.transform.SetParent(основа.transform, false);
        Text текстНадписи = надпись.GetComponent<Text>();
        текстНадписи.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Текст текст = надпись.AddComponent<Текст>();
        текст.содержимое = "Загрузка... 0%";
        текст.размер = 40;
        текст.цвет = Цвет.белый;
        текстНадписи.alignment = TextAnchor.MiddleCenter;
        Растянуть(надпись.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, 20), new Vector2(300, 90));

        GameObject полоса = new GameObject("Полоса", typeof(RectTransform));
        полоса.transform.SetParent(основа.transform, false);
        Image фонПолосы = полоса.AddComponent<Image>();
        фонПолосы.sprite = белыйКвадрат;
        фонПолосы.color = new Color(0.2f, 0.2f, 0.25f);
        Растянуть(полоса.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -30), new Vector2(300, 0));

        GameObject заполнение = new GameObject("Заполнение", typeof(RectTransform));
        заполнение.transform.SetParent(полоса.transform, false);
        заполнение.AddComponent<Image>().sprite = белыйКвадрат;
        Растянуть(заполнение.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
        Картинка полоска = заполнение.AddComponent<Картинка>();
        полоска.режим = РежимКартинки.Заполнение;
        полоска.плавность = 0;
        полоска.окраска = ОкраскаКартинки.Градиент;

        ЭкранЗагрузки экран = основа.gameObject.AddComponent<ЭкранЗагрузки>();
        экран.полоска = полоска;
        экран.текст = текстНадписи;

        АналогиRunity.Добавить(основа.gameObject);

        System.IO.Directory.CreateDirectory(папкаResources);
        string путь = $"{папкаResources}/{ЭкранЗагрузки.имяОбразца}.prefab";
        PrefabUtility.SaveAsPrefabAsset(основа.gameObject, путь);
        Object.DestroyImmediate(основа.gameObject);
        AssetDatabase.Refresh();
        return путь;
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Окно диалога", false, 18)]
    private static void СоздатьОкноДиалога(MenuCommand menuCommand)
    {
        Sprite белыйКвадрат = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guidБелогоКвадрата));

        GameObject окно = new GameObject("Окно диалога", typeof(RectTransform));
        Image фон = окно.AddComponent<Image>();
        фон.sprite = белыйКвадрат;
        фон.color = new Color(0.07f, 0.07f, 0.1f, 0.92f);

        GameObject портрет = new GameObject("Портрет", typeof(RectTransform));
        портрет.transform.SetParent(окно.transform, false);
        Image картинкаПортрета = портрет.AddComponent<Image>();
        картинкаПортрета.preserveAspect = true;
        Растянуть((RectTransform)портрет.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 20), new Vector2(220, -20));

        Text имя = ТекстОкна(окно.transform, "Имя", 30, FontStyle.Bold, new Vector2(240, -60), new Vector2(-20, -15));
        имя.color = new Color(1f, 0.85f, 0.4f);
        Text реплика = ТекстОкна(окно.transform, "Реплика", 26, FontStyle.Normal, new Vector2(240, -170), new Vector2(-20, -65));

        GameObject ответы = new GameObject("Ответы", typeof(RectTransform));
        ответы.transform.SetParent(окно.transform, false);
        Растянуть((RectTransform)ответы.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(240, 15), new Vector2(-20, 90));
        ГруппаЭлементов группа = ответы.AddComponent<ГруппаЭлементов>();
        группа.режим = РежимГруппы.Горизонтально;
        группа.отступМежду = 10;

        GameObject образец = DefaultControls.CreateButton(РесурсыИнтерфейса());
        образец.name = "Образец ответа";
        образец.transform.SetParent(ответы.transform, false);
        Text надписьОбразца = образец.GetComponentInChildren<Text>();
        надписьОбразца.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        надписьОбразца.fontSize = 22;
        надписьОбразца.text = "Ответ";

        ОкноДиалога компонентОкна = окно.AddComponent<ОкноДиалога>();
        компонентОкна.текстИмени = имя;
        компонентОкна.текстРеплики = реплика;
        компонентОкна.портрет = картинкаПортрета;
        компонентОкна.образецОтвета = образец.GetComponent<Button>();

        ПоместитьВИнтерфейс(menuCommand, окно);
        Растянуть((RectTransform)окно.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 290));
    }

    [MenuItem("GameObject/Runity/Интерфейс/Основа/Игровое меню", false, 17)]
    private static void СоздатьИгровоеМеню(MenuCommand menuCommand)
    {
        Sprite белыйКвадрат = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guidБелогоКвадрата));
        GameObject объектМеню = CreateObject(menuCommand, "Игровое меню");
        ИгровоеМеню меню = объектМеню.AddComponent<ИгровоеМеню>();

        ГруппаИнтерфейса Панель(string имя, string заголовок, Color цвет, params (string надпись, string действие)[] кнопки)
        {
            GameObject панель = new GameObject(имя, typeof(RectTransform));
            Image фон = панель.AddComponent<Image>();
            фон.sprite = белыйКвадрат;
            фон.color = цвет;
            панель.AddComponent<CanvasGroup>();
            ГруппаИнтерфейса группа = панель.AddComponent<ГруппаИнтерфейса>();
            группа.времяПерехода = 0.3f;
            // В редакторе панели не закрывают сцену. Во время игры меню показывает их само
            панель.GetComponent<CanvasGroup>().alpha = 0;

            GameObject надпись = DefaultControls.CreateText(РесурсыИнтерфейса());
            надпись.name = "Заголовок";
            надпись.transform.SetParent(панель.transform, false);
            Растянуть((RectTransform)надпись.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, 140), new Vector2(450, 240));
            Text текст = надпись.GetComponent<Text>();
            текст.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            текст.fontSize = 56;
            текст.color = Color.white;
            текст.alignment = TextAnchor.MiddleCenter;
            текст.text = заголовок;

            for (int i = 0; i < кнопки.Length; i++)
            {
                GameObject кнопка = DefaultControls.CreateButton(РесурсыИнтерфейса());
                кнопка.name = кнопки[i].надпись;
                кнопка.transform.SetParent(панель.transform, false);
                RectTransform область = (RectTransform)кнопка.transform;
                область.anchoredPosition = new Vector2(0, 60 - i * 85);
                область.sizeDelta = new Vector2(380, 66);
                Text подпись = кнопка.GetComponentInChildren<Text>();
                подпись.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                подпись.fontSize = 28;
                подпись.text = кнопки[i].надпись;
                ПодключитьКнопку(кнопка.AddComponent<КнопкаИнтерфейса>(), объектМеню, кнопки[i].действие);
            }

            ПоместитьВИнтерфейс(menuCommand, панель);
            Растянуть((RectTransform)панель.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return группа;
        }

        меню.пауза = Панель("Пауза", "Пауза", new Color(0.05f, 0.05f, 0.1f, 0.85f),
            ("Продолжить", "ИгровоеМеню/ Продолжить ()"), ("Начать заново", "ИгровоеМеню/ Заново ()"), ("В главное меню", "ИгровоеМеню/ ВМеню ()"));
        меню.поражение = Панель("Поражение", "Вы проиграли", new Color(0.25f, 0.03f, 0.03f, 0.85f),
            ("Попробовать снова", "ИгровоеМеню/ Заново ()"), ("В главное меню", "ИгровоеМеню/ ВМеню ()"));
        меню.победа = Панель("Победа", "Победа!", new Color(0.05f, 0.18f, 0.08f, 0.85f),
            ("Сыграть ещё раз", "ИгровоеМеню/ Заново ()"), ("В главное меню", "ИгровоеМеню/ ВМеню ()"));

        Selection.activeObject = объектМеню;
    }

    /// <summary>
    /// Добавляет кнопке действие при нажатии: метод компонента на указанном объекте, например "ИгровоеМеню/ Заново ()"
    /// </summary>
    public static void ПодключитьКнопку(КнопкаИнтерфейса кнопка, GameObject цель, string действие, string строка = "")
    {
        SerializedObject данные = new SerializedObject(кнопка);
        SerializedProperty список = данные.FindProperty("_приНажатии.события");
        int номер = список.arraySize;
        список.arraySize++;
        SerializedProperty событие = список.GetArrayElementAtIndex(номер);
        событие.FindPropertyRelative("объект").objectReferenceValue = цель;
        событие.FindPropertyRelative("компонент").stringValue = действие;
        событие.FindPropertyRelative("_string").stringValue = строка;
        данные.ApplyModifiedPropertiesWithoutUndo();
    }

    static Text ТекстОкна(Transform родитель, string имя, int размер, FontStyle стиль, Vector2 отступОт, Vector2 отступДо)
    {
        GameObject объект = new GameObject(имя, typeof(RectTransform));
        объект.transform.SetParent(родитель, false);
        Растянуть((RectTransform)объект.transform, new Vector2(0, 1), new Vector2(1, 1), отступОт, отступДо);
        Text текст = объект.AddComponent<Text>();
        текст.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        текст.fontSize = размер;
        текст.fontStyle = стиль;
        текст.color = Color.white;
        текст.alignment = TextAnchor.UpperLeft;
        return текст;
    }

    [MenuItem("GameObject/Runity/Механики/Журнал заданий", false, 0)]
    private static void СоздатьЖурналЗаданий(MenuCommand menuCommand)
    {
        GameObject newObj = DefaultControls.CreateText(РесурсыИнтерфейса());
        newObj.name = "Журнал заданий";
        Text текст = newObj.GetComponent<Text>();
        текст.fontSize = 24;
        текст.color = Color.white;
        текст.alignment = TextAnchor.UpperRight;
        текст.supportRichText = true;
        newObj.AddComponent<ЖурналЗаданий>().текст = текст;
        ПоместитьВИнтерфейс(menuCommand, newObj);
        Растянуть((RectTransform)newObj.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-520, -400), new Vector2(-20, -20));
    }

    static void Растянуть(RectTransform область, Vector2 якорьОт, Vector2 якорьДо, Vector2 отступОт, Vector2 отступДо)
    {
        область.anchorMin = якорьОт;
        область.anchorMax = якорьДо;
        область.offsetMin = отступОт;
        область.offsetMax = отступДо;
    }

    [MenuItem("GameObject/Runity/Интерфейс/Инвентарь/Ячейка инвентаря", false, 20)]
    private static void СоздатьЯчейкуИнвентаря(MenuCommand menuCommand)
    {
        ЯчейкаИнвентаря ячейка = ЯчейкаИнвентаря.СоздатьСтандартную(null, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
        ПоместитьВИнтерфейс(menuCommand, ячейка.gameObject);
    }

    [MenuItem("GameObject/Runity/Интерфейс/Инвентарь/Блок инвентаря", false, 21)]
    private static void СоздатьБлокИнвентаря(MenuCommand menuCommand)
    {
        GameObject newObj = new GameObject("Блок инвентаря", typeof(RectTransform));
        БлокИнвентаря блок = newObj.AddComponent<БлокИнвентаря>();
        ПоместитьВИнтерфейс(menuCommand, newObj);
        блок.инвентарь = Object.FindAnyObjectByType<Инвентарь>();
        блок.Перестроить();
    }

    [MenuItem("GameObject/Runity/Интерфейс/Элементы/Видео с камеры", false, 16)]
    private static void СоздатьВидеоСКамеры(MenuCommand menuCommand)
    {
        GameObject newObj = new GameObject("Видео с камеры", typeof(RectTransform));
        newObj.AddComponent<ВидеоСКамеры>();
        newObj.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 320);
        ПоместитьВИнтерфейс(menuCommand, newObj);
    }

    #endregion
}
#endif