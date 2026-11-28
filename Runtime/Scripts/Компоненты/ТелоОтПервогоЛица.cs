using UnityEngine;

/// <summary>
/// Тело персонажа для вида от первого лица: голова прячется, чтобы камера не смотрела изнутри неё, а остальное тело видно,
/// если посмотреть вниз. Ставится на модель человека (Humanoid) внутри Игрока
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Управление игроком/Тело от первого лица")]
public class ТелоОтПервогоЛица : Компонент
{
    [SerializeField] Флажок _прятатьГолову = да;
    /// <summary>
    /// Если "да", голова (а с ней волосы, шлем и борода) не рисуется, чтобы не загораживать камеру
    /// </summary>
    public Флажок прятатьГолову { get => _прятатьГолову; set => _прятатьГолову = value; }

    Transform голова;

    void Start()
    {
        Animator аниматор = GetComponentInChildren<Animator>();
        if (аниматор != null && аниматор.isHuman) голова = аниматор.GetBoneTransform(HumanBodyBones.Head);
    }

    // Анимации человека не меняют размер костей, поэтому голову достаточно сжать после того, как анимация поставила позу
    void LateUpdate()
    {
        if (голова != null) голова.localScale = прятатьГолову ? Vector3.one * 0.0001f : Vector3.one;
    }
}
