using UnityEngine;
using HedgehogTeam.EasyTouch;

public class FireButtonController : MonoBehaviour {

    public PlayerController player;

    //Subscribe to events 
    void OnEnable()
    {
        EasyTouch.On_TouchStart += On_TouchStart;
    }

    //Unsubscribe 
    void OnDisable()
    {
        EasyTouch.On_TouchStart -= On_TouchStart;
    }

    // Unsubscribe 
    void OnDestroy()
    {
        EasyTouch.On_TouchStart -= On_TouchStart;
    }


    // At the touch beginning 
    public void On_TouchStart(Gesture gesture)
    {
        if (gesture.pickedObject == gameObject)
        {
            player.FireShot();
        }
    }
}
