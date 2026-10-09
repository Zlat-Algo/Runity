using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТело2D))]
[AddComponentMenu("  Runity/ Передвижение/Передвижение 2D сбоку")]
public class Передвижение2DСбоку : Передвижение
{
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТело2D>();
    void OnValidate() => Awake();
    public ФизическоеТело2D оригинал => (ФизическоеТело2D)оригинальныйКомпонент;

    List<Collider2D> contacts = new List<Collider2D>();
    
    [SerializeField] float _силаПрыжка = 7;
    public float силаПрыжка { get => _силаПрыжка; set => _силаПрыжка = value; }

    void Update()
    {
        if (Управление.вперёдУдерживается && contacts.Count > 0)
        {
            оригинал.движениеY = силаПрыжка;
        }

        оригинал.движениеX = 
            (Управление.вправоУдерживается ? скорость : 0) +
            (Управление.влевоУдерживается ? -скорость : 0);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (contacts.IndexOf(collision.collider) == -1)
        {
            if (TryGetComponent(out Collider2D myCollider) && collision.otherCollider == myCollider)
            {
                contacts.Add(collision.collider);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (TryGetComponent(out Collider2D myCollider) && collision.otherCollider == myCollider)
        {
            contacts.Remove(collision.collider);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Передвижение2DСбоку))]
internal class Передвижение2DСбокуEditor : RunityEditor<Передвижение2DСбоку>
{
    public override void OnInspectorGUI()
    {
        Текст("Этим объектом может управлять игрок, чтобы перемещаться как в платформерах");

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Сила прыжка", x => x.силаПрыжка,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif