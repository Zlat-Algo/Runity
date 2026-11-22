#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

// Русские названия настроек компонентов Unity для раздела «Прочее». Если перевода нет, остаётся английское название.
// Сюда легко добавлять новые переводы: слева - название настройки в инспекторе Unity, справа - по-русски
internal static class ПереводНастроек
{
    static readonly Dictionary<string, string> слова = new Dictionary<string, string>
    {
        // Общее
        ["Enabled"] = "Включён", ["Material"] = "Материал", ["Materials"] = "Материалы", ["Mesh"] = "Модель", ["Size"] = "Размер", ["Center"] = "Центр",
        ["Radius"] = "Радиус", ["Height"] = "Высота", ["Width"] = "Ширина", ["Direction"] = "Направление", ["Offset"] = "Смещение", ["Color"] = "Цвет",
        ["Mode"] = "Режим", ["Priority"] = "Приоритет", ["Order"] = "Порядок", ["Speed"] = "Скорость", ["Volume"] = "Громкость", ["Pitch"] = "Высота звука",
        ["Loop"] = "Повторять", ["Layer"] = "Слой", ["Sprite"] = "Спрайт", ["Flip X"] = "Отразить по X", ["Flip Y"] = "Отразить по Y", ["Is Trigger"] = "Триггер",
        ["Provides Contacts"] = "Сообщать о касаниях", ["Layer Overrides"] = "Свои слои столкновений", ["Include Layers"] = "Добавить слои", ["Exclude Layers"] = "Исключить слои",
        ["Layer Override Priority"] = "Приоритет своих слоёв", ["Convex"] = "Выпуклый", ["Cooking Options"] = "Обработка модели",

        // Физическое тело
        ["Mass"] = "Масса", ["Linear Damping"] = "Сопротивление движению", ["Angular Damping"] = "Сопротивление вращению", ["Drag"] = "Сопротивление движению",
        ["Angular Drag"] = "Сопротивление вращению", ["Automatic Center Of Mass"] = "Центр масс сам", ["Automatic Tensor"] = "Инерция сама", ["Use Gravity"] = "Гравитация",
        ["Is Kinematic"] = "Кинематическое", ["Interpolate"] = "Сглаживание движения", ["Collision Detection"] = "Обнаружение столкновений", ["Constraints"] = "Ограничения",
        ["Freeze Position"] = "Заморозить положение", ["Freeze Rotation"] = "Заморозить вращение", ["Gravity Scale"] = "Сила гравитации", ["Body Type"] = "Тип тела",
        ["Simulated"] = "Участвует в физике", ["Sleeping Mode"] = "Режим сна", ["Auto Mass"] = "Масса сама", ["Linear Drag"] = "Сопротивление движению",

        // Отрисовка
        ["Cast Shadows"] = "Отбрасывать тени", ["Receive Shadows"] = "Принимать тени", ["Static Shadow Caster"] = "Статичная тень", ["Contribute Global Illumination"] = "Участвует в освещении",
        ["Receive Global Illumination"] = "Получать освещение", ["Motion Vectors"] = "Векторы движения", ["Dynamic Occlusion"] = "Скрываться за препятствиями",
        ["Rendering Layer Mask"] = "Слои отрисовки", ["Light Probes"] = "Пробы света", ["Reflection Probes"] = "Пробы отражений", ["Anchor Override"] = "Своя точка для проб",
        ["Sorting Layer"] = "Слой сортировки", ["Order in Layer"] = "Порядок в слое", ["Draw Mode"] = "Режим рисования", ["Mask Interaction"] = "Взаимодействие с маской",
        ["Sprite Sort Point"] = "Точка сортировки", ["Additional Settings"] = "Дополнительно", ["Lighting"] = "Освещение", ["Probes"] = "Пробы",

        // Свет
        ["Type"] = "Тип", ["Range"] = "Дальность", ["Spot Angle"] = "Угол прожектора", ["Intensity"] = "Яркость", ["Indirect Multiplier"] = "Сила отражённого света",
        ["Shadow Type"] = "Тип теней", ["Culling Mask"] = "Слои", ["Render Mode"] = "Способ отрисовки", ["Temperature"] = "Температура", ["Use Color Temperature"] = "Цвет по температуре",
        ["Bounce Intensity"] = "Сила отражённого света", ["Strength"] = "Сила", ["Bias"] = "Смещение теней", ["Normal Bias"] = "Смещение по нормали", ["Near Plane"] = "Ближняя граница",
        ["Cookie"] = "Узор света", ["Flare"] = "Блик", ["Halo"] = "Ореол", ["Shadow Resolution"] = "Качество теней",

        // Камера
        ["Clear Flags"] = "Фон", ["Background"] = "Цвет фона", ["Projection"] = "Проекция", ["Field of View"] = "Угол обзора", ["Field Of View"] = "Угол обзора",
        ["Clipping Planes"] = "Границы отрисовки", ["Viewport Rect"] = "Область экрана", ["Depth"] = "Порядок", ["Target Texture"] = "Рисовать в текстуру",
        ["Occlusion Culling"] = "Скрывать заслонённое", ["Allow HDR"] = "Широкий диапазон яркости", ["Allow MSAA"] = "Сглаживание MSAA", ["Target Display"] = "Экран",
        ["Physical Camera"] = "Физическая камера", ["Orthographic Size"] = "Размер",

        // Звук
        ["Audio Resource"] = "Звук", ["AudioClip"] = "Звук", ["Output"] = "Выход", ["Mute"] = "Без звука", ["Bypass Effects"] = "Без эффектов", ["Play On Awake"] = "Играть на старте",
        ["Stereo Pan"] = "Стереобаланс", ["Spatial Blend"] = "Объёмность", ["Reverb Zone Mix"] = "Эхо", ["Doppler Level"] = "Эффект Доплера", ["Spread"] = "Разброс",
        ["Volume Rolloff"] = "Затухание с расстоянием", ["Min Distance"] = "Ближнее расстояние", ["Max Distance"] = "Дальнее расстояние", ["3D Sound Settings"] = "Объёмный звук",

        // Навигация
        ["Agent Type"] = "Тип агента", ["Base Offset"] = "Базовое смещение", ["Angular Speed"] = "Скорость поворота", ["Acceleration"] = "Ускорение",
        ["Stopping Distance"] = "Зона остановки", ["Auto Braking"] = "Тормозить перед целью", ["Quality"] = "Качество", ["Auto Traverse Off Mesh Link"] = "Сам проходит переходы",
        ["Auto Repath"] = "Сам перестраивает путь", ["Area Mask"] = "Области", ["Carve"] = "Вырезать в карте", ["Move Threshold"] = "Порог движения",
        ["Time To Stationary"] = "Время до неподвижности", ["Carve Only Stationary"] = "Вырезать только неподвижное", ["Shape"] = "Форма",

        // Интерфейс
        ["Pixel Perfect"] = "Попиксельно", ["Sort Order"] = "Порядок", ["Additional Shader Channels"] = "Каналы шейдера", ["UI Scale Mode"] = "Масштаб интерфейса",
        ["Reference Resolution"] = "Эталонное разрешение", ["Screen Match Mode"] = "Подгонка под экран", ["Match"] = "Ширина или высота", ["Reference Pixels Per Unit"] = "Пикселей на единицу",
        ["Raycast Target"] = "Ловит мышь", ["Raycast Padding"] = "Отступ для мыши", ["Maskable"] = "Обрезается маской", ["Image Type"] = "Режим", ["Preserve Aspect"] = "Сохранять пропорции",
        ["Fill Center"] = "Заполнять середину", ["Fill Method"] = "Способ заполнения", ["Fill Origin"] = "Откуда заполнять", ["Fill Amount"] = "Заполненность", ["Clockwise"] = "По часовой",
        ["Pixels Per Unit Multiplier"] = "Множитель пикселей", ["Text"] = "Текст", ["Font"] = "Шрифт", ["Font Style"] = "Начертание", ["Font Size"] = "Размер шрифта",
        ["Line Spacing"] = "Межстрочный интервал", ["Rich Text"] = "Форматирование", ["Alignment"] = "Выравнивание", ["Align By Geometry"] = "Выравнивать по буквам",
        ["Horizontal Overflow"] = "Перенос по ширине", ["Vertical Overflow"] = "Перенос по высоте", ["Best Fit"] = "Подбирать размер", ["Interactable"] = "Можно нажимать",
        ["Transition"] = "Переход", ["Navigation"] = "Навигация клавишами", ["Alpha"] = "Непрозрачность", ["Blocks Raycasts"] = "Ловит мышь", ["Ignore Parent Groups"] = "Не слушать родителя",
        ["Padding"] = "Поля", ["Spacing"] = "Отступ между", ["Child Alignment"] = "Выравнивание", ["Reverse Arrangement"] = "Обратный порядок",
        ["Control Child Size"] = "Подгонять размер", ["Use Child Scale"] = "Учитывать масштаб", ["Child Force Expand"] = "Растягивать", ["Cell Size"] = "Размер клетки",
        ["Start Corner"] = "Первый угол", ["Start Axis"] = "Первая ось", ["Constraint"] = "Ограничение", ["Constraint Count"] = "Количество",

        // Соединения и прочее
        ["Connected Body"] = "Связанное тело", ["Anchor"] = "Точка крепления", ["Axis"] = "Ось", ["Spring"] = "Пружина", ["Damper"] = "Гашение", ["Break Force"] = "Сила разрыва",
        ["Break Torque"] = "Момент разрыва", ["Enable Collision"] = "Сталкиваться с телом", ["Use Spring"] = "Пружинить", ["Use Motor"] = "Мотор", ["Use Limits"] = "Пределы",
        ["Motor"] = "Мотор", ["Limits"] = "Пределы", ["Auto Configure Connected Anchor"] = "Точка крепления сама", ["Connected Anchor"] = "Точка на связанном теле",
        ["Distance"] = "Расстояние", ["Frequency"] = "Частота", ["Damping Ratio"] = "Гашение", ["Max Distance Only"] = "Только наибольшее расстояние",
        ["Video Clip"] = "Ролик", ["URL"] = "Ссылка", ["Source"] = "Источник", ["Playback Speed"] = "Скорость", ["Wait For First Frame"] = "Ждать первый кадр",
        ["Skip On Drop"] = "Пропускать кадры", ["Aspect Ratio"] = "Пропорции", ["Audio Output Mode"] = "Вывод звука", ["Positions"] = "Точки", ["Use World Space"] = "В мировых координатах",
        ["Corner Vertices"] = "Скругление углов", ["End Cap Vertices"] = "Скругление концов", ["Time"] = "Время", ["Min Vertex Distance"] = "Шаг точек", ["Autodestruct"] = "Удалить в конце",
        ["Emitting"] = "Рисует", ["Controller"] = "Контроллер", ["Avatar"] = "Аватар", ["Apply Root Motion"] = "Движение из анимации", ["Update Mode"] = "Обновление",
        ["Culling Mode"] = "Когда не видно", ["Slope Limit"] = "Крутизна склона", ["Step Offset"] = "Высота ступеньки", ["Skin Width"] = "Толщина кожи",
        ["Min Move Distance"] = "Наименьший шаг",
    };

    /// <summary>
    /// Русское название настройки Unity. Если перевода нет - исходное название
    /// </summary>
    public static string Перевести(string название) => слова.TryGetValue(название, out string перевод) ? перевод : название;

    // Имя поля Runity в подпись: «_времяКойота» -> «Время койота»
    public static string ИзИмени(string имяПоля)
    {
        string имя = имяПоля.TrimStart('_');
        if (имя.Length == 0) return имяПоля;
        StringBuilder подпись = new StringBuilder();
        for (int i = 0; i < имя.Length; i++)
        {
            char буква = имя[i];
            if (i > 0 && char.IsUpper(буква) && !char.IsUpper(имя[i - 1])) подпись.Append(' ').Append(char.ToLowerInvariant(буква));
            else подпись.Append(i == 0 ? char.ToUpperInvariant(буква) : буква);
        }
        return подпись.ToString();
    }
}
#endif
