using UnityEngine;
using System.Collections;

public class BulletController : MonoBehaviour {

    public Rigidbody2D rb2D;
    public float speed;    

    void Start ()
    {
        rb2D.velocity = speed * transform.up;
	}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
        {
            BoardController.instance.player1.explosionFX.SetActive(true);
            BoardController.instance.player1.isHit = true;
        }
        else if (other.CompareTag("Player2"))
        {
            BoardController.instance.player2.explosionFX.SetActive(true);
            BoardController.instance.player2.isHit = true;
        }
        if (!other.CompareTag("FireButton")) Destroy(gameObject);
    }
}
