using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MonoBehaviourPlus : MonoBehaviour
{
    public GameObject объект => gameObject;
    public string имя => name;
    public Transform трансформация => transform;


    Rigidbody _rigidbody = null;
    public Rigidbody rigidbody
    {
        get
        {
            if (_rigidbody == null)
            {
                if (TryGetComponent(out Rigidbody component))
                {
                    _rigidbody = component;
                }
                else
                {
                    Debug.LogError("Компонент Rigidbody не найден", gameObject);
                }
            }
            return _rigidbody;
        }
        set
        {
            _rigidbody = value;
        }
    }

    ФизическоеТело _физическоеТело = null;
    public ФизическоеТело физическоеТело
    {
        get
        {
            if (_физическоеТело == null)
            {
                if (TryGetComponent(out ФизическоеТело component))
                {
                    _физическоеТело = component;
                }
                else
                {
                    Debug.LogError("Компонент ФизическоеТело не найден", gameObject);
                }
            }
            return _физическоеТело;
        }
        set
        {
            _физическоеТело = value;
        }
    }

    Rigidbody2D _rigidbody2D = null;
    public Rigidbody2D rigidbody2D
    {
        get
        {
            if (_rigidbody2D == null)
            {
                if (TryGetComponent(out Rigidbody2D component))
                {
                    _rigidbody2D = component;
                }
                else
                {
                    Debug.LogError("Компонент Rigidbody2D не найден", gameObject);
                }
            }
            return _rigidbody2D;
        }
        set
        {
            _rigidbody2D = value;
        }
    }

    ФизическоеТело2D _физическоеТело2D = null;
    public ФизическоеТело2D физическоеТело2D
    {
        get
        {
            if (_физическоеТело2D == null)
            {
                if (TryGetComponent(out ФизическоеТело2D component))
                {
                    _физическоеТело2D = component;
                }
                else
                {
                    Debug.LogError("Компонент ФизическоеТело2D не найден", gameObject);
                }
            }
            return _физическоеТело2D;
        }
        set
        {
            _физическоеТело2D = value;
        }
    }

    SpriteRenderer _отрисовщикСпрайта = null;
    public SpriteRenderer отрисовщикСпрайта
    {
        get
        {
            if (_отрисовщикСпрайта == null)
            {
                if (TryGetComponent(out SpriteRenderer component))
                {
                    _отрисовщикСпрайта = component;
                }
                else
                {
                    Debug.LogError("Компонент SpriteRenderer не найден", gameObject);
                }
            }
            return _отрисовщикСпрайта;
        }
        set
        {
            _отрисовщикСпрайта = value;
        }
    }

    Image _изображение = null;
    public Image изображение
    {
        get
        {
            if (_изображение == null)
            {
                if (TryGetComponent(out Image component))
                {
                    _изображение = component;
                }
                else
                {
                    Debug.LogError("Компонент Image не найден", gameObject);
                }
            }
            return _изображение;
        }
        set
        {
            _изображение = value;
        }
    }

    RawImage _rawImage = null;
    public RawImage rawImage
    {
        get
        {
            if (_rawImage == null)
            {
                if (TryGetComponent(out RawImage component))
                {
                    _rawImage = component;
                }
                else
                {
                    Debug.LogError("Компонент RawImage не найден", gameObject);
                }
            }
            return _rawImage;
        }
        set
        {
            _rawImage = value;
        }
    }

    public ТипКомпонента НайтиКомпонент<ТипКомпонента>() => GetComponent<ТипКомпонента>();
    public Component НайтиКомпонент(string имяКомпонента) => GetComponent(имяКомпонента);

    public void ВызватьСЗадержкой(Action метод, float время) => Invoke(nameof(метод), время);

    #region Получить позицию курсора
    protected Vector2 mouseScreenPosition
        {
            get
            {
                if (Mouse.current != null)
                {
                    return Mouse.current.position.ReadValue();
                }
                else
                {
                    Debug.LogError("Мышь не найдена");
                    return Vector2.zero;
                }
            }
        }
        protected Vector2 mouseWorldPosition
        {
            get
            {
                if (Camera.main != null)
                {
                    return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                }
                else
                {
                    Debug.LogError("Камера не найдена");
                    return Vector2.zero;
                }
            }
        }
        protected Vector3 mouseWorldPosition3D
        {
            get
            {
                if (Camera.main != null)
                {
                    return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                }
                else
                {
                    Debug.LogError("Камера не найдена");
                    return Vector3.zero;
                }
            }
        }
        protected Vector2 mouseWorldDelta => mouseWorldPosition - (Vector2)transform.position;
        protected Vector2 mouseWorldDirection => mouseWorldDelta.normalized;
        protected Vector3 mouseWorldDelta3D => mouseWorldPosition3D - transform.position;
        protected Vector3 mouseWorldDirection3D => mouseWorldDelta3D.normalized;
    #endregion

    #region Print

        #region Расширение возможностей
            protected void print(object message, Object context) => Debug.Log(message, context);
            protected void printWarning(object message) => Debug.LogWarning(message);
            protected void printWarning(object message, Object context) => Debug.LogWarning(message, context);
            protected void printError(object message) => Debug.LogError(message);
            protected void printError(object message, Object context) => Debug.LogError(message, context);
        #endregion

        #region Добавление вариантов с большой буквой
            protected void Print(object message) => print(message);
            protected void Print(object message, Object context) => print(message, context);
            protected void PrintWarning(object message) => printWarning(message);
            protected void PrintWarning(object message, Object context) => printWarning(message, context);
            protected void PrintError(object message) => printError(message);
            protected void PrintError(object message, Object context) => printError(message, context);
        #endregion

    #endregion

    #region Быстрое создание векторов
        protected Vector2 V2() => new Vector2();
        protected Vector2 V2(float x, float y) => new Vector2(x, y);
        protected Vector3 V3() => new Vector3();
        protected Vector3 V3(float x, float y) => V3(x, y, 0);
        protected Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);
        protected Vector2 Направление2D() => V2();
        protected Vector2 Направление2D(float x, float y) => V2(x, y);
        protected Vector2 Направление3D() => V3();
        protected Vector2 Направление3D(float x, float y) => V3(x, y);
        protected Vector2 Направление3D(float x, float y, float z) => V3(x, y, z);
    #endregion

    /*#region Быстрое получение RigidBody
        protected Rigidbody myRB => GetComponent<Rigidbody>();
        protected Rigidbody2D myRB2D => GetComponent<Rigidbody2D>();
    #endregion

    #region Загрузка и перезагрузка сцен
        protected void LoadScene(int sceneBuildIndex) => SceneManager.LoadScene(sceneBuildIndex);
        protected void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
        protected void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        protected void LoadNextScene()
        {
            if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError("Следующая сцена не найдена", gameObject);
                return;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    #endregion*/

    #region Instantiate

    #region Instantiate без кватерниона
    protected T Instantiate<T>(T original, Vector3 position) where T : Object => Instantiate(original, position, Quaternion.identity);
            protected T Instantiate<T>(T original, Vector3 position, Vector3 eulerAngles) where T : Object => Instantiate(original, position, Quaternion.Euler(eulerAngles));
            protected T Instantiate<T>(T original, Vector3 position, Transform parent) where T : Object => Instantiate(original, position, Quaternion.identity, parent);
            protected T Instantiate<T>(T original, Vector3 position, Vector3 eulerAngles, Transform parent) where T : Object => Instantiate(original, position, Quaternion.Euler(eulerAngles), parent);
        #endregion

        #region Спавнить в указанном направлении
            protected T Instantiate<T>(T original, Vector3 position, Vector3 target, bool in2D, Transform parent = null) where T : Object
    {
                Quaternion rotation = Quaternion.identity;
                Vector2 direction = target - position;
                if (in2D)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rotation = Quaternion.Euler(0, 0, angle);
                }
                else
                {
                    rotation = Quaternion.LookRotation(direction);
                }

                if (parent != null)
                    return Instantiate(original, position, rotation, parent);
                else
                    return Instantiate(original, position, rotation);
                
            }
            protected T Instantiate<T>(T original, Vector3 position, GameObject target, bool in2D, Transform parent = null) where T : Object => Instantiate(original, position, target.transform.position, in2D, parent);
            
        #endregion

        #region Instantiate переименован в Spawn
            protected T Заспавнить<T>(T original) where T : Object => Instantiate(original, transform.position);
            protected T Заспавнить<T>(T original, Vector3 position) where T : Object => Instantiate(original, position);
            protected T Заспавнить<T>(T original, Vector3 position, Vector3 eulerAngles) where T : Object => Instantiate(original, position, eulerAngles);
            protected T Заспавнить<T>(T original, Vector3 position, Transform parent) where T : Object => Instantiate(original, position, parent);
            protected T Заспавнить<T>(T original, Vector3 position, Vector3 eulerAngles, Transform parent) where T : Object => Instantiate(original, position, eulerAngles, parent);
            protected T Заспавнить<T>(T original, Vector3 position, Vector3 target, bool in2D, Transform parent = null) where T : Object => Instantiate(original, position, target, in2D, parent);
            protected T Заспавнить<T>(T original, Vector3 position, GameObject target, bool in2D, Transform parent = null) where T : Object => Instantiate(original, position, target, in2D, parent);
        #endregion

    #endregion

    #region Получение направления до другой точки
        protected Vector3 GetVectorTo(Vector3 target) => transform.position.GetVectorTo(target);
        protected Vector3 GetVectorTo(GameObject target) => transform.position.GetVectorTo(target);
        protected Vector3 GetDirectionTo(Vector3 target) => transform.position.GetDirectionTo(target);
        protected Vector3 GetDirectionTo(GameObject target) => transform.position.GetDirectionTo(target);
    #endregion

    protected void ЗакрытьИгру()
    {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
        #endif
    }
}

public static class ExtentionsPlus
{

    #region Предустановленные варианты получения InputValue
        public static bool GetBool(this InputValue inputValue) => inputValue.Get<bool>();
        public static int GetInt(this InputValue inputValue) => inputValue.Get<int>();
        public static float GetFloat(this InputValue inputValue) => (float)inputValue.Get<double>();
        public static Vector2 GetVector2(this InputValue inputValue) => inputValue.Get<Vector2>();
        public static Vector3 GetVector3(this InputValue inputValue) => inputValue.Get<Vector3>();
    #endregion

    #region Изменить одно направление у вектора

        #region У позиции
            public static void УстановитьX(this Transform transform, float x) => transform.position = new Vector3(x, transform.position.y, transform.position.z);
            public static void SetY(this Transform transform, float y) => transform.position = new Vector3(transform.position.x, y, transform.position.z);
            public static void SetZ(this Transform transform, float z) => transform.position = new Vector3(transform.position.x, transform.position.y, z);
        #endregion

        #region У Vector2
            public static Vector2 SetX(this Vector2 vector2, float x) => new Vector2(x, vector2.y);
            public static Vector2 SetY(this Vector2 vector2, float y) => new Vector2(vector2.x, y);
        #endregion

        #region У Vector3
            public static Vector3 SetX(this Vector3 vector3, float x) => new Vector3(x, vector3.y, vector3.z);
            public static Vector3 SetY(this Vector3 vector3, float y) => new Vector3(vector3.x, y, vector3.z);
            public static Vector3 SetZ(this Vector3 vector3, float z) => new Vector3(vector3.x, vector3.y, z);
    #endregion

    #endregion

    #region Передавать в TMP_Text числа
        public static void SetText(this TMP_Text text, int value) => text.SetText(value.ToString());
        public static void SetText(this TMP_Text text, float value) => text.SetText(value.ToString());
    #endregion

    #region Получение направления до другой точки

        #region Вектор
            public static Vector3 GetVectorTo(this Vector3 original, Vector3 target) => target - original;
            public static Vector3 GetVectorTo(this Vector3 original, GameObject target) => target.transform.position - original;
            public static Vector3 GetVectorTo(this GameObject original, Vector3 target) => target - original.transform.position;
            public static Vector3 GetVectorTo(this GameObject original, GameObject target) => target.transform.position - original.transform.position;
        #endregion

        #region Нормализованный вектор
            public static Vector3 GetDirectionTo(this Vector3 original, Vector3 target) => original.GetVectorTo(target).normalized;
            public static Vector3 GetDirectionTo(this Vector3 original, GameObject target) => original.GetVectorTo(target).normalized;
            public static Vector3 GetDirectionTo(this GameObject original, Vector3 target) => original.GetVectorTo(target).normalized;
            public static Vector3 GetDirectionTo(this GameObject original, GameObject target) => original.GetVectorTo(target).normalized;
        #endregion

    #endregion

    public static GameObject Телепортировать(this GameObject obj, Vector3 position)
    {
        obj.transform.position = position;
        return obj;
    }

    public static GameObject Передвинуть(this GameObject obj, Vector3 position)
    {
        obj.transform.position += position;
        return obj;
    }

    public static GameObject УстановитьПоворот(this GameObject obj, Vector3 rotation)
    {
        obj.transform.eulerAngles = rotation;
        return obj;
    }

    public static GameObject Повернуть(this GameObject obj, Vector3 rotation)
    {
        obj.transform.eulerAngles += rotation;
        return obj;
    }

    public static GameObject УстановитьПоворот2D(this GameObject obj, float rotation)
    {
        obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, rotation);
        return obj;
    }

    public static GameObject Повернуть2D(this GameObject obj, float rotation)
    {
        obj.transform.Rotate(Vector3.forward * rotation);
        return obj;
    }

    public static GameObject УстановитьРазмер(this GameObject obj, Vector3 scale)
    {
        obj.transform.localScale = scale;
        return obj;
    }

    public static GameObject Увеличить(this GameObject obj, Vector3 scale)
    {
        obj.transform.localScale += scale;
        return obj;
    }

    public static GameObject SetColor(this GameObject obj, Color color)
    {
        if (obj.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color = color;
        }
        else if (obj.TryGetComponent(out Image image))
        {
            image.color = color;
        }
        else if (obj.TryGetComponent(out RawImage rawImage))
        {
            rawImage.color = color;
        }
        else
        {
            Debug.LogError("Не найден компонент, подходящий для изменения цвета", obj);
        }

        return obj;
    }

    public static GameObject AddColor(this GameObject obj, Color color)
    {
        if (obj.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color += color;
        }
        else if (obj.TryGetComponent(out Image image))
        {
            image.color += color;
        }
        else if (obj.TryGetComponent(out RawImage rawImage))
        {
            rawImage.color += color;
        }
        else
        {
            Debug.LogError("Не найден компонент, подходящий для изменения цвета", obj);
        }

        return obj;
    }

    public static GameObject SetAlpha(this GameObject obj, float alpha)
    {
        if (obj.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.SetAlpha(alpha);
        }
        else if (obj.TryGetComponent(out Image image))
        {
            image.SetAlpha(alpha);
        }
        else if (obj.TryGetComponent(out RawImage rawImage))
        {
            rawImage.SetAlpha(alpha);
        }
        else
        {
            Debug.LogError("Не найден компонент, подходящий для изменения цвета", obj);
        }

        return obj;
    }

    public static GameObject AddAlpha(this GameObject obj, float alpha)
    {
        if (obj.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.AddAlpha(alpha);
        }
        else if (obj.TryGetComponent(out Image image))
        {
            image.AddAlpha(alpha);
        }
        else if (obj.TryGetComponent(out RawImage rawImage))
        {
            rawImage.AddAlpha(alpha);
        }
        else
        {
            Debug.LogError("Не найден компонент, подходящий для изменения цвета", obj);
        }

        return obj;
    }

    public static void УстановитьЦвет(this SpriteRenderer spriteRenderer, Color color)
    {
        spriteRenderer.color = color;
    }

    public static void SetColor(this Image image, Color color)
    {
        image.color = color;
    }

    public static void SetColor(this RawImage rawImage, Color color)
    {
        rawImage.color = color;
    }

    public static void AddColor(this SpriteRenderer spriteRenderer, Color color)
    {
        spriteRenderer.color += color;
    }

    public static void AddColor(this Image image, Color color)
    {
        image.color += color;
    }

    public static void AddColor(this RawImage rawImage, Color color)
    {
        rawImage.color += color;
    }

    public static void SetAlpha(this SpriteRenderer spriteRenderer, float alpha)
    {
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    public static void SetAlpha(this RawImage rawImage, float alpha)
    {
        rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, alpha);
    }

    public static void AddAlpha(this SpriteRenderer spriteRenderer, float alpha)
    {
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, spriteRenderer.color.a + alpha);
    }

    public static void AddAlpha(this Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a + alpha);
    }

    public static void AddAlpha(this RawImage rawImage, float alpha)
    {
        rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, rawImage.color.a + alpha);
    }

    public static GameObject Удалить(this GameObject obj)
    {
        Object.Destroy(obj);
        return obj;
    }

    public static GameObject Удалить(this GameObject obj, float time)
    {
        Object.Destroy(obj, time);
        return obj;
    }

    public static GameObject ТелепортироватьК(this GameObject obj, GameObject targetObj)
    {
        obj.transform.position = targetObj.transform.position;
        return obj;
    }

    public static GameObject ПовернутьКак(this GameObject obj, GameObject targetObj)
    {
        obj.transform.rotation = targetObj.transform.rotation;
        return obj;
    }

    public static ТипКомпонента НайтиКомпонент<ТипКомпонента>(this GameObject gameObject) => gameObject.GetComponent<ТипКомпонента>();
    public static Component НайтиКомпонент(this GameObject gameObject, string имяКомпонента) => gameObject.GetComponent(имяКомпонента);

    public static Диапазон УстановитьРежим(this Диапазон диапазон, bool режимДиапазона)
    {
        диапазон.диапазон = режимДиапазона;
        return диапазон;
    }
}