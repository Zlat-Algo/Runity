#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Игроки от первого и третьего лица: без модели или сразу с персонажем Runity, у которого работают анимации
internal static class ИгрокиRunity
{
    const string первое = "GameObject/Runity/Передвижение/Игроки/Игрок (от первого лица)/";
    const string третье = "GameObject/Runity/Передвижение/Игроки/Игрок (от третьего лица)/";

    /// <summary>
    /// Создаёт игрока. Если указан персонаж, его модель стоит внутри, анимации переключаются сами,
    /// а капсула игрока подогнана под рост персонажа (точка игрока - у ног)
    /// </summary>
    internal static GameObject Создать(bool отПервогоЛица, ВидПерсонажа? вид, GameObject родитель = null)
    {
        GameObject игрок = new GameObject("Игрок");
        GameObjectUtility.SetParentAndAlign(игрок, родитель);
        Undo.RegisterCreatedObjectUndo(игрок, "Игрок");

        Персонаж персонаж = игрок.AddComponent<Персонаж>();
        if (!отПервогоЛица)
        {
            персонаж.относительноЧего = ОтносительноЧегоДвижение.ОтносительноКамеры;
            персонаж.поворачиватьПоДвижению = true;
        }

        GameObject модель = вид.HasValue ? МанекенRunity.Создать(вид.Value, игрок.transform, "Модель") : null;
        float рост = 2;
        Vector3 глаза = new Vector3(0, 0.6f, 0);
        if (модель != null)
        {
            модель.transform.localPosition = Vector3.zero;
            модель.transform.localRotation = Quaternion.identity;
            рост = Рост(модель);
            CharacterController контроллер = игрок.GetComponent<CharacterController>();
            контроллер.height = рост;
            контроллер.radius = Mathf.Clamp(рост * 0.16f, 0.2f, 0.8f);
            контроллер.center = new Vector3(0, рост / 2 + контроллер.skinWidth, 0);
            контроллер.stepOffset = Mathf.Min(контроллер.stepOffset, рост * 0.25f);

            // Глаза - чуть впереди головы: так, посмотрев вниз, видно своё тело, а не его изнанку
            Animator аниматор = модель.GetComponent<Animator>();
            Transform голова = аниматор != null && аниматор.isHuman ? аниматор.GetBoneTransform(HumanBodyBones.Head) : null;
            float высотаГлаз = голова != null ? игрок.transform.InverseTransformPoint(голова.position).y + рост * 0.05f : рост * 0.93f;
            глаза = new Vector3(0, высотаГлаз, рост * 0.08f);
            if (отПервогоЛица) модель.AddComponent<ТелоОтПервогоЛица>();
        }
        else if (!отПервогоЛица)
        {
            GameObject капсула = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            капсула.name = "Модель";
            Object.DestroyImmediate(капсула.GetComponent<Collider>());
            капсула.transform.SetParent(игрок.transform, false);
            капсула.AddComponent<Моделлер>();
        }

        GameObject камера = new GameObject("Камера");
        Undo.RegisterCreatedObjectUndo(камера, "Камера");
        камера.tag = "MainCamera";
        if (отПервогоЛица)
        {
            камера.transform.SetParent(игрок.transform, false);
            камера.transform.localPosition = глаза;
            игрок.AddComponent<ОбзорМышкой>().камера = камера.transform;
        }
        else
        {
            Vector3 куда = игрок.transform.position + Vector3.up * (модель != null ? рост * 0.6f : 0);
            камера.transform.position = игрок.transform.position + new Vector3(0, 2 + рост, -3.5f * рост - 1);
            камера.transform.LookAt(куда);
            камера.AddComponent<Следование>().цель = игрок.transform;
        }
        Камера свояКамера = камера.AddComponent<Камера>();
        свояКамера.перспектива = true;
        свояКамера.близостьОтрисовки = 0.01;
        if (Object.FindAnyObjectByType<AudioListener>() == null) камера.AddComponent<СлушательЗвука>();

        Selection.activeObject = игрок;
        СвёрнутыеКомпоненты.Свернуть(игрок);
        СвёрнутыеКомпоненты.Свернуть(камера);
        return игрок;
    }

    // Рост модели в позе покоя: по запечённым сеткам всех частей
    static float Рост(GameObject модель)
    {
        float низ = float.MaxValue, верх = float.MinValue;
        foreach (SkinnedMeshRenderer часть in модель.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh сетка = new Mesh();
            часть.BakeMesh(сетка, true);
            foreach (Vector3 точка in сетка.vertices)
            {
                float y = модель.transform.InverseTransformPoint(часть.transform.TransformPoint(точка)).y;
                низ = Mathf.Min(низ, y); верх = Mathf.Max(верх, y);
            }
            Object.DestroyImmediate(сетка);
        }
        foreach (MeshFilter фильтр in модель.GetComponentsInChildren<MeshFilter>())
        {
            if (фильтр.sharedMesh == null) continue;
            Bounds рамка = фильтр.sharedMesh.bounds;
            низ = Mathf.Min(низ, модель.transform.InverseTransformPoint(фильтр.transform.TransformPoint(рамка.min)).y);
            верх = Mathf.Max(верх, модель.transform.InverseTransformPoint(фильтр.transform.TransformPoint(рамка.max)).y);
        }
        return верх > низ ? верх - Mathf.Min(0, низ) : 2;
    }

    static void Меню(MenuCommand команда, bool отПервогоЛица, ВидПерсонажа? вид) => Создать(отПервогоЛица, вид, команда.context as GameObject);

    [MenuItem(первое + "Без модели", false, -51)] static void П0(MenuCommand к) => Меню(к, true, null);
    [MenuItem(первое + "Манекен", false, -40)] static void П1(MenuCommand к) => Меню(к, true, ВидПерсонажа.Манекен);
    [MenuItem(первое + "Манекен-девушка", false, -40)] static void П2(MenuCommand к) => Меню(к, true, ВидПерсонажа.МанекенДевушка);
    [MenuItem(первое + "Гном", false, -40)] static void П3(MenuCommand к) => Меню(к, true, ВидПерсонажа.Гном);
    [MenuItem(первое + "Гном-девушка", false, -40)] static void П4(MenuCommand к) => Меню(к, true, ВидПерсонажа.ГномДевушка);
    [MenuItem(первое + "Великан", false, -40)] static void П5(MenuCommand к) => Меню(к, true, ВидПерсонажа.Великан);
    [MenuItem(первое + "Скелет", false, -40)] static void П6(MenuCommand к) => Меню(к, true, ВидПерсонажа.Скелет);
    [MenuItem(первое + "Зомби", false, -40)] static void П7(MenuCommand к) => Меню(к, true, ВидПерсонажа.Зомби);
    [MenuItem(первое + "Простой манекен", false, -40)] static void П8(MenuCommand к) => Меню(к, true, ВидПерсонажа.Простой);

    [MenuItem(третье + "Без модели", false, -51)] static void Т0(MenuCommand к) => Меню(к, false, null);
    [MenuItem(третье + "Манекен", false, -40)] static void Т1(MenuCommand к) => Меню(к, false, ВидПерсонажа.Манекен);
    [MenuItem(третье + "Манекен-девушка", false, -40)] static void Т2(MenuCommand к) => Меню(к, false, ВидПерсонажа.МанекенДевушка);
    [MenuItem(третье + "Гном", false, -40)] static void Т3(MenuCommand к) => Меню(к, false, ВидПерсонажа.Гном);
    [MenuItem(третье + "Гном-девушка", false, -40)] static void Т4(MenuCommand к) => Меню(к, false, ВидПерсонажа.ГномДевушка);
    [MenuItem(третье + "Великан", false, -40)] static void Т5(MenuCommand к) => Меню(к, false, ВидПерсонажа.Великан);
    [MenuItem(третье + "Скелет", false, -40)] static void Т6(MenuCommand к) => Меню(к, false, ВидПерсонажа.Скелет);
    [MenuItem(третье + "Зомби", false, -40)] static void Т7(MenuCommand к) => Меню(к, false, ВидПерсонажа.Зомби);
    [MenuItem(третье + "Простой манекен", false, -40)] static void Т8(MenuCommand к) => Меню(к, false, ВидПерсонажа.Простой);
}
#endif
