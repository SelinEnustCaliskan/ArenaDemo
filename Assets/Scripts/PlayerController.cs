using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public float torque;
    private float rotationalSpeed = -90f;
    public Rigidbody2D rb2D;

    public GameObject bullet;
    public Transform shotSpawn;
    
    public float fireRate;
    private float nextFire;

    public GameObject explosionFX;
    public GameObject[] lifeCounters;
    public int noOfLivesLeft;

    public float backfireThrust;
    public bool isHit;

    private WaitForSeconds delay = new WaitForSeconds(0.25f);

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        for (int i = 0; i < BoardController.instance.totalNoOfLives; i++)
        {
            lifeCounters[i].SetActive(true);
        }
        noOfLivesLeft = BoardController.instance.totalNoOfLives;
    }

    private void Update()
    {
        if (BoardController.instance.gameInProgress)
        {
            transform.Rotate(0, 0, Time.deltaTime * rotationalSpeed);
        }

        if (isHit)
        {
            isHit = false;
            StartCoroutine(HitProcessor());
        }
    }

    void FixedUpdate()
    {
        //rb2D.AddTorque(torque);        
    }

    public void FireShot()
    {
        if (Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            Instantiate(bullet, shotSpawn.position, shotSpawn.rotation);       

            rb2D.AddRelativeForce(Vector3.down * backfireThrust);
            rb2D.AddTorque(torque);          
        }
    }    

    private IEnumerator HitProcessor()
    {
        noOfLivesLeft--;
        lifeCounters[noOfLivesLeft].SetActive(false);        

        if (noOfLivesLeft == 0)
        {
            BoardController.instance.gameInProgress = false;
            BoardController.instance.loser = gameObject.tag;
            yield return null;
        }
        else
        {            
            yield return delay;
            explosionFX.SetActive(false);
        }  
    }
}
