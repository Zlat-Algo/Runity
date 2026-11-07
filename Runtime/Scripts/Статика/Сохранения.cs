using UnityEngine;

/// <summary>
/// Запоминает значения между запусками игры. Каждое значение хранится под своим названием
/// </summary>
public static class Сохранения
{
    /// <summary>
    /// Сохраняет целое число под указанным названием
    /// </summary>
    public static void СохранитьЧисло(Строка название, Число значение)
    {
        PlayerPrefs.SetInt(название, значение);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Сохраняет дробное число под указанным названием
    /// </summary>
    public static void СохранитьДробное(Строка название, Дробное значение)
    {
        PlayerPrefs.SetFloat(название, значение);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Сохраняет текст под указанным названием
    /// </summary>
    public static void СохранитьСтроку(Строка название, Строка значение)
    {
        PlayerPrefs.SetString(название, значение);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Сохраняет "да" или "нет" под указанным названием
    /// </summary>
    public static void СохранитьФлажок(Строка название, Флажок значение)
    {
        PlayerPrefs.SetInt(название, значение ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Загружает целое число. Если его не сохраняли, вернёт значение по умолчанию
    /// </summary>
    public static Число ЗагрузитьЧисло(Строка название, Число поУмолчанию = default) => PlayerPrefs.GetInt(название, поУмолчанию);

    /// <summary>
    /// Загружает дробное число. Если его не сохраняли, вернёт значение по умолчанию
    /// </summary>
    public static Дробное ЗагрузитьДробное(Строка название, Дробное поУмолчанию = default) => PlayerPrefs.GetFloat(название, поУмолчанию);

    /// <summary>
    /// Загружает текст. Если его не сохраняли, вернёт пустую строку
    /// </summary>
    public static Строка ЗагрузитьСтроку(Строка название) => PlayerPrefs.GetString(название, "");

    /// <summary>
    /// Загружает "да" или "нет". Если значение не сохраняли, вернёт "нет"
    /// </summary>
    public static Флажок ЗагрузитьФлажок(Строка название) => PlayerPrefs.GetInt(название, 0) == 1;

    /// <summary>
    /// Даёт "да", если значение с таким названием уже сохранено
    /// </summary>
    public static Флажок Есть(Строка название) => PlayerPrefs.HasKey(название);

    /// <summary>
    /// Удаляет одно сохранённое значение
    /// </summary>
    public static void Удалить(Строка название)
    {
        PlayerPrefs.DeleteKey(название);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Удаляет все сохранения игры
    /// </summary>
    public static void УдалитьВсё()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
