using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
//ml agents
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;


public class TetrisAgent : Agent
{
    private float lastKeyDown;
    private float timeKeyPressed;
    private int current;
    public bool pretrained;
    private float fitness;
    private float currentFitness;

    float m_TimeSinceDecision;
    public float timeBetweenDecisionsAtInference;

    [Tooltip("Because we want an observation right before making a decision, we can force " +
             "a camera to render before making a decision. Place the agentCam here if using " +
             "RenderTexture as observations.")]
    public Camera renderCamera1;
    public Camera renderCamera2;

    public NNModel easy;
    public NNModel medium;
    public NNModel hard;

    public int tetrisHeight;

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;

        if (getKey(KeyCode.A))
        {
            discreteActionsOut[0] = 1;
        }
        else if (getKey(KeyCode.D))
        {
            discreteActionsOut[0] = 2;
        }
        else if (getKey(KeyCode.W))
        {
            discreteActionsOut[0] = 3;
        }
        else if (getKey(KeyCode.S))
        {
            discreteActionsOut[0] = 4;
        }
        else if (getKey(KeyCode.Space))
        {
            discreteActionsOut[0] = 5;
        }
    }

    public void setDifficulty(int level)
    {
        InferenceDevice inferenceDevice = InferenceDevice.CPU;

        if (level == 1)
        {
            SetModel("easy", easy, inferenceDevice);
            Debug.Log("Current Model is easy");
        }
        else if (level == 2)
        {
            SetModel("medium", medium, inferenceDevice);
            Debug.Log("Current Model is medium");
        }
        else if (level == 3)
        {
            SetModel("hard", hard, inferenceDevice);
            Debug.Log("Current Model is hard");
        }
    }

    public override void OnEpisodeBegin()
    {
        gameObject.GetComponent<GameController>().clearField();
        gameObject.GetComponent<GameController>().getOtherField().GetComponent<GameController>().clearField();
        sharedData.setGameNotOver(true);
        fitness = 0;
    }

    // public override void CollectObservations(VectorSensor sensor)
    // {
    //     int rowLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(0);
    //     int colLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(1);

    //     string arrayString = "";
    //     for (int i = colLength-1; i >= 0; i--)
    //     {
    //         for (int j = 0; j < rowLength ; j++)
    //         {
    //             arrayString += string.Format("{0}\t", gameObject.GetComponent<GameController>().getField().grid[j,i]);
    //         }
    //         arrayString += System.Environment.NewLine + System.Environment.NewLine;
    //     }

    //     //providing the grid to the feature vector
    //     for (int m = 0; m < 10; m++)
    //     {
    //         for (int n = 0; n < 20; n++)
    //         {
    //             current = gameObject.GetComponent<GameController>().getField().grid[m,n];
    //             if (current > 0)
    //             {
    //                 sensor.AddObservation(1);
    //                 Debug.Log(1);
    //             }
    //             else
    //             {
    //                 sensor.AddObservation(0);
    //                 Debug.Log(0);
    //             }
    //         }
    //     }
    // }

    //Agent.OnActionReceived() method receives actions and assigns the reward.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        int maxHeight = 0;
        bool lose = false;

        //Actions, size = 2
        int movement = actionBuffers.DiscreteActions[0];

        //left
        if (movement == 1)
        {
            gameObject.GetComponent<GameController>().getField().block.move(-1);
            // AddReward(-0.05f);
        }

        //right
        else if (movement == 2)
        {
            gameObject.GetComponent<GameController>().getField().block.move(1);
            // AddReward(-0.05f);
        }

        //up
        else if (movement == 3)
        {
            gameObject.GetComponent<GameController>().getField().block.rotate();
            // AddReward(-0.05f);
        }

        //down
        else if (movement == 4)
        {
            gameObject.GetComponent<GameController>().getField().block.move(0, -1);
            // AddReward(-0.05f);
        }

        //drop
        else if (movement == 5)
        {
            int newRow = gameObject.GetComponent<GameController>().getField().blockDown();

            gameObject.GetComponent<GameController>().getBlock().transform.localPosition = new Vector3(
                gameObject.GetComponent<GameController>().getBlock().transform.localPosition.x, newRow,
                gameObject.GetComponent<GameController>().getBlock().transform.localPosition.z);

            gameObject.GetComponent<GameController>().getField().block.row = newRow;
            gameObject.GetComponent<GameController>().getField().setBlock();
            gameObject.GetComponent<GameController>().getField().isRedrawed = true;
            // AddReward(-0.05f);
        }
        
        

        if (gameObject.GetComponent<GameController>().getField().lineHitFloor == true)
        {
            int lines = gameObject.GetComponent<GameController>().getField().linesMatchedCopy.Count;
            
            // if (lines > 0)
            // {
            //     float lowestLineMatched = (float)((gameObject.GetComponent<GameController>().getField().linesMatchedCopy).Min());
            //     float multiplier = (20f-lowestLineMatched)/5f;
            //     float rewardAdded = lines*lines*10f*multiplier;
                
            //     Debug.Log("Line Cleared - Reward Added: " + rewardAdded.ToString());
            //     AddReward(rewardAdded);
            // }
            // else
            // {
            //     Debug.Log("Block Placed - Reward Added: " + (1f).ToString());
            //     AddReward(1f);
			// }


            int height = 0;
            int holes = 0;
            int bumpiness = 0;

            int currentHeight = 0;
            int previousHeight = 0;
            if ( !pretrained && lines > 0)
            {
                // Debug.Log("Line Cleared - Reward Added: " + (lines*lines).ToString());
                Debug.Log("Number of lines cleared: " + lines.ToString());
                AddReward(lines*lines);
            }


            //compute reward for each block
            for (int m = 0; m < 10; m++)
            {
                currentHeight = 0;

                for (int n = 19; n >= 0; n--)
                {
                    if (currentHeight == 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] > 0)
                    {
                        currentHeight = n + 1;
                        height = height + currentHeight;

                        if (currentHeight > maxHeight)
                        {
                            maxHeight = currentHeight;
                        }
                    }
                    else if (currentHeight > 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] == -1)
                    {
                        holes = holes + 1;
                    }
                }

                if (m != 0)
                {
                    bumpiness = bumpiness + Mathf.Abs(currentHeight - previousHeight);
                }

                previousHeight = currentHeight;
                tetrisHeight = maxHeight;
            }
            currentFitness = -0.51f*height + 0.76f*lines - 0.36f*holes - 0.18f*bumpiness;
            if (!pretrained){
                // Debug.Log("Added Reward: " + (currentFitness-fitness).ToString());
                AddReward(currentFitness-fitness);
            }


            fitness = currentFitness;

            gameObject.GetComponent<GameController>().getField().lineHitFloor = false;
            gameObject.GetComponent<GameController>().getField().linesMatchedCopy.Clear();

            if (maxHeight > 19)
            {
                lose = true;
            }
        }

        if (!pretrained && sharedData.getGameNotOver() == false)
        {
            if (gameObject.GetComponent<GameController>().thisPlayerLost)
            {
                AddReward(-10f);
                Debug.Log("Game Over - Reward Added: " + (-10f).ToString());
                // int rowLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(0);
                // int colLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(1);
                // string arrayString = "";
                // for (int i = 0; i < rowLength; i++)
                // {
                //     for (int j = 0; j < colLength; j++)
                //     {
                //         arrayString += string.Format("{0} ", gameObject.GetComponent<GameController>().getField().grid[i, j]);
                //     }
                //     arrayString += System.Environment.NewLine + System.Environment.NewLine;
                // }

                // Debug.Log(arrayString);
            }
            else
            {
                AddReward(+10f);
                Debug.Log("Game Over - Reward Added: " + (+10f).ToString());
                
                // int rowLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(0);
                // int colLength = gameObject.GetComponent<GameController>().getField().grid.GetLength(1);
                // string arrayString = "";
                // for (int i = 0; i < rowLength; i++)
                // {
                //     for (int j = 0; j < colLength; j++)
                //     {
                //         arrayString += string.Format("{0} ", gameObject.GetComponent<GameController>().getField().grid[i, j]);
                //     }
                //     arrayString += System.Environment.NewLine + System.Environment.NewLine;
                // }

                // Debug.Log(arrayString);
            }
            EndEpisode();
        }
    }

    bool getKey(KeyCode key)
    {
        bool keyDown = Input.GetKeyDown(key);
        bool pressed = Input.GetKey(key) && Time.time - lastKeyDown > 0.5f && Time.time - timeKeyPressed > 0.05f;

        if (keyDown)
        {
            lastKeyDown = Time.time;
        }

        if (pressed)
        {
            timeKeyPressed = Time.time;
        }

        return keyDown || pressed;
    }

    public void FixedUpdate()
    {
        WaitTimeInference();
    }

    void WaitTimeInference()
    {
        if (renderCamera1 != null)
        {
            renderCamera1.Render();
        }
        if (renderCamera2 != null)
        {
            renderCamera2.Render();
        }

        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }

    int getMaxHeight(){
        int height = 0;
        int holes = 0;
        int bumpiness = 0;
        int maxHeightReturned = 0 ;
        int currentHeight = 0;
        int previousHeight = 0;
        // if ( !pretrained && lines > 0)
        // {
        //     // Debug.Log("Line Cleared - Reward Added: " + (lines*lines).ToString());
        //     Debug.Log("Number of lines cleared: " + lines.ToString());
        //     AddReward(lines*lines);
        // }


        //compute reward for each block
        for (int m = 0; m < 10; m++)
        {
            currentHeight = 0;

            for (int n = 19; n >= 0; n--)
            {
                if (currentHeight == 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] > 0)
                {
                    currentHeight = n + 1;
                    height = height + currentHeight;

                    if (currentHeight > maxHeightReturned)
                    {
                        maxHeightReturned = currentHeight;
                    }
                }
                else if (currentHeight > 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] == -1)
                {
                    holes = holes + 1;
                }
            }

            if (m != 0)
            {
                bumpiness = bumpiness + Mathf.Abs(currentHeight - previousHeight);
            }

            previousHeight = currentHeight;
            tetrisHeight = maxHeightReturned;
        }
        return currentHeight;
    }
}