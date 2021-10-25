using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

class sharedData : MonoBehaviour 
{ 
    
    // static bool gameNotOver = true;
    // static int linesCleared;
    // static int level = 0;
    // static Text currentLevel;
    // static float tickChange = 0f;
    //
    // void Awake()
    // {
    //     currentLevel = GetComponentInChildren<Text>();
    // }
    //
    // public static bool getGameNotOver()
    // {
    //     return gameNotOver;
    // }
    //
    // public static int getLinesCleared()
    // {
    //     return linesCleared;
    // }
    //
    // public static int getLevel()
    // {
    //     return level;
    // }
    //
    // public static float getTickChange()
    // {
    //     return tickChange;
    // }
    //
    // public static void setGameNotOver(bool value)
    // {
    //     gameNotOver = value;
    // }
    //
    // public static void setLinesCleared(int value)
    // {
    //     setLevel(getLevel() + 1);
    //     linesCleared = value;
    //     if (value % 10 == 0 && getLevel() <= 9)
    //     {
    //         setLevel(getLevel() + 1);
    //     }
    // }
    //
    // private static void setLevel(int value)
    // {
    //     level = value;
    //     currentLevel.text = "Level " + value;
    //     tickChange += 0.005f;
    // }
    //
    // public static void resetTickChange()
    // {
    //     tickChange = 0f;
    // }

    // static int count = -1;
    // private static GameController[] gameControllers;
    //
    // private void Awake()
    // {
    //     gameControllers = (GameController[]) Object.FindObjectsOfType(typeof(GameController));
    // }
    //
    // public static GameController getGameController(int id)
    // {
    //     foreach (GameController i in (GameController[]) Object.FindObjectsOfType(typeof(GameController)))
    //     {
    //         Debug.Log((GameController[]) Object.FindObjectsOfType(typeof(GameController)));
    //         Debug.Log(i.name);
    //         Debug.Log(i.name[7]);
    //         if (Char.Equals(i.name[7], (char) id))
    //         {
    //             return i;
    //         }
    //     }
    //
    //     return null;

        // count++;
        // if (count == 6)
        // {
        //     count = 0;
        // }
        //
        // foreach (GameController i in (GameController[]) Object.FindObjectsOfType(typeof(GameController)))
        // {
        //     Debug.Log((GameController[]) Object.FindObjectsOfType(typeof(GameController)));
        //     Debug.Log(i.name);
        //     Debug.Log(i.name[7]);
        //     if (Char.Equals(i.name[7], (char) count))
        //     {
        //         return i;
        //     }
        // }
        //
        // Debug.Log("An Error has occured");
        // Debug.Log(count);
        // return null;

        // GameController temp = (GameController) Object.FindObjectsOfType(typeof(GameController))[count];
        // Debug.Log(temp);
        // //return (GameController) Object.FindObjectsOfType(typeof(GameController))[count];
        // return temp;
   // } 
}