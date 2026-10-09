using UnityEngine;

[DisallowMultipleComponent]
public abstract class Передвижение : RunityComponent
{
    [SerializeField] float _скорость = 5;
    public float скорость { get => _скорость; set => _скорость = value; }
}
