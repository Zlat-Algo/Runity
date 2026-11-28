#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// Объекты, созданные из меню Runity, показывают в инспекторе развёрнутыми только компоненты Runity.
// Компоненты Unity, на которых они построены, свёрнуты: их настройки и так есть в разделе «Прочее»
/// <summary>
/// Сворачивает в инспекторе компоненты Unity у объектов, созданных из меню Runity
/// </summary>
public static class СвёрнутыеКомпоненты
{
    /// <summary>
    /// Сворачивает у объекта и его детей все компоненты, кроме компонентов Runity
    /// </summary>
    public static void Свернуть(Object объект)
    {
        if (объект is Component компонент) Свернуть(компонент.gameObject);
        else if (объект is GameObject игровой) Свернуть(игровой);
    }

    /// <summary>
    /// Сворачивает у объекта и его детей все компоненты, кроме компонентов Runity
    /// </summary>
    public static void Свернуть(GameObject объект)
    {
        if (объект == null) return;
        // Компоненты ещё добавляются, поэтому сворачиваем после того, как меню закончит работу
        EditorApplication.delayCall += () =>
        {
            if (объект == null) return;
            foreach (Component компонент in объект.GetComponentsInChildren<Component>(true))
            {
                if (компонент == null || компонент is Transform || компонент is Компонент) continue;
                InternalEditorUtility.SetIsInspectorExpanded(компонент, false);
            }
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        };
    }
}
#endif
