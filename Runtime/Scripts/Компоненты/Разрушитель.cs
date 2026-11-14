#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Удаляет объект через указанное время
/// </summary>
[AddComponentMenu("  Runity/ Работа с объектами/ Создание и удаление/Разрушитель")]
public class Разрушитель : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] GameObject _цель;
    /// <summary>
    /// Какой объект удалить. Можно взять объект со сцены. Если не указать, то удалится этот объект
    /// </summary>
    public GameObject цель { get => _цель; set => _цель = value; }

    [SerializeField] float _таймер;
    /// <summary>
    /// Через сколько секунд удалить
    /// </summary>
    public float таймер { get => _таймер; set => _таймер = value; }

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
internal class РазрушительEditor : RunityEditor<Разрушитель>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект удалит другой объект через указанное время. Если не указан другой объект, то удалит себя");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => ПолеОбъекта<GameObject>(title, value, true));

        Поле("Таймер", x => x.таймер,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif
