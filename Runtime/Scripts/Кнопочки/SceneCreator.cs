using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class SceneCreator
{
    [MenuItem("Assets/Create/Сцена", false, -100000)]
    private static void CreateScene()
    {
        string parentFolder = GetSelectedFolder();

        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(parentFolder, "Новая сцена.unity")
        );

        Texture2D icon =
            EditorGUIUtility.IconContent("SceneAsset Icon").image
            as Texture2D;

        var action =
            ScriptableObject.CreateInstance<CreateSceneAction>();

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            EntityId.None,
            action,
            path,
            icon,
            null
        );
    }

    private sealed class CreateSceneAction : AssetCreationEndAction
    {
        public override void Action(
            EntityId entityId,
            string pathName,
            string resourceFile)
        {
            string temporaryPath =
                "Assets/__TemporarySceneForCreation.unity";

            temporaryPath = AssetDatabase.GenerateUniqueAssetPath(
                temporaryPath
            );

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive
            );

            try
            {
                EditorSceneManager.SaveScene(scene, temporaryPath);

                AssetDatabase.MoveAsset(temporaryPath, pathName);

                AssetDatabase.ImportAsset(
                    pathName,
                    ImportAssetOptions.ForceSynchronousImport
                );

                Object sceneAsset =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(pathName);

                ProjectWindowUtil.ShowCreatedAsset(sceneAsset);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
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

        string directory = Path.GetDirectoryName(path);

        return string.IsNullOrEmpty(directory)
            ? "Assets"
            : directory.Replace("\\", "/");
    }
}