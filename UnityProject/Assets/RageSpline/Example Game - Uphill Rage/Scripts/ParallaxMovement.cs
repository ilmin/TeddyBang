using UnityEngine;
using System.Collections;

public class ParallaxMovement : MonoBehaviour {

    public Camera cam;
    private Vector3 lastCameraPosition;
	private Prop prop;
    public float amount;

	// Use this for initialization
	void Start () {
		prop = GetComponent(typeof(Prop)) as Prop;
        lastCameraPosition = cam.transform.position;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if(prop == null || prop.activeProp) {
        	Vector3 camMove = cam.transform.position - lastCameraPosition;
        	transform.position += camMove * (1f-amount);
        	lastCameraPosition = cam.transform.position;
		}
	}
}
