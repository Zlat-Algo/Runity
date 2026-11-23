#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Узнаёт скелет человека по именам костей (Mixamo, Blender, 3ds Max, Asset Store, русские имена манекена Runity)
/// и строит аватар Humanoid. С ним анимации одних людей подходят другим
/// </summary>
public static class СкелетЧеловека
{
    // Части тела Unity и как их обычно называют. Сторона (лево/право) определяется отдельно
    static readonly (string часть, bool сторона, string[] имена)[] части =
    {
        ("Hips", false, new[] { "hips", "hip", "pelvis", "таз" }),
        ("Spine", false, new[] { "spine", "spine0", "spine01", "spine00", "поясница", "abdomen", "torso", "позвоночник" }),
        ("Chest", false, new[] { "chest", "spine1", "spine02", "грудь", "chest1" }),
        ("UpperChest", false, new[] { "upperchest", "spine2", "spine03", "верхгруди" }),
        ("Neck", false, new[] { "neck", "neck1", "neck01", "шея" }),
        ("Head", false, new[] { "head", "голова" }),
        ("Shoulder", true, new[] { "shoulder", "clavicle", "collar", "ключица" }),
        ("UpperArm", true, new[] { "arm", "upperarm", "uparm", "плечо" }),
        ("LowerArm", true, new[] { "forearm", "lowerarm", "elbow", "предплечье" }),
        ("Hand", true, new[] { "hand", "wrist", "кисть" }),
        ("UpperLeg", true, new[] { "upleg", "upperleg", "thigh", "бедро" }),
        ("LowerLeg", true, new[] { "leg", "lowerleg", "shin", "calf", "knee", "голень" }),
        ("Foot", true, new[] { "foot", "ankle", "стопа" }),
        ("Toes", true, new[] { "toe", "toes", "toebase", "ball", "носок" }),
    };

    static readonly string[] обязательные =
    {
        "Hips", "Spine", "Head", "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand",
        "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftFoot", "RightFoot"
    };

    static readonly string[] приставки = { "mixamorig", "bip01", "bip001", "b", "def", "bone", "jnt", "j", "cc", "base", "ctrl" };

    // Имя кости без стороны и приставок: "mixamorig:LeftForeArm" - ("forearm", лево), "Плечо.Л" - ("плечо", лево), "B-upper_arm.L" - ("upperarm", лево)
    static (string суть, int сторона) Разобрать(string имя)
    {
        string н = имя.ToLowerInvariant();
        int двоеточие = н.LastIndexOf(':');
        if (двоеточие >= 0) н = н.Substring(двоеточие + 1);
        int сторона = 0;
        if (н.Contains("left")) { сторона = -1; н = н.Replace("left", " "); }
        else if (н.Contains("right")) { сторона = 1; н = н.Replace("right", " "); }
        string[] слова = н.Split(new[] { ' ', '_', '.', '-', '|' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> остаток = new List<string>();
        foreach (string слово in слова)
        {
            if (слово == "l" || слово == "л" || слово == "lf" || слово == "лев") { сторона = -1; continue; }
            if (слово == "r" || слово == "п" || слово == "rt" || слово == "прав") { сторона = 1; continue; }
            if (приставки.Contains(слово)) continue;
            остаток.Add(слово);
        }
        return (string.Concat(остаток), сторона);
    }

    /// <summary>
    /// Какой кости модели соответствует каждая часть тела человека. Пусто, если скелет не похож на человеческий
    /// </summary>
    public static Dictionary<string, Transform> Найти(Transform корень)
    {
        Dictionary<string, Transform> найдено = new Dictionary<string, Transform>();
        Dictionary<string, int> глубина = new Dictionary<string, int>();
        foreach (Transform кость in корень.GetComponentsInChildren<Transform>(true))
        {
            if (кость == корень || кость.GetComponent<Renderer>() != null) continue;
            (string суть, int сторона) = Разобрать(кость.name);
            int уровень = 0;
            for (Transform t = кость; t != корень && t != null; t = t.parent) уровень++;
            foreach (var часть in части)
            {
                if (!часть.имена.Contains(суть)) continue;
                if (часть.сторона && сторона == 0) continue;
                string ключ = часть.сторона ? (сторона < 0 ? "Left" : "Right") + часть.часть : часть.часть;
                // Из похожих берём ближайшую к корню: «Spine» раньше «Spine1»
                if (!найдено.ContainsKey(ключ) || уровень < глубина[ключ]) { найдено[ключ] = кость; глубина[ключ] = уровень; }
            }
        }
        return обязательные.All(найдено.ContainsKey) ? найдено : new Dictionary<string, Transform>();
    }

    /// <summary>
    /// Строит аватар Humanoid по текущей позе модели (лучше всего - поза буквой Т). Даёт null, если скелет не человеческий
    /// </summary>
    public static Avatar Построить(Transform корень)
    {
        Dictionary<string, Transform> кости = Найти(корень);
        if (кости.Count == 0) return null;

        Transform[] все = корень.GetComponentsInChildren<Transform>(true);
        if (все.Select(t => t.name).Distinct().Count() != все.Length) return null; // Аватару нужны разные имена костей

        HumanDescription описание = new HumanDescription
        {
            human = кости.Select(п => new HumanBone { humanName = ИмяЧасти(п.Key), boneName = п.Value.name, limit = new HumanLimit { useDefaultValues = true } }).ToArray(),
            skeleton = все.Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray(),
            upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
            armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0, hasTranslationDoF = false,
        };
        Avatar аватар = AvatarBuilder.BuildHumanAvatar(корень.gameObject, описание);
        if (аватар == null || !аватар.isValid || !аватар.isHuman)
        {
            if (аватар != null) UnityEngine.Object.DestroyImmediate(аватар);
            return null;
        }
        аватар.name = корень.name + " (скелет человека)";
        return аватар;
    }

    // В HumanTrait части называются с пробелами: «Left Upper Arm»
    static string ИмяЧасти(string ключ)
    {
        string сПробелами = string.Concat(ключ.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
        return HumanTrait.BoneName.FirstOrDefault(имя => имя.Replace(" ", "") == ключ) ?? сПробелами;
    }

    /// <summary>
    /// Переводит обычную анимацию модели в анимацию Humanoid (мышцы вместо поворотов костей), чтобы она подходила другим людям
    /// </summary>
    public static AnimationClip ВЧеловеческий(AnimationClip клип, Avatar аватар, Transform корень)
    {
        if (клип == null || аватар == null || !аватар.isHuman) return null;
        Transform[] все = корень.GetComponentsInChildren<Transform>(true);
        (Vector3, Quaternion, Vector3)[] покой = все.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();

        HumanPoseHandler поза = new HumanPoseHandler(аватар, корень);
        try
        {
            float частота = 30;
            int кадров = Mathf.Max(2, Mathf.CeilToInt(клип.length * частота) + 1);
            int мышц = HumanTrait.MuscleCount;
            Keyframe[][] мышцы = new Keyframe[мышц][];
            for (int m = 0; m < мышц; m++) мышцы[m] = new Keyframe[кадров];
            Keyframe[][] тело = new Keyframe[7][];
            for (int k = 0; k < 7; k++) тело[k] = new Keyframe[кадров];

            HumanPose сейчас = new HumanPose();
            for (int f = 0; f < кадров; f++)
            {
                float время = Mathf.Min(клип.length, f / частота);
                клип.SampleAnimation(корень.gameObject, время);
                поза.GetHumanPose(ref сейчас);
                for (int m = 0; m < мышц; m++) мышцы[m][f] = new Keyframe(время, сейчас.muscles[m]);
                тело[0][f] = new Keyframe(время, сейчас.bodyPosition.x);
                тело[1][f] = new Keyframe(время, сейчас.bodyPosition.y);
                тело[2][f] = new Keyframe(время, сейчас.bodyPosition.z);
                тело[3][f] = new Keyframe(время, сейчас.bodyRotation.x);
                тело[4][f] = new Keyframe(время, сейчас.bodyRotation.y);
                тело[5][f] = new Keyframe(время, сейчас.bodyRotation.z);
                тело[6][f] = new Keyframe(время, сейчас.bodyRotation.w);
            }

            AnimationClip человеческий = new AnimationClip { name = клип.name, frameRate = частота };
            for (int m = 0; m < мышц; m++)
                AnimationUtility.SetEditorCurve(человеческий, EditorCurveBinding.FloatCurve("", typeof(Animator), HumanTrait.MuscleName[m]), Гладкая(мышцы[m]));
            string[] корни = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
            for (int k = 0; k < 7; k++)
                AnimationUtility.SetEditorCurve(человеческий, EditorCurveBinding.FloatCurve("", typeof(Animator), корни[k]), Гладкая(тело[k]));

            // Всё, что не кости (формы лица, видимость и прочее), переносится как было
            foreach (EditorCurveBinding привязка in AnimationUtility.GetCurveBindings(клип))
                if (привязка.type != typeof(Transform)) AnimationUtility.SetEditorCurve(человеческий, привязка, AnimationUtility.GetEditorCurve(клип, привязка));

            AnimationClipSettings настройки = AnimationUtility.GetAnimationClipSettings(клип);
            настройки.keepOriginalOrientation = true;
            настройки.keepOriginalPositionY = true;
            настройки.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(человеческий, настройки);
            return человеческий;
        }
        finally
        {
            поза.Dispose();
            for (int i = 0; i < все.Length; i++) { все[i].localPosition = покой[i].Item1; все[i].localRotation = покой[i].Item2; все[i].localScale = покой[i].Item3; }
        }
    }

    static AnimationCurve Гладкая(Keyframe[] ключи)
    {
        AnimationCurve кривая = new AnimationCurve(ключи);
        for (int i = 0; i < ключи.Length; i++) AnimationUtility.SetKeyLeftTangentMode(кривая, i, AnimationUtility.TangentMode.ClampedAuto);
        for (int i = 0; i < ключи.Length; i++) AnimationUtility.SetKeyRightTangentMode(кривая, i, AnimationUtility.TangentMode.ClampedAuto);
        return кривая;
    }
}
#endif
