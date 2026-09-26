using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Передвижение))]
[AddComponentMenu("  Runity/ Передвижение/Бег")]
public class Бег : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Передвижение>();
    void OnValidate() => Awake();
    public Передвижение оригинал => (Передвижение)оригинальныйКомпонент;

    [SerializeField] float _увеличениеСкорости = 2;
    public float увеличениеСкорости { get => _увеличениеСкорости; set => _увеличениеСкорости = value; }

    void Update()
    {
        if (Управление.УскорениеНажато)
        {
            оригинал.скорость += увеличениеСкорости;
        }
        if (Управление.УскорениеОтжато)
        {
            оригинал.скорость -= увеличениеСкорости;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Бег))]
public class БегEditor : RunityEditor<Бег>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект может ускоряться при удержании клавиши Shift");

        Пробел();

        Поле("Увеличение скорости", x => x.увеличениеСкорости,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif
