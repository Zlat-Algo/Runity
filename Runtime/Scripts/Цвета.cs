using UnityEngine;

public static class Цвета
{
    public static Color Новый(float красный, float зелёный, float синий, float видимость = 1) => new Color(красный, зелёный, синий, видимость);

    public static Color белый => Color.white;
    public static Color чёрный => Color.black;
    public static Color прозрачный => Color.clear;

    public static Color красный => Color.red;
    public static Color тёмноКрасный => Color.darkRed;
    public static Color мягкийКрасный => Color.softRed;
    public static Color кирпичный => Color.firebrick;
    public static Color малиновый => Color.crimson;
    public static Color индийскийКрасный => Color.indianRed;
    public static Color розовоКоричневый => Color.rosyBrown;

    public static Color синий => Color.blue;
    public static Color тёмноСиний => Color.darkBlue;
    public static Color среднеСиний => Color.mediumBlue;
    public static Color полуночноСиний => Color.midnightBlue;
    public static Color королевскийСиний => Color.royalBlue;
    public static Color васильковый => Color.cornflowerBlue;
    public static Color стальнойСиний => Color.steelBlue;
    public static Color небесноСиний => Color.skyBlue;
    public static Color светлоСиний => Color.lightBlue;
    public static Color голубой => Color.lightSkyBlue;
    public static Color яркоГолубой => Color.deepSkyBlue;
    public static Color мягкийСиний => Color.softBlue;
    public static Color кадетскийСиний => Color.cadetBlue;

    public static Color зелёный => Color.green;
    public static Color тёмноЗелёный => Color.darkGreen;
    public static Color леснойЗелёный => Color.forestGreen;
    public static Color морскойЗелёный => Color.seaGreen;
    public static Color среднийМорскойЗелёный => Color.mediumSeaGreen;
    public static Color светлыйМорскойЗелёный => Color.lightSeaGreen;
    public static Color светлоЗелёный => Color.lightGreen;
    public static Color бледноЗелёный => Color.paleGreen;
    public static Color лаймовый => Color.limeGreen;
    public static Color жёлтоЗелёный => Color.yellowGreen;
    public static Color салатовый => Color.greenYellow;
    public static Color мягкийЗелёный => Color.softGreen;
    public static Color весеннийЗелёный => Color.springGreen;
    public static Color яркийВесеннийЗелёный => Color.mediumSpringGreen;
    public static Color шартрез => Color.chartreuse;
    public static Color газонный => Color.lawnGreen;
    public static Color оливковый => Color.olive;
    public static Color оливковоЗелёный => Color.oliveDrab;
    public static Color тёмноОливковый => Color.darkOliveGreen;

    public static Color жёлтый => Color.yellow;
    public static Color золотой => Color.gold;
    public static Color золотистый => Color.goldenRod;
    public static Color тёмноЗолотистый => Color.darkGoldenRod;
    public static Color светлоЗолотистый => Color.lightGoldenRod;
    public static Color светлоЗолотой => Color.lightGoldenRodYellow;
    public static Color бледноЗолотистый => Color.paleGoldenRod;
    public static Color хаки => Color.khaki;
    public static Color тёмныйХаки => Color.darkKhaki;
    public static Color лимонный => Color.lemonChiffon;
    public static Color мягкийЖёлтый => Color.softYellow;
    public static Color светлоЖёлтый => Color.lightYellow;

    public static Color оранжевый => Color.orange;
    public static Color тёмноОранжевый => Color.darkOrange;
    public static Color красноОранжевый => Color.orangeRed;
    public static Color коралловый => Color.coral;
    public static Color томатный => Color.tomato;
    public static Color лососевый => Color.salmon;
    public static Color светлоЛососевый => Color.lightSalmon;
    public static Color тёмноЛососевый => Color.darkSalmon;
    public static Color персиковый => Color.peachPuff;
    public static Color бисквитный => Color.bisque;
    public static Color мокасиновый => Color.moccasin;
    public static Color навахо => Color.navajoWhite;
    public static Color папайя => Color.papayaWhip;
    public static Color песочный => Color.sandyBrown;
    public static Color перуанский => Color.peru;
    public static Color коричневый => Color.brown;
    public static Color тёмноКоричневый => Color.saddleBrown;
    public static Color бурый => Color.burlywood;
    public static Color сиенна => Color.sienna;
    public static Color загар => Color.tan;

    public static Color розовый => Color.pink;
    public static Color светлоРозовый => Color.lightPink;
    public static Color яркоРозовый => Color.hotPink;
    public static Color насыщенноРозовый => Color.deepPink;
    public static Color розовоФиолетовый => Color.violetRed;
    public static Color среднийРозовоФиолетовый => Color.mediumVioletRed;
    public static Color бледныйРозовоФиолетовый => Color.paleVioletRed;
    public static Color лавандовоРозовый => Color.lavenderBlush;

    public static Color пурпурный => Color.magenta;
    public static Color тёмноПурпурный => Color.darkMagenta;
    public static Color фиолетовый => Color.purple;
    public static Color тёмноФиолетовый => Color.darkViolet;
    public static Color светлоФиолетовый => Color.violet;
    public static Color орхидеевый => Color.orchid;
    public static Color тёмноОрхидеевый => Color.darkOrchid;
    public static Color среднеОрхидеевый => Color.mediumOrchid;
    public static Color среднеПурпурный => Color.mediumPurple;
    public static Color индиго => Color.indigo;
    public static Color лавандовый => Color.lavender;
    public static Color сливовый => Color.plum;
    public static Color пурпурныйРебекки => Color.rebeccaPurple;

    public static Color циановый => Color.cyan;
    public static Color тёмноЦиановый => Color.darkCyan;
    public static Color светлоЦиановый => Color.lightCyan;
    public static Color бирюзовый => Color.turquoise;
    public static Color тёмноБирюзовый => Color.darkTurquoise;
    public static Color среднеБирюзовый => Color.mediumTurquoise;
    public static Color бледноБирюзовый => Color.paleTurquoise;
    public static Color аквамариновый => Color.aquamarine;
    public static Color среднеАквамариновый => Color.mediumAquamarine;
    public static Color лазурный => Color.azure;
    public static Color цветМорскойВолны => Color.teal;

    public static Color серебряный => Color.silver;
    public static Color светлоСерый => Color.lightGray;
    public static Color тёмноСерый => Color.darkGray;
    public static Color серый => Color.gray;
    public static Color тусклоСерый => Color.dimGray;
    public static Color грифельноСерый => Color.slateGray;
    public static Color светлоГрифельноСерый => Color.lightSlateGray;

    public static Color серый10 => Color.gray1;
    public static Color серый20 => Color.gray2;
    public static Color серый30 => Color.gray3;
    public static Color серый40 => Color.gray4;
    public static Color серый50 => Color.gray5;
    public static Color серый60 => Color.gray6;
    public static Color серый70 => Color.gray7;
    public static Color серый80 => Color.gray8;
    public static Color серый90 => Color.gray9;

    public static Color снежный => Color.snow;
    public static Color дымчатоБелый => Color.whiteSmoke;
    public static Color призрачноБелый => Color.ghostWhite;
    public static Color цветочныйБелый => Color.floralWhite;
    public static Color слоноваяКость => Color.ivory;
    public static Color медвяный => Color.honeydew;
    public static Color мятный => Color.mintCream;
    public static Color льняной => Color.linen;
    public static Color кремовый => Color.seashell;
    public static Color античныйБелый => Color.antiqueWhite;
    public static Color миндальный => Color.blanchedAlmond;
    public static Color бежевый => Color.beige;
    public static Color кукурузный => Color.cornsilk;
    public static Color староеКружево => Color.oldLace;
    public static Color розоватоБелый => Color.mistyRose;
    public static Color пудровоСиний => Color.powderBlue;
    public static Color пшеничный => Color.wheat;
}
