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

    // Время в виде текста: 75 секунд - это «01:15» или «1 м. 15 с.». При обратном отсчёте время округляется вверх: «1» видно, пока не наступит ровно ноль
    internal static string Время(float секунд, ФорматВремени формат, int знаковПослеЗапятой, bool писатьЕдиницы, bool округлятьВверх)
    {
        int знаков = Mathf.Clamp(знаковПослеЗапятой, 0, 3);
        long доли = (long)Mathf.Pow(10, знаков);
        double точно = (double)секунд * доли;
        long всего = округлятьВверх ? (long)System.Math.Ceiling(точно - 0.0001) : (long)System.Math.Floor(точно + 0.0001);
        if (всего < 0) всего = 0;

        long целыхСекунд = всего / доли;
        long дробь = всего % доли;
        string хвост = знаков > 0 ? System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + дробь.ToString(new string('0', знаков)) : "";
        long часы = целыхСекунд / 3600, минуты = целыхСекунд / 60 % 60, сек = целыхСекунд % 60;

        switch (формат)
        {
            case ФорматВремени.Секунды:
                return писатьЕдиницы ? $"{целыхСекунд}{хвост} с." : $"{целыхСекунд}{хвост}";
            case ФорматВремени.МинутыИСекунды:
                long всегоМинут = целыхСекунд / 60;
                return писатьЕдиницы ? $"{всегоМинут} м. {сек:00}{хвост} с." : $"{всегоМинут:00}:{сек:00}{хвост}";
            default:
                return писатьЕдиницы ? $"{часы} ч. {минуты:00} м. {сек:00}{хвост} с." : $"{часы:00}:{минуты:00}:{сек:00}{хвост}";
        }
    }
}
