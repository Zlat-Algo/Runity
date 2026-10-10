#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

    [MenuItem("GameObject/Runity/Передвижение/Навигационная карта", false, -51)]
    private static void НавигационнаяКарта(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Навигационная карта");

        newObj.AddComponent<НавигационнаяКарта>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Навигационный агент", false, -51)]
    private static void НавигационныйАгент(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Навигационный агент");

        newObj.AddComponent<НавигационныйАгент>();
    }

    [MenuItem("GameObject/Runity/Передвижение/Игрок (для платформера)", false, -51)]
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

    [MenuItem("GameObject/Runity/Передвижение/Игрок (для вида сверху)", false, -51)]
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
    }

    [MenuItem("GameObject/Runity/Камера 3D", false, 0)]
    private static void Камера3D(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Камера");

        Камера камера = newObj.AddComponent<Камера>();
        камера.перспектива = true;
    }

    [MenuItem("GameObject/Runity/3D/Моделлер", false, -101)]
    private static void Моделлер(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Моделлер");

        newObj.AddComponent<Моделлер>();
    }
}
#endif