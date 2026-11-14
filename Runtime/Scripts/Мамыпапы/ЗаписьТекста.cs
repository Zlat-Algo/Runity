using UnityEngine;
using UnityEngine.UI;

// Пишет строку в обычный текст Unity или в текст TextMeshPro. Нужен Таймеру и Выводу значения
internal static class ЗаписьТекста
{
    internal static bool Подходит(Graphic вывод) => вывод is Text || вывод is TMPro.TMP_Text;

    internal static void Записать(Graphic вывод, string строка)
    {
        if (вывод is Text текст)
        {
            if (текст.text != строка) текст.text = строка;
        }
        else if (вывод is TMPro.TMP_Text текстTMP)
        {
            if (текстTMP.text != строка) текстTMP.text = строка;
        }
    }

    // Если текст не указан, берётся текст с того же объекта
    internal static Graphic НайтиРядом(Component компонент)
    {
        if (компонент.TryGetComponent(out Text текст)) return текст;
        if (компонент.TryGetComponent(out TMPro.TMP_Text текстTMP)) return текстTMP;
        return null;
    }
}
