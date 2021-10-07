using UnityEngine;
using System.Collections;
using HedgehogTeam.EasyTouch;

public class Button : MonoBehaviour {

	public string methodName;
    public GameObject buttonObject;

    private WaitForSeconds waitTime = new WaitForSeconds(0.1f);

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
		if ( gesture.pickedObject == gameObject ) 
		{

            if (buttonObject != null)
            {                
               StartCoroutine(PressButton());                
            }
            else
            {                
                MenuController.instance.CallMethod(methodName);
            }

		}
	}

    private IEnumerator PressButton()
    {
        Vector3 startingScale = buttonObject.transform.localScale;
        Vector3 newScale = new Vector3(0.9f, 0.9f, 1);

        float elapsedTime = 0;
        float time = 0.05f;

        while (elapsedTime < time)
        {
            buttonObject.transform.localScale = Vector3.Lerp(startingScale, newScale, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return waitTime;

        elapsedTime = 0;

        startingScale = newScale;
        newScale = Vector3.one;

        while (elapsedTime < time)
        {
            buttonObject.transform.localScale = Vector3.Lerp(startingScale, newScale, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        MenuController.instance.CallMethod(methodName);
    }

}
