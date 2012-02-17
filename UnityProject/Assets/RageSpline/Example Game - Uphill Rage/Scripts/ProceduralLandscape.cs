using UnityEngine;
using System.Collections;

public class ProceduralLandscape: MonoBehaviour {

    public bool background = false;
    public float backgroundHeight = 10f;
    public int pointCount;
    public float startX;
    public float startX2;
    public float minY;
    public float maxY;
    public float curSteepness = 0.1f;
    public float steepness = 0.1f;
	public Camera cam;
    
    private float pointGapWidth;
    private IRageSpline rageSpline;
    
	// Use this for initialization
	void Start () {
        rageSpline = GetComponent(typeof(RageSpline)) as IRageSpline;
        CreateNewLandscape();
	}

    public void CreateNewLandscape()
    {
        //transform.position = new Vector3(0f,0f,0f);
        
        pointGapWidth = (startX2 - startX) / (float)(pointCount - 1);

        rageSpline.ClearPoints();
        for (int i = 0; i < pointCount; i++)
        {
            float x = startX + (float)i * pointGapWidth;
            curSteepness = steepness + x * 0.00001f;
            rageSpline.AddPointWorldSpace(i, GetNewLandscapePoint(x), transform.right * pointGapWidth * 0.33f);
        }

        rageSpline.SetPoint(0, new Vector3(rageSpline.GetPosition(0).x, 0f, 0f));
        rageSpline.SetPoint(1, new Vector3(rageSpline.GetPosition(1).x, 0f, 0f));

        rageSpline.RefreshMesh();
    }


	// Update is called once per frame
	void FixedUpdate () {
		
        for (int i = 0; i < rageSpline.GetPointCount() - 1; i++)
        {
            if (rageSpline.GetPositionWorldSpace(i + 1).x < cam.transform.position.x - (1024f/768f) * cam.orthographicSize * 2f)
            {
                rageSpline.RemovePoint(i - 1);
                float x = (rageSpline.GetPositionWorldSpace(rageSpline.GetPointCount() - 1).x + pointGapWidth);
                rageSpline.AddPointWorldSpace(rageSpline.GetPointCount(), GetNewLandscapePoint(x), transform.right * pointGapWidth * 0.33f);

                transform.position += new Vector3(pointGapWidth, 0f, 0f);
                for (int a = 0; a < rageSpline.GetPointCount(); a++)
                {
                    rageSpline.SetPoint(a, rageSpline.GetPosition(a) + new Vector3(-pointGapWidth, 0f, 0f));
                }
 
                rageSpline.RefreshMesh();

            }

        }

        curSteepness = steepness + transform.position.x * 0.0001f;
        maxY = Mathf.Clamp(transform.position.x * 0.05f, 0f, 10f);
        
	}

    public Vector3 GetNewLandscapePoint(float x)
    {
        if (background)
        {
            return new Vector3(x, Random.Range(minY, maxY) + x * curSteepness + backgroundHeight, 0f);
        }
        else
        {
            return new Vector3(x, Random.Range(minY, maxY) + x * curSteepness, 0f);
        }
    }
}
