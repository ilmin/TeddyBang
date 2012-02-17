using UnityEngine;
using System.Collections;

public class ScoreCounter : MonoBehaviour {

    public GUIText guitext;
    public GameObject hero;
    private bool isGameOver = false;
	// Use this for initialization
	void Start () {
        guitext.text = "Score: 0";
	}
	
	// Update is called once per frame
	void Update () {
        if ((Mathf.FloorToInt(hero.transform.position.x) + 1) > 10 && !isGameOver)
        {
            guitext.text = "Score: " + (Mathf.FloorToInt(hero.transform.position.x) + 1);
        }
	}

    public void GameOver()
    {
        isGameOver = true;
        guitext.material.SetColor("_Color", new Color(0f, 0.75f, 0f));
    }
}
