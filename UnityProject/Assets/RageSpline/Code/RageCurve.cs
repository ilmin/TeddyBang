using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RageCurve
{
    public Vector3[] precalcNormals;
    public Vector3[] precalcPositions;
    public RageSplinePoint[] points;

    public RageCurve Clone()
    {
        Vector3[] pts = new Vector3[points.Length];
        Vector3[] ctrl = new Vector3[points.Length * 2];
        float[] width = new float[points.Length];
        bool[] natural = new bool[points.Length];

        for (int i = 0; i < pts.Length; i++)
        {
            pts[i] = points[i].point;
            width[i] = points[i].widthMultiplier;
            ctrl[i] = points[i].inCtrl;
            ctrl[i + 1] = points[i].outCtrl;
            natural[i] = points[i].natural;
        }
        return new RageCurve(pts, ctrl, natural, width);
    }

    public RageCurve(Vector3[] pts, Vector3[] ctrl, bool[] natural, float[] width)
    {
        points = new RageSplinePoint[pts.Length];

        for (int i = 0; i < pts.Length; i++)
        {
            points[i] = new RageSplinePoint(pts[i], ctrl[i * 2], ctrl[i * 2 + 1], width[i], natural[i]);
        }
    }

    public float GetWidth(float t)
    {
        if (points.Length > 0)
        {
            if (t > 0.999f || t < 0f)
            {
                t = mod(t, 0.999f);
            }

            int i = GetFloorIndex(t);

            float c = t * (float)points.Length - (float)i;

            
            if (c < 0.5f)
            {
                c = (c*2) * (c*2) / 2f;
            }
            else
            {
                c = (1f - (1f - (c - 0.5f) * 2f) * (1f - (c - 0.5f) * 2f))/2f + 0.5f;
            }
             

            if (i < points.Length - 1)
            {
                return points[i].widthMultiplier * (1f - c) + points[i + 1].widthMultiplier * c;
            }
            else
            {
                return points[i].widthMultiplier * (1f - c) + points[0].widthMultiplier * c;
            }
        }
        else
        {
            return 0f;
        }
    }
        

    public Vector3 GetNormal(float splinePosition)
    {
        if (splinePosition > 0.999f || splinePosition < 0f)
        {
            splinePosition = mod(splinePosition, 0.999f);
        }
        return precalcNormals[Mathf.Clamp(Mathf.FloorToInt(splinePosition * precalcNormals.Length), 0, precalcNormals.Length-1)];
    }
	
	public Vector3 GetNormalInterpolated(float splinePosition)
    {
        if (splinePosition > 0.999f || splinePosition < 0f)
        {
            splinePosition = mod(splinePosition, 0.999f);
        }
        Vector3 n1 = precalcNormals[Mathf.Clamp(Mathf.FloorToInt(splinePosition * precalcNormals.Length), 0, precalcNormals.Length-1)];
		Vector3 n2 = precalcNormals[Mathf.Clamp(Mathf.FloorToInt(splinePosition * precalcNormals.Length)+1, 0, precalcNormals.Length-1)];
		float subPos = splinePosition * precalcNormals.Length - (float)Mathf.FloorToInt(splinePosition * precalcNormals.Length);
		
		Vector3 norm = n1 * (1f-subPos) + n2 * (subPos);
		norm.Normalize();
		return norm;
    }

    public Vector3 GetNormal(int i)
    {
        if (i >= points.Length || i < 0)
        {
            i = mod(i, points.Length);
        }

        return precalcNormals[Mathf.Clamp(Mathf.FloorToInt(((float)i/(float)points.Length) * precalcNormals.Length), 0, precalcNormals.Length - 1)];
    }

    private Vector3 CalculateNormal(float t, Vector3 up)
    {
        if (points.Length > 0)
        {

            t = Mathf.Clamp01(t);
            float t1 = t - 0.001f;
            t1 = mod(t1, 1f);

            int i1 = GetFloorIndex(t1);
            int i2 = GetCeilIndex(t1);
            float f1 = GetSegmentPosition(t1);

            RageSplinePoint p1 = points[i1];
            RageSplinePoint p2 = points[i2];

            float t2 = t + 0.001f;
            t2 = mod(t2, 1f);

            int i3 = GetFloorIndex(t2);
            int i4 = GetCeilIndex(t2);
            float f2 = GetSegmentPosition(t2);

            RageSplinePoint p3 = points[i3];
            RageSplinePoint p4 = points[i4];

            Vector3 tangent1 = (-3f * p1.point + 9f * (p1.point + p1.outCtrl) - 9f * (p2.point + p2.inCtrl) + 3f * p2.point) * f1 * f1
                + (6f * p1.point - 12f * (p1.point + p1.outCtrl) + 6f * (p2.point + p2.inCtrl)) * f1
                - 3f * p1.point + 3f * (p1.point + p1.outCtrl);

            Vector3 tangent2 = (-3f * p3.point + 9f * (p3.point + p3.outCtrl) - 9f * (p4.point + p4.inCtrl) + 3f * p4.point) * f2 * f2
                + (6f * p3.point - 12f * (p3.point + p3.outCtrl) + 6f * (p4.point + p4.inCtrl)) * f2
                - 3f * p3.point + 3f * (p3.point + p3.outCtrl);

            return Vector3.Cross((tangent1.normalized + tangent2.normalized) * 0.5f, up).normalized;
        }
        else
        {
            return new Vector3(1f, 0f, 0f);
        }
    }

    public Vector3 GetAvgNormal(float t, float dist, int samples)
    {
        Vector3 normal = new Vector3();
        float maxP = 999999f;
        float minP = -999999f;
        int ceil = GetCeilIndex(t);
        int flr = GetFloorIndex(t);

        if (!points[ceil].natural)
        {
            if (ceil > 0)
            {
                maxP = (float)ceil / points.Length - 0.01f;
            }
            else
            {
                maxP = points.Length - 0.01f;
            }
        }

        if (!points[flr].natural)
        {
            if (flr < points.Length-1)
            {
                minP = (float)flr / points.Length + 0.01f;
            }
            else
            {
                minP = 0.01f;
            }
        }

        for (float p = t - dist / 2f; p < t + dist / 2f + dist * 0.5f / (float)samples; p += dist / (float)samples)
        {
            if (p > minP && p < maxP)
            {
                normal += GetNormal(p);    
            }
        }
        return normal.normalized;
    }

    public void setCtrl(int index, int ctrlIndex, Vector3 value)
    {
        if (points[index].natural)
        {
            if (ctrlIndex == 0)
            {
                points[index].inCtrl = value;
                points[index].outCtrl = value * -1f;
            }
            else
            {
                points[index].inCtrl = value * -1f;
                points[index].outCtrl = value;
            }
        }
        else
        {
            if (ctrlIndex == 0)
            {
                points[index].inCtrl = value;
            }
            else
            {
                points[index].outCtrl = value;
            }
        }
    }

    public int GetFloorIndex(float t)
    {
        int i = 0;

        i = Mathf.FloorToInt(t * (float)points.Length);
        
        if (i >= points.Length || i < 0)
        {
            i = mod(i, points.Length);
        }
                
        return i;
    }

    public int GetCeilIndex(float t)
    {
        int i = 0;

        i = Mathf.FloorToInt(t * (float)points.Length) + 1;

        if (i >= points.Length || i < 0)
        {
            i = mod(i, points.Length);
        }

        return i;
    }

    public RageSplinePoint GetRageSplinePoint(int index)
    {
        if (index >= points.Length || index < 0)
        {
            index = mod(index, points.Length);
        }
        return points[index];
    }

    public Vector3 GetPointFast(float splinePosition)
    {
        if (splinePosition > 0.999f || splinePosition < 0f)
        {
            splinePosition = mod(splinePosition, 0.999f);
        }
        return precalcPositions[Mathf.Clamp(Mathf.FloorToInt(splinePosition * (float)precalcPositions.Length), 0, precalcPositions.Length - 1)];
    }

    public Vector3 GetPointFastInterpolated(float splinePosition)
    {
        if (splinePosition > 0.999f || splinePosition < 0f)
        {
            splinePosition = mod(splinePosition, 0.999f);
        }
        Vector3 p1 = precalcPositions[Mathf.Clamp(Mathf.FloorToInt(splinePosition * precalcPositions.Length), 0, precalcPositions.Length - 1)];
        Vector3 p2 = precalcPositions[Mathf.Clamp(Mathf.FloorToInt(splinePosition * precalcPositions.Length) + 1, 0, precalcPositions.Length - 1)];
        float subPos = splinePosition * precalcPositions.Length - (float)Mathf.FloorToInt(splinePosition * precalcPositions.Length);

        Vector3 pos = p1 * (1f - subPos) + p2 * (subPos);
        return pos;
    }

    public Vector3 GetPoint(float t)
    {
        if (t < 0.00001f || t > 0.99999f)
        {
            t = mod(t, 1f);
        }

        int i = GetFloorIndex(t);
        int i2 = GetCeilIndex(t);
        float f = GetSegmentPosition(t);

        RageSplinePoint p1 = points[i];
        RageSplinePoint p2 = points[i2];

        if (Mathf.Approximately(t, 1f))
        {
            Debug.Log(i + "," + i2);
        }

        float d = 1f - f;
        return d * d * d * p1.point + 3f * d * d * f * (p1.point + p1.outCtrl) + 3f * d * f * f * (p2.point + p2.inCtrl) + f * f * f * p2.point;
    }
    
    public float GetSegmentPosition(float t)
    {
        if (t < 0 || t > 1f)
        {
            t = mod(t, 1f);
        }
        int i = GetFloorIndex(t);

        float f = Mathf.Clamp01((t * (float)points.Length) - (float)i);

        return f;
    }

    public Vector3 GetMiddle(int accuracy)
    {
        Vector3 middle = new Vector2();
        for (int i = 0; i < accuracy; i++)
        {
            middle += GetPoint((float)i / (float)accuracy);
        }
        middle = middle * (1f / (float)accuracy);
        return middle;
    }
	
	public float GetLength(int accuracy) {
        float len = 0f;
		Vector3 oldPoint = GetPoint(0f);
        for (int i = 0; i < accuracy; i++)
        {
			Vector3 newPoint = GetPoint((float)i / (float)accuracy);
            len += (newPoint-oldPoint).magnitude;
        }
        return len;
	}
	
    public Vector3 GetMin(int accuracy, float start, float end)
    {
        start = Mathf.Clamp01(start);
        end = Mathf.Clamp01(end);

        Vector3 min = new Vector3(99999999f, 99999999f, 99999999f);
        for (int i = 0; i < accuracy; i++)
        {
            Vector3 p = GetPoint(((float)i / (float)accuracy) * (end - start) + (start));
            if (p.x < min.x)
            {
                min.x = p.x;
            }
            if (p.y < min.y)
            {
                min.y = p.y;
            }
            if (p.z < min.z)
            {
                min.z = p.z;
            }
        }
        return min;
    }

    public Vector3 GetMax(int accuracy, float start, float end)
    {
        start = Mathf.Clamp01(start);
        end = Mathf.Clamp01(end);

        Vector3 max = new Vector3(-99999999f, -99999999f, -99999999f);
        for (int i = 0; i < accuracy; i++)
        {
            Vector3 p = GetPoint(((float)i / (float)accuracy) * (end - start) + (start));
            if (p.x > max.x)
            {
                max.x = p.x;
            }
            if (p.y > max.y)
            {
                max.y = p.y;
            }
            if (p.z > max.z)
            {
                max.z = p.z;
            }
        }
        return max;
    }

    public Vector3[] GetSmoothCtrlForNewPoint(float splinePosition)
    {
        int newIndex = GetCeilIndex(splinePosition);
        float segmentPos = GetSegmentPosition(splinePosition);

        
        Vector3 p = GetPoint(splinePosition);
        Vector3 p0 = points[mod(newIndex - 1, points.Length)].point;
        Vector3 p3 = points[mod(newIndex, points.Length)].point;
        Vector3 p1 = p0 + points[mod(newIndex - 1, points.Length)].outCtrl;
        Vector3 p2 = p3 + points[mod(newIndex, points.Length)].inCtrl;
        //Debug.Log("p:" + p + ",p0:" + p0 + ",p1:" + p1 + ",p2:" + p2 + ",p3:" + p3);
        
        Vector3 bp0 = Vector3.Lerp(p0, p1, segmentPos);
        Vector3 bp1 = Vector3.Lerp(p1, p2, segmentPos);
        Vector3 bp2 = Vector3.Lerp(p2, p3, segmentPos);

        Vector3 cp0 = Vector3.Lerp(bp0, bp1, segmentPos);
        Vector3 cp1 = Vector3.Lerp(bp1, bp2, segmentPos);

        Vector3[] values = new Vector3[2];
        values[0] = cp0 - p;
        values[1] = cp1 - p;

        return values; 
    }

    public int AddRageSplinePoint(float splinePosition)
    {
        RageSplinePoint[] tmpPoints = new RageSplinePoint[points.Length + 1];
        
        int newIndex = GetCeilIndex(splinePosition);
                
        Vector3 tangent = (GetPoint(splinePosition + 0.001f) - GetPoint(splinePosition - 0.001f)).normalized;
        float mag = points[mod(newIndex-1, points.Length)].outCtrl.magnitude * 0.25f + points[mod(newIndex, points.Length)].inCtrl.magnitude * 0.25f;
        tmpPoints[newIndex] = new RageSplinePoint(GetPoint(splinePosition), mag * tangent * -1f, mag * tangent, GetWidth(splinePosition), true);
        
        //tmpPoints[newIndex] = new RageSplinePoint(GetPoint(splinePosition), GetSmoothCtrlForNewPoint(splinePosition)[0], GetSmoothCtrlForNewPoint(splinePosition)[1], GetWidth(splinePosition), false);

        for (int i = 0; i < tmpPoints.Length; i++)
        {
            if (i < newIndex)
            {
                tmpPoints[i] = points[i];
            }
            if (i > newIndex)
            {
                tmpPoints[i] = points[i-1];
            }
        }
        points = tmpPoints;
        return newIndex;
    }

    public void AddRageSplinePoint(int index, Vector3 position)
    {
        RageSplinePoint[] tmpPoints = new RageSplinePoint[points.Length + 1];
        float splinePosition = (float)index / (float)points.Length + 1f / (float)points.Length;
        Vector3 tangent = position - GetPoint(splinePosition - 0.001f).normalized;
        float mag = (points[mod(index, points.Length)].point - points[mod(index + 1, points.Length)].point).magnitude * 0.25f;
        tmpPoints[index] = new RageSplinePoint(position, mag * tangent * -1f, mag * tangent, GetWidth(splinePosition), true);
        for (int i = 0; i < tmpPoints.Length; i++)
        {
            if (i < index)
            {
                tmpPoints[i] = points[i];
            }
            if (i > index)
            {
                tmpPoints[i] = points[i - 1];
            }
        }
        points = tmpPoints;
    }

    public void ClearPoints()
    {
        points = new RageSplinePoint[0];
    }

    public void AddRageSplinePoint(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width,  bool natural)
    {
        RageSplinePoint[] tmpPoints = new RageSplinePoint[points.Length + 1];
        tmpPoints[index] = new RageSplinePoint(position, inCtrl, outCtrl, width, natural);
        for (int i = 0; i < tmpPoints.Length; i++)
        {
            if (i < index)
            {
                tmpPoints[i] = points[i];
            }
            if (i > index)
            {
                tmpPoints[i] = points[i - 1];
            }
        }
        points = tmpPoints;
    }

    public void DelPoint(int index)
    {
        if (points.Length > 2)
        {
            RageSplinePoint[] tmpPoints = new RageSplinePoint[points.Length - 1];
            for (int i = 0; i < tmpPoints.Length; i++)
            {
                if (i < index)
                {
                    tmpPoints[i] = points[i];
                }
                if (i >= index)
                {
                    tmpPoints[i] = points[i + 1];
                }
            }
            points = tmpPoints;
        }
    }

    public float GetNearestSplinePoint(Vector3 position, int accuracy)
    {
        float nearestSqrDist = 99999999999f;
        float nearestPoint = 0f;
        for (int i = 0; i < accuracy; i++)
        {
            Vector3 p = GetPoint((float)i / (float)accuracy);
            if ((position - p).sqrMagnitude < nearestSqrDist)
            {
                nearestPoint = (float)i / (float)accuracy;
                nearestSqrDist = (position - p).sqrMagnitude;
            }
        }
        return nearestPoint;
    }
	
	public int GetNearestSplinePointIndex(float splinePosition)
    {
        float segmentPos = GetSegmentPosition(splinePosition);
		if(segmentPos > 0.5f) {
			return GetCeilIndex(splinePosition);
		} else {
			return GetFloorIndex(splinePosition);
		}
    }
    
    public void PrecalcNormals(int points)
    {
        precalcNormals = new Vector3[points];
        Vector3 up = new Vector3(0f, 0f, -1f);
        for (int i = 0; i < points; i++)
        {
            precalcNormals[i] = CalculateNormal((float)i / (float)(points-1), up);
        }
    }

    public void PrecalcPositions(int points)
    {
        precalcPositions = new Vector3[points];
        for (int i = 0; i < points; i++)
        {
            precalcPositions[i] = GetPoint((float)i / (float)(points - 1));
            Debug.Log("precalcPositions["+i+"]=" + precalcPositions[i]);
        }
        
    }

    public void ForceZeroZ()
    {
        foreach (RageSplinePoint point in points)
        {
            if(!Mathf.Approximately(point.point.z, 0f)) {
                point.point = new Vector3(point.point.x, point.point.y, 0f);
            }
            if (!Mathf.Approximately(point.inCtrl.z, 0f))
            {
                point.inCtrl = new Vector3(point.inCtrl.x, point.inCtrl.y, 0f);
            }
            if (!Mathf.Approximately(point.outCtrl.z, 0f))
            {
                point.outCtrl = new Vector3(point.outCtrl.x, point.outCtrl.y, 0f);
            }
        }
    }

    private int mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    private float mod(float x, float m)
    {
        return (x % m + m) % m;
    }

    public bool Intersects(Vector3 limitStart, Vector3 limitDirection, Vector3 vecStart, Vector3 vec)
    {
        float side1 = GetVectorSide(limitStart, limitDirection, vecStart);
        float side2 = GetVectorSide(limitStart, limitDirection, vecStart + vec);

        if (side1 > 0f && side2 < 0f || side1 < 0f && side2 > 0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector3 LimitToOtherSideOfVector(Vector3 limitStart, Vector3 limitDirection, Vector3 vecStart, Vector3 vec)
    {
        float side1 = GetVectorSide(limitStart, limitDirection, vecStart);
        float side2 = GetVectorSide(limitStart, limitDirection, vecStart + vec);
                
        if (side1 > 0f && side2 < 0f || side1 < 0f && side2 > 0f)
        {
            return Intersect(limitStart, limitStart+limitDirection, vecStart, vecStart + vec);
        }
        else
        {
            return vecStart+vec;
        }
    }

    public float GetVectorSide(Vector3 offset, Vector3 vec, Vector3 position)
    {
        Vector3 A = offset;
        Vector3 B = offset + vec;
        Vector3 C = position;
        return (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x);
    }

    public Vector3 Intersect(Vector3 line1V1, Vector3 line1V2, Vector3 line2V1, Vector3 line2V2)
    {
        //Line1
        float A1 = line1V2.y - line1V1.y;
        float B1 = line1V1.x - line1V2.x;
        float C1 = A1 * line1V1.x + B1 * line1V1.y;

        //Line2
        float A2 = line2V2.y - line2V1.y;
        float B2 = line2V1.x - line2V2.x;
        float C2 = A2 * line2V1.x + B2 * line2V1.y;

        float det = A1 * B2 - A2 * B1;

        if (det == 0)
        {
            return line2V2;//parallel lines
        }
        else
        {
            float x = (B2 * C1 - B1 * C2) / det;
            float y = (A1 * C2 - A2 * C1) / det;
            return new Vector3(x, y, 0);
        }
    }

}

[System.Serializable]
public class RageSplinePoint
{
    public Vector3 point, inCtrl, outCtrl;
    public float widthMultiplier = 1f;
    public bool natural;

    public RageSplinePoint Clone()
    {
        return new RageSplinePoint(point, inCtrl, outCtrl, widthMultiplier, natural);
    }

    public RageSplinePoint(Vector3 point, Vector3 inCtrl, Vector3 outCtrl, float width, bool natural)
    {
        this.point = point;
        this.inCtrl = inCtrl;
        this.outCtrl = outCtrl;
        this.widthMultiplier = width;
        this.natural = natural;
    }
    
}


