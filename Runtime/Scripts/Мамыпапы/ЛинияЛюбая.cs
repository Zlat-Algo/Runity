using UnityEngine;

/// <summary>
/// Общие настройки для Линии и Следа: материал по умолчанию, который видно в URP
/// </summary>
internal static class МатериалЛиний
{
    internal static void ПроверитьМатериал(Renderer отрисовщик)
    {
#if UNITY_EDITOR
        if (отрисовщик != null && отрисовщик.sharedMaterial == null)
        {
            отрисовщик.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        }
#endif
    }
}
