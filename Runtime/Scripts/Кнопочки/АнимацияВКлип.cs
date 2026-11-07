#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Превращает Кадровую анимацию в обычный анимационный клип Unity, чтобы использовать её в Animator Controller и Timeline
/// </summary>
public static class АнимацияВКлип
{
    [MenuItem("Assets/Runity/Превратить в анимационный клип", false, 2000)]
    static void ДляСпрайтера() => ПревратитьВыбранные(typeof(SpriteRenderer));

    [MenuItem("Assets/Runity/Превратить в анимационный клип для интерфейса", false, 2001)]
    static void ДляИнтерфейса() => ПревратитьВыбранные(typeof(Image));

    [MenuItem("Assets/Runity/Превратить в анимационный клип", true)]
    [MenuItem("Assets/Runity/Превратить в анимационный клип для интерфейса", true)]
    static bool ВыбранаАнимация() => Selection.activeObject is КадроваяАнимация;

    static void ПревратитьВыбранные(System.Type дляЧего)
    {
        AnimationClip последний = null;
        foreach (Object объект in Selection.objects)
        {
            if (объект is КадроваяАнимация анимация) последний = Превратить(анимация, дляЧего);
        }
        if (последний != null)
        {
            Selection.activeObject = последний;
            EditorGUIUtility.PingObject(последний);
        }
    }

    /// <summary>
    /// Создаёт рядом с Кадровой анимацией файл .anim с теми же кадрами и скоростью
    /// </summary>
    public static AnimationClip Превратить(КадроваяАнимация анимация, System.Type дляЧего)
    {
        List<Sprite> кадры = Кадры(анимация);
        if (кадры.Count == 0)
        {
            EditorUtility.DisplayDialog("Runity", "В этой анимации нет кадров", "Понятно");
            return null;
        }

        AnimationClip клип = new AnimationClip { frameRate = анимация.кадровВСекунду };
        EditorCurveBinding привязка = EditorCurveBinding.PPtrCurve("", дляЧего, "m_Sprite");

        ObjectReferenceKeyframe[] ключи = new ObjectReferenceKeyframe[кадры.Count + 1];
        float длительностьКадра = 1f / анимация.кадровВСекунду;
        for (int i = 0; i < кадры.Count; i++)
        {
            ключи[i] = new ObjectReferenceKeyframe { time = i * длительностьКадра, value = кадры[i] };
        }
        // Последний кадр должен показываться столько же, сколько остальные
        ключи[кадры.Count] = new ObjectReferenceKeyframe { time = кадры.Count * длительностьКадра, value = кадры[кадры.Count - 1] };

        AnimationUtility.SetObjectReferenceCurve(клип, привязка, ключи);

        AnimationClipSettings настройки = AnimationUtility.GetAnimationClipSettings(клип);
        настройки.loopTime = анимация.повторять;
        AnimationUtility.SetAnimationClipSettings(клип, настройки);

        string путь = AssetDatabase.GetAssetPath(анимация);
        string суффикс = дляЧего == typeof(Image) ? " (интерфейс)" : "";
        string путьКлипа = AssetDatabase.GenerateUniqueAssetPath($"{Path.GetDirectoryName(путь)}/{анимация.name}{суффикс}.anim".Replace("\\", "/"));
        AssetDatabase.CreateAsset(клип, путьКлипа);
        AssetDatabase.SaveAssets();
        return клип;
    }

    // Кадры-картинки становятся спрайтами, потому что анимационный клип умеет менять только спрайты
    static List<Sprite> Кадры(КадроваяАнимация анимация)
    {
        if (анимация.спрайты.Count > 0) return new List<Sprite>(анимация.спрайты);

        List<Sprite> кадры = new List<Sprite>();
        foreach (Texture2D картинка in анимация.картинки)
        {
            if (картинка == null) continue;

            string путь = AssetDatabase.GetAssetPath(картинка);
            Sprite спрайт = AssetDatabase.LoadAssetAtPath<Sprite>(путь);
            if (спрайт == null && AssetImporter.GetAtPath(путь) is TextureImporter импортёр)
            {
                импортёр.textureType = TextureImporterType.Sprite;
                импортёр.spriteImportMode = SpriteImportMode.Single;
                импортёр.SaveAndReimport();
                спрайт = AssetDatabase.LoadAssetAtPath<Sprite>(путь);
            }
            if (спрайт != null) кадры.Add(спрайт);
        }
        return кадры;
    }
}
#endif
