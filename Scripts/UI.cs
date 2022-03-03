using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    private float gameTime = 120;
    private int sheepCount = 25;
    private float score = 0;
    private int minFlock = 13;
    public float energy = 0;
    public Text UIText;
    // Start is called before the first frame update
    void Start()
    {
        //display score and sheep count
    }

    // Update is called once per frame
    void Update()
    {
        //display stats
        UIText.text = ("Time: " + ((int)gameTime + 1) + "\nSheep: " + sheepCount + "\nEnergy: " + (int)energy);

        if (gameTime > 0)
        {
            gameTime -= Time.deltaTime;
            sheepCount = GameObject.FindGameObjectsWithTag("sheep").Length;
            score += sheepCount * Time.deltaTime;
            //update score and sheep count
        }

        //lose condition
        if (sheepCount < minFlock)
        {
            endGame(false);
        }
        
        //win condition
        if (gameTime <= 0)
        {
            endGame(true);
        }
    }
    //stop time and tally up the score

    //scoring
    //(120*25*1) + (2*500) + (25*200) + 999
    //1 ppsps (point per sheep per second)
    //500 bonus points per boulder hit
    //200 bonus points per surviving sheep
    //999 bonus points if all ^ achieved
    void endGame(bool win)
    {
        Time.timeScale = 0;

        if (win == true)
        {
            int sheepBonus = GameObject.FindGameObjectsWithTag("sheep").Length * 200;
            int boulderBonus = GameObject.FindGameObjectsWithTag("boulder").Length * 500;
            int perfectionBonus = 999;
            score += sheepBonus;
            score += boulderBonus;
            if (score >= 9000)
            {
                score += perfectionBonus;
                UIText.text = ("You win!" + "\nSheep saved: " + sheepCount + "\nSheep saved bonus: " + sheepBonus + "\nBoulder hit bonus: " + boulderBonus + "\nPerfection bonus: " + perfectionBonus + "\nFinal Score: " + (int)score);
            }
            else
            {
                UIText.text = ("You win!" + "\nSheep saved: " + sheepCount + "\nSheep saved bonus: " + sheepBonus + "\nBoulder hit bonus: " + boulderBonus + "\nFinal Score: " + (int)score);
            }
        }
        if (win == false)
        {
            UIText.text = ("You lose" + "\nFinal Score: " + (int)score);
        }
        enabled = false;

    }
}
