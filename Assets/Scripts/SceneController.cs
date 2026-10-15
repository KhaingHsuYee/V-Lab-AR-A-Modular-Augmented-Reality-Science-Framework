using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // 1. Call this from your Chemistry Button
    public void GoToChemistryLab()
    {
        SceneManager.LoadScene("Chemistry_Home"); // <-- Match your exact scene name
    }

    // 2. Call this from your Physics Button
    public void GoToPhysicsLab()
    {
        SceneManager.LoadScene("Physics_Home");   // <-- Match your exact scene name
    }

    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);   // <-- Match your exact scene name
    }

    // 3. Call this from your Lab Back Buttons to return Home
    public void GoBackToHome()
    {
        SceneManager.LoadScene("HomeScreen");         // <-- Match your exact scene name
    }

    // 4. Call this from your Exit Button
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game is closing..."); 
    }
}