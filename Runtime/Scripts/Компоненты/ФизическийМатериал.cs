#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Как сочетаются трение или упругость двух касающихся объектов
/// </summary>
public enum СочетаниеФизики { Среднее, Наименьшее, Наибольшее, Произведение }

/// <summary>
/// Физический материал: насколько поверхность скользкая и прыгучая. Один материал работает и в 3D, и в 2D: лёд, резина, батут, липкая грязь.
/// Перетащите его в поле «Материал» любого коллайдера
/// </summary>
public class ФизическийМатериал : ScriptableObject
{
    [SerializeField, HideInInspector] PhysicsMaterial _для3D;
    [SerializeField, HideInInspector] PhysicsMaterial2D _для2D;

    [SerializeField, Диапазон(0, 1)] float _трение = 0.6f;
    /// <summary>
    /// Насколько поверхность шершавая: 0 - скользит, как лёд, 1 - почти не скользит
    /// </summary>
    public Дробное трение { get => _трение; set { _трение = Mathf.Clamp01(value); Обновить(); } }

    [SerializeField, Диапазон(0, 1)] float _упругость = 0;
    /// <summary>
    /// Насколько объект отскакивает: 0 - не отскакивает, 1 - отскакивает так же высоко, как падал (батут, мячик)
    /// </summary>
    public Дробное упругость { get => _упругость; set { _упругость = Mathf.Clamp01(value); Обновить(); } }

    [SerializeField] СочетаниеФизики _сочетаниеТрения = СочетаниеФизики.Среднее;
    /// <summary>
    /// Как сочетается трение с поверхностью другого объекта. «Наименьшее» - лёд скользкий, даже если на него наступили резиной
    /// </summary>
    public СочетаниеФизики сочетаниеТрения { get => _сочетаниеТрения; set { _сочетаниеТрения = value; Обновить(); } }

    [SerializeField] СочетаниеФизики _сочетаниеУпругости = СочетаниеФизики.Среднее;
    /// <summary>
    /// Как сочетается упругость с поверхностью другого объекта. «Наибольшее» - батут пружинит всегда
    /// </summary>
    public СочетаниеФизики сочетаниеУпругости { get => _сочетаниеУпругости; set { _сочетаниеУпругости = value; Обновить(); } }

    /// <summary>
    /// Настоящий физический материал Unity для 3D
    /// </summary>
    public PhysicsMaterial для3D { get { Создать(); return _для3D; } }

    /// <summary>
    /// Настоящий физический материал Unity для 2D
    /// </summary>
    public PhysicsMaterial2D для2D { get { Создать(); return _для2D; } }

    public static implicit operator PhysicsMaterial(ФизическийМатериал материал) => материал != null ? материал.для3D : null;
    public static implicit operator PhysicsMaterial2D(ФизическийМатериал материал) => материал != null ? материал.для2D : null;

    static PhysicsMaterialCombine Сочетание(СочетаниеФизики сочетание) => сочетание switch
    {
        СочетаниеФизики.Наименьшее => PhysicsMaterialCombine.Minimum,
        СочетаниеФизики.Наибольшее => PhysicsMaterialCombine.Maximum,
        СочетаниеФизики.Произведение => PhysicsMaterialCombine.Multiply,
        _ => PhysicsMaterialCombine.Average
    };

    // Материалы Unity хранятся внутри этого ассета, поэтому не теряются и попадают в сборку
    void Создать()
    {
        bool новые = _для3D == null || _для2D == null;
        if (_для3D == null) _для3D = new PhysicsMaterial(name) { hideFlags = HideFlags.NotEditable };
        if (_для2D == null) _для2D = new PhysicsMaterial2D(name) { hideFlags = HideFlags.NotEditable };
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) && (!AssetDatabase.Contains(_для3D) || !AssetDatabase.Contains(_для2D)))
        {
            // Пока Unity импортирует ассеты, добавлять в них ничего нельзя - добавим чуть позже
            if (EditorApplication.isUpdating) EditorApplication.delayCall += () => { if (this != null) Создать(); };
            else
            {
                if (!AssetDatabase.Contains(_для3D)) AssetDatabase.AddObjectToAsset(_для3D, this);
                if (!AssetDatabase.Contains(_для2D)) AssetDatabase.AddObjectToAsset(_для2D, this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }
#endif
        if (новые) Обновить();
    }

    /// <summary>
    /// Переносит настройки в материалы Unity
    /// </summary>
    public void Обновить()
    {
        if (_для3D != null)
        {
            _для3D.dynamicFriction = _трение;
            _для3D.staticFriction = _трение;
            _для3D.bounciness = _упругость;
            _для3D.frictionCombine = Сочетание(_сочетаниеТрения);
            _для3D.bounceCombine = Сочетание(_сочетаниеУпругости);
            _для3D.name = name;
        }
        if (_для2D != null)
        {
            _для2D.friction = _трение;
            _для2D.bounciness = _упругость;
            _для2D.name = name;
        }
#if UNITY_EDITOR
        if (_для3D != null) EditorUtility.SetDirty(_для3D);
        if (_для2D != null) EditorUtility.SetDirty(_для2D);
#endif
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () => { if (this != null) Создать(); };
#endif
        Обновить();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ФизическийМатериал))]
internal class ФизическийМатериалEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Перетащите этот материал в поле «Материал» любого коллайдера - 3D или 2D", MessageType.None);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_трение"), new GUIContent("Трение", Справка.Описание(typeof(ФизическийМатериал), "трение")));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_упругость"), new GUIContent("Упругость", Справка.Описание(typeof(ФизическийМатериал), "упругость")));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_сочетаниеТрения"), new GUIContent("Сочетание трения", Справка.Описание(typeof(ФизическийМатериал), "сочетаниеТрения")));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_сочетаниеУпругости"), new GUIContent("Сочетание упругости", Справка.Описание(typeof(ФизическийМатериал), "сочетаниеУпругости")));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
