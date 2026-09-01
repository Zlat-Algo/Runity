using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Физика 3D/Физическое тело")]
public class ФизическоеТело : MonoBehaviourPlus
{
    public Rigidbody оригинал { get; private set; }

    void Awake() => OnValidate();

    void OnValidate()
    {
        оригинал = GetComponent<Rigidbody>();
    }
}