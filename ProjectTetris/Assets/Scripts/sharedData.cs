using UnityEngine;
using UnityEngine.UI;

class sharedData : MonoBehaviour 
{ 
    static bool gameNotOver = true;
    static int linesCleared;
    static int level = 0;
    static Text currentLevel;
    static float tickChange = 0f;

    void Awake()
    {
        currentLevel =  GameObject.FindWithTag("LevelText").GetComponent<Text>(); 
    }
   
    public static bool getGameNotOver()
    {    
        return gameNotOver;
    }
         
    public static int getLinesCleared()
    {
        return linesCleared;
    }

    public static int getLevel()
    {
        return level;
    }

    public static float getTickChange()
    {
        return tickChange;
    }

    public static void setGameNotOver(bool value)
    {
        gameNotOver = value;
    }

    public static void setLinesCleared(int value)
    {
        linesCleared = value;
        if (value % 10 == 0 && getLevel() <= 9)
        {
            setLevel(getLevel() + 1);
        }
    }

    private static void setLevel(int value)
    {
        level = value;
        currentLevel.text = "Level " + value;
        tickChange += 0.005f;
    }

    public static void resetTickChange()
    {
        tickChange = 0f;
    }
}