using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class PrefabCreator
{
    [MenuItem("Assets/Create/Префаб", false, -100000)]
    private static void CreatePrefab()
    {
        string parentFolder = GetSelectedFolder();

        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(parentFolder, "Новый префаб.prefab")
        );

        Texture2D icon =
            EditorGUIUtility.IconContent("Prefab Icon").image
            as Texture2D;

        var action =
            ScriptableObject.CreateInstance<CreatePrefabAction>();

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            EntityId.None,
            action,
            path,
            icon,
            null
        );
    }

    private sealed class CreatePrefabAction : AssetCreationEndAction
    {
        public override void Action(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
            // Временный GameObject.
            GameObject gameObject =
                new GameObject(
                    Path.GetFileNameWithoutExtension(pathName)
                );

            try
            {
                // Создаём prefab.
                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        gameObject,
                        pathName
                    );

                if (prefab == null)
                    return;

                // Выделяем созданный prefab.
                ProjectWindowUtil.ShowCreatedAsset(prefab);
            }
            finally
            {
                // Временный объект больше не нужен.
                Object.DestroyImmediate(gameObject);
            }
        }

        public override void Cancelled(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
        }
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

        string directory =
            Path.GetDirectoryName(path);

        return string.IsNullOrEmpty(directory)
            ? "Assets"
            : directory.Replace("\\", "/");
    }
}