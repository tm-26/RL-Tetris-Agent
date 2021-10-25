using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Field
{
    public int[,] grid;
    public int[,] pastGrid;
    public int columns;
    public int rows;
    public int score = 0;
    public Block block;
    public bool isRedrawed = false;
    public bool lineHitFloor = false;
    public List<int> linesMatched = new List<int>();
    public List<int> linesMatchedCopy = new List<int>();
    public int scoreToAdd = 0;

    readonly GameController gc = (GameController) Object.FindObjectOfType(typeof(GameController));

    public Field()
    {
        reset();
    }

    private void reset()
    {
        columns = 10;
        rows = 20;
        grid = new int[columns, rows];
        // grid[0,0] = 1;
        block = new Block(this);

        for (int m = 0; m < columns; m++)
        {
            for (int n = 0; n < rows; n++)
            {
                grid[m, n] = -1;
            }
        }

        isRedrawed = true;
    }

    public void checkFloor()
    {
        if ((block.row - block.Height) <= 0) setBlock();
    }

    public void setBlock()
    {
        pastGrid = new int[columns, rows];

        for (int m = 0; m < columns; m++)
        {
            for (int n = 0; n < rows; n++)
            {
                pastGrid[m, n] = grid[m, n];
            }
        }

        for (int i = 0; i <= 3; i++)
        {
            int c = block.column + block.pattern[i * 2] - 1;
            int r = block.row - block.pattern[i * 2 + 1];
            if (r < 0 || r > rows - 1) continue;
            if (c < 0 || c > columns - 1) continue;
            grid[c, r] = block.color;
            if (r == 19)
            {
                gc.gameOver();
            }
        }

        if (sharedData.getGameNotOver())
        {
            gc.audioSource.clip = gc.goingDownSound;
            gc.audioSource.Play();
        }

        block.reset();
        linesCheck();
        isRedrawed = true;
    }

    private void linesCheck()
    {
        lineHitFloor = true;
        bool isCheck = true;

        while (isCheck)
        {
            isCheck = false;

            for (int r = 0; r < rows; r++)
            {
                int rowFill = 0;

                for (int c = 0; c < columns; c++)
                {
                    if (grid[c, r] > -1)
                    {
                        rowFill++;
                    }
                }

                if (rowFill >= columns)
                {
                    linesMatched.Add(r);
                    linesMatchedCopy.Add(r);

                    sharedData.setLinesCleared(sharedData.getLinesCleared() + 1);
                    ArrayUtils.removeLine(grid, r);
                    isCheck = true;
                    break;
                }
            }
        }

        score += getScore(linesMatched.Count);
    }

    public void moveUp(int lineNum, int[,] pastGrid)
    {
        bool foundWhite = false;

        //Checks if white blocks are already present, if so we are not going to put to other side
        for (int c = 0; c < columns; c++)
        {
            if (pastGrid[c, lineNum] == 7)
                foundWhite = true;
        }

        if (!foundWhite)
        {
            //deep copy of the current grid
            int[,] test = new int[columns, rows];

            for (int m = 0; m < columns; m++)
            {
                for (int n = 0; n < rows; n++)
                {
                    test[m, n] = grid[m, n];
                }
            }

            //moving things up
            for (int r = 1; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    //Add to new field
                    grid[c, r] = test[c, r - 1];
                }
            }

            //if in past grid of the other side, it was empty (was about to complete that row), keep it empty on this side
            //else, white block
            for (int c = 0; c < columns; c++)
            {
                if (pastGrid[c, lineNum] == -1)
                {
                    grid[c, 0] = -1;
                }
                else
                {
                    grid[c, 0] = 7;
                }
            }

            //update pastGrid to current grid
            pastGrid = new int[columns, rows];

            for (int m = 0; m < columns; m++)
            {
                for (int n = 0; n < rows; n++)
                {
                    pastGrid[m, n] = grid[m, n];
                }
            }
        }

        for (int c = 0; c < columns; c++)
        {
            if (grid[c, 19] != -1)
            {
                Debug.Log("game over");
                gc.gameOver();
            }
        }
    }

    public bool checkIntersec(int[] pat, int column = 0, int row = 0)
    {
        for (int i = 0; i <= 3; i++)
        {
            int c = column + pat[i * 2] - 1;
            int r = row - pat[i * 2 + 1];
            if (r < 0 || r >= rows - 1) continue;
            if (c < 0 || c >= columns) continue;
            if (grid[c, r] >= 0) return true;
        }

        return false;
    }

    // TODO: move to Block class.
    // Return stop row.
    public int blockDown()
    {
        int row = block.row;

        int h = block.Height;

        while (row > h - 1)
        {
            if (checkIntersec(block.pattern, block.column, row)) return row + 1;
            row--;
        }

        return block.Height;
    }

    public int getScore(int cleared)
    {
        int level = sharedData.getLevel();
        int addVal = 0;

        if (cleared == 1)
        {
            addVal = 40 * (level + 1);
        }
        else if (cleared == 2)
        {
            addVal = 100 * (level + 1);
        }
        else if (cleared == 3)
        {
            addVal = 300 * (level + 1);
        }
        else if (cleared == 4)
        {
            addVal = 1200 * (level + 1);
        }

        scoreToAdd = addVal;

        return addVal;
    }

    public void resetScoreToAdd()
    {
        scoreToAdd = 0;
    }

    public float currHeight()
    {
        int tetrisHeight = 0;
        if (lineHitFloor == true)
        {
            int maxHeight = 0;
            int height = 0;
            int currentHeight = 0;
            int previousHeight = 0;


            //compute reward for each block
            for (int m = 0; m < 10; m++)
            {
                currentHeight = 0;

                for (int n = 19; n >= 0; n--)
                {
                    if (currentHeight == 0 && grid[m, n] > 0)
                    {
                        currentHeight = n + 1;
                        height = height + currentHeight;

                        if (currentHeight > maxHeight)
                        {
                            maxHeight = currentHeight;
                        }
                    }
                }

                previousHeight = currentHeight;
                tetrisHeight = maxHeight;
            }
        }
        
        return tetrisHeight;
    }
}