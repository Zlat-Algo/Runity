using UnityEngine;
using UnityEngine.InputSystem;

public enum РежимМышки { Отключенная, Ограниченная, ОграниченнаяНевидимая, Свободная}

/// <summary>
/// Всё, что связано с мышкой
/// </summary>
public static class Мышка
{
    /// <summary>
    /// Видно ли курсор и может ли он выходить за окно игры
    /// </summary>
    public static РежимМышки режим
    {
        get => Cursor.lockState switch
        {
            CursorLockMode.Locked => РежимМышки.Отключенная,
            CursorLockMode.None => РежимМышки.Свободная,
            CursorLockMode.Confined => Cursor.visible ? РежимМышки.Ограниченная : РежимМышки.ОграниченнаяНевидимая,
            _ => РежимМышки.Отключенная
        };
        set
        {
            switch (value)
            {
                case РежимМышки.Отключенная:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case РежимМышки.Свободная:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case РежимМышки.Ограниченная:
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    break;
                case РежимМышки.ОграниченнаяНевидимая:
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Прокрутка колёсика за этот кадр: больше нуля - вверх, меньше нуля - вниз
    /// </summary>
    public static Дробное прокрутка => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;

    /// <summary>
    /// Смещение мышки за этот кадр в пикселях
    /// </summary>
    public static Направление2D смещение => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

    /// <summary>
    /// Где находится курсор на экране, в пикселях
    /// </summary>
    public static Направление2D позицияНаЭкране
    {
        get
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            else
            {
                Debug.LogError("Мышь не найдена");
                return Vector2.zero;
            }
        }
        set
        {
            if (Mouse.current != null)
            {
                Mouse.current.WarpCursorPosition(value);
            }
            else
            {
                Debug.LogError("Мышь не найдена");
            }
        }
    }
    /// <summary>
    /// На какую точку мира указывает курсор в 2D
    /// </summary>
    public static Направление2D позицияВМире2D
    {
        get
        {
            if (Camera.main != null)
            {
                return Camera.main.ScreenToWorldPoint(позицияНаЭкране);
            }
            else
            {
                Debug.LogError("Камера не найдена");
                return Vector2.zero;
            }
        }
    }
    /// <summary>
    /// На какую точку мира указывает курсор, если смотреть главной камерой
    /// </summary>
    public static Направление3D позицияВМире3D
    {
        get
        {
            if (Camera.main != null)
            {
                return Camera.main.ScreenToWorldPoint(позицияНаЭкране);
            }
            else
            {
                Debug.LogError("Камера не найдена");
                return Vector3.zero;
            }
        }
    }
}
