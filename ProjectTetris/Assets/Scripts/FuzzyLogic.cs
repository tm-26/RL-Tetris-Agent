using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuzzyLogic : MonoBehaviour
{
    public AnimationCurve professional;
    public AnimationCurve med;
    public AnimationCurve amateur;

    private float amateurValue = 0f;
    private float medValue = 0f;
    private float professionalValue = 0f;


    private static float amateur1;
    private static float professional1;
    private static float med1;


    public void Update()
    {
        //Get skillFactor
        float skillFactor = gameObject.GetComponent<GameController>().skillFactor; 
        float inputValue = skillFactor;
        
        amateurValue = amateur.Evaluate(inputValue);
        medValue = med.Evaluate(inputValue);
        professionalValue = professional.Evaluate(inputValue);

        amateur1 = amateurValue;
        med1 = medValue;
        professional1 = professionalValue;
    }


    public float EvalAmateur(float skillFactor)
    {
        amateur1 = amateur.Evaluate(skillFactor);
        return amateur1;
    }

    public float EvalMed(float skillFactor)
    {
        med1 = med.Evaluate(skillFactor);
        return med1;
    }

    public float EvalPro(float skillFactor)
    {
        professional1 = professional.Evaluate(skillFactor);
        return professional1;
    }
}
