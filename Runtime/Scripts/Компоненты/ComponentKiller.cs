using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class ComponentKiller : Компонент
{
    protected void OnEnable() { }
    protected void OnDisable() { }

    public Component pretargetComponent;
    public Component targetComponent;

    public void SetTargets((Component, Component) targets)
    {
        pretargetComponent = targets.Item1;
        targetComponent = targets.Item2;
    }

    void Update()
    {
#if !UNITY_EDITOR
        if (pretargetComponent != null)
            Destroy(pretargetComponent);
        Destroy(targetComponent);
        Destroy(this);
#endif
#if UNITY_EDITOR
        if (pretargetComponent != null)
            DestroyImmediate(pretargetComponent);
        DestroyImmediate(targetComponent);
        DestroyImmediate(this);
#endif
    }
}
