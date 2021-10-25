using UnityEngine;

public class Block
{
    public int[] pattern = new int[8]; // Rotated pattern.
    public int color;
    public int column = 4;
    public int row = 0;
    public bool isRedrawed = false;
    
    private int rotation = 0;
    private readonly Field field;
    private int[] newPattern = new int[8]; // Pre-Rotated pattern.
    //private readonly System.Random rand = new System.Random();

    private int Width
    {
        get
        {
            return Mathf.Max(pattern[0], pattern[2], pattern[4], pattern[6]);
        }
    }

    public int Height
    {
        get
        {
            return Mathf.Max(pattern[1], pattern[3], pattern[5], pattern[7]);
        }
    }

    public Block(Field field)
    {
        this.field = field;
        reset();
    }

    public void tick()
    {
        move(0,-1);
    }

    public void move(int deltaX=0, int deltaY=0)
    {
        if ((row + deltaY)-Height <= -1) field.checkFloor();

        if (deltaX != 0 && !field.checkIntersec(pattern, column + deltaX, row + deltaY)) // side move
        {
            
            setPosition(deltaX, deltaY);
            return;
        }

        if (deltaY != 0 && field.checkIntersec(pattern, column + deltaX, row + deltaY)) // down move
        {
            field.setBlock();
            return;
        }

        if (deltaY != 0)
        {
            setPosition(deltaX, deltaY);
        }
    }

    private void setPosition(int deltaX = 0, int deltaY = 0)
    {
        column += deltaX;
        row += deltaY;
        if ( (column + Width) > (field.columns - 1)) column = field.columns - Width;; // Move left.
        column = Mathf.Clamp(column, 0, field.columns - 1);
        isRedrawed = true;
    }

    public void reset()
    {
        setBlock(Random.Range(0, 7));
    }

    public bool rotate()
    {
        int newRotation = rotation;
        newRotation++;
        if (newRotation > 3 || newRotation < 0)
        {
            newRotation = 0;
        }

        newPattern = BlocksPatterns.patterns[color][newRotation];
        
        if (!field.checkIntersec(newPattern, column, row)) 
        {
            rotation = newRotation;
            pattern = newPattern;
            // for bounds check
            if (column + Width > field.columns - 1) column = field.columns - Width; // Move left.
            if ( (row - Height) <= 0 )
            {
                return true;
            }
            column = Mathf.Clamp(column, 0, field.columns - 1); 
    
            isRedrawed = true;
        }

        return false;

    }

    private void setBlock(int id)
    {
        column = 5;
        row = field.rows;
        color = id;
        pattern = (int[])BlocksPatterns.patterns[color][0].Clone();
        isRedrawed = true;
    }
}

