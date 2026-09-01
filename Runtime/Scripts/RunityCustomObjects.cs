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

    [MenuItem("GameObject/Runity/Пустышка", false, 0)]
    private static void Пустой(MenuCommand menuCommand)
    {
        CreateObject(menuCommand, "Пустышка");
    }

    [MenuItem("GameObject/Runity/2D/Спрайтер", false, 0)]
    private static void Спрайт(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спрайтер");

        newObj.AddComponent<Спрайтер>();
    }

    [MenuItem("GameObject/Runity/2D/Спрайтер с физикой", false, 0)]
    private static void СпрайтСФизикой(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спрайтер");

        newObj.AddComponent<Спрайтер>();
        newObj.AddComponent<ФизическоеТело2D>();
    }

    [MenuItem("GameObject/Runity/Спавнер", false, 0)]
    private static void Спавнер(MenuCommand menuCommand)
    {
        GameObject newObj = CreateObject(menuCommand, "Спавнер");

        newObj.AddComponent<Спавнер>();
    }
}
#endif