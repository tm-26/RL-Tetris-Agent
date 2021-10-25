using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

//ml agents
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class TetrisAgent : Agent
{    
    private float lastKeyDown;
    private float timeKeyPressed;
    private int current;

    private float fitness;
    private float currentFitness;

    float m_TimeSinceDecision;
    public float timeBetweenDecisionsAtInference;

    [Tooltip("Because we want an observation right before making a decision, we can force " +
        "a camera to render before making a decision. Place the agentCam here if using " +
        "RenderTexture as observations.")]
    public Camera renderCamera;

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

    public override void OnEpisodeBegin()
    {
        gameObject.GetComponent<GameController>().clearField();
        gameObject.GetComponent<GameController>().gameNotOver = true;
        fitness = 0;
    }
    
    // Observations via grid inputs instead of Visual input   
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
        //Actions, size = 2
        int movement = actionBuffers.DiscreteActions[0];
        
        //left
        if (movement == 1)
        {
            gameObject.GetComponent<GameController>().getField().block.move(-1);
        }

        //right
        else if (movement == 2)
        {
            gameObject.GetComponent<GameController>().getField().block.move(1);
        }
        
        //up
        else if (movement == 3)
        {
            gameObject.GetComponent<GameController>().handleAgentRotate();
        }

        //down
        else if (movement == 4)
        {
            gameObject.GetComponent<GameController>().getField().block.move(0, -1);
        }
        
        //drop
        else if (movement == 5)
        {
            int newRow = gameObject.GetComponent<GameController>().getField().blockDown();
            gameObject.GetComponent<GameController>().getBlock().transform.localPosition = new Vector3(gameObject.GetComponent<GameController>().getBlock().transform.localPosition.x, newRow, gameObject.GetComponent<GameController>().getBlock().transform.localPosition.z);
            gameObject.GetComponent<GameController>().getField().block.row = newRow;
            gameObject.GetComponent<GameController>().getField().setBlock();
            gameObject.GetComponent<GameController>().getField().isRedrawed = true;
        }

        if(gameObject.GetComponent<GameController>().getField().lineHitFloor)
        {
            int lines = gameObject.GetComponent<GameController>().getField().linesMatchedCopy.Count;
            
            if (lines > 0)
            {
                float lowestLineMatched = (float)((gameObject.GetComponent<GameController>().getField().linesMatchedCopy).Min());
                float multiplier = (20f-lowestLineMatched)/5f;
                float rewardAdded = lines*lines*10f*multiplier;
                
                Debug.Log("Line Cleared - Reward Added: " + rewardAdded.ToString());
                AddReward(rewardAdded);
            }
            else
            {
                int height = 0;
                int maxHeight = 0;
                int holes = 0;
                int bumpiness = 0;

                int currentHeight = 0;
                int previousHeight = 0;

                for (int m = 0; m < 10; m++)
                {   
                    currentHeight = 0;

                    for (int n = 19; n >= 0; n--)
                    {                
                        if(currentHeight == 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] > 0){
                            currentHeight = n+1;
                            if (currentHeight > maxHeight)
                            {
                                maxHeight = currentHeight;
                            } 
                            height = height + currentHeight;
                        }
                        else if(currentHeight > 0 && gameObject.GetComponent<GameController>().getField().grid[m, n] == -1)
                        {
                            holes = holes + 1;
                        }
                    }

                    if(m != 0)
                    {
                        bumpiness = bumpiness + Mathf.Abs(currentHeight-previousHeight);
                    }
                    previousHeight = currentHeight;
                }

                currentFitness = - 0.50f*maxHeight - 0.05f*holes - 0.30f*bumpiness;
                Debug.Log("Added Reward: " + (currentFitness-fitness+2f).ToString());
                AddReward(currentFitness-fitness+2f);

                fitness = currentFitness;
			}
            

            gameObject.GetComponent<GameController>().getField().lineHitFloor = false;
            gameObject.GetComponent<GameController>().getField().linesMatchedCopy.Clear();
        }

        if (!gameObject.GetComponent<GameController>().gameNotOver)
        {
            Debug.Log("Game Over - Reward Added: " + (-20f).ToString());
            AddReward(-10f);  
            EndEpisode();
        }
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

    public void FixedUpdate()
    {
        WaitTimeInference();
    }

    void WaitTimeInference()
    {
        if (renderCamera != null)
        {
            renderCamera.Render();
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
}
