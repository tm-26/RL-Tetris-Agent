using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameController : MonoBehaviour {

    public static float CELL_SIZE = 1.0f;
    public static float MOVE_DURATION = 0.05f;

    public GameObject CellPrefab;
    public GameObject BlockPrefab;
    public GameObject Field;
    public GameObject Zero;
    public Text scoreText;
    public bool player1;
    public GameObject OtherField;
    public RectTransform gameOverCanvas;
    public TextMeshProUGUI gameWinner;
    public Button restart;
    public Button quit;
    public AudioSource audioSource;
    public AudioClip gameOverSound;
    public AudioClip goingDownSound;
    public AudioClip lineClear;
    public AudioClip tetris;
    public GameObject mainCamera;
    public bool playedGameOverSound;
    public GameObject otherField;
    public GameObject soundComesOutOfMe;
    public bool thisPlayerLost = false;
    private Field field;
    private GameObject block;
    private GameObject[] blockCells = new GameObject[4];
    private GameObject[,] fieldCells = new GameObject[10,20];
    private int newRow;
    private float tickValue = 0.0f;
    private float maxTick = 1.650f;
    private float lastKeyDown;
    private float timeKeyPressed;
    private ParticleSystem particles;
    private GameObject p1Particles;
    private GameObject p2Particles;

    public FuzzyLogic fuzzy;
    public float skillFactor;
    private float amateurVal;
    private float medVal;
    private float proVal;
    public bool amateur;
    public bool med;
    public bool pro;

    void Start ()
    {
        playedGameOverSound = false;
        audioSource = GetComponent<AudioSource>();
        restart.onClick.AddListener(restartGame);
        quit.onClick.AddListener(quitGame);

        p1Particles = GameObject.Find("P1");
        p2Particles = GameObject.Find("P2");
    }

    private void Awake()
    {
        field = new Field();
        block = Instantiate(BlockPrefab);
        block.transform.SetParent(Zero.transform,false);
        buildBlock();
    }

    private void buildBlock()
    {
        for (int i = 0; i <= 3; i++) // Allways four cells.
        {
            if (blockCells[i] != null)
            {
                blockCells[i].transform.parent = null;
                DestroyImmediate(blockCells[i]);
            }

            float x = field.block.pattern[i * 2];
            float y = -(float) field.block.pattern[i * 2 + 1];
            GameObject c = Instantiate(CellPrefab);
            c.transform.SetParent(block.transform, true);
            c.transform.localPosition = new Vector3(x - 0.5f, y + 0.5f, -0.7f);
            var view = c.GetComponent<CellViewController>();
            view.CurrentMatID = field.block.color;
            blockCells[i] = c;
        }
    }

    private void updateField()
    {
        GameObject cell;

        for (int col = 0; col < field.columns; col++)
        {
            for (int row = 0; row < field.rows; row++)
            {
                if (field.grid[col, row] > -1)
                {
                    if (fieldCells[col, row] != null)
                    {
                        cell = fieldCells[col, row];
                        cell.GetComponent<CellViewController>().CurrentMatID = field.grid[col, row];
                    }
                    else
                    {
                        cell = Instantiate(CellPrefab);
                        cell.transform.localPosition = new Vector3((float)col - 0.5f +1, (float)row + 0.5f, -0.7f);
                        cell.GetComponent<CellViewController>().CurrentMatID = field.grid[col, row];
                        cell.transform.SetParent(Zero.transform, false);
                        fieldCells[col, row] = cell;
                    }
                }

                if (field.grid[col, row] < 0 && fieldCells[col, row] != null )
                {
                    DestroyImmediate(fieldCells[col, row]);
                    fieldCells[col, row] = null;
                }
            }
        }
    }

    void Update()
    {
        if (sharedData.getGameNotOver())
        {
            tickValue += (Time.deltaTime + sharedData.getTickChange());
            // affects how often tick() in Block is called

            string lineNum;

            if (field.linesMatched.Count > 0)
            {
                for (int i = 0; i < field.linesMatched.Count; i++)
                {
                    // Debug.Log(field.linesMatched[i] + i);
                    OtherField.GetComponent<GameController>().field.moveUp(field.linesMatched[i] + i, field.pastGrid);

                    lineNum = (field.linesMatched[i] + i).ToString();

                    // depending on which player, loop through player's board's particle systems and play the one which matches
                    if(player1)
                    {
                        foreach(Transform child in p1Particles.transform)
                        {
                            // Debug.Log(field.linesMatched[i] + i);
                            if(child.gameObject.name == lineNum){
                                particles = child.gameObject.GetComponent<ParticleSystem>();
                                particles.Play();
                                
                            }
                        }

                    }
                    else
                    {
                        foreach(Transform child in p2Particles.transform)
                        {
                            if(child.gameObject.name == lineNum){
                                particles = child.gameObject.GetComponent<ParticleSystem>();
                                particles.Play();
                            }
                        }
                    }
                }
                OtherField.GetComponent<GameController>().updateField();
                field.linesMatched.Clear();
            }

            if (tickValue >= maxTick)
            {
                tickValue = 0;
                field.block.tick();
            }

            if (field.block.isRedrawed)
            {
                scoreText.text = "Score: " + field.score;
                rotateHandler();
                moveHandler();
                field.block.isRedrawed = false;
            }

            if (field.isRedrawed)
            {
                updateField();
                field.isRedrawed = false;
            }

            // Handle inputs
            if (!player1)
            {
                if (getKey(KeyCode.LeftArrow))
                {
                    field.block.move(-1);
                }
                else if (getKey(KeyCode.RightArrow))
                {
                    field.block.move(1);
                }
                else if (getKey(KeyCode.UpArrow))
                {
                    if (field.block.rotate())
                    {
                        newRow = field.blockDown();
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, newRow,
                            block.transform.localPosition.z);
                        onDownTweenComplete();
                    }
                    
                }
                else if (getKey(KeyCode.DownArrow))
                {
                    field.block.move(0, -1);
                }
                else if (getKey(KeyCode.RightControl))
                {
                    newRow = field.blockDown();
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, newRow,
                        block.transform.localPosition.z);
                    onDownTweenComplete();
                    // audioSource.clip = goingDownSound;
                    // audioSource.Play();
                }
            }
            else
            {
                if (getKey(KeyCode.A))
                {
                    field.block.move(-1);
                }
                else if (getKey(KeyCode.D))
                {
                    field.block.move(1);
                }
                else if (getKey(KeyCode.W))
                {
                    if (field.block.rotate())
                    {
                        newRow = field.blockDown();
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, newRow,
                            block.transform.localPosition.z);
                        onDownTweenComplete();
                    }
                }
                else if (getKey(KeyCode.S))
                {
                    field.block.move(0, -1);
                }
                else if (getKey(KeyCode.Space))
                {
                    newRow = field.blockDown();
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, newRow,
                        block.transform.localPosition.z);
                    onDownTweenComplete();
                }
            }
        }
        if (getKey(KeyCode.F))
        {
            field.score = field.score + 10;
            Debug.Log("Current Score" + field.score);
        }

        if (getKey(KeyCode.G))
        {
            field.score = field.score - 10;
            Debug.Log("Current Score" + field.score);
        }


        if (player1)
        {
            skillFactor = getSkillFactor();
            amateurVal = fuzzy.EvalAmateur(skillFactor);
            proVal = fuzzy.EvalPro(skillFactor);
            medVal = fuzzy.EvalMed(skillFactor);
            // Debug.Log("Am:" + amateurVal + " Med:" + medVal + " Pro:" + proVal);
            if (amateurVal > medVal && amateurVal > proVal && amateur == false)
            {
                //Change Difficuly to easy
                setFalse();
                OtherField.GetComponent<TetrisAgent>().setDifficulty(1);
                amateur = true;
            }
            else if (medVal > amateurVal && medVal > proVal && med == false)
            {
                //Change Difficuly to med
                setFalse();
                OtherField.GetComponent<TetrisAgent>().setDifficulty(2);
                med = true;
            }
            else if (proVal > amateurVal && proVal > medVal && pro == false)
            {
                //Change Difficuly to hard
                setFalse();
                OtherField.GetComponent<TetrisAgent>().setDifficulty(3);
                pro = true;
            }
        }
    }

    private void setFalse()
    {
        amateur = false;
        med = false;
        pro = false;
    }

    private float getSkillFactor()
    {
        float currentHeight = field.currHeight(); 
        return (float) (field.score * (1 / Mathf.Log((currentHeight + 2), 2)));
    }

    private void onDownTweenComplete()
    {
        field.block.row = newRow;
        field.setBlock();
        field.isRedrawed = true;
    }

    private void blockReset()
    {
        field.block.reset();
    }

    public void clearField()
    {
        field = new Field();
    }

    public void moveHandler()
    {
        float newX = field.block.column;
        float newY = field.block.row;
        block.transform.localPosition = new Vector3(newX, newY, 0);
    }

    public void rotateHandler()
    {
        buildBlock();
    }

    bool getKey(KeyCode key) {
        bool keyDown = Input.GetKeyDown(key);
        bool pressed = Input.GetKey(key) && Time.time - lastKeyDown > 0.5f && Time.time - timeKeyPressed > 0.05f;

        if (keyDown) {
            lastKeyDown = Time.time;
        }
        if (pressed) {
            timeKeyPressed = Time.time;
        }
 
        return keyDown || pressed;
    }

    public void gameOver()
    {
        thisPlayerLost = false;

        for (int c = 0; c < field.columns; c++)
        {
            if (field.grid[c, 19] != -1)
            {
                thisPlayerLost = true;
                break;
            }
        }

        if (thisPlayerLost)
        {
            gameWinner.text = player1 ? "Player 2 Wins" : "Player 1 Wins";
        }
        else
        {
            gameWinner.text = player1 ? "Player 1 Wins" : "Player 2 Wins";
        }

        sharedData.setGameNotOver(false);
    }

    void restartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        sharedData.setGameNotOver(true);
        sharedData.resetTickChange();
    }

    void quitGame()
    {
        Application.Quit();
    }

    public Field getField()
    {
        return field;
    }

    public GameObject getOtherField()
    {
        return OtherField;
    }

    public GameObject getBlock()
    {
        return block;
    }
    private IEnumerator playGameOverSound()
    {
        if (!playedGameOverSound)
        {
            yield return new WaitForSeconds(0.1f);
            audioSource = soundComesOutOfMe.GetComponent<AudioSource>();
            audioSource.clip = gameOverSound;
            audioSource.Play();
            Field.SetActive(false);
            otherField.SetActive(false);
        }
    }
}