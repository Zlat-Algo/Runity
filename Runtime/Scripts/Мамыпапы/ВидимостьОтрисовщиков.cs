using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Общий учёт того, почему отрисовщик спрятан: Скрытие вдали и Скрытие за препятствиями не должны мешать друг другу.
// Объект рисуется, только если его не прячет ни одна причина
internal static class ВидимостьОтрисовщиков
{
    class Состояние
    {
        public bool далеко;
        public bool заслонён;
        public bool тениЗаслонённого;
        public ShadowCastingMode исходныеТени;
    }

    static readonly Dictionary<Renderer, Состояние> состояния = new Dictionary<Renderer, Состояние>();

    public static void Далеко(Renderer отрисовщик, bool далеко) => Изменить(отрисовщик, с => с.далеко = далеко);

    public static void Заслонён(Renderer отрисовщик, bool заслонён, bool оставлятьТени) => Изменить(отрисовщик, с => { с.заслонён = заслонён; с.тениЗаслонённого = оставлятьТени; });

    static void Изменить(Renderer отрисовщик, System.Action<Состояние> изменение)
    {
        if (отрисовщик == null) return;
        if (!состояния.TryGetValue(отрисовщик, out Состояние с))
        {
            с = new Состояние { исходныеТени = отрисовщик.shadowCastingMode };
            состояния[отрисовщик] = с;
        }
        изменение(с);

        // Заслонённый объект может по-прежнему отбрасывать тень: она бывает видна, даже когда сам он за стеной
        bool толькоТень = с.заслонён && !с.далеко && с.тениЗаслонённого && с.исходныеТени != ShadowCastingMode.Off;
        отрисовщик.forceRenderingOff = с.далеко || (с.заслонён && !толькоТень);
        отрисовщик.shadowCastingMode = толькоТень ? ShadowCastingMode.ShadowsOnly : с.исходныеТени;

        if (!с.далеко && !с.заслонён) состояния.Remove(отрисовщик);
    }
}
