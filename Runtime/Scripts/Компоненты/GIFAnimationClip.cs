using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New GIF Animation Clip",
    menuName = "Animations/GIF Animation Clip"
)]
public class GIFAnimationClip : ScriptableObject
{
    [SerializeField]
    private List<Texture2D> frames = new List<Texture2D>();

    [SerializeField, Min(0.01f)]
    private float framesPerSecond = 12f;

    [SerializeField]
    private bool loop = true;

    [SerializeField, Min(0.01f)]
    private float pixelsPerUnit = 100f;

    [NonSerialized]
    private Sprite[] runtimeSprites;

    [NonSerialized]
    private Texture2D[] cachedTextures;

    public int FrameCount => frames != null ? frames.Count : 0;

    public float FramesPerSecond => Mathf.Max(0.01f, framesPerSecond);

    public bool Loop => loop;

    public float PixelsPerUnit => Mathf.Max(0.01f, pixelsPerUnit);

    public Texture2D GetTexture(int index)
    {
        if (frames == null || index < 0 || index >= frames.Count)
            return null;

        return frames[index];
    }

    public Sprite GetSprite(int index)
    {
        Texture2D texture = GetTexture(index);

        if (texture == null)
            return null;

        EnsureCache();

        if (runtimeSprites[index] != null &&
            cachedTextures[index] == texture)
        {
            return runtimeSprites[index];
        }

        DestroySprite(runtimeSprites[index]);

        runtimeSprites[index] = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit
        );

        runtimeSprites[index].name = $"{name}_Frame_{index}";
        cachedTextures[index] = texture;

        return runtimeSprites[index];
    }

    private void EnsureCache()
    {
        if (runtimeSprites != null &&
            cachedTextures != null &&
            runtimeSprites.Length == FrameCount)
        {
            return;
        }

        ReleaseCache();

        runtimeSprites = new Sprite[FrameCount];
        cachedTextures = new Texture2D[FrameCount];
    }

    private void ReleaseCache()
    {
        if (runtimeSprites != null)
        {
            foreach (Sprite sprite in runtimeSprites)
                DestroySprite(sprite);
        }

        runtimeSprites = null;
        cachedTextures = null;
    }

    private void OnDisable()
    {
        ReleaseCache();
    }

    private static void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (Application.isPlaying)
            Destroy(sprite);
        else
            DestroyImmediate(sprite);
    }
}



[CustomEditor(typeof(GIFAnimationClip))]
public class GIFAnimationClipEditor : Editor
{
    private SerializedProperty framesProperty;
    private SerializedProperty fpsProperty;
    private SerializedProperty loopProperty;
    private SerializedProperty pixelsPerUnitProperty;

    private bool previewPlaying;
    private int previewFrame;
    private double lastPreviewTime;

    private GIFAnimationClip Clip => (GIFAnimationClip)target;

    private void OnEnable()
    {
        framesProperty = serializedObject.FindProperty("frames");
        fpsProperty = serializedObject.FindProperty("framesPerSecond");
        loopProperty = serializedObject.FindProperty("loop");
        pixelsPerUnitProperty = serializedObject.FindProperty("pixelsPerUnit");

        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        previewPlaying = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(fpsProperty);
        EditorGUILayout.PropertyField(loopProperty);
        EditorGUILayout.PropertyField(pixelsPerUnitProperty);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Animation Frames", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(framesProperty, true);

        serializedObject.ApplyModifiedProperties();

        int count = Clip.FrameCount;

        if (count == 0)
        {
            previewFrame = 0;
            previewPlaying = false;

            EditorGUILayout.HelpBox(
                "Добавь текстуры в список Frames.",
                MessageType.Info
            );

            return;
        }

        previewFrame = Mathf.Clamp(previewFrame, 0, count - 1);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(previewPlaying ? "Pause" : "Play"))
        {
            previewPlaying = !previewPlaying;
            lastPreviewTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("First Frame"))
        {
            previewFrame = 0;
        }

        if (GUILayout.Button("Next Frame"))
        {
            previewFrame = (previewFrame + 1) % count;
        }

        EditorGUILayout.EndHorizontal();

        previewFrame = EditorGUILayout.IntSlider(
            "Frame",
            previewFrame,
            0,
            count - 1
        );

        Texture2D texture = Clip.GetTexture(previewFrame);

        if (texture != null)
        {
            float aspect = (float)texture.width / texture.height;
            aspect = Mathf.Max(0.01f, aspect);

            float height = Mathf.Min(300f, 220f / aspect);

            Rect rect = GUILayoutUtility.GetRect(
                100f,
                height,
                GUILayout.ExpandWidth(true)
            );

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            GUI.DrawTexture(
                rect,
                texture,
                ScaleMode.ScaleToFit,
                true
            );
        }

        EditorGUILayout.LabelField(
            $"Frame {previewFrame + 1} / {count}"
        );

        Repaint();
    }

    private void UpdatePreview()
    {
        if (!previewPlaying || target == null)
            return;

        int count = Clip.FrameCount;

        if (count == 0)
        {
            previewPlaying = false;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double frameDuration = 1.0 / Clip.FramesPerSecond;

        if (now - lastPreviewTime >= frameDuration)
        {
            int steps = Mathf.Max(
                1,
                (int)((now - lastPreviewTime) / frameDuration)
            );

            lastPreviewTime += steps * frameDuration;
            previewFrame += steps;

            if (Clip.Loop)
            {
                previewFrame %= count;
            }
            else if (previewFrame >= count - 1)
            {
                previewFrame = count - 1;
                previewPlaying = false;
            }

            Repaint();
        }
    }
}
