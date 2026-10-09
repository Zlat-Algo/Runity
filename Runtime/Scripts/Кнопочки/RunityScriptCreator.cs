using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class RunityScriptCreator
{
    private const string ScriptTemplate = @"using UnityEngine;

public class #SCRIPTNAME# : Компонент
{
    // Этот метод выполняется один раз на старте
    void Start()
    {
        
    }

    // Этот метод выполняется каждый кадр
    void Update()
    {
        
    }
}";

    [MenuItem("Assets/Create/Скрипт", false, -100000)]
    private static void CreateMyClass()
    {
        string folder = GetSelectedFolder();

        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(folder, "НовыйСкрипт.cs")
        );

        Texture2D icon =
            EditorGUIUtility.IconContent("cs Script Icon").image
            as Texture2D;

        var action =
            ScriptableObject.CreateInstance<CreateScriptAction>();

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            EntityId.None,
            action,
            path,
            icon,
            null
        );
    }

    [MenuItem("Assets/Create/Скрипт", true)]
    private static bool ValidateCreateMyClass()
    {
        return true;
    }

    private static string GetSelectedFolder()
    {
        string path = "Assets";

        Object selected = Selection.activeObject;

        if (selected == null)
            return path;

        path = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(path))
            return "Assets";

        if (AssetDatabase.IsValidFolder(path))
            return path;

        string directory = Path.GetDirectoryName(path);

        return string.IsNullOrEmpty(directory)
            ? "Assets"
            : directory.Replace("\\", "/");
    }

    private sealed class CreateScriptAction : AssetCreationEndAction
    {
        public override void Action(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(pathName);

            // Имя класса должно быть валидным идентификатором.
            string scriptName = fileName.Replace(" ", "");

            string content = ScriptTemplate.Replace(
                "#SCRIPTNAME#",
                scriptName
            );

            File.WriteAllText(pathName, content);

            AssetDatabase.ImportAsset(
                pathName,
                ImportAssetOptions.ForceSynchronousImport
            );

            MonoScript script =
                AssetDatabase.LoadAssetAtPath<MonoScript>(
                    pathName
                );

            ProjectWindowUtil.ShowCreatedAsset(script);
        }

        public override void Cancelled(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
            // Пользователь отменил создание файла.
        }
    }
}