using UnityEngine;
using System.Collections;

public class BoardController : MonoBehaviour {

    public static BoardController instance = null;

    public PlayerController player1;
    public PlayerController player2;

    public int totalNoOfLives;
    public bool gameInProgress = true;
    public string loser;

    public GameObject gameOverPopup;
    public Transform player1Image;
    public Transform player2Image;
    public GameObject player1WinText;
    public GameObject player2WinText;

    private WaitForSeconds delay = new WaitForSeconds(0.8f);


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update ()
    {
        if (gameInProgress)
        {
            //if (Input.GetKey(KeyCode.Space))
            //{
            //    player1.FireShot();
            //}
            //if (Input.GetKey(KeyCode.UpArrow))
            //{
            //    player2.FireShot();
            //}
        }
        else
        {
            StartCoroutine(EndGame());
        }
	}

    IEnumerator EndGame()
    {
        player1.rb2D.velocity = Vector2.zero;
        player2.rb2D.velocity = Vector2.zero;

        yield return delay;

        gameOverPopup.SetActive(true);

        if (loser == "Player2")
        {
            player1WinText.SetActive(true);
            player2WinText.SetActive(false);
            player1Image.localScale = new Vector3(1.5f, 1.5f, 1);
        }
        else if (loser == "Player1")
        {
            player1WinText.SetActive(false);
            player2WinText.SetActive(true);
            player2Image.localScale = new Vector3(1.5f, 1.5f, 1);
        }
    }
    
}
