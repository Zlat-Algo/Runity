using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class RunityComponent : MonoBehaviourPlus
{
    public Component оригинальныйКомпонент { get; protected set; }
}

#if UNITY_EDITOR
public abstract class RunityEditor<T> : Editor where T : RunityComponent
{
    protected TAsset НайтиАссет<TAsset>(string guid) where TAsset : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guid));
    }

    protected void Текст(string message)
    {
        EditorGUILayout.HelpBox(message, MessageType.None);
    }

    protected void Кнопка(string message, Action callback)
    {
        if (GUILayout.Button(message))
        {
            callback();
        }
    }

    protected void Кнопка(string message, string tooltip, Action callback)
    {
        if (GUILayout.Button(new GUIContent(message, tooltip)))
        {
            callback();
        }
    }

    protected bool МиниКнопка(string message, string tooltip)
    {
        return GUILayout.Button(new GUIContent(message, tooltip), EditorStyles.miniButton, GUILayout.Width(25));
    }

    protected void МиниКнопка(string message, string tooltip, Action callback)
    {
        if (GUILayout.Button(new GUIContent(message, tooltip), EditorStyles.miniButton, GUILayout.Width(25)))
        {
            callback();
        }
    }

    protected void Пробел()
    {
        EditorGUILayout.Space();
    }

    protected void Пробел(float ширина)
    {
        EditorGUILayout.Space(ширина);
    }

    protected TObj Объект<TObj>(GUIContent title, UnityEngine.Object obj) where TObj : UnityEngine.Object
    {
        return (TObj)EditorGUILayout.ObjectField(title, obj, typeof(TObj), false);
    }

    protected void НачатьСобытия()
    {
        serializedObject.Update();
    }

    protected void Событие(string отображаемоеНазвание, string названиеСобытия)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(названиеСобытия), new GUIContent(отображаемоеНазвание));
    }

    protected void ЗакончитьСобытия()
    {
        serializedObject.ApplyModifiedProperties();
    }

    protected void НачатьГоризонтальнуюГруппу() => EditorGUILayout.BeginHorizontal();
    protected void ЗакончитьГоризонтальнуюГруппу() => EditorGUILayout.EndHorizontal();

    protected T компонент => (T)target;

    protected T[] components
    {
        get
        {
            T[] result = new T[targets.Length];

            for (int i = 0; i < targets.Length; i++)
                result[i] = (T)targets[i];

            return result;
        }
    }

    protected bool IsChanged<TValue>(Func<T, TValue> getter)
    {
        if (targets.Length < 2)
            return false;

        TValue первое = getter(компонент);

        foreach (T объект in components)
        {
            if (!Equals(первое, getter(объект)))
                return true;
        }

        return false;
    }

    protected void ChangeOriginals(string title, Func<T, UnityEngine.Object> getOriginal, Action<T> changeAction)
    {
        foreach (T component in components)
        {
            UnityEngine.Object original =
                getOriginal(component);

            if (original == null)
                continue;

            Undo.RecordObject(
                original,
                title
            );

            changeAction(component);

            EditorUtility.SetDirty(original);
        }
    }

    protected TValue Поле<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)propertyInfo.GetValue(component);
        }

        void Set(T component, TValue value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        TValue value = drawer(
            new GUIContent(title),
            Get(компонент)
        );

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected Диапазон ПолеСДиапазоном(string title, Expression<Func<T, Диапазон>> property)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        Диапазон Get(T component)
        {
            return (Диапазон)propertyInfo.GetValue(component);
        }

        void Set(T component, Диапазон value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        Диапазон value = Get(компонент);

        /*Диапазон value = drawer(
            new GUIContent(title),
            Get(component)
        );*/

        if (value.диапазон)
        {
            GUILayout.Label(title);
            GUILayout.Label(" от ", GUILayout.Width(25));
            value.минимальное = EditorGUILayout.FloatField(value.минимальное, GUILayout.Width(75));
            GUILayout.Label(" до ", GUILayout.Width(25));
            value.максимальное = EditorGUILayout.FloatField(value.максимальное, GUILayout.Width(75));
            if (МиниКнопка("↕", "Случайное"))
            {
                value.диапазон = false;
                //Set(component, value);
            }
        }
        else
        {
            value.минимальное = EditorGUILayout.FloatField(new GUIContent(title), value.минимальное);
            if (МиниКнопка("=", "Постоянное"))
            {
                value.диапазон = true;
                //Set(component, value);
            }
        }
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected Диапазон ПолеСДиапазоном(string title, Expression<Func<T, float>> propertyMin, Expression<Func<T, float>> propertyMax, Expression<Func<T, bool>> propertyMode)
    {
        PropertyInfo propertyMinInfo = GetPropertyInfo(propertyMin);
        PropertyInfo propertyMaxInfo = GetPropertyInfo(propertyMax);
        PropertyInfo propertyModeInfo = GetPropertyInfo(propertyMode);

        Диапазон Get(T component)
        {
            Диапазон диапазон = new Диапазон();
            диапазон.минимальное = (float)propertyMinInfo.GetValue(component);
            диапазон.максимальное = (float)propertyMaxInfo.GetValue(component);
            диапазон.диапазон = (bool)propertyModeInfo.GetValue(component);
            return диапазон;
        }

        void Set(T component, Диапазон value)
        {
            propertyMinInfo.SetValue(component, value.минимальное);
            propertyMaxInfo.SetValue(component, value.максимальное);
            propertyModeInfo.SetValue(component, value.диапазон);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        Диапазон value = Get(компонент);

        /*Диапазон value = drawer(
            new GUIContent(title),
            Get(component)
        );*/

        if (value.диапазон)
        {
            GUILayout.Label(title);
            GUILayout.Label(" от ", GUILayout.Width(25));
            value.минимальное = EditorGUILayout.FloatField(value.минимальное, GUILayout.Width(75));
            GUILayout.Label(" до ", GUILayout.Width(25));
            value.максимальное = EditorGUILayout.FloatField(value.максимальное, GUILayout.Width(75));
            if (МиниКнопка("↕", "Случайное"))
            {
                value.диапазон = false;
                //Set(component, value);
            }
        }
        else
        {
            value.минимальное = EditorGUILayout.FloatField(new GUIContent(title), value.минимальное);
            if (МиниКнопка("=", "Постоянное"))
            {
                value.диапазон = true;
                //Set(component, value);
            }
        }
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected bool2 СинхрополеСДвойнымBool(string title, Expression<Func<T, bool>> propertyFirst, Expression<Func<T, bool>> propertySecond)
    {
        PropertyInfo propertyFirstInfo = GetPropertyInfo(propertyFirst);
        PropertyInfo propertySecondInfo = GetPropertyInfo(propertySecond);

        bool2 Get(T component)
        {
            return new bool2(
                (bool)propertyFirstInfo.GetValue(component),
                (bool)propertySecondInfo.GetValue(component));
        }

        void Set(T component, bool2 value)
        {
            propertyFirstInfo.SetValue(component, value.x);
            propertySecondInfo.SetValue(component, value.y);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        bool2 value = Get(компонент);
        /*GUILayout.Label(title);
        GUILayout.Label("X", GUILayout.Width(25));
        value.x = EditorGUILayout.Toggle(value.x, GUILayout.Width(25));
        GUILayout.Label("Y", GUILayout.Width(25));
        value.y = EditorGUILayout.Toggle(value.y);*/
        Rect rect = EditorGUILayout.GetControlRect();
        rect = EditorGUI.PrefixLabel(rect, new GUIContent("Flip"));
        float width = 30f;
        EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, width, rect.height),"X", value.x);
        EditorGUI.ToggleLeft(new Rect(rect.x + width + 10f, rect.y, width, rect.height),"Y", value.y);
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                component => Set(component, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    PropertyInfo GetPropertyInfo<TValue>(Expression<Func<T, TValue>> property)
    {
        PropertyInfo propertyInfo = ((MemberExpression)property.Body).Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("GUIField ожидает свойство.");
        return propertyInfo;
    }

    protected TValue Синхрополе<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)propertyInfo.GetValue(component);
        }

        void Set(T component, TValue value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue =
            IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        TValue value = drawer(
            new GUIContent(title),
            Get(компонент)
        );

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                component => Set(component, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected TEnum EnumField<TEnum>(string title, Func<T, TEnum> getter, Action<T, TEnum> setter) where TEnum : Enum
    {
        EditorGUI.showMixedValue =
            IsChanged(getter);

        EditorGUI.BeginChangeCheck();

        TEnum value = (TEnum)EditorGUILayout.EnumPopup(
            label: title,
            getter(компонент)
        );

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                объект => setter(объект, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }
}
#endif