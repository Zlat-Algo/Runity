#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>
/// Советник по оптимизации: проверяет проект и открытые сцены и предлагает исправления одной кнопкой.
/// Ускоряет игру, уменьшает размер сборки и время загрузок. Ничего не меняет без нажатия кнопки
/// </summary>
public class СоветникОптимизации : EditorWindow
{
    // Один совет: что не так, почему это важно и как исправить
    class Совет
    {
        public string раздел;
        public string заголовок;
        public string пояснение;
        public MessageType важность;
        public string кнопка;
        public Action исправить;
        public Object объект;
    }

    List<Совет> советы = new List<Совет>();
    Vector2 прокрутка;
    bool проверено;

    [MenuItem("Runity/Советник по оптимизации", false, 3)]
    public static void Открыть()
    {
        СоветникОптимизации окно = GetWindow<СоветникОптимизации>("Советник по оптимизации");
        окно.minSize = new Vector2(460, 300);
        окно.Проверить();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Советник проверяет проект и открытые сцены. Кнопки исправляют проблему сразу, остальное можно поправить вручную по подсказке. Щёлкните по названию, чтобы выделить объект или ассет", MessageType.None);
        if (GUILayout.Button("Проверить ещё раз", GUILayout.Height(26))) Проверить();
        if (!проверено) return;

        if (советы.Count == 0)
        {
            EditorGUILayout.HelpBox("Всё хорошо: советник не нашёл, что улучшить", MessageType.Info);
            return;
        }

        List<Совет> массовые = советы.Where(с => с.исправить != null).ToList();
        if (массовые.Count > 1 && GUILayout.Button($"Исправить всё, что можно ({массовые.Count})"))
        {
            if (EditorUtility.DisplayDialog("Советник по оптимизации", $"Применить {массовые.Count} исправлений? Их можно будет поменять обратно в настройках", "Применить", "Отмена"))
            {
                foreach (Совет совет in массовые) совет.исправить();
                AssetDatabase.SaveAssets();
                Проверить();
                return;
            }
        }

        прокрутка = EditorGUILayout.BeginScrollView(прокрутка);
        foreach (IGrouping<string, Совет> раздел in советы.GroupBy(с => с.раздел))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(раздел.Key, EditorStyles.boldLabel);
            foreach (Совет совет in раздел)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (совет.объект != null)
                {
                    if (GUILayout.Button(совет.заголовок, EditorStyles.linkLabel)) { Selection.activeObject = совет.объект; EditorGUIUtility.PingObject(совет.объект); }
                }
                else EditorGUILayout.LabelField(совет.заголовок, EditorStyles.wordWrappedLabel);
                EditorGUILayout.HelpBox(совет.пояснение, совет.важность);
                if (совет.исправить != null && GUILayout.Button(совет.кнопка ?? "Исправить"))
                {
                    совет.исправить();
                    AssetDatabase.SaveAssets();
                    Проверить();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void Добавить(string раздел, string заголовок, string пояснение, MessageType важность = MessageType.Warning, Action исправить = null, string кнопка = null, Object объект = null) =>
        советы.Add(new Совет { раздел = раздел, заголовок = заголовок, пояснение = пояснение, важность = важность, исправить = исправить, кнопка = кнопка, объект = объект });

    /// <summary>
    /// Проверяет проект и составляет список советов
    /// </summary>
    public void Проверить()
    {
        советы.Clear();
        try
        {
            ПроверитьРедактор();
            ПроверитьПроект();
            ПроверитьТекстуры();
            ПроверитьЗвуки();
            ПроверитьМодели();
            ПроверитьСцены();
        }
        finally { EditorUtility.ClearProgressBar(); }
        проверено = true;
        Repaint();
    }

    /// <summary>
    /// Сколько советов найдено при последней проверке
    /// </summary>
    public int количествоСоветов => советы.Count;

    void ПроверитьРедактор()
    {
        const string раздел = "Скорость работы в редакторе";
        if (!EditorSettings.enterPlayModeOptionsEnabled || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) == 0)
            Добавить(раздел, "Запуск игры каждый раз перезагружает сцену",
                "Если не перезагружать сцену при нажатии Play, игра запускается заметно быстрее. Объекты при этом всё равно возвращаются в исходное состояние после остановки",
                MessageType.Info, () =>
                {
                    EditorSettings.enterPlayModeOptionsEnabled = true;
                    EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableSceneReload;
                }, "Запускать без перезагрузки сцены");

        if (EditorSettings.refreshImportMode != AssetDatabase.RefreshImportMode.OutOfProcessPerQueue)
            Добавить(раздел, "Ассеты импортируются по одному",
                "Unity умеет импортировать текстуры и модели сразу несколькими процессами. Большие папки с ассетами будут добавляться в проект быстрее",
                MessageType.Info, () => EditorSettings.refreshImportMode = AssetDatabase.RefreshImportMode.OutOfProcessPerQueue, "Импортировать параллельно");
    }

    void ПроверитьПроект()
    {
        const string раздел = "Настройки игры";
        if (!PlayerSettings.gcIncremental)
            Добавить(раздел, "Сборка мусора останавливает игру целиком",
                "Пошаговая сборка мусора распределяет работу по кадрам, и игра не подвисает на доли секунды",
                MessageType.Warning, () => PlayerSettings.gcIncremental = true, "Включить пошаговую сборку");

        if (QualitySettings.vSyncCount == 0 && Application.targetFrameRate <= 0)
            Добавить(раздел, "Частота кадров ничем не ограничена",
                "Без вертикальной синхронизации игра рисует сотни кадров в секунду: греется видеокарта, шумят вентиляторы, садится батарея ноутбука. Включите синхронизацию в Runity - Настройки графики",
                MessageType.Info, () => QualitySettings.vSyncCount = 1, "Включить вертикальную синхронизацию");

        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            if (!urp.useSRPBatcher)
                Добавить(раздел, "Отключено пакетирование SRP Batcher",
                    "Оно сильно сокращает работу процессора на отрисовку множества объектов с одинаковыми шейдерами",
                    MessageType.Warning, () => { urp.useSRPBatcher = true; EditorUtility.SetDirty(urp); }, "Включить", urp);
            if (urp.msaaSampleCount > 4)
                Добавить(раздел, $"Сглаживание MSAA x{urp.msaaSampleCount}",
                    "Сглаживание выше x4 почти незаметно, но дорого для видеокарты",
                    MessageType.Info, () => { urp.msaaSampleCount = 4; EditorUtility.SetDirty(urp); }, "Поставить x4", urp);
            if (urp.renderScale > 1.01f)
                Добавить(раздел, $"Масштаб отрисовки {urp.renderScale:0.##}",
                    "Картинка рисуется больше экрана и потом уменьшается - это в разы дороже. Обычно хватает 1",
                    MessageType.Warning, () => { urp.renderScale = 1; EditorUtility.SetDirty(urp); }, "Поставить 1", urp);
        }

        if (EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes.Count(с => с.enabled) == 0)
            Добавить(раздел, "В сборке нет ни одной сцены", "Откройте Runity - Список сцен и отметьте сцены, которые должны попасть в игру", MessageType.Error);
    }

    // Только ассеты самого проекта: в пакетах менять ничего нельзя
    static IEnumerable<string> Ассеты(string фильтр) =>
        AssetDatabase.FindAssets(фильтр, new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct();

    void ПроверитьТекстуры()
    {
        const string раздел = "Текстуры и картинки";
        List<TextureImporter> несжатые = new List<TextureImporter>();
        List<TextureImporter> огромные = new List<TextureImporter>();
        List<TextureImporter> лишниеУровни = new List<TextureImporter>();
        string[] пути = Ассеты("t:Texture2D").ToArray();
        for (int i = 0; i < пути.Length; i++)
        {
            if (i % 50 == 0) EditorUtility.DisplayProgressBar("Советник по оптимизации", "Текстуры: " + пути[i], (float)i / пути.Length);
            if (!(AssetImporter.GetAtPath(пути[i]) is TextureImporter импорт)) continue;
            Texture2D текстура = AssetDatabase.LoadAssetAtPath<Texture2D>(пути[i]);
            if (текстура == null) continue;
            int размер = Mathf.Max(текстура.width, текстура.height);

            if (импорт.textureCompression == TextureImporterCompression.Uncompressed && размер >= 256 && импорт.textureType != TextureImporterType.Lightmap) несжатые.Add(импорт);
            if (импорт.maxTextureSize > 2048 && размер > 2048) огромные.Add(импорт);
            if (импорт.textureType == TextureImporterType.Sprite && импорт.mipmapEnabled) лишниеУровни.Add(импорт);
        }

        if (несжатые.Count > 0)
            Добавить(раздел, $"Несжатые текстуры: {несжатые.Count}",
                "Несжатая текстура занимает в 4-8 раз больше памяти и дольше загружается. Сжатие обычно незаметно глазу. Пиксель-арт после сжатия стоит проверить: если цвета поплыли, верните «Без сжатия» в настройках картинки",
                MessageType.Warning, () => Применить(несжатые, и => и.textureCompression = TextureImporterCompression.Compressed), "Сжать", несжатые[0] != null ? AssetDatabase.LoadMainAssetAtPath(несжатые[0].assetPath) : null);
        if (огромные.Count > 0)
            Добавить(раздел, $"Текстуры больше 2048 пикселей: {огромные.Count}",
                "Такие текстуры редко нужны целиком, а занимают десятки мегабайт памяти. Ограничение до 2048 почти всегда незаметно",
                MessageType.Info, () => Применить(огромные, и => и.maxTextureSize = 2048), "Ограничить до 2048", AssetDatabase.LoadMainAssetAtPath(огромные[0].assetPath));
        if (лишниеУровни.Count > 0)
            Добавить(раздел, $"Спрайты с уровнями детализации (mipmaps): {лишниеУровни.Count}",
                "Спрайтам интерфейса и 2D-игр уменьшенные копии не нужны, а памяти они занимают на треть больше",
                MessageType.Info, () => Применить(лишниеУровни, и => и.mipmapEnabled = false), "Отключить", AssetDatabase.LoadMainAssetAtPath(лишниеУровни[0].assetPath));
    }

    static void Применить<T>(List<T> импорты, Action<T> изменить) where T : AssetImporter
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (T импорт in импорты)
            {
                изменить(импорт);
                импорт.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
    }

    void ПроверитьЗвуки()
    {
        const string раздел = "Звуки и музыка";
        List<AudioImporter> длинныеВПамяти = new List<AudioImporter>();
        List<AudioImporter> короткиеПотоком = new List<AudioImporter>();
        foreach (string путь in Ассеты("t:AudioClip"))
        {
            if (!(AssetImporter.GetAtPath(путь) is AudioImporter импорт)) continue;
            AudioClip звук = AssetDatabase.LoadAssetAtPath<AudioClip>(путь);
            if (звук == null) continue;
            AudioClipLoadType загрузка = импорт.defaultSampleSettings.loadType;
            if (звук.length > 15 && загрузка == AudioClipLoadType.DecompressOnLoad) длинныеВПамяти.Add(импорт);
            if (звук.length < 3 && загрузка == AudioClipLoadType.Streaming) короткиеПотоком.Add(импорт);
        }

        if (длинныеВПамяти.Count > 0)
            Добавить(раздел, $"Длинные звуки целиком распаковываются в память: {длинныеВПамяти.Count}",
                "Музыка и длинные звуки распаковываются при загрузке сцены: это мегабайты памяти и долгая загрузка. Потоковое чтение проигрывает их прямо с диска",
                MessageType.Warning, () => Применить(длинныеВПамяти, и => { AudioImporterSampleSettings н = и.defaultSampleSettings; н.loadType = AudioClipLoadType.Streaming; и.defaultSampleSettings = н; }),
                "Читать потоком", AssetDatabase.LoadMainAssetAtPath(длинныеВПамяти[0].assetPath));
        if (короткиеПотоком.Count > 0)
            Добавить(раздел, $"Короткие звуки читаются потоком: {короткиеПотоком.Count}",
                "Для коротких звуков (выстрел, шаг) потоковое чтение даёт задержку перед звуком. Их лучше держать в памяти",
                MessageType.Info, () => Применить(короткиеПотоком, и => { AudioImporterSampleSettings н = и.defaultSampleSettings; н.loadType = AudioClipLoadType.DecompressOnLoad; и.defaultSampleSettings = н; }),
                "Держать в памяти", AssetDatabase.LoadMainAssetAtPath(короткиеПотоком[0].assetPath));
    }

    void ПроверитьМодели()
    {
        const string раздел = "Модели";
        List<ModelImporter> читаемые = new List<ModelImporter>();
        foreach (string путь in Ассеты("t:Model"))
        {
            if (AssetImporter.GetAtPath(путь) is ModelImporter импорт && импорт.isReadable) читаемые.Add(импорт);
        }
        if (читаемые.Count > 0)
            Добавить(раздел, $"Модели с включённым Read/Write: {читаемые.Count}",
                "Такая модель хранится в памяти дважды. Чтение нужно, только если модель меняется кодом во время игры",
                MessageType.Info, () => Применить(читаемые, и => и.isReadable = false), "Отключить чтение", AssetDatabase.LoadMainAssetAtPath(читаемые[0].assetPath));
    }

    void ПроверитьСцены()
    {
        const string раздел = "Открытые сцены";
        Light[] лампы = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        Light[] сТенями = лампы.Where(л => л.type != LightType.Directional && л.shadows != LightShadows.None && л.lightmapBakeType != LightmapBakeType.Baked).ToArray();
        if (сТенями.Length > 4)
            Добавить(раздел, $"Ламп с тенями в реальном времени: {сТенями.Length}",
                "Каждая точечная лампа с тенями рисует сцену ещё 6 раз. Оставьте тени 2-4 главным лампам, остальным отключите",
                MessageType.Warning, () => { foreach (Light л in сТенями.OrderByDescending(л => л.intensity * л.range).Skip(4)) { Undo.RecordObject(л, "Тени"); л.shadows = LightShadows.None; } },
                "Оставить тени 4 самым ярким", сТенями[0]);

        MeshRenderer[] отрисовщики = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        int вершин = отрисовщики.Sum(о => о.TryGetComponent(out MeshFilter ф) && ф.sharedMesh != null ? ф.sharedMesh.vertexCount : 0);
        if (вершин > 3_000_000)
            Добавить(раздел, $"На сценах {вершин / 1000000f:0.#} млн вершин",
                "Это много даже для мощных компьютеров. Упростите дальние модели, добавьте Группы детализации (LOD) или Скрытие за препятствиями",
                MessageType.Warning);

        GameObject[] неподвижные = отрисовщики.Select(о => о.gameObject)
            .Where(о => !GameObjectUtility.AreStaticEditorFlagsSet(о, StaticEditorFlags.BatchingStatic) && о.GetComponentInParent<Rigidbody>() == null && о.GetComponentInParent<Animator>() == null && о.GetComponentInParent<RunityComponent>() == null)
            .ToArray();
        if (неподвижные.Length > 50)
            Добавить(раздел, $"Объектов без отметки «Статичный»: {неподвижные.Length}",
                "Unity может объединить неподвижные объекты при отрисовке, но только если они отмечены статичными. Отметьте статичными стены, землю, дома и деревья - то, что никогда не двигается",
                MessageType.Info, объект: неподвижные[0]);

        Camera[] камеры = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        if (камеры.Length > 2)
            Добавить(раздел, $"Включённых камер: {камеры.Length}", "Каждая камера рисует сцену заново. Выключите камеры, которые сейчас не нужны", MessageType.Warning, объект: камеры[0]);

        int частиц = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude).Sum(ч => ч.main.maxParticles);
        if (частиц > 50000)
            Добавить(раздел, $"Систем частиц на {частиц} частиц", "Уменьшите «Наибольшее количество частиц» у Частиц: огромные значения тратят память, даже если частиц на экране мало", MessageType.Info);
    }
}
#endif
