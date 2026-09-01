using UnityEngine;
using UnityEngine.InputSystem;

public enum РежимМышки { Отключенная, Ограниченная, ОграниченнаяНевидимая, Свободная}

public static class Мышка
{
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

    public static Vector2 позицияНаЭкране
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
    }
    public static Vector2 позицияВМире2D
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
    public static Vector3 позицияВМире3D
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
