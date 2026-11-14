using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Анимация из отдельных картинок, спрайтов из атласа или гифки. Проигрывается компонентом Проигрыватель кадров
/// </summary>
[CreateAssetMenu(fileName = "Новая анимация", menuName = "Runity/Кадровая анимация", order = -100)]
public class КадроваяАнимация : ScriptableObject
{
    [SerializeField, FormerlySerializedAs("frames")]
    List<Texture2D> _картинки = new List<Texture2D>();

    [SerializeField]
    List<Sprite> _спрайты = new List<Sprite>();

    [SerializeField, Min(0.01f), FormerlySerializedAs("framesPerSecond")]
    float _кадровВСекунду = 12f;

    [SerializeField, FormerlySerializedAs("loop")]
    bool _повторять = true;

    [SerializeField, Min(0.01f), FormerlySerializedAs("pixelsPerUnit")]
    float _пикселейНаЕдиницу = 100f;

    [NonSerialized] Sprite[] спрайтыИзКартинок;
    [NonSerialized] Texture2D[] картинкиВКэше;

    /// <summary>
    /// Кадры из отдельных спрайтов. Если они есть, то Картинки не используются
    /// </summary>
    public List<Sprite> спрайты => _спрайты;

    /// <summary>
    /// Кадры из отдельных картинок
    /// </summary>
    public List<Texture2D> картинки => _картинки;

    /// <summary>
    /// Сколько всего кадров в анимации
    /// </summary>
    public int количествоКадров => _спрайты.Count > 0 ? _спрайты.Count : _картинки.Count;

    /// <summary>
    /// Сколько кадров показывается за секунду
    /// </summary>
    public float кадровВСекунду { get => Mathf.Max(0.01f, _кадровВСекунду); set => _кадровВСекунду = value; }

    /// <summary>
    /// Начинается ли анимация заново, когда доиграла
    /// </summary>
    public bool повторять { get => _повторять; set => _повторять = value; }

    /// <summary>
    /// Сколько пикселей картинки занимает одну единицу в мире. Используется только для кадров из Картинок
    /// </summary>
    public float пикселейНаЕдиницу => Mathf.Max(0.01f, _пикселейНаЕдиницу);

    /// <summary>
    /// Даёт спрайт нужного кадра
    /// </summary>
    public Sprite ПолучитьСпрайт(int номер)
    {
        if (_спрайты.Count > 0)
        {
            return номер >= 0 && номер < _спрайты.Count ? _спрайты[номер] : null;
        }

        if (номер < 0 || номер >= _картинки.Count || _картинки[номер] == null)
        {
            return null;
        }

        Texture2D картинка = _картинки[номер];

        ПодготовитьКэш();

        if (спрайтыИзКартинок[номер] != null && картинкиВКэше[номер] == картинка)
        {
            return спрайтыИзКартинок[номер];
        }

        УдалитьСпрайт(спрайтыИзКартинок[номер]);

        спрайтыИзКартинок[номер] = Sprite.Create(
            картинка,
            new Rect(0, 0, картинка.width, картинка.height),
            new Vector2(0.5f, 0.5f),
            пикселейНаЕдиницу
        );

        спрайтыИзКартинок[номер].name = $"{name}_Кадр_{номер}";
        картинкиВКэше[номер] = картинка;

        return спрайтыИзКартинок[номер];
    }

    void ПодготовитьКэш()
    {
        if (спрайтыИзКартинок != null && спрайтыИзКартинок.Length == _картинки.Count)
        {
            return;
        }

        ОчиститьКэш();

        спрайтыИзКартинок = new Sprite[_картинки.Count];
        картинкиВКэше = new Texture2D[_картинки.Count];
    }

    void ОчиститьКэш()
    {
        if (спрайтыИзКартинок != null)
        {
            foreach (Sprite спрайт in спрайтыИзКартинок)
            {
                УдалитьСпрайт(спрайт);
            }
        }

        спрайтыИзКартинок = null;
        картинкиВКэше = null;
    }

    void OnDisable()
    {
        ОчиститьКэш();
    }

    static void УдалитьСпрайт(Sprite спрайт)
    {
        if (спрайт == null) return;

        if (Application.isPlaying) Destroy(спрайт);
        else DestroyImmediate(спрайт);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(КадроваяАнимация))]
internal class КадроваяАнимацияEditor : Editor
{
    // Картинка в окне Project: первый кадр анимации
    public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
    {
        КадроваяАнимация анимация = (КадроваяАнимация)target;
        Sprite первый = анимация.спрайты.Count > 0 || анимация.картинки.Count > 0 ? анимация.ПолучитьСпрайт(0) : null;
        return ПревьюАссетов.ИзСпрайта(первый, width, height) ?? base.RenderStaticPreview(assetPath, subAssets, width, height);
    }

    bool просмотрИдёт;
    int кадрПросмотра;
    double времяПрошлогоКадра;
    Texture2D атлас;

    КадроваяАнимация анимация => (КадроваяАнимация)target;

    void OnEnable() => EditorApplication.update += ОбновитьПросмотр;

    void OnDisable()
    {
        EditorApplication.update -= ОбновитьПросмотр;
        просмотрИдёт = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Анимация из кадров. Кадрами могут быть спрайты, отдельные картинки, спрайты из атласа или кадры гифки. Проигрывает её компонент Проигрыватель кадров", MessageType.None);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_кадровВСекунду"), new GUIContent("Кадров в секунду"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_повторять"), new GUIContent("Повторять"));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Откуда взять кадры", EditorStyles.boldLabel);

        if (GUILayout.Button("Импортировать из гифки..."))
        {
            ИмпортГифки.ВыбратьИИмпортировать(анимация);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.BeginHorizontal();
        атлас = (Texture2D)EditorGUILayout.ObjectField("Атлас", атлас, typeof(Texture2D), false);
        using (new EditorGUI.DisabledScope(атлас == null))
        {
            if (GUILayout.Button("Взять кадры", GUILayout.Width(100)))
            {
                ВзятьКадрыИзАтласа();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_спрайты"), new GUIContent("Спрайты"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_картинки"), new GUIContent("Картинки"), true);

        if (анимация.спрайты.Count == 0)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_пикселейНаЕдиницу"), new GUIContent("Пикселей на единицу"));
        }

        serializedObject.ApplyModifiedProperties();

        НарисоватьПросмотр();
    }

    void ВзятьКадрыИзАтласа()
    {
        List<Sprite> кадры = ИмпортГифки.СпрайтыАтласа(атлас);

        if (кадры.Count == 0)
        {
            EditorUtility.DisplayDialog("Runity", "В этом файле нет спрайтов. Выберите у него Sprite Mode: Multiple и разрежьте его в Sprite Editor", "Понятно");
            return;
        }

        Undo.RecordObject(анимация, "Взять кадры из атласа");
        анимация.спрайты.Clear();
        анимация.спрайты.AddRange(кадры);
        EditorUtility.SetDirty(анимация);
    }

    void НарисоватьПросмотр()
    {
        int количество = анимация.количествоКадров;

        if (количество == 0)
        {
            кадрПросмотра = 0;
            просмотрИдёт = false;
            EditorGUILayout.HelpBox("Добавьте кадры, и здесь появится просмотр анимации", MessageType.Info);
            return;
        }

        кадрПросмотра = Mathf.Clamp(кадрПросмотра, 0, количество - 1);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Просмотр", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(просмотрИдёт ? "Пауза" : "Играть"))
        {
            просмотрИдёт = !просмотрИдёт;
            времяПрошлогоКадра = EditorApplication.timeSinceStartup;
        }
        if (GUILayout.Button("Первый кадр"))
        {
            кадрПросмотра = 0;
        }
        if (GUILayout.Button("Следующий кадр"))
        {
            кадрПросмотра = (кадрПросмотра + 1) % количество;
        }
        EditorGUILayout.EndHorizontal();

        кадрПросмотра = EditorGUILayout.IntSlider("Кадр", кадрПросмотра, 0, количество - 1);

        Sprite спрайт = анимация.ПолучитьСпрайт(кадрПросмотра);

        if (спрайт != null)
        {
            Rect область = GUILayoutUtility.GetRect(100f, 220f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(область, new Color(0.15f, 0.15f, 0.15f));

            Rect кусочек = спрайт.textureRect;
            Texture2D текстура = спрайт.texture;
            Rect координаты = new Rect(кусочек.x / текстура.width, кусочек.y / текстура.height, кусочек.width / текстура.width, кусочек.height / текстура.height);

            float масштаб = Mathf.Min(область.width / кусочек.width, область.height / кусочек.height);
            Vector2 размер = кусочек.size * масштаб;
            Rect картинка = new Rect(область.center - размер / 2, размер);

            GUI.DrawTextureWithTexCoords(картинка, текстура, координаты, true);
        }

        EditorGUILayout.LabelField($"Кадр {кадрПросмотра + 1} из {количество}");
    }

    void ОбновитьПросмотр()
    {
        if (!просмотрИдёт || target == null) return;

        int количество = анимация.количествоКадров;

        if (количество == 0)
        {
            просмотрИдёт = false;
            return;
        }

        double сейчас = EditorApplication.timeSinceStartup;
        double длительностьКадра = 1.0 / анимация.кадровВСекунду;

        if (сейчас - времяПрошлогоКадра < длительностьКадра) return;

        int шагов = Mathf.Max(1, (int)((сейчас - времяПрошлогоКадра) / длительностьКадра));
        времяПрошлогоКадра += шагов * длительностьКадра;
        кадрПросмотра += шагов;

        if (анимация.повторять)
        {
            кадрПросмотра %= количество;
        }
        else if (кадрПросмотра >= количество - 1)
        {
            кадрПросмотра = количество - 1;
            просмотрИдёт = false;
        }

        Repaint();
    }
}
#endif
