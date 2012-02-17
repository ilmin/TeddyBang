using UnityEngine;
using System.Collections;

public class PropHandler : MonoBehaviour {

    public GameObject landscape;
    private IRageSpline landscapeSpline;
    private ArrayList props;
    public float gameareaWidth = 60f;

    public float rockSpawnFreq = 5f;
    private float curRockSpawnFreq = 5f;
    private float nextRockSpawn;

    public float treeSpawnFreq = 5f;
    private float curTreeSpawnFreq = 5f;
    private float nextTreeSpawn;

    public float cloudSpawnFreq = 5f;
    private float curCloudSpawnFreq = 5f;
    private float nextCloudSpawn;

    public float deadTreeSpawnFreq = 10f;
    private float curDeadTreeSpawnFreq = 10f;
    private float nextDeadTreeSpawn;

    void Awake()
    {
        props = new ArrayList();

        foreach (Prop prop in GameObject.FindSceneObjectsOfType(typeof(Prop)) as Prop[])
        {
            props.Add(prop);
        }

        ResetSpawnTimers();
        StartNewGame();
    }

	void Start () {
        landscapeSpline = landscape.GetComponent(typeof(RageSpline)) as IRageSpline;
        InvokeRepeating("CheckForOutOfBounds", 0.1f, 1f);
	}

    void StartNewGame()
    {
        foreach (Prop prop in props)
        {
            ResetGameObject(prop.gameObject);
            prop.transform.position = new Vector3(-20f - Random.Range(0f, 20f), 0f + Random.Range(-100f, 100f), prop.transform.position.z);
            prop.activeProp = false;
        }
    }
	
	void Update () {
        if (Time.time > nextRockSpawn)
        {
            Spawn("Rock");
            nextRockSpawn = Time.time + Random.Range(curRockSpawnFreq * 0.5f, curRockSpawnFreq * 1.5f);
            curRockSpawnFreq = Mathf.Clamp(rockSpawnFreq * (20f/transform.position.x) + Mathf.Sin(Time.time*3f)*1.2f + 1f, 0.33f, 100f);
        }
        if (Time.time > nextTreeSpawn)
        {
            Spawn("Tree");
            nextTreeSpawn = Time.time + Random.Range(curTreeSpawnFreq * 0.33f, curTreeSpawnFreq * 1.75f);
            curTreeSpawnFreq = treeSpawnFreq + Mathf.Sin(Time.time * 5f) * treeSpawnFreq * 0.8f + 0.5f;
        }
        if (Time.time > nextDeadTreeSpawn)
        {
            Spawn("DeadTree");
            nextDeadTreeSpawn = Time.time + Random.Range(curDeadTreeSpawnFreq * 0.33f, curDeadTreeSpawnFreq * 1.75f);
        }
        if (Time.time > nextCloudSpawn)
        {
            Spawn("Cloud");
            nextCloudSpawn = Time.time + Random.Range(curCloudSpawnFreq * 0.33f, curCloudSpawnFreq * 1.75f);
        }

	}

    private void ResetSpawnTimers()
    {
        curRockSpawnFreq = rockSpawnFreq;
        nextRockSpawn = Time.time + Random.Range(curRockSpawnFreq * 0.5f, curRockSpawnFreq * 1.5f);

        curTreeSpawnFreq = treeSpawnFreq;
        nextTreeSpawn = 2f;

        curDeadTreeSpawnFreq = deadTreeSpawnFreq;
        nextDeadTreeSpawn = Time.time + Random.Range(curDeadTreeSpawnFreq * 0.5f, curDeadTreeSpawnFreq * 1.5f);
    }

    private void CheckForOutOfBounds()
    {
        foreach (Prop prop in props)
        {
            if (prop != null)
            {
                if (prop.transform.position.x < transform.position.x - gameareaWidth * 0.5f)
                {
                    prop.activeProp = false;
					ResetGameObject(prop.gameObject);
                }
            }
        }
    }

    public void Spawn(string name)
	{
		
        Vector3 newPos = transform.position + Vector3.right * gameareaWidth * 0.5f;
        float newSplinePos = landscapeSpline.GetNearestSplinePositionWorldSpace(newPos, 30);

        ArrayList cached = new ArrayList();
        foreach (Prop prop in props)
        {
            if (prop.name == name)
            {
                if (prop.activeProp == true)
                {
                    cached.Add(prop.gameObject);
                }
                else
                {
                    ResetGameObject(prop.gameObject);              
                    PlantGameObjectToLandscape(prop.gameObject, newSplinePos, 0.5f);
                    prop.activeProp = true;

                    if (name == "Rock")
                    {
                        foreach (RageSpline spline in prop.transform.GetComponentsInChildren(typeof(RageSpline)))
                        {
                            //ShuffleSpline(spline, 0.25f);
                            spline.RefreshMesh();
                        }
                    }

                    if (name == "Cloud")
                    {
                        prop.transform.position += Vector3.up * Random.Range(1f, 30f);
                    }
                    return;
                }
            }
        }
        if (cached != null)
        {
            GameObject src = cached[Random.Range(0, cached.Count - 1)] as GameObject;
            if (src != null)
            {
                GameObject newObj = Instantiate(src) as GameObject;
                newObj.name = src.name;
                props.Add(newObj.GetComponent(typeof(Prop)) as Prop);
                PlantGameObjectToLandscape(newObj, newSplinePos, 0.5f);
                if (name == "Cloud")
                {
                    newObj.transform.position += Vector3.up * Random.Range(1f, 30f);
                }
            }
        }
        
    }

    private void ShuffleSpline(IRageSpline rageSpline, float shuffleAmount)
    {
        for (int i = 0; i < rageSpline.GetPointCount(); i++)
        {
            Vector3 point = rageSpline.GetPosition(i);
            point += new Vector3(Random.Range(shuffleAmount*-0.5f, shuffleAmount*0.5f), Random.Range(shuffleAmount*-0.5f, shuffleAmount*0.5f), 0f);
            rageSpline.SetPoint(i, point);
        }
    }

    private void PlantGameObjectToLandscape(GameObject obj, float splinePos, float faceSplineNormal)
    {
        float oldZ = obj.transform.position.z;

        obj.transform.position = landscapeSpline.GetPositionWorldSpace(splinePos);
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, oldZ);
        obj.transform.LookAt(obj.transform.position + transform.forward, (landscapeSpline.GetNormalInterpolated(splinePos)*faceSplineNormal + Vector3.up*(1f-faceSplineNormal)).normalized);

        if (obj.name == "Rock")
        {
            obj.transform.position += new Vector3(Random.Range(-4f, 4f), Random.Range(0f, 5f), 0f);
            obj.rigidbody.velocity = new Vector3(Random.Range(Mathf.Clamp(-0.01f*transform.position.x, -60f, 0f), 0f), 0f, 0f);
        }
    }

    private void ResetGameObject(GameObject obj)
    {
        if (obj != null)
        {
            if (obj.rigidbody != null)
            {
                obj.rigidbody.velocity = new Vector3();
                obj.rigidbody.angularVelocity = new Vector3();
            }
            obj.transform.rotation = Quaternion.identity;
        }
    }

}
