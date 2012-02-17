using UnityEngine;
using System.Collections;

public class RageDude : MonoBehaviour {

    public float moveForce = 200f;
    public float jumpForce = 10f;
	public float gravity = 200f;
	public float airControlTorque=15f;
	
    private bool onGround = true;
	private float lastOnGround=0f;
    private float lastJump = 0f;
    public GameObject scoreCounter;
    public GameObject landscape;
    private bool isGameOver = false;

	void Start () {
    
	}

    void Update()
    {
        bool jump = false;

        if ((Input.GetMouseButtonDown(0) || Input.touches.Length > 0) && Time.time - lastJump > 0.33f)
        {
            lastJump = Time.time;
            jump = true;
        }
        if (jump && (onGround || Time.time - lastOnGround < 0.2f))
        {
            lastJump = Time.time;
            rigidbody.velocity += jumpForce * 0.1f * new Vector3(0.1f,1f,0f);
            onGround = false;
        } 
    }

	void FixedUpdate () {
        if (!isGameOver)
        {
            if (onGround)
            {
                rigidbody.AddTorque(new Vector3(0f, 0f, -moveForce * Time.fixedDeltaTime), ForceMode.Force);
                rigidbody.AddForce(gravity * Vector3.down * Time.fixedDeltaTime, ForceMode.Force);
                if (Mathf.Abs(rigidbody.velocity.y) > 0.001f)
                {
                    collider.material.dynamicFriction = 0.04f + 3f / Mathf.Abs(rigidbody.velocity.y);
                    collider.material.staticFriction = 0.12f + 3f / Mathf.Abs(rigidbody.velocity.y);
                }
            }
            else
            {
                if (Input.GetMouseButton(0) || Input.touchCount > 0)
                {
                    rigidbody.angularVelocity = new Vector3(0f, 0f, -airControlTorque);
                    rigidbody.AddForce(gravity * 0.4f * new Vector3(0.05f, -0.9f) * Time.fixedDeltaTime, ForceMode.Force);
                }
                else
                {
                    rigidbody.AddForce(gravity * Vector3.down * Time.fixedDeltaTime, ForceMode.Force);
                }
            }

        }
        else
        {
            rigidbody.AddForce(gravity * Vector3.down * Time.fixedDeltaTime, ForceMode.Force);
        }
        
    }

    void OnCollisionEnter(Collision collision)
    {
		foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.otherCollider.name == "Landscape")
            {
                onGround = true;
			}
            if (contact.otherCollider.name == "_Rock" && !isGameOver)
            {
                isGameOver = true;
                IRageSpline rageSpline = GetComponent(typeof(RageSpline)) as IRageSpline;
                rageSpline.SetFillColor1(new Color(1f, 0.2f, 0.2f));
                rageSpline.RefreshMesh();
                scoreCounter.SendMessage("GameOver");
                Invoke("GameOverFinal", 2f);
            }
        }

        onGround = true;
	}

    public void GameOverFinal()
    {
        Application.LoadLevel(0);
    }

	
	void OnCollisionExit(Collision collision)
    {
		lastOnGround = Time.time;
        onGround = false;
    }
}
