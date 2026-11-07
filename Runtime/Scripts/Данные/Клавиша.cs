using UnityEngine.InputSystem;

/// <summary>
/// Клавиша клавиатуры или кнопка мыши. Используется в Управление.КлавишаНажата и в компоненте Реакция на клавишу
/// </summary>
public enum Клавиша
{
    A = Key.A, B = Key.B, C = Key.C, D = Key.D, E = Key.E, F = Key.F, G = Key.G, H = Key.H, I = Key.I,
    J = Key.J, K = Key.K, L = Key.L, M = Key.M, N = Key.N, O = Key.O, P = Key.P, Q = Key.Q, R = Key.R,
    S = Key.S, T = Key.T, U = Key.U, V = Key.V, W = Key.W, X = Key.X, Y = Key.Y, Z = Key.Z,

    Цифра0 = Key.Digit0, Цифра1 = Key.Digit1, Цифра2 = Key.Digit2, Цифра3 = Key.Digit3, Цифра4 = Key.Digit4,
    Цифра5 = Key.Digit5, Цифра6 = Key.Digit6, Цифра7 = Key.Digit7, Цифра8 = Key.Digit8, Цифра9 = Key.Digit9,

    F1 = Key.F1, F2 = Key.F2, F3 = Key.F3, F4 = Key.F4, F5 = Key.F5, F6 = Key.F6,
    F7 = Key.F7, F8 = Key.F8, F9 = Key.F9, F10 = Key.F10, F11 = Key.F11, F12 = Key.F12,

    Пробел = Key.Space, Энтер = Key.Enter, Таб = Key.Tab, Эскейп = Key.Escape, Бэкспейс = Key.Backspace,
    ЛевыйШифт = Key.LeftShift, ПравыйШифт = Key.RightShift,
    ЛевыйКонтрол = Key.LeftCtrl, ПравыйКонтрол = Key.RightCtrl,
    ЛевыйАльт = Key.LeftAlt, ПравыйАльт = Key.RightAlt,

    СтрелкаВверх = Key.UpArrow, СтрелкаВниз = Key.DownArrow, СтрелкаВлево = Key.LeftArrow, СтрелкаВправо = Key.RightArrow,

    Тильда = Key.Backquote, Минус = Key.Minus, Равно = Key.Equals,
    ЛеваяСкобка = Key.LeftBracket, ПраваяСкобка = Key.RightBracket, ОбратныйСлэш = Key.Backslash,
    ТочкаСЗапятой = Key.Semicolon, Кавычка = Key.Quote, Запятая = Key.Comma, Точка = Key.Period, Слэш = Key.Slash,
    КапсЛок = Key.CapsLock, Делит = Key.Delete, Инсерт = Key.Insert, Хоум = Key.Home, Энд = Key.End,
    ПейджАп = Key.PageUp, ПейджДаун = Key.PageDown,

    // Кнопки мыши идут после всех клавиш клавиатуры, чтобы не пересекаться с ними
    ЛКМ = 1001, ПКМ = 1002, СКМ = 1003, КолесоВверх = 1004, КолесоВниз = 1005, МышьНазад = 1006, МышьВперёд = 1007
}
