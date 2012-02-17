using UnityEngine;
using System.Collections;

public class CameraFollow2 : MonoBehaviour {

    public GameObject objectToFollow;
    private Vector3 origOffset;
    
    public float followSpeed = 0.5f;

	// Use this for initialization
	void Start () {
        /*
		iPhoneKeyboard.autorotateToPortrait = false; 
		iPhoneKeyboard.autorotateToPortraitUpsideDown = false; 
		iPhoneKeyboard.autorotateToLandscapeRight = false; 
		iPhoneKeyboard.autorotateToLandscapeLeft = false;
        */
        origOffset = - objectToFollow.transform.position + transform.position;
        
	}
	
	// Update is called once per frame
	void Update() {
        float z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, objectToFollow.transform.position + origOffset, Time.smoothDeltaTime * followSpeed);
        transform.position = new Vector3(transform.position.x, transform.position.y, z);
	}

    
}
