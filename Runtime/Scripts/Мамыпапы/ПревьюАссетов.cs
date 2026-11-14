#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Картинки для ассетов Runity в окне Project: материал выглядит как шар с материалом, вещь - как её иконка
internal static class ПревьюАссетов
{
    // Спрайт может лежать в атласе и быть нечитаемым, поэтому он перерисовывается через видеокарту
    public static Texture2D ИзСпрайта(Sprite спрайт, int ширина, int высота)
    {
        if (спрайт == null || спрайт.texture == null) return null;

        Texture2D текстура = спрайт.texture;
        Rect область = спрайт.textureRect;
        float масштаб = Mathf.Min(ширина / область.width, высота / область.height);
        int w = Mathf.Max(1, Mathf.RoundToInt(область.width * масштаб));
        int h = Mathf.Max(1, Mathf.RoundToInt(область.height * масштаб));

        RenderTexture холст = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture было = RenderTexture.active;
        Graphics.Blit(текстура, холст,
            new Vector2(область.width / текстура.width, область.height / текстура.height),
            new Vector2(область.x / текстура.width, область.y / текстура.height));

        Texture2D результат = new Texture2D(ширина, высота, TextureFormat.RGBA32, false);
        Color32[] пусто = new Color32[ширина * высота];
        результат.SetPixels32(пусто);
        RenderTexture.active = холст;
        результат.ReadPixels(new Rect(0, 0, w, h), (ширина - w) / 2, (высота - h) / 2);
        результат.Apply();

        RenderTexture.active = было;
        RenderTexture.ReleaseTemporary(холст);
        return результат;
    }

    public static Texture2D ИзОбъекта(Object объект, string путь, int ширина, int высота)
    {
        if (объект == null) return null;
        Editor редактор = Editor.CreateEditor(объект);
        try
        {
            return редактор.RenderStaticPreview(путь, null, ширина, высота);
        }
        finally
        {
            Object.DestroyImmediate(редактор);
        }
    }
}
#endif
