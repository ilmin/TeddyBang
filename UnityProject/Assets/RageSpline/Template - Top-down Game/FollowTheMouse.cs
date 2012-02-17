using UnityEngine;
using System.Collections;

public class FollowTheMouse : MonoBehaviour {

    public float maxSpeed;
    public float moveSpeed;
    public float turnSpeed;
    public float origParticleMinSize;
    public float origParticleMaxSize;
    public float origParticleLocalYSpeed;
    public ParticleEmitter emitter;

	// Use this for initialization
	void Start () {
        origParticleMinSize = emitter.minSize;
        origParticleMaxSize = emitter.maxSize;
        origParticleLocalYSpeed = emitter.localVelocity.y;
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 1.0f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        Vector3 dirVec = worldPos - transform.position;

        if (dirVec.magnitude > 1f)
        {
            rigidbody.velocity += dirVec * Time.deltaTime * moveSpeed;
            if (Mathf.Abs(Vector3.Dot(transform.right * -1f, dirVec.normalized)) > 0.01f)
            {
                rigidbody.angularVelocity = new Vector3(0f, 0f, Vector3.Dot(transform.right*-1f, dirVec.normalized) * turnSpeed);
            }
        }

        if (rigidbody.velocity.magnitude > maxSpeed)
        {
            rigidbody.velocity = rigidbody.velocity.normalized * maxSpeed;
        }

        emitter.minSize = origParticleMinSize * (rigidbody.velocity.magnitude / maxSpeed);
        emitter.maxSize = origParticleMaxSize * (rigidbody.velocity.magnitude / maxSpeed);
        emitter.localVelocity = new Vector3(0f, origParticleLocalYSpeed * (rigidbody.velocity.magnitude / maxSpeed), 0f);
	}
}
