using UnityEngine;
using UnityEngine.SceneManagement;

public class quitOnEsc : MonoBehaviour
{
    
    public bool isGame;

    void Start()
    {
        isGame = SceneManager.GetActiveScene().name != "Main Menu";
    }
    
    void Update()
    {
        if (Input.GetKey ("escape")) {
            if (isGame)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                Application.Quit();
            }
        }      
    }
}
