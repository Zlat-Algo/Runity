using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Что происходит после реплики или ответа
/// </summary>
public enum ПереходДиалога { Дальше, КРеплике, Закончить }

/// <summary>
/// Вариант ответа игрока
/// </summary>
[Serializable]
public class ОтветДиалога
{
    [Подсказка("Что написано на кнопке ответа")]
    public string текст = "Ответ";

    [Подсказка("Дальше - к следующей реплике по порядку, К реплике - к реплике с указанным номером, Закончить - завершить разговор")]
    public ПереходДиалога переход;

    [Подсказка("Номер реплики, к которой перейти. Реплики считаются с нуля")]
    public int номерРеплики;

    [Подсказка("Если написать сюда слово, то при выборе ответа сработает реакция с таким же словом у Собеседника или Окна диалога")]
    public string метка = "";
}

/// <summary>
/// Одна реплика разговора
/// </summary>
[Serializable]
public class РепликаДиалога
{
    [Подсказка("Кто говорит. Имя показывается над текстом")]
    public string говорящий = "";

    [Подсказка("Картинка говорящего. Можно не указывать")]
    public Sprite портрет;

    [МногострочныйТекст(3), Подсказка("Что говорит персонаж")]
    public string текст = "";

    [Подсказка("Варианты ответа. Если их нет, разговор идёт дальше по нажатию")]
    public List<ОтветДиалога> ответы = new List<ОтветДиалога>();

    [Подсказка("Что происходит после реплики, если у неё нет ответов")]
    public ПереходДиалога переход;

    [Подсказка("Номер реплики, к которой перейти. Реплики считаются с нуля")]
    public int номерРеплики;

    [Подсказка("Если написать сюда слово, то при показе реплики сработает реакция с таким же словом")]
    public string метка = "";
}

/// <summary>
/// Разговор: реплики по порядку и варианты ответа, которые ведут к другим репликам.
/// Показывается в Окне диалога, а запускается Собеседником
/// </summary>
[CreateAssetMenu(fileName = "Новый диалог", menuName = "Runity/Диалог", order = -93)]
public class Диалог : ScriptableObject
{
    [SerializeField] List<РепликаДиалога> _реплики = new List<РепликаДиалога> { new РепликаДиалога { говорящий = "Персонаж", текст = "Привет!" } };
    /// <summary>
    /// Реплики разговора. Разговор начинается с реплики номер 0
    /// </summary>
    public List<РепликаДиалога> реплики => _реплики;

    /// <summary>
    /// Даёт реплику по номеру или пусто, если такой нет
    /// </summary>
    public РепликаДиалога Реплика(Число номер) => номер >= 0 && номер < _реплики.Count ? _реплики[номер] : null;

    /// <summary>
    /// Куда ведёт переход от реплики с этим номером. -1 означает конец разговора
    /// </summary>
    public Число Следующая(Число текущая, ПереходДиалога переход, Число номерРеплики)
    {
        switch (переход)
        {
            case ПереходДиалога.Закончить: return -1;
            case ПереходДиалога.КРеплике: return номерРеплики >= 0 && номерРеплики < _реплики.Count ? (int)номерРеплики : -1;
            default: return (int)текущая + 1 < _реплики.Count ? (int)текущая + 1 : -1;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Диалог))]
internal class ДиалогEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Добавьте реплики. Номер реплики написан слева - на него могут ссылаться ответы. Если у реплики нет ответов, разговор идёт дальше по нажатию", MessageType.None);
        EditorGUILayout.Space();

        serializedObject.Update();
        SerializedProperty реплики = serializedObject.FindProperty("_реплики");
        for (int i = 0; i < реплики.arraySize; i++)
        {
            SerializedProperty реплика = реплики.GetArrayElementAtIndex(i);
            string говорящий = реплика.FindPropertyRelative("говорящий").stringValue;
            string текст = реплика.FindPropertyRelative("текст").stringValue;
            string кратко = текст.Length > 40 ? текст.Substring(0, 40) + "..." : текст;

            EditorGUILayout.BeginHorizontal();
            реплика.isExpanded = EditorGUILayout.Foldout(реплика.isExpanded, $"{i}. {говорящий}: {кратко}", true);
            if (GUILayout.Button(new GUIContent("✕", "Удалить реплику"), EditorStyles.miniButton, GUILayout.Width(24)))
            {
                реплики.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (!реплика.isExpanded) continue;

            EditorGUI.indentLevel++;
            SerializedProperty поле = реплика.Copy();
            SerializedProperty конец = реплика.GetEndProperty();
            bool внутрь = true;
            while (поле.NextVisible(внутрь) && !SerializedProperty.EqualContents(поле, конец))
            {
                внутрь = false;
                if (поле.name == "номерРеплики" && реплика.FindPropertyRelative("переход").enumValueIndex != (int)ПереходДиалога.КРеплике) continue;
                if ((поле.name == "переход" || поле.name == "номерРеплики") && реплика.FindPropertyRelative("ответы").arraySize > 0) continue;
                EditorGUILayout.PropertyField(поле, true);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("Добавить реплику"))
        {
            реплики.arraySize++;
            SerializedProperty новая = реплики.GetArrayElementAtIndex(реплики.arraySize - 1);
            новая.FindPropertyRelative("текст").stringValue = "";
            новая.FindPropertyRelative("ответы").arraySize = 0;
            новая.FindPropertyRelative("переход").enumValueIndex = 0;
            новая.FindPropertyRelative("метка").stringValue = "";
            новая.isExpanded = true;
        }
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        Справка.ОтобразитьВИнспекторе(typeof(Диалог), new[] { "реплики" });
    }
}
#endif
