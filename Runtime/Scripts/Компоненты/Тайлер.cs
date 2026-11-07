#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Карта из плиток: уровни рисуются кистью, как в графическом редакторе
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
[AddComponentMenu("  Runity/ Визуал/ 2D/Тайлер")]
public class Тайлер : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Tilemap>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Tilemap оригинал => (Tilemap)оригинальныйКомпонент;
    TilemapRenderer отрисовщик => GetComponent<TilemapRenderer>();

    /// <summary>
    /// Цвет, на который умножаются все плитки
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    /// <summary>
    /// Какой слой рисуется поверх других: чем больше число, тем выше
    /// </summary>
    public Число порядокОтрисовки
    {
        get => отрисовщик.sortingOrder;
        set => отрисовщик.sortingOrder = value;
    }

    /// <summary>
    /// Если "да", то сквозь плитки нельзя пройти
    /// </summary>
    public Флажок твёрдый
    {
        get => TryGetComponent(out TilemapCollider2D _);
        set
        {
            if (value && !твёрдый)
            {
                gameObject.AddComponent<TilemapCollider2D>();
            }
            else if (!value && TryGetComponent(out TilemapCollider2D коллайдер))
            {
                if (Application.isPlaying) Destroy(коллайдер);
                else DestroyImmediate(коллайдер);
            }
        }
    }

    /// <summary>
    /// Сколько плиток стоит на карте
    /// </summary>
    public Число количествоПлиток
    {
        get
        {
            int количество = 0;
            foreach (Vector3Int клетка in оригинал.cellBounds.allPositionsWithin)
            {
                if (оригинал.HasTile(клетка)) количество++;
            }
            return количество;
        }
    }

    /// <summary>
    /// Ставит плитку в клетку с указанными координатами
    /// </summary>
    public void Поставить(Число x, Число y, TileBase плитка)
    {
        оригинал.SetTile(new Vector3Int(x, y, 0), плитка);
    }

    /// <summary>
    /// Убирает плитку из клетки
    /// </summary>
    public void Убрать(Число x, Число y)
    {
        оригинал.SetTile(new Vector3Int(x, y, 0), null);
    }

    /// <summary>
    /// Заполняет плитками прямоугольник между двумя клетками
    /// </summary>
    public void Заполнить(Число x1, Число y1, Число x2, Число y2, TileBase плитка)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
        {
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
            {
                Поставить(x, y, плитка);
            }
        }
    }

    /// <summary>
    /// Убирает все плитки
    /// </summary>
    public void Очистить() => оригинал.ClearAllTiles();

    /// <summary>
    /// Даёт "да", если в этой клетке есть плитка
    /// </summary>
    public Флажок ЕстьПлитка(Число x, Число y) => оригинал.HasTile(new Vector3Int(x, y, 0));

    /// <summary>
    /// Даёт плитку из клетки. Если её нет, будет пусто
    /// </summary>
    public TileBase ПлиткаВКлетке(Число x, Число y) => оригинал.GetTile(new Vector3Int(x, y, 0));

    /// <summary>
    /// Какая клетка находится в этой точке мира
    /// </summary>
    public Направление2D КлеткаВТочке(Направление3D точка)
    {
        Vector3Int клетка = оригинал.WorldToCell(точка);
        return new Направление2D(клетка.x, клетка.y);
    }

    /// <summary>
    /// Даёт центр клетки в координатах мира
    /// </summary>
    public Направление3D ЦентрКлетки(Число x, Число y) => оригинал.GetCellCenterWorld(new Vector3Int(x, y, 0));
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Тайлер))]
internal class ТайлерEditor : RunityEditor<Тайлер>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект хранит карту из плиток. Откройте палитру, перетащите в неё спрайты и рисуйте уровень кистью прямо в сцене");

        Пробел();

        if (компонент.GetComponentInParent<Grid>() == null)
        {
            Текст("Тайлеру нужен родитель с компонентом Grid. Проще всего создать его через меню Runity - 2D - Тайлер", MessageType.Error);
            Пробел();
        }

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Поле("Порядок отрисовки", x => x.порядокОтрисовки,
            (title, value) => EditorGUILayout.IntField(title, value));

        Поле("Твёрдый", x => x.твёрдый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Текст($"Плиток на карте: {компонент.количествоПлиток}");

        Кнопка("Открыть палитру", () => EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette"));
    }
}
#endif
