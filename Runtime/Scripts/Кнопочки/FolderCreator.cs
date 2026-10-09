using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class FolderCreator
{
    [MenuItem("Assets/Create/Папка", false, -100000)]
    private static void CreateFolder()
    {
        string parentFolder = GetSelectedFolder();

        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(parentFolder, "Новая папка")
        );

        Texture2D icon =
            EditorGUIUtility.IconContent("Folder Icon").image
            as Texture2D;

        var action =
            ScriptableObject.CreateInstance<CreateFolderAction>();

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            EntityId.None,
            action,
            path,
            icon,
            null
        );
    }

    private sealed class CreateFolderAction : AssetCreationEndAction
    {
        public override void Action(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
            string parentFolder =
                Path.GetDirectoryName(pathName)
                    .Replace("\\", "/");

            string folderName =
                Path.GetFileName(pathName);

            string guid = AssetDatabase.CreateFolder(
                parentFolder,
                folderName
            );

            if (string.IsNullOrEmpty(guid))
                return;

            string createdPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Object folder =
                AssetDatabase.LoadAssetAtPath<Object>(
                    createdPath
                );

            ProjectWindowUtil.ShowCreatedAsset(folder);
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