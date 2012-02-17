using UnityEngine;
using System.Collections;

public class Prop : MonoBehaviour {

    public RageSpline mySpline;
    public RageSpline landscape;
    public bool activeProp=false;
	
	// Use this for initialization
	void Start () {
        mySpline = GetComponent(typeof(RageSpline)) as RageSpline;
	}
	
}
