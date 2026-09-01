using UnityEngine;
using UnityEngine.SceneManagement;

public static class Сцены
{
    public static void Запустить(int номерСцены) => SceneManager.LoadScene(номерСцены);
    public static void Запустить(string имяСцены) => SceneManager.LoadScene(имяСцены);
    public static void Перезапустить() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public static bool ЗапуститьСледующую()
    {
        if (SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return true;
        }
        else
        {
            Консоль.НапечататьОшибку("Следующая сцена не существует!");
            return false;
        }
    }
    public static int номерАктивнойСцены => SceneManager.GetActiveScene().buildIndex;
    public static string имяАктивнойСцены => SceneManager.GetActiveScene().name;
}
