using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Синхронизация положения, при которой объект двигает его владелец, а не хозяин игры. Так управление отзывается без задержки
/// </summary>
[AddComponentMenu("")]
public class ВладельческаяСинхронизация : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
