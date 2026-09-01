using UnityEditor;
using UnityEngine;

[AddComponentMenu("  Runity/ Работа с объектами/Разрушитель")]
public class Разрушитель : RunityComponent
{
    public GameObject цель { get => _цель; set => _цель = value; }
    [SerializeField] GameObject _цель;
    public float таймер { get => _таймер; set => _таймер = value; }
    [SerializeField] float _таймер;

    void Start()
    {
        if (цель == null)
        {
            Destroy(gameObject, таймер);
        }
        else
        {
            Destroy(цель, таймер);
            Destroy(this, таймер);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Разрушитель))]
public class РазрушительEditor : RunityEditor<Разрушитель>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект удалит другой объект через указанное время. Если не указан другой объект, то удалит себя");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => (GameObject)EditorGUILayout.ObjectField(title, value, typeof(GameObject), false));

        Поле("Таймер", x => x.таймер,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif