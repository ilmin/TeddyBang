using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class RageSpline : MonoBehaviour, IRageSpline
{
    public enum Outline { None = 0, Loop, Free };
    public Outline outline = Outline.Loop;
    public Color outlineColor1 = Color.black;
    public Color outlineColor2 = Color.black;
    public float OutlineWidth = 1f;
    public float outlineTexturingScale = 10f;
    public enum OutlineGradient { None = 0, Default, Inverse }
    public OutlineGradient outlineGradient = OutlineGradient.None;
    public float outlineNormalOffset = 0f;
	public enum Corner { Default = 0, Beak };
    public Corner corners;
		
    public enum Fill { None = 0, Solid, Gradient, Landscape };
    public Fill fill = Fill.Solid;
    public float landscapeBottomDepth = 10f;
    public Color fillColor1 = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color fillColor2 = new Color(0.4f, 0.4f, 0.4f, 1f);

    public enum UVMapping { None = 0, Fill, Outline };
    public UVMapping UVMapping1 = UVMapping.None;
    public UVMapping UVMapping2 = UVMapping.None;

    public Vector2 gradientOffset = new Vector2(-5f,5f);
    public float gradientAngle = 0f;
    public float gradientScale = 0.1f;
    public Vector2 textureOffset = new Vector2(-5f,-5f);
    public float textureAngle = 0f;
    public float textureScale = 0.1f;
    public Vector2 textureOffset2 = new Vector2(5f, 5f);
    public float textureAngle2 = 0f;
    public float textureScale2 = 0.1f;

    public enum Emboss { None = 0, Sharp, Blurry };
    public Emboss emboss = Emboss.None;
    public Color embossColor1 = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color embossColor2 = new Color(0.25f, 0.25f, 0.25f, 1f);
    public float embossAngle = 180f;
    public float embossOffset = 0.5f;
    public float embossSize = 10f;
    public float embossCurveSmoothness = 10f;
        
    public enum Physics { None = 0, Boxed, MeshCollider, OutlineMeshCollider };
    public Physics physics = Physics.None;
    public bool createPhysicsInEditor=false;
    public bool createConvexMeshCollider = false;
    public PhysicMaterial physicsMaterial=null;

    public int vertexCount = 64;
    public int physicsColliderCount = 32;

    public float colliderZDepth = 100f;
    public float colliderNormalOffset = 0f;
    
    public float boxColliderDepth = 1f;
    private BoxCollider[] boxColliders;

    public int lastPhysicsVertsCount = 0;
    
    public float antiAliasingWidth = 0.5f;
    public float landscapeOutlineAlign = 1f;

    public bool showSplineGizmos=true;
    public bool showOtherGizmos=true;
    public float maxBeakLength = 3f;

	public bool optimize = false;
	public float optimizeAngle = 5.0f;

    public enum ShowCoordinates { None = 0, Local, World };
    public ShowCoordinates showCoordinates = ShowCoordinates.None;
	public float gridSize=5f;
	public Color gridColor=new Color(1f,1f,1f,0.2f);
	public bool showGrid=true;
	public int gridExpansion=2;
	public bool snapToGrid=true;
	public bool snapHandlesToGrid=false;
    public bool showNumberInputs = false;

    public RageSplineStyle style;
    public string defaultStyleName="Stylename";

    public bool styleLocalGradientPositioning;
    public bool styleLocalTexturePositioning;
    public bool styleLocalEmbossPositioning;
    public bool styleLocalAntialiasing;
    public bool styleLocalVertexCount;
    public bool styleLocalPhysicsColliderCount;

    public bool showWireFrameInEditor = false;
    public bool hideHandles = false;

    public RageCurve spline;
    public bool lowQualityRender = false;
    public bool inverseTriangleDrawOrder = false;
    public float overLappingVertsShift=0.01f;
    private ArrayList overLappingVerts;

    static private Quaternion normalRotationQuat = Quaternion.AngleAxis(90, Vector3.forward);
	
    public struct RageVertex
    {
        public Vector3 position;
        public Vector2 uv1;
        public Vector2 uv2;
        public Color color;
        public Vector3 normal;
        public float splinePosition;
        public float splineSegmentPosition;
        public RageSplinePoint curveStart;
        public RageSplinePoint curveEnd;
    }

    public void Awake()
    {
        if (spline == null)
        {
            CreateDefaultSpline();
        }
        overLappingVerts = new ArrayList();

        MeshFilter meshFilter = GetComponent(typeof(MeshFilter)) as MeshFilter;

        if (!Application.isPlaying)
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }
            RefreshMesh();
        } 
        else 
        {
            if (meshFilter == null)
            {
                RefreshMesh();
            }
            else
            {
                if (meshFilter.sharedMesh == null)
                {
                    RefreshMesh();
                }
                else
                {
                    if (meshFilter.mesh.vertexCount == 0)
                    {
                        RefreshMesh();
                    }

                    if (spline == null)
                    {
                        CreateDefaultSpline();
                    }

                    spline.PrecalcNormals(GetVertexCount() + 1);

                    if (GetPhysics() == Physics.Boxed)
                    {
                        CreateBoxCollidersCache();
                    }

                    if (!GetCreatePhysicsInEditor() && GetPhysics() != Physics.None)
                    {
                        RefreshPhysics();
                    }
                }
            }
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (showSplineGizmos)
        {
            DrawSplineGizmos();
        }
		if(showGrid && showCoordinates != RageSpline.ShowCoordinates.None) 
		{
			DrawGrid();
		}
        if (showOtherGizmos)
        {
            if (GetEmboss() != Emboss.None)
            {
                DrawEmbossGizmos();
            }
            if (GetFill() == Fill.Gradient)
            {
                DrawGradientGizmos();
            }
            if (GetTexturing1() != UVMapping.None)
            {
                if (GetTexturing1() == UVMapping.Fill && GetFill() != Fill.None)
                {
                    DrawTexturingGizmos();
                }
            }
            if (GetTexturing2() != UVMapping.None)
            {
                if (GetTexturing2() == UVMapping.Fill && GetFill() != Fill.None)
                {
                    DrawTexturingGizmos2();
                }
            }
        }
	}

    

    public void RefreshMesh()
    {
        // Make sure the vertex count is even
        SetVertexCount(GetVertexCount());

        if (overLappingVerts == null)
        {
            overLappingVerts = new ArrayList();
        }
        if (GetComponent(typeof(MeshFilter)) as MeshFilter == null)
        {
            gameObject.AddComponent(typeof(MeshFilter));
        }
        if (GetComponent(typeof(MeshRenderer)) as MeshRenderer == null)
        {
            MeshRenderer meshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            Material mat = Resources.LoadAssetAtPath("Assets/RageSpline/Materials/RageSplineMaterial.mat", typeof(Material)) as Material;
            meshRenderer.sharedMaterial = mat;
        }
        if(Mathf.Abs(gameObject.transform.localScale.x)>0f && Mathf.Abs(gameObject.transform.localScale.y)>0f && Mathf.Abs(gameObject.transform.localScale.z)>0f) {
            if (spline == null)
            {
                CreateDefaultSpline();
            }
            spline.PrecalcNormals(GetVertexCount()+1);
			GenerateMesh(true);
			RefreshPhysics();
		}
    }

    public void RefreshMesh(bool refreshFillTriangulation, bool refreshNormals, bool refreshPhysics)
    {
        // Make sure the vertex count is even
        SetVertexCount(GetVertexCount());

        //Debug.Log("RefreshMesh " + refreshFillTriangulation + "," + refreshNormals + "," + refreshPhysics);
        if (Mathf.Abs(gameObject.transform.localScale.x) > 0f && Mathf.Abs(gameObject.transform.localScale.y) > 0f && Mathf.Abs(gameObject.transform.localScale.z) > 0f)
        {
            if (refreshNormals)
            {
                spline.PrecalcNormals(GetVertexCount() + 1);
            }

            GenerateMesh(refreshFillTriangulation);
            
            if (refreshPhysics)
            {
                RefreshPhysics();
            }
        }
    }

    public void RefreshMeshInEditor(bool forceRefresh, bool triangulation, bool precalcNormals)
    {
        // Make sure the vertex count is even
        SetVertexCount(GetVertexCount());

        if (overLappingVerts == null)
        {
            overLappingVerts = new ArrayList();
        } 
        if (!forceRefresh && GetVertexCount() > 128)
        {
            lowQualityRender = true;
        }
        else
        {
            lowQualityRender = false;
        }
        if (GetVertexCount() <= 128 || forceRefresh)
        {
            if (GetComponent(typeof(MeshFilter)) as MeshFilter == null)
            {
                gameObject.AddComponent(typeof(MeshFilter));
            }
            if (GetComponent(typeof(MeshRenderer)) as MeshRenderer == null)
            {
                MeshRenderer meshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                Material mat = Resources.LoadAssetAtPath("Assets/RageSpline/Materials/RageSplineMaterial.mat", typeof(Material)) as Material;
                meshRenderer.sharedMaterial = mat;
            }
            if (Mathf.Abs(gameObject.transform.localScale.x) > 0f && Mathf.Abs(gameObject.transform.localScale.y) > 0f && Mathf.Abs(gameObject.transform.localScale.z) > 0f)
            {
                if (forceRefresh)
                {
                    if (!pointsAreInClockWiseOrder())
                    {
                        flipPointOrder();
                    }
                }

                if (precalcNormals)
                {
                    spline.PrecalcNormals(GetVertexCount() + 1);
                }


                GenerateMesh(triangulation);
                if (forceRefresh)
                {
                    RefreshPhysics();
                }
            }
        }
        
    }


    public void GenerateMesh(bool refreshTriangulation)
    {
        ForceZeroZ();

        if (GetFill() != Fill.None)
        {
            ShiftOverlappingControlPoints();
        }

        bool fillAntialiasing = false;
        if (GetAntialiasingWidth() > 0f && (GetOutline() == Outline.None || Mathf.Abs(GetOutlineNormalOffset())>0f) || inverseTriangleDrawOrder)
        {
            fillAntialiasing = true;
        }

        bool outlineAntialiasing = false;
        if (GetAntialiasingWidth() > 0f)
        {
            outlineAntialiasing = true;
        }

        bool embossAntialiasing = false;
        if (GetAntialiasingWidth() > 0f)
        {
            embossAntialiasing = true;
        }

        bool multipleMaterials = false;
        MeshRenderer renderer = GetComponent(typeof(MeshRenderer)) as MeshRenderer;
        if(renderer != null) {
            if (renderer.sharedMaterials.GetLength(0) > 1)
            {
                multipleMaterials = true;
            }
        }

        RageVertex[] outlineVerts = GenerateOutlineVerts(outlineAntialiasing, multipleMaterials);
        RageVertex[] fillVerts = GenerateFillVerts(fillAntialiasing, multipleMaterials);
        RageVertex[] embossVerts = GenerateEmbossVerts(embossAntialiasing);
        
        int vertexCount = outlineVerts.Length + fillVerts.Length + embossVerts.Length;
        Vector3[] verts = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector2[] uvs2 = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];

        int v = 0;
        for (int i = 0; i < fillVerts.Length; i++)
        {
            verts[v] = fillVerts[i].position;
            uvs[v] = fillVerts[i].uv1;
            uvs2[v] = fillVerts[i].uv2;
            colors[v] = fillVerts[i].color;
            v++;
        }
        for (int i = 0; i < embossVerts.Length; i++)
        {
            verts[v] = embossVerts[i].position;
            uvs[v] = embossVerts[i].uv1;
            uvs2[v] = embossVerts[i].uv2;
            colors[v] = embossVerts[i].color;
            v++;
        }
        for (int i=0; i < outlineVerts.Length; i++)
        {
            verts[v] = outlineVerts[i].position;
            uvs[v] = outlineVerts[i].uv1;
            uvs2[v] = outlineVerts[i].uv2;
            colors[v] = outlineVerts[i].color;
            v++;
        }
        
        /*
        if (inverseTriangleDrawOrder && refreshTriangulation)
        {
            int len = triangles.Length;
            int[] triangles2 = new int[triangles.Length];
            for (int i = 0; i < len; i+=3)
            {
                triangles2[len - i - 3] = triangles[i];
                triangles2[len - i - 3 + 1] = triangles[i+1];
                triangles2[len - i - 3 + 2] = triangles[i+2];
            }
            triangles = triangles2;
        }
        */
        
        MeshFilter mFilter = GetComponent(typeof(MeshFilter)) as MeshFilter;

        // Fix for the inertia tensor problem. Object mesh needs some depth.
        if (verts.Length > 0)
        {
            verts[0] += new Vector3(0f, 0f, -0.001f);
        }
        
        //Mesh mesh = new Mesh();
        Mesh mesh = mFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
        }

        if (refreshTriangulation)
        {
            mesh.Clear();
        }

        mesh.vertices = verts;

        if (refreshTriangulation)
        {
            GenerateTriangles(mesh, fillVerts, embossVerts, outlineVerts, fillAntialiasing, embossAntialiasing, outlineAntialiasing, multipleMaterials);
        }

        mesh.uv = uvs;
        mesh.uv2 = uvs2;
        mesh.colors = colors;

        //mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mFilter.sharedMesh = mesh;

        if (GetFill() != Fill.None)
        {
            UnshiftOverlappingControlPoints();
        }

    }
	
    public void scalePoints(Vector3 middle, float scale)
    {
        for (int i = 0; i < GetPointCount(); i++)
        {
            Vector3 pos = GetPosition(i);

            SetPoint(i, (pos - middle) * scale + middle);
        }
    }

    public void scaleHandles(float scale)
    {
        for (int i = 0; i < GetPointCount(); i++)
        {
            Vector3 inCtrl = GetInControlPositionPointSpace(i);
            Vector3 outCtrl = GetOutControlPositionPointSpace(i);
            SetPoint(i, GetPosition(i), inCtrl * scale, outCtrl * scale);
        }
    
    }

    public void setPivotCenter()
    {
        Vector3 middle = GetMiddle();
        for (int i = 0; i < GetPointCount(); i++)
        {
            SetPoint(i, GetPosition(i) - middle);
        }
        transform.position += transform.TransformPoint(middle) - transform.position;
    }

    public void CreateBoxCollidersCache()
    {
        boxColliders = new BoxCollider[GetPhysicsColliderCount()];
        int childcount = transform.GetChildCount();
        
        int boxI = 0;
        for (int i = 0; i < childcount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name.Substring(0, 4).Equals("ZZZ_"))
            {
                BoxCollider boxCollider = child.GetComponent(typeof(BoxCollider)) as BoxCollider;
                if (boxCollider != null)
                {
                    if(boxI < boxColliders.Length) {
                        boxColliders[boxI++] = boxCollider;
                    }
                    else
                    {
                        Debug.Log("Error caching the boxcolliders. Amount of the boxcolliders doesn't match the count variable.");
                    }
                }
            }
        }

    }

    public void ShiftOverlappingControlPoints()
    {
        if (overLappingVerts != null)
        {
            overLappingVerts.Clear();
            int pCount = GetPointCount();
            for (int i = 0; i < pCount; i++)
            {
                for (int i2 = i + 1; i2 < pCount; i2++)
                {
                    if (Mathf.Approximately(GetPosition(i).x, GetPosition(i2).x))
                    {
                        if (Mathf.Approximately(GetPosition(i).y, GetPosition(i2).y))
                        {
                            SetPoint(i, GetPosition(i) + GetNormal(i) * -1f * overLappingVertsShift);
                            SetPoint(i2, GetPosition(i2) + GetNormal(i2) * -1f * overLappingVertsShift);
                            overLappingVerts.Add(i);
                            overLappingVerts.Add(i2);
                        }
                    }
                }
            }
        }
    }

    public void UnshiftOverlappingControlPoints()
    {
        if (overLappingVerts != null)
        {
            foreach (int i in overLappingVerts)
            {
                SetPoint(i, GetPosition(i) + GetNormal(i) * overLappingVertsShift);
            }
        }
    }

    public RageVertex[] GenerateOutlineVerts(bool antialiasing, bool multipleMaterials)
    {
        RageVertex[] outlineVerts = new RageVertex[0];

        if (GetOutline() != Outline.None)
        {
            RageVertex[] splits=null;
			
            if (GetOutline() == Outline.Free && GetFill() != Fill.None && GetFill() != Fill.Landscape)
            {
                splits = GetSplits(GetVertexCount() - (Mathf.FloorToInt((float)GetVertexCount() * 1f / GetPointCount())), 0f, 1f - 1f / GetPointCount());
            } 
            else
            {
                splits = GetSplits(GetVertexCount(), 0f, 1f);
			}

            int vertsInBand = splits.Length;
            float uvPos = 0f;
						
            if (antialiasing)
            {
                outlineVerts = new RageVertex[splits.Length * 4];
            }
            else
            {
                outlineVerts = new RageVertex[splits.Length * 2];
            }

            for (int v = 0; v < splits.Length; v++)
            {
                Vector3 normal = new Vector3();
                float edgeWidth = GetOutlineWidth(splits[v].splinePosition * GetLastSplinePosition());
                float AAWidth = GetAntialiasingWidth(splits[v].splinePosition * GetLastSplinePosition());
            
                if (corners != Corner.Beak)
                {
                    if (GetFill() != Fill.Landscape)
                    {
                        normal = GetNormal(splits[v].splinePosition);
                    }
                    else
                    {
                        normal = new Vector3(0f, 1f, 0f) * GetLandscapeOutlineAlign() + GetNormal(splits[v].splinePosition) * (1f - GetLandscapeOutlineAlign());
                        normal.Normalize();
                    }
                } 
                else 
                {
                    if ((outline != Outline.Free) || (v < splits.Length - 1 && v > 0))
                    {
                        normal = FindNormal(splits[GetIndex(v - 1, splits.Length)].position, splits[v].position, splits[GetIndex(v + 1, splits.Length)].position, edgeWidth);
                        normal *= -1;
                        edgeWidth = 1f;
                    }
                    else
                    {
                        if ((v < splits.Length - 1 && v > 0))
                        {
                            normal = GetNormal(splits[v].splinePosition * GetLastSplinePosition());
                        }
                        else
                        {
                            if (v == 0)
                            {
                                normal = FindNormal(splits[0].position + (splits[0].position - splits[1].position), splits[0].position, splits[1].position, edgeWidth);
                                normal *= -1;
                                edgeWidth = 1f;
                            }
                            else
                            {
                                normal = FindNormal(splits[splits.Length - 1].position + (splits[splits.Length - 1].position - splits[splits.Length - 2].position), splits[splits.Length - 1].position, splits[splits.Length - 2].position, edgeWidth);
                                edgeWidth = 1f;
                            }
                            
                        }
                    }
                                        
                    if (normal.magnitude > this.maxBeakLength * this.OutlineWidth)
                    {
                        normal = normal.normalized * this.maxBeakLength * this.OutlineWidth;
                    }
                }

                if (v > 0)
                {
                    uvPos += (splits[v].position - splits[v - 1].position).magnitude;
                }

                Vector3 scaledNormal = ScaleToLocal(normal.normalized);
                Vector3 normalizedNormal = normal.normalized;
                				
                if (antialiasing)
                {
                    Vector3 freeLineCapTangent = new Vector3(0f, 0f, 0f);
                    if (v == 0 && GetOutline() == Outline.Free)
                    {
                        freeLineCapTangent = Vector3.Cross(normal, new Vector3(0f, 0f, -1f)) * AAWidth;
                    }
                    else if (v == splits.Length - 1 && GetOutline() == Outline.Free)
                    {
                        freeLineCapTangent = Vector3.Cross(normal, new Vector3(0f, 0f, -1f)) * -AAWidth;
                    }

                    outlineVerts[v + 0 * vertsInBand].position = splits[v].position + scaledNormal * AAWidth + normal * edgeWidth * 0.5f + normalizedNormal * GetOutlineNormalOffset() + freeLineCapTangent;
                    outlineVerts[v + 1 * vertsInBand].position = splits[v].position + normal * edgeWidth * 0.5f + normalizedNormal * GetOutlineNormalOffset();
                    outlineVerts[v + 2 * vertsInBand].position = splits[v].position - normal * edgeWidth * 0.5f + normalizedNormal * GetOutlineNormalOffset();
                    outlineVerts[v + 3 * vertsInBand].position = splits[v].position - normal * edgeWidth * 0.5f - scaledNormal * AAWidth + normalizedNormal * GetOutlineNormalOffset() + freeLineCapTangent;
                }
                else
                {
                    outlineVerts[v + 0 * vertsInBand].position = splits[v].position + normal * edgeWidth * 0.5f + normal * GetOutlineNormalOffset();
                    outlineVerts[v + 1 * vertsInBand].position = splits[v].position - normal * edgeWidth * 0.5f + normal * GetOutlineNormalOffset();
                }
                

                Color outlineCol1 = Color.black;
                Color outlineCol2 = Color.black;
                switch (GetOutlineGradient())
                {
                    case OutlineGradient.None:
                        outlineCol1 = GetOutlineColor1();
                        outlineCol2 = GetOutlineColor1();
                        break;
                    case OutlineGradient.Default:
                        outlineCol1 = GetOutlineColor1();
                        outlineCol2 = GetOutlineColor2();
                        break;
                    case OutlineGradient.Inverse:
                        outlineCol1 = GetOutlineColor2();
                        outlineCol2 = GetOutlineColor1();
                        break;
                }

                if (antialiasing)
                {
                    outlineVerts[v + 0 * vertsInBand].color = outlineCol2 * new Color(1f, 1f, 1f, 0f);
                    outlineVerts[v + 1 * vertsInBand].color = outlineCol2 * new Color(1f, 1f, 1f, 1f);
                    outlineVerts[v + 2 * vertsInBand].color = outlineCol1 * new Color(1f, 1f, 1f, 1f);
                    outlineVerts[v + 3 * vertsInBand].color = outlineCol1 * new Color(1f, 1f, 1f, 0f);
                }
                
                else
                {
                    outlineVerts[v + 0 * vertsInBand].color = outlineCol2 * new Color(1f, 1f, 1f, 1f);
                    outlineVerts[v + 1 * vertsInBand].color = outlineCol1 * new Color(1f, 1f, 1f, 1f);
                }

                float AAWidthRelatedToEdgeWidth = 0f;
                if (AAWidth > 0f && edgeWidth > 0f && antialiasing)
                {
                    AAWidthRelatedToEdgeWidth = AAWidth / edgeWidth;
                }

                if (!multipleMaterials)
                {
                    switch (GetTexturing1())
                    {
                        case UVMapping.None:
                        case UVMapping.Fill:
                            outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(0f, 0f);
                            outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(0f, 0f);
                            if (antialiasing)
                            {
                                outlineVerts[v + 2 * vertsInBand].uv1 = new Vector2(0f, 0f);
                                outlineVerts[v + 3 * vertsInBand].uv1 = new Vector2(0f, 0f);
                            }
                            break;
                        case UVMapping.Outline:
                            if (antialiasing)
                            {
                                outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f);
                                outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f - AAWidthRelatedToEdgeWidth * 0.5f);
                                outlineVerts[v + 2 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), AAWidthRelatedToEdgeWidth * 0.5f);
                                outlineVerts[v + 3 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0f);
                            }
                            else
                            {
                                outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f);
                                outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0f);
                            }
                            break;
                    }

                    switch (GetTexturing2())
                    {
                        case UVMapping.None:
                        case UVMapping.Fill:
                            if (antialiasing)
                            {
                                outlineVerts[v + 0 * vertsInBand].uv2 = new Vector2(0f, 0f);
                                outlineVerts[v + 1 * vertsInBand].uv2 = new Vector2(0f, 0f);
                                outlineVerts[v + 2 * vertsInBand].uv2 = new Vector2(0f, 0f);
                                outlineVerts[v + 3 * vertsInBand].uv2 = new Vector2(0f, 0f);
                            }
                            else
                            {
                                outlineVerts[v + 0 * vertsInBand].uv2 = new Vector2(0f, 0f);
                                outlineVerts[v + 1 * vertsInBand].uv2 = new Vector2(0f, 0f);
                            }
                            break;
                        case UVMapping.Outline:
                            if (antialiasing)
                            {
                                outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f);
                                outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f - AAWidthRelatedToEdgeWidth * 0.5f);
                                outlineVerts[v + 2 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), AAWidthRelatedToEdgeWidth * 0.5f);
                                outlineVerts[v + 3 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0f);
                            }
                            else
                            {
                                outlineVerts[v + 0 * vertsInBand].uv2 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0.99f);
                                outlineVerts[v + 1 * vertsInBand].uv2 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0.01f);
                            }
                            break;
                    }
                }
                else
                {
                    if (antialiasing)
                    {
                        outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f);
                        outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 1f - AAWidthRelatedToEdgeWidth * 0.5f);
                        outlineVerts[v + 2 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), AAWidthRelatedToEdgeWidth * 0.5f);
                        outlineVerts[v + 3 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0f);
                    }
                    else
                    {
                        outlineVerts[v + 0 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0.99f);
                        outlineVerts[v + 1 * vertsInBand].uv1 = new Vector2(uvPos / GetOutlineTexturingScaleInv(), 0.01f);
                    }
                }

            }
        }
        else
        {
            outlineVerts = new RageVertex[0];
        }
        /*
        for (int index = 0; index < outlineVerts.Length; index++)
        {
            outlineVerts[index].position -= transform.forward * 0.2f;
        }
        */
        return outlineVerts;
    }

    public RageVertex[] GenerateFillVerts(bool antialiasing, bool multipleMaterials)
    {
        RageVertex[] fillVerts = new RageVertex[0];

        switch(GetFill()) {
            case Fill.None:
                break;

            case Fill.Solid:
            case Fill.Gradient:
                RageVertex[] splits = GetSplits(GetVertexCount()-1, 0f, 1f - 1f/GetVertexCount());

                if (antialiasing)
                {
                    fillVerts = new RageVertex[splits.Length * 2];
                }
                else
                {
                    fillVerts = new RageVertex[splits.Length];
                }

                for (int v = 0; v < splits.Length; v++)
                {
                    Vector3 normal = GetNormal(splits[v].splinePosition);
                    Vector3 scaledNormal = ScaleToLocal(normal);

                    if (antialiasing)
                    {
                        fillVerts[v].position = splits[v].position;
                        fillVerts[v + splits.Length].position = splits[v].position + scaledNormal * GetAntialiasingWidth();
                    
                        fillVerts[v].color = GetFillColor(fillVerts[v].position);
                        fillVerts[v + splits.Length].color = GetFillColor(fillVerts[v + splits.Length].position) * new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
						fillVerts[v].position = splits[v].position;
                        fillVerts[v].color = GetFillColor(fillVerts[v].position);
                    }

                    if (!multipleMaterials)
                    {
                        switch (GetTexturing1())
                        {
                            case UVMapping.None:
                            case UVMapping.Outline:
                                fillVerts[v].uv1 = new Vector2(0f, 0f);
                                break;
                            case UVMapping.Fill:
                                fillVerts[v].uv1 = GetFillUV(fillVerts[v].position);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits.Length].uv1 = GetFillUV(fillVerts[v + splits.Length].position);
                                }
                                break;
                        }

                        switch (GetTexturing2())
                        {
                            case UVMapping.None:
                            case UVMapping.Outline:
                                fillVerts[v].uv2 = new Vector2(0f, 0f);
                                break;
                            case UVMapping.Fill:
                                fillVerts[v].uv2 = GetFillUV2(fillVerts[v].position);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits.Length].uv2 = GetFillUV2(fillVerts[v + splits.Length].position);
                                }
                                break;
                        }
                    }
                    else
                    {
                        fillVerts[v].uv1 = GetFillUV(fillVerts[v].position);
                        if (antialiasing)
                        {
                            fillVerts[v + splits.Length].uv1 = GetFillUV(fillVerts[v + splits.Length].position);
                        }
                    }
                }
        
                break;

            case Fill.Landscape:
                RageVertex[] splits2 = GetSplits(GetVertexCount(), 0f, 1f);
                                
                float bottomY = GetBounds().yMin - Mathf.Clamp(GetLandscapeBottomDepth(), 1f, 100000000f);;

                if (antialiasing)
                {
                    fillVerts = new RageVertex[splits2.Length * 3];
                }
                else
                {
                    fillVerts = new RageVertex[splits2.Length * 2];
                }

                for (int v = 0; v < splits2.Length; v++)
                {
                    Vector3 normal = GetNormal(splits2[v].splinePosition);
                    Vector3 scaledNormal = ScaleToLocal(normal);

                    if (antialiasing)
                    {
                        fillVerts[v].position = new Vector3(splits2[v].position.x, bottomY);
                        fillVerts[v + splits2.Length].position = splits2[v].position;
                        fillVerts[v + splits2.Length * 2].position = splits2[v].position + scaledNormal * GetAntialiasingWidth();

                        fillVerts[v].color = GetFillColor2();
                        fillVerts[v + splits2.Length].color = GetFillColor1();
                        fillVerts[v + splits2.Length * 2].color = GetFillColor1()*new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        fillVerts[v].position = new Vector3(splits2[v].position.x, bottomY);
                        fillVerts[v + splits2.Length].position = splits2[v].position;

                        fillVerts[v].color = GetFillColor2();
                        fillVerts[v + splits2.Length].color = GetFillColor1();
                    }

                    if (!multipleMaterials)
                    {
                        switch (GetTexturing1())
                        {
                            case UVMapping.None:
                            case UVMapping.Outline:
                                fillVerts[v].uv1 = new Vector2(0f, 0f);
                                fillVerts[v + splits2.Length].uv1 = new Vector2(0f, 0f);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits2.Length * 2].uv1 = new Vector2(0f, 0f);
                                }
                                break;
                            case UVMapping.Fill:
                                fillVerts[v].uv1 = GetFillUV(fillVerts[v].position);
                                fillVerts[v + splits2.Length].uv1 = GetFillUV(fillVerts[v + splits2.Length].position);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits2.Length * 2].uv1 = GetFillUV(fillVerts[v + splits2.Length * 2].position);
                                }
                                break;
                        }

                        switch (GetTexturing2())
                        {
                            case UVMapping.None:
                            case UVMapping.Outline:
                                fillVerts[v].uv2 = new Vector2(0f, 0f);
                                fillVerts[v + splits2.Length].uv2 = new Vector2(0f, 0f);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits2.Length * 2].uv2 = new Vector2(0f, 0f);
                                }
                                break;
                            case UVMapping.Fill:
                                fillVerts[v].uv2 = GetFillUV(fillVerts[v].position);
                                fillVerts[v + splits2.Length].uv2 = GetFillUV(fillVerts[v + splits2.Length].position);
                                if (antialiasing)
                                {
                                    fillVerts[v + splits2.Length * 2].uv2 = GetFillUV(fillVerts[v + splits2.Length * 2].position);
                                }
                                break;
                        }
                    }
                    else
                    {
                        fillVerts[v].uv1 = GetFillUV(fillVerts[v].position);
                        fillVerts[v + splits2.Length].uv1 = GetFillUV(fillVerts[v + splits2.Length].position);
                        if (antialiasing)
                        {
                            fillVerts[v + splits2.Length * 2].uv1 = GetFillUV(fillVerts[v + splits2.Length * 2].position);
                        }
                    }
                }

                break;
        }
                    
        return fillVerts;
    }

    public RageVertex[] GenerateEmbossVerts(bool antialiasing)
    {
        RageVertex[] splits = GetSplits(GetVertexCount(), 0f, 1f);
        RageVertex[] embossVerts = new RageVertex[0];
                
        if (GetEmboss() != Emboss.None)
        {
            int vertsInBand = splits.Length;

            if (antialiasing)
            {
                embossVerts = new RageVertex[splits.Length * 4];
            }
            else
            {
                embossVerts = new RageVertex[splits.Length * 2];
            }

            Vector3 sunVector = RotatePoint2D_CCW(new Vector3(0f, -1f, 0f), GetEmbossAngleDeg() / (180f / Mathf.PI));
            Vector3[] embossVectors = new Vector3[splits.Length];
            Vector3[] normals = new Vector3[splits.Length];
            float[] dots = new float[splits.Length];
            float[] mags = new float[splits.Length];

            for (int v = 0; v < splits.Length; v++)
            {
                float p = (float)v / (float)splits.Length;
                normals[v] = spline.GetAvgNormal(p * GetLastSplinePosition(), 0.05f, 3);
                if (v == splits.Length - 1)
                {
                    normals[v] = normals[0];
                }
                dots[v] = Vector3.Dot(sunVector, normals[v]);
                mags[v] = Mathf.Clamp01(Mathf.Abs(dots[v]) - GetEmbossOffset());
                if (dots[v] > 0f)
                {
                    embossVectors[v] = (sunVector - normals[v] * 2f).normalized * GetEmbossSize() * mags[v];
                }
                else
                {
                    embossVectors[v] = (sunVector + normals[v] * 2f).normalized * GetEmbossSize() * mags[v] * -1f;
                }
            }

            for (int v = 0; v < splits.Length; v++)
            {
                Vector3 embossVector = new Vector3();
                int v2 = v;
                if (v == splits.Length - 1)
                {
                    v2 = 0;
                }
                for (int i = -Mathf.FloorToInt(GetEmbossSmoothness()); i <= Mathf.FloorToInt(GetEmbossSmoothness()) + 1; i++)
                {
                    if (i != 0)
                    {
                        embossVector += embossVectors[mod(v2 - i, splits.Length)] * (1f - (float)Mathf.Abs(i) / (GetEmbossSmoothness() + 1));
                    }
                    else
                    {
                        embossVector += embossVectors[mod(v2 - i, splits.Length)];
                    }
                }
                embossVector *= 1f / (Mathf.FloorToInt(GetEmbossSmoothness()) * 2 + 1);

                

                if (antialiasing)
                {
                    embossVerts[v + 0 * vertsInBand].position = splits[v].position - embossVector.normalized * GetAntialiasingWidth() * 1f;
                    embossVerts[v + 1 * vertsInBand].position = splits[v].position;
                    embossVerts[v + 2 * vertsInBand].position = splits[v].position + embossVector;
                    embossVerts[v + 3 * vertsInBand].position = splits[v].position + embossVector + embossVector.normalized * GetAntialiasingWidth();
                }
                else
                {
                    embossVerts[v + 0 * vertsInBand].position = splits[v].position;
                    embossVerts[v + 1 * vertsInBand].position = splits[v].position + embossVector;
                }

                if (embossVector.sqrMagnitude > 0.0001f)
                {
                    if (dots[v] < 0f)
                    {
                        if (GetEmboss() == Emboss.Sharp)
                        {
                            if (antialiasing)
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 2 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 3 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                            }
                            else
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                            }
                        }
                        else
                        {
                            if (antialiasing)
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 2 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 3 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                            }
                            else
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                            }
                        }
                    }
                    else
                    {
                        if (GetEmboss() == Emboss.Sharp)
                        {
                            if (antialiasing)
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 2 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 3 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                            }
                            else
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                            }
                        }
                        else
                        {
                            if (antialiasing)
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 2 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                                embossVerts[v + 3 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                            }
                            else
                            {
                                embossVerts[v + 0 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, Mathf.Clamp01(mags[v] * 4f));
                                embossVerts[v + 1 * vertsInBand].color = GetEmbossColor2() * new Color(1f, 1f, 1f, 0f);
                            }
                        }
                    }
                }
                else
                {
                    if (antialiasing)
                    {
                        embossVerts[v + 0 * vertsInBand].position = splits[v].position - embossVector.normalized * GetAntialiasingWidth();
                        embossVerts[v + 1 * vertsInBand].position = splits[v].position;
                        embossVerts[v + 2 * vertsInBand].position = splits[v].position;
                        embossVerts[v + 3 * vertsInBand].position = splits[v].position;
                    }
                    else
                    {
                        embossVerts[v + 0 * vertsInBand].position = splits[v].position;
                        embossVerts[v + 1 * vertsInBand].position = splits[v].position;
                    }

                    if (antialiasing)
                    {
                        embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                        embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                        embossVerts[v + 2 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                        embossVerts[v + 3 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        embossVerts[v + 0 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                        embossVerts[v + 1 * vertsInBand].color = GetEmbossColor1() * new Color(1f, 1f, 1f, 0f);
                    }
                }

                if (antialiasing)
                {
                    embossVerts[v + 0 * vertsInBand].uv1 = new Vector2(0f, 0f);
                    embossVerts[v + 1 * vertsInBand].uv1 = new Vector2(0f, 0f);
                    embossVerts[v + 2 * vertsInBand].uv1 = new Vector2(0f, 0f);
                    embossVerts[v + 3 * vertsInBand].uv1 = new Vector2(0f, 0f);
                }
                else
                {
                    embossVerts[v + 0 * vertsInBand].uv1 = new Vector2(0f, 0f);
                    embossVerts[v + 1 * vertsInBand].uv1 = new Vector2(0f, 0f);
                }
            }
        }
        else
        {
            embossVerts = new RageVertex[0];
        }

        /*
        for (int index = 0; index < embossVerts.Length; index++)
        {
            embossVerts[index].position -= transform.forward * 0.1f;
        }*/

        return embossVerts;
    }

    public RageVertex[] GetSplits(int vertCount, float start, float end)
    {
        RageVertex[] splits = new RageVertex[vertCount+1];

        for (int v = 0; v < splits.Length; v++)
        {
            splits[v].splinePosition = Mathf.Clamp01(((float)v / (float)(vertCount)) * (end-start) + start);

            if (Mathf.Approximately(splits[v].splinePosition, 1f) && !SplineIsOpenEnded())
            {
                splits[v].splinePosition = 0f;
            }

            splits[v].splineSegmentPosition = spline.GetSegmentPosition(splits[v].splinePosition);

            splits[v].position = GetPosition(splits[v].splinePosition);
            splits[v].curveStart = spline.points[spline.GetFloorIndex(splits[v].splinePosition)];
            splits[v].curveEnd = spline.points[spline.GetCeilIndex(splits[v].splinePosition)];
                   
            splits[v].color = new Color(1f, 1f, 1f, 1f);
        }
		
		if (GetOptimize())
		{
			splits = Optimize(splits);
		}

        
        if (splits.Length != vertCount)
        {
            //Debug.Log("splits.length:" + splits.Length + ", vertCount:" + vertCount);
        }

        return splits;
    }
	
	private RageVertex[] Optimize(RageVertex[] array)
	{
		BitArray toRemove = new BitArray(array.Length, false);
		int removeCount = 0;
		
		// check i-th vertex if we can remove it
		// if we can - remove it, and check next
		int prev = 0;
		for (int i = 1; i < array.Length - 1; i++)
		{
			Vector3 a = array[prev].position;
			Vector3 v = array[i].position;
			Vector3 b = array[i + 1].position;
			
			Vector3 v1 = a - v;
			Vector3 v2 = b - v;
			
			float acos = Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude);
			float dif = 180.0f - Mathf.Rad2Deg * Mathf.Acos(acos);
			//Debug.Log(dif);
				
			if (dif < GetOptimizeAngle())
			{
				toRemove[i] = true;
				removeCount++;
			}
			else
			{
				prev = i;
			}
		}
		
		if (removeCount == 0)
		{
			return array;
		}
		
		RageVertex[] result = new RageVertex[array.Length - removeCount];
		
		// copy
		int index = 0;
		for (int i = 0; i < result.Length; i++)
		{
			while(toRemove[index])
			{
				index++;
			}
					
			result[i] = array[index];
			index++;
		}
		
		//Debug.Log(array.Length + " / " + result.Length);
		
		return result;
	}

    public int GetOutlineTriangleCount(RageVertex[] outlineVerts, bool AAoutline)
    {
        if (GetOutline() != Outline.None)
        {
            if (AAoutline)
            {
                if (GetOutline() == Outline.Free)
                {
                    return ((outlineVerts.Length / 4) - 1) * 6 + 4;
                }
                else
                {
                    return ((outlineVerts.Length / 4) - 1) * 6;
                }
            }
            else
            {
                return outlineVerts.Length - 2;
            }
        }
        else
        {
            return 0;
        }
    }

    public int GetFillTriangleCount(RageVertex[] fillVerts, bool AAfill)
    {
        switch (GetFill())
        {
            case Fill.None:
                return 0;
            case Fill.Solid:
            case Fill.Gradient:
                if (AAfill)
                {
                    return (fillVerts.Length / 2) - 2 + fillVerts.Length;
                }
                else
                {
                    return fillVerts.Length - 2;
                }
            case Fill.Landscape:
                if (AAfill)
                {
                    return ((fillVerts.Length / 3) - 1) * 4;
                }
                else
                {
                    return ((fillVerts.Length / 2) - 1) * 2;
                }
        }
        return 0;
    }
    public int GetEmbossTriangleCount(RageVertex[] embossVerts, bool AAemboss)
    {
        if (GetEmboss() != Emboss.None)
        {
            if (AAemboss)
            {
                return ((embossVerts.Length / 4) - 1) * 6;
            }
            else
            {
                return embossVerts.Length - 2;
            }
        }
        else
        {
            return 0;
        }
    }
    
    public void GenerateTriangles(Mesh mesh, RageVertex[] fillVerts, RageVertex[] embossVerts, RageVertex[] outlineVerts, bool AAfill, bool AAemboss, bool AAoutline, bool multipleMaterials)
    {
        int[] tris = null;
        
        Vector2[] verts = new Vector2[0];

        if (GetFill() != Fill.Landscape)
        {
            if (AAfill)
            {
                verts = new Vector2[fillVerts.Length / 2];
            }
            else
            {
                verts = new Vector2[fillVerts.Length];
            }

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector2(fillVerts[i].position.x, fillVerts[i].position.y);
            }
        }   

        tris = new int[GetOutlineTriangleCount(outlineVerts, AAoutline) * 3 + GetFillTriangleCount(fillVerts, AAfill) * 3 + GetEmbossTriangleCount(embossVerts, AAemboss) * 3];

        int tIndex = 0;
        int vertsPerBand = 0;
        int bandCount = 0;

        switch (GetFill())
        {
            case Fill.None:

                break;
            case Fill.Solid:
            case Fill.Gradient:
                Triangulator triangulator = new Triangulator(verts);
                
                int[] fillTris = triangulator.Triangulate();
                
                for (int i = 0; i < fillTris.Length; i++)
                {
                    tris[tIndex++] = fillTris[i];
                }
                break;
            case Fill.Landscape:

                if (AAfill)
                {
                    bandCount = 2;
                    vertsPerBand = fillVerts.Length / 3;
                }
                else
                {
                    bandCount = 1;
                    vertsPerBand = fillVerts.Length / 2;
                }

                for (int v = 0; v < vertsPerBand-1; v++)
                {
                    for (int b = 0; b < bandCount; b++)
                    {
                        tris[tIndex++] = v + b * vertsPerBand;
                        tris[tIndex++] = v + (b + 1) * vertsPerBand;
                        tris[tIndex++] = v + (b + 1) * vertsPerBand + 1;
                        tris[tIndex++] = v + b * vertsPerBand;
                        tris[tIndex++] = v + (b + 1) * vertsPerBand + 1;
                        tris[tIndex++] = v + b * vertsPerBand + 1;
                    }
                }

            break;
        }

        vertsPerBand = 0;
        bandCount = 0;     

        // fill antialiasing triangles
        if (AAfill)
        {
            vertsPerBand = verts.Length-1;

            for (int v = 0; v <= vertsPerBand; v++)
            {
                for (int b = 0; b < 1; b++)
                {
                    tris[tIndex++] = v + b * vertsPerBand;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand + 1;
                    tris[tIndex++] = v + b * vertsPerBand;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand + 1;
                    tris[tIndex++] = v + b * vertsPerBand + 1;
                }
            }

            
        }

        if (AAemboss)
        {
            vertsPerBand = embossVerts.Length / 4;
            bandCount = 3;
        }
        else
        {
            vertsPerBand = embossVerts.Length / 2;
            bandCount = 1;
        }

        for (int v = 0; v < vertsPerBand-1; v++)
        {
            for (int b = 0; b < bandCount; b++)
            {
                if (v < vertsPerBand - 1)
                {
                    tris[tIndex++] = v + b * vertsPerBand + fillVerts.Length;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand + 1 + fillVerts.Length;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand + fillVerts.Length;
                    tris[tIndex++] = v + b * vertsPerBand + fillVerts.Length;
                    tris[tIndex++] = v + b * vertsPerBand + 1 + fillVerts.Length;
                    tris[tIndex++] = v + (b + 1) * vertsPerBand + 1 + fillVerts.Length;
                }
            }
        }

        if (AAoutline)
        {
            vertsPerBand = outlineVerts.Length / 4;
            bandCount = 3;
        }
        else
        {
            vertsPerBand = outlineVerts.Length / 2;
            bandCount = 1;
        }

        for (int v = 0; v < vertsPerBand-1; v++)
        {
            for (int b = 0; b < bandCount; b++)
            {
                tris[tIndex++] = v + b * vertsPerBand + embossVerts.Length + fillVerts.Length;
                tris[tIndex++] = v + (b + 1) * vertsPerBand + 1 + embossVerts.Length + fillVerts.Length;
                tris[tIndex++] = v + (b + 1) * vertsPerBand + embossVerts.Length + fillVerts.Length;
                tris[tIndex++] = v + b * vertsPerBand + embossVerts.Length + fillVerts.Length;
                tris[tIndex++] = v + b * vertsPerBand + 1 + embossVerts.Length + fillVerts.Length;
                tris[tIndex++] = v + (b + 1) * vertsPerBand + 1 + embossVerts.Length + fillVerts.Length;
            }
        }

        // Free outline AA caps
        if (GetOutline() == Outline.Free && AAoutline)
        {
            int vertsInBand = outlineVerts.Length / 4;
            tris[tIndex++] = 0 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 2 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = 0 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 3 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 2 + embossVerts.Length + fillVerts.Length;

            tris[tIndex++] = vertsInBand * 1 - 1 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 3 - 1 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 2 - 1 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 1 - 1 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 4 - 1 + embossVerts.Length + fillVerts.Length;
            tris[tIndex++] = vertsInBand * 3 - 1 + embossVerts.Length + fillVerts.Length;
        }

        if (multipleMaterials)
        {
            int ii = 0;
            int[] outlineTriangles = new int[GetOutlineTriangleCount(outlineVerts, AAoutline) * 3];
            int[] restOfTriangles = new int[tris.Length - GetOutlineTriangleCount(outlineVerts, AAoutline) * 3];
            mesh.subMeshCount = 2;

            for (; ii < restOfTriangles.Length; ii++)
            {
                restOfTriangles[ii] = tris[ii];
            }
            if (GetTexturing1() == UVMapping.Fill)
            {
                mesh.SetTriangles(restOfTriangles, 0);
            }
            if (GetTexturing2() == UVMapping.Fill)
            {
                mesh.SetTriangles(restOfTriangles, 1);
            }            
            for (; ii < tris.Length; ii++)
            {
                outlineTriangles[ii - restOfTriangles.Length] = tris[ii];
            }

            if (GetTexturing1() == UVMapping.Outline)
            {
                mesh.SetTriangles(outlineTriangles, 0);
            }
            if (GetTexturing2() == UVMapping.Outline)
            {
                mesh.SetTriangles(outlineTriangles, 1);
            }
        }
        else
        {
            
            if (inverseTriangleDrawOrder)
            {
                int len = tris.Length;
                int[] triangles2 = new int[tris.Length];
                for (int i = 0; i < len; i+=3)
                {
                    triangles2[len - i - 3] = tris[i];
                    triangles2[len - i - 3 + 1] = tris[i + 1];
                    triangles2[len - i - 3 + 2] = tris[i + 2];
                }
                tris = triangles2;
            }
            
            mesh.triangles = tris;
        }
    }

    public Color GetFillColor(Vector3 position)
    {
        switch (GetFill())
        {
            case Fill.Solid:
                return GetFillColor1();
             case Fill.Gradient:
                Vector3 middle = GetGradientOffset();
                Vector2 rotated = RotatePoint2D_CCW((position - middle), -GetGradientAngleDeg() / (180f / Mathf.PI)) * GetGradientScaleInv() * 0.5f;
                float v = rotated.y + 0.5f;
                return Mathf.Clamp(v, 0f, 1f) * GetFillColor1() + (1f - Mathf.Clamp(v, 0f, 1f)) * GetFillColor2();
        }
        return GetFillColor1();
    }

    public Vector2 GetFillUV(Vector3 position)
    {
        Vector3 middle = GetTextureOffset();
        Vector2 rotated = RotatePoint2D_CCW(position - middle, -GetTextureAngleDeg() / (180f / Mathf.PI)) * GetTextureScaleInv();
        rotated += new Vector2(0.5f, 0.5f);
        return rotated;
    }

    public Vector2 GetFillUV2(Vector3 position)
    {
        Vector3 middle = GetTextureOffset2();
        Vector2 rotated = RotatePoint2D_CCW(position - middle, -GetTextureAngle2Deg() / (180f / Mathf.PI)) * GetTextureScale2Inv();
        rotated += new Vector2(0.5f, 0.5f);
        return rotated;
    }

    public Vector2 RotatePoint2D_CCW(Vector3 p, float angle)
    {
        return new Vector2(p.x * Mathf.Cos(-angle) - p.y * Mathf.Sin(-angle), p.y * Mathf.Cos(-angle) + p.x * Mathf.Sin(-angle));
    }

    public float Vector2Angle_CCW(Vector2 normal)
    {
        Vector3 up = new Vector3(0f, 1f, 0f);
        if (normal.x < 0f)
        {
            return Mathf.Acos(up.x * normal.x + up.y * normal.y) * Mathf.Rad2Deg * -1f + 360f;
        }
        else
        {
            return (Mathf.Acos(up.x * normal.x + up.y * normal.y) * Mathf.Rad2Deg * -1f + 360f) * -1f + 360f;
        }
    }

    public int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
	
	private float mod(float x, float m)
    {
        return (x % m + m) % m;
    }
	
	public float SnapToGrid(float val, float gridsize) {
		if(mod(val, gridsize) < gridsize*0.5f) {
			return val - mod(val, gridsize);
		} else {
			return val + (gridsize - mod(val, gridsize));
		}
	}
	
	public Vector3 SnapToGrid(Vector3 val, float gridsize) {
		return new Vector3(SnapToGrid(val.x, gridsize), SnapToGrid(val.y, gridsize));
	}

    public void RefreshPhysics()
    {
        //Debug.Log("RefreshPhysics");
        if (boxColliders == null && GetPhysics() == Physics.Boxed)
		{
			boxColliders = new BoxCollider[1];
		}

        if (!Application.isPlaying && !GetCreatePhysicsInEditor())
        {
            DestroyPhysicsChildren();
        }

        if (Application.isPlaying || GetCreatePhysicsInEditor())
        {

            switch (GetPhysics())
            {
                case Physics.None:
                    DestroyPhysicsChildren();
                    break;

                case Physics.Boxed:

                    DestroyMeshCollider();

                    if (lastPhysicsVertsCount != GetSplits(physicsColliderCount, 0f, 1f).Length || boxColliders[0] == null)
                    {
                        if (!Application.isPlaying)
                        {
                            DestroyPhysicsChildren();
                        }

                        lastPhysicsVertsCount = GetSplits(physicsColliderCount, 0f, 1f).Length;

                        RageVertex[] splits = new RageVertex[0];

                        splits = GetSplits(physicsColliderCount, 0f, 1f);

                        boxColliders = new BoxCollider[splits.Length - 1];

                        int t = 0;
                        t = splits.Length - 1;

                        for (int i = 0; i < t; i++)
                        {
                            GameObject newObj = new GameObject();
                            newObj.name = "ZZZ_" + gameObject.name + "_BoxCollider";
                            newObj.transform.parent = transform;
                            BoxCollider box = newObj.AddComponent(typeof(BoxCollider)) as BoxCollider;
                            box.material = GetPhysicsMaterial();

                            int i2 = i + 1;

                            Vector3 pos;
                            Vector3 pos2;
                            Vector3 norm = GetNormal(splits[i].splinePosition);
                            Vector3 norm2 = GetNormal(splits[i2].splinePosition);

                            pos = splits[i].position - norm * (GetBoxColliderDepth() * 0.5f - GetPhysicsNormalOffset());
                            pos2 = splits[i2].position - norm2 * (GetBoxColliderDepth() * 0.5f - GetPhysicsNormalOffset());

                            newObj.layer = transform.gameObject.layer;
                            newObj.tag = transform.gameObject.tag;
                            newObj.gameObject.transform.localPosition = (pos + pos2) * 0.5f;
                            newObj.gameObject.transform.LookAt(transform.TransformPoint(newObj.gameObject.transform.localPosition + Vector3.Cross((pos - pos2).normalized, new Vector3(0f, 0f, -1f))), new Vector3(1f, 0f, 0f));
                            newObj.gameObject.transform.localScale = new Vector3(GetPhysicsZDepth(), ((pos + norm * GetBoxColliderDepth() * 0.5f) - (pos2 + norm2 * GetBoxColliderDepth() * 0.5f)).magnitude, 1f * GetBoxColliderDepth());
                            boxColliders[i] = box;
                        }
                    }
                    else
                    {
                        int i = 0;
                        RageVertex[] splits = new RageVertex[0];
                        splits = GetSplits(GetPhysicsColliderCount(), 0f, 1f);

                        foreach (BoxCollider obj in boxColliders)
                        {
                            obj.material = GetPhysicsMaterial();
                            int i2 = i + 1;

                            Vector3 norm = GetNormal(splits[i].splinePosition);
                            Vector3 norm2 = GetNormal(splits[i2].splinePosition);
                            Vector3 pos;
                            Vector3 pos2;

                            pos = splits[i].position - norm * (GetBoxColliderDepth() * 0.5f - GetPhysicsNormalOffset());
                            pos2 = splits[i2].position - norm2 * (GetBoxColliderDepth() * 0.5f - GetPhysicsNormalOffset());

                            obj.gameObject.transform.localPosition = (pos + pos2) * 0.5f;
                            obj.gameObject.transform.LookAt(transform.TransformPoint(obj.gameObject.transform.localPosition + Vector3.Cross((pos - pos2).normalized, new Vector3(0f, 0f, -1f))), new Vector3(1f, 0f, 0f));
                            obj.gameObject.transform.localScale = new Vector3(GetPhysicsZDepth(), ((pos + norm * GetBoxColliderDepth() * 0.5f) - (pos2 + norm2 * GetBoxColliderDepth() * 0.5f)).magnitude, 1f * GetBoxColliderDepth());
                            i++;
                        }

                        lastPhysicsVertsCount = physicsColliderCount;
                    }

                    break;

                case Physics.MeshCollider:

                    DestroyBoxColliders();

                    if (GetPhysicsColliderCount() > 2 && (GetCreatePhysicsInEditor() || Application.isPlaying))
                    {

                        Vector3[] verts = null;
                        RageVertex[] splits2 = null;
                        int[] tris = null;

                        int tt = 0;

                        if (GetFill() != Fill.Landscape)
                        {
                            
                            splits2 = GetSplits(GetPhysicsColliderCount(), 0f, 1f);
                            verts = new Vector3[splits2.Length * 2];
                            //verts = new Vector3[GetPhysicsColliderCount() * 2];
                            tris = new int[verts.Length * 3 + (verts.Length - 2) * 6];
                            //splits2 = GetSplits(GetPhysicsColliderCount(), 0f, 1f);

                            for (int v = 0; v < verts.Length; v += 2)
                            {
                                verts[v] = splits2[v / 2].position + new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f) + GetNormal(splits2[v / 2].splinePosition) * GetPhysicsNormalOffset();
                                verts[v + 1] = splits2[v / 2].position + new Vector3(0f, 0f, GetPhysicsZDepth() * -0.5f) + GetNormal(splits2[v / 2].splinePosition) * GetPhysicsNormalOffset();

                                if (v < verts.Length - 2)
                                {
                                    tris[tt + 0] = v + 0;
                                    tris[tt + 1] = v + 2;
                                    tris[tt + 2] = v + 1;
                                    tris[tt + 3] = v + 1;
                                    tris[tt + 4] = v + 2;
                                    tris[tt + 5] = v + 3;
                                }
                                else
                                {
                                    tris[tt + 0] = v + 0;
                                    tris[tt + 1] = 0;
                                    tris[tt + 2] = v + 1;
                                    tris[tt + 3] = v + 1;
                                    tris[tt + 4] = 0;
                                    tris[tt + 5] = 1;
                                }

                                tt += 6;
                            }
                            
                        }
                        else
                        {
                            splits2 = GetSplits(GetPhysicsColliderCount(), 0f, 1f);
                            verts = new Vector3[splits2.Length * 2 + 4];
                            tris = new int[verts.Length * 3 + (verts.Length - 2) * 6];
                            
                            //verts = new Vector3[GetPhysicsColliderCount() * 2 + 4];
                            //tris = new int[verts.Length * 3 + (verts.Length - 2) * 6];
                            //splits2 = GetSplits(GetPhysicsColliderCount() - 1, 0f, 1f);
                            float bottomY = GetBounds().yMin - Mathf.Clamp(GetLandscapeBottomDepth(), 1f, 100000000f);

                            verts[0] = new Vector3(splits2[0].position.x, bottomY, GetPhysicsZDepth() * 0.5f);
                            verts[1] = new Vector3(splits2[0].position.x, bottomY, GetPhysicsZDepth() * -0.5f);

                            for (int v = 2; v < verts.Length - 2; v += 2)
                            {
                                verts[v] = splits2[(v - 2) / 2].position + new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f) + GetNormal(splits2[(v - 2) / 2].splinePosition) * GetPhysicsNormalOffset();
                                verts[v + 1] = splits2[(v - 2) / 2].position + new Vector3(0f, 0f, GetPhysicsZDepth() * -0.5f) + GetNormal(splits2[(v - 2) / 2].splinePosition) * GetPhysicsNormalOffset();
                            }

                            for (int v = 0; v < verts.Length; v += 2)
                            {
                                if (v < verts.Length - 2)
                                {
                                    tris[tt + 0] = v + 0;
                                    tris[tt + 1] = v + 2;
                                    tris[tt + 2] = v + 1;
                                    tris[tt + 3] = v + 1;
                                    tris[tt + 4] = v + 2;
                                    tris[tt + 5] = v + 3;
                                }
                                else
                                {
                                    tris[tt + 0] = v + 0;
                                    tris[tt + 1] = 0;
                                    tris[tt + 2] = v + 1;
                                    tris[tt + 3] = v + 1;
                                    tris[tt + 4] = 0;
                                    tris[tt + 5] = 1;
                                }

                                tt += 6;
                            }

                            verts[verts.Length - 2] = new Vector3(splits2[splits2.Length - 1].position.x, bottomY, GetPhysicsZDepth() * 0.5f);
                            verts[verts.Length - 1] = new Vector3(splits2[splits2.Length - 1].position.x, bottomY, GetPhysicsZDepth() * -0.5f);

                        }


                        Vector2[] pverts = new Vector2[verts.Length / 2];
                        for (int i = 0; i < pverts.Length; i++)
                        {
                            pverts[i] = new Vector2(verts[i * 2].x, verts[i * 2].y);
                        }

                        Vector2[] pverts2 = new Vector2[verts.Length / 2];
                        for (int i = 0; i < pverts2.Length; i++)
                        {
                            pverts2[i] = new Vector2(verts[i * 2 + 1].x, verts[i * 2 + 1].y);
                        }

                        Triangulator triangulator = new Triangulator(pverts);
                        int[] fillTris = triangulator.Triangulate();
                        //for (int i = fillTris.Length - 1; i >= 0; i--)
                        for (int i = 0; i < fillTris.Length; i++)
                        {
                            tris[tt++] = fillTris[i] * 2;
                        }

                        Triangulator triangulator2 = new Triangulator(pverts2);
                        int[] fillTris2 = triangulator2.Triangulate();
                        //for (int i = 0; i < fillTris2.Length; i++)
                        for (int i = fillTris2.Length-1; i >= 0; i--)
                        {
                            tris[tt++] = fillTris2[i] * 2 + 1;
                        }
                        

                        MeshCollider meshCollider = gameObject.GetComponent(typeof(MeshCollider)) as MeshCollider;
                        bool wasNull = false;
                        if (meshCollider == null)
                        {
                            wasNull = true;
                        }

                        Mesh colMesh = null;
                        if (wasNull)
                        {
                            colMesh = new Mesh();
                        }
                        else
                        {
                            if (meshCollider.sharedMesh != null)
                            {
                                colMesh = meshCollider.sharedMesh;
                            }
                            else
                            {
                                colMesh = new Mesh();
                            }
                        }

                        colMesh.Clear();
                        colMesh.vertices = verts;
                        colMesh.triangles = tris;
                        colMesh.RecalculateBounds();
                        colMesh.RecalculateNormals();
                        colMesh.Optimize();

                        if (wasNull)
                        {
                            //MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
                            meshCollider = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
                        }
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = colMesh;

                        meshCollider.sharedMaterial = physicsMaterial;
                        meshCollider.convex = GetCreateConvexMeshCollider();

                    }

                    break;
                case Physics.OutlineMeshCollider:

                    DestroyBoxColliders();

                    if (GetPhysicsColliderCount() > 2 && (GetCreatePhysicsInEditor() || Application.isPlaying))
                    {

                        int splitCount = GetPhysicsColliderCount();

                        Vector3[] verts = new Vector3[(splitCount + 1) * 4];
                        int[] tris = new int[splitCount * 24];

                        int v = 0;
                        for (int i = 0; i <= splitCount; i++)
                        {
                            float splinePos = (float)i / (float)splitCount;
                            Vector3 normal = GetNormal(splinePos);
                            verts[v++] = GetPosition(splinePos) + normal * GetOutlineWidth() * 0.5f + normal * GetOutlineNormalOffset() + new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f);
                            verts[v++] = GetPosition(splinePos) + normal * GetOutlineWidth() * -0.5f + normal * GetOutlineNormalOffset() + new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f);
                            verts[v++] = GetPosition(splinePos) + normal * GetOutlineWidth() * -0.5f + normal * GetOutlineNormalOffset() - new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f);
                            verts[v++] = GetPosition(splinePos) + normal * GetOutlineWidth() * 0.5f + normal * GetOutlineNormalOffset() - new Vector3(0f, 0f, GetPhysicsZDepth() * 0.5f);
                        }

                        int t = 0;
                        for (int i = 0; i < splitCount; i++)
                        {

                            tris[t++] = i * 4 + 0;
                            tris[t++] = i * 4 + 0 + 4 + 1;
                            tris[t++] = i * 4 + 0 + 4;
                            tris[t++] = i * 4 + 0;
                            tris[t++] = i * 4 + 0 + 1;
                            tris[t++] = i * 4 + 0 + 4 + 1;
                            
                            tris[t++] = i * 4 + 1;
                            tris[t++] = i * 4 + 1 + 4 + 1;
                            tris[t++] = i * 4 + 1 + 4;
                            tris[t++] = i * 4 + 1;
                            tris[t++] = i * 4 + 1 + 1;
                            tris[t++] = i * 4 + 1 + 4 + 1;
                            
                            tris[t++] = i * 4 + 2;
                            tris[t++] = i * 4 + 2 + 4 + 1;
                            tris[t++] = i * 4 + 2 + 4;
                            tris[t++] = i * 4 + 2;
                            tris[t++] = i * 4 + 2 + 1;
                            tris[t++] = i * 4 + 2 + 4 + 1;
                            
                            tris[t++] = i * 4 + 3;
                            tris[t++] = i * 4 + 3 + 1;
                            tris[t++] = i * 4 + 3 + 4;
                            tris[t++] = i * 4 + 3;
                            tris[t++] = i * 4;
                            tris[t++] = i * 4 + 3 + 1;
                            
                            /*
                            tris[t++] = i * 4 + 0;
                            tris[t++] = i * 4 + 0 + 4;
                            tris[t++] = i * 4 + 0 + 4 + 1;
                            tris[t++] = i * 4 + 0;
                            tris[t++] = i * 4 + 0 + 4 + 1;
                            tris[t++] = i * 4 + 0 + 1;

                            tris[t++] = i * 4 + 1;
                            tris[t++] = i * 4 + 1 + 4;
                            tris[t++] = i * 4 + 1 + 4 + 1;
                            tris[t++] = i * 4 + 1;
                            tris[t++] = i * 4 + 1 + 4 + 1;
                            tris[t++] = i * 4 + 1 + 1;

                            tris[t++] = i * 4 + 2;
                            tris[t++] = i * 4 + 2 + 4;
                            tris[t++] = i * 4 + 2 + 4 + 1;
                            tris[t++] = i * 4 + 2;
                            tris[t++] = i * 4 + 2 + 4 + 1;
                            tris[t++] = i * 4 + 2 + 1;

                            tris[t++] = i * 4 + 3;
                            tris[t++] = i * 4 + 3 + 4;
                            tris[t++] = i * 4 + 3 + 1;
                            tris[t++] = i * 4 + 3;
                            tris[t++] = i * 4 + 3 + 1;
                            tris[t++] = i * 4;
                            */
                        }

                        MeshCollider meshCollider = gameObject.GetComponent(typeof(MeshCollider)) as MeshCollider;
                        bool wasNull = false;
                        if (meshCollider == null)
                        {
                            wasNull = true;
                        }

                        Mesh colMesh = null;
                        if (wasNull)
                        {
                            colMesh = new Mesh();
                        }
                        else
                        {
                            if (meshCollider.sharedMesh != null)
                            {
                                colMesh = meshCollider.sharedMesh;
                            }
                            else
                            {
                                colMesh = new Mesh();
                            }
                        }

                        colMesh.Clear();
                        colMesh.vertices = verts;
                        colMesh.triangles = tris;
                        colMesh.RecalculateBounds();
                        colMesh.RecalculateNormals();
                        colMesh.Optimize();

                        if (wasNull)
                        {
                            //MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
                            meshCollider = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
                        }
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = colMesh;

                        meshCollider.sharedMaterial = physicsMaterial;
                        meshCollider.convex = GetCreateConvexMeshCollider();

                    }

                    break;
            }
        }
        
    }

    public void ForceZeroZ()
    {
        spline.ForceZeroZ();
    }

    public void DestroyBoxColliders()
    {
        int i = 0;
        int safe = transform.childCount + 1;
        while (transform.childCount > 0 && i < transform.childCount && safe > 0)
        {
            safe--;
            if (transform.GetChild(i).GetComponent(typeof(BoxCollider)) != null)
            {
                if (transform.GetChild(i).name.Substring(0, 3).Equals("ZZZ"))
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
            else
            {
                i++;
            }
        }
    }

    public void DestroyMeshCollider()
    {
        MeshCollider meshCollider = gameObject.GetComponent(typeof(MeshCollider)) as MeshCollider;
        if (meshCollider != null)
        {
            DestroyImmediate(meshCollider.sharedMesh);
            DestroyImmediate(meshCollider);
        }

    }

    public void DestroyPhysicsChildren()
    {
        DestroyMeshCollider();
        DestroyBoxColliders();        
    }


    
    public Vector3 ScaleToGlobal(Vector3 vec)
    {
        return new Vector3(vec.x * (transform.lossyScale.x), vec.y * (transform.lossyScale.y), vec.z * (transform.lossyScale.z));      
    }

    public Vector3 ScaleToLocal(Vector3 vec)
    {
        return new Vector3(vec.x * (1f / transform.lossyScale.x), vec.y * (1f / transform.lossyScale.y), vec.z * (1f / transform.lossyScale.z));
    }
					
	public int GetNearestPointIndex(float splinePosition)
	{
		return spline.GetNearestSplinePointIndex(splinePosition);
	}
	
    public int GetNearestPointIndex(Vector3 pos)
    {
        if (!Mathf.Approximately(pos.z, 0f))
        {
            pos = new Vector3(pos.x, pos.y, 0f);
        }

        float nearestDist = (pos - spline.points[0].point).sqrMagnitude;
        int nearestIndex = 0;

        for (int i = 1; i < spline.points.Length; i++)
        {
            if ((pos - spline.points[i].point).sqrMagnitude < nearestDist)
            {
                nearestDist = (pos - spline.points[i].point).sqrMagnitude;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    public void CreateDefaultSpline()
    {
        Vector3[] pts = new Vector3[2];
        Vector3[] ctrl = new Vector3[2 * 2];
        float[] width = new float[2];
        bool[] natural = new bool[2];
        
        pts[0] = new Vector3(
            0f,
            -Camera.main.orthographicSize*0.4f,
            0f
            );

        width[0] = 1f;
        ctrl[0] = new Vector3(Camera.main.orthographicSize * 0.3f, 0f, 0f);
        ctrl[1] = new Vector3(Camera.main.orthographicSize * -0.3f, 0f, 0f);
        natural[0] = true;

        pts[1] = new Vector3(
            0f,
            Camera.main.orthographicSize * 0.4f,
            0f
            );

        width[1] = 1f;
        ctrl[2] = new Vector3(Camera.main.orthographicSize * -0.3f, 0f, 0f);
        ctrl[3] = new Vector3(Camera.main.orthographicSize * 0.3f, 0f, 0f);
        natural[1] = true;
        
        spline = new RageCurve(pts, ctrl, natural, width);
    }

    public bool pointsAreInClockWiseOrder()
    {
        /*
        float sumZ = 0f;
        for(int i=0; i<GetPointCount()-1; i++) 
        {
            Vector3 p1 = GetPosition(i);
            Vector3 p2 = GetPosition(i + 1);
            Vector3 p3 = GetPosition(i + 2);

            sumZ += Vector3.Cross(p2 - p1, p3 - p1).z;
        }

        if (sumZ < 0f || GetFill() == Fill.Landscape || GetPointCount()<3)
        {
            return true;
        }
        else
        {
            return false;
        }*/
        /*
        var min = Vector3.zero; 
        int minIndex = 0;
        for(int i=0; i<GetPointCount()-1; i++)
        {
            var thisPos = GetPosition(i);
            if ((thisPos.y < min.y) || (thisPos.y == min.y && thisPos.x > min.x))
            {
                min = thisPos;
                minIndex = i;
            }
        }

        int minForeIndex = minIndex - 1;
        if (minForeIndex == -1) 
            minForeIndex = GetPointCount()-1;
        Vector3 minFore = GetPosition(minForeIndex);

        int minAftIndex = minIndex + 1;
        if (minAftIndex > GetPointCount() - 1)
            minAftIndex = 0;
        Vector3 minAft = GetPosition(minAftIndex);

        var crossz = Vector3.Cross(min - minFore, minAft - min).z;
        return (crossz < 0) || GetFill() == Fill.Landscape || (GetOutline() == Outline.Free);
    
        */
      
        float area = 0f;
        var pointCount = GetPointCount();
        if (pointCount < 3 || GetFill() == Fill.Landscape) return true;

        for(int i=0; i<pointCount; i++) 
        {
            Vector3 p1 = GetPosition(i);
            //Adds the first point to the end of the sum
            Vector3 p2 = (i + 1 > pointCount - 1) ? GetPosition(0) : GetPosition(i + 1);

            area += p1.x * p2.y - p2.x * p1.y;
        }

        return (area < 0f) || GetFill() == Fill.Landscape || (GetOutline() == Outline.Free);
    }
    
    public void flipPointOrder()
    {
        RageSplinePoint[] newPoints = new RageSplinePoint[GetPointCount()];
        for (int i = 0; i < GetPointCount(); i++)
        {
            Vector3 inCtrl = spline.points[GetPointCount() - i - 1].inCtrl;
            Vector3 outCtrl = spline.points[GetPointCount() - i - 1].outCtrl;
            newPoints[i] = spline.points[GetPointCount() - i - 1];
            newPoints[i].inCtrl = outCtrl;
            newPoints[i].outCtrl = inCtrl;
        }
        spline.points = newPoints;
    }


    public class Triangulator
    {
        public List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];

            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }
                
            int nv = n;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2; )
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);

                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        public float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        public bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        public bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }







    // Gizmos

    public void DrawSplineGizmos()
    {
        Vector3 p = GetPosition(0f);

        Gizmos.color = new Color(1f,1f,1f,1f);

        for (int v = 1; v <= GetVertexCount(); v++)
        {
            Vector3 tmp = GetPosition(Mathf.Clamp01((float)v / (float)(GetVertexCount())));
            Gizmos.DrawLine(transform.TransformPoint(p), transform.TransformPoint(tmp));
            p = tmp;
        }

        if (!hideHandles)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < GetPointCount(); i++)
            {
                Gizmos.DrawLine(GetPositionWorldSpace(i), GetInControlPositionWorldSpace(i));
                Gizmos.DrawLine(GetPositionWorldSpace(i), GetOutControlPositionWorldSpace(i));
            }
        }
    }

    public void DrawEmbossGizmos()
    {
        Vector3 up = new Vector3(0f, 1f, 0f);
        Vector3 middle = spline.GetMiddle(10);
        Vector3 point = RotatePoint2D_CCW(up, GetEmbossAngleDeg() * Mathf.Deg2Rad) * (GetEmbossSize() * 4f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.TransformPoint(middle), transform.TransformPoint(middle + point));
    }

    public void DrawGradientGizmos()
    {
        Vector3 up = new Vector3(0f, 1f, 0f);
        Vector3 middle = GetGradientOffset();
        Vector3 point = RotatePoint2D_CCW(up, GetGradientAngleDeg() * Mathf.Deg2Rad) * (1f / GetGradientScaleInv());

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.TransformPoint(middle), transform.TransformPoint(middle + point));

        Gizmos.color = Color.green * new Color(1f, 1f, 1f, 0.2f);
        Vector3 worldMiddle = transform.TransformPoint(middle);
        for (int i = 4; i <= 360; i += 4)
        {
            Gizmos.DrawLine(
                worldMiddle + ScaleToGlobal(RotatePoint2D_CCW(up, (i - 4) * Mathf.Deg2Rad) * (1f / GetGradientScaleInv())),
                worldMiddle + ScaleToGlobal(RotatePoint2D_CCW(up, i * Mathf.Deg2Rad) * (1f / GetGradientScaleInv()))
                );
        }
    }

    public void DrawTexturingGizmos()
    {

        Vector3 up = new Vector3(0f, 1f, 0f);
        Vector3 middle = GetTextureOffset();
        Vector3 point = RotatePoint2D_CCW(up, GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv());

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.TransformPoint(middle), transform.TransformPoint(middle + point));

        Vector2 mid = middle;

        Gizmos.color = Color.magenta * new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, 0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, 0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, 0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, -0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, -0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, -0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, -0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, 0.5f, 0f), GetTextureAngleDeg() * Mathf.Deg2Rad) * (1f / GetTextureScaleInv())));
    

    }


    public void DrawTexturingGizmos2()
    {

        Vector3 up = new Vector3(0f, 1f, 0f);
        Vector3 middle = GetTextureOffset2();
        Vector3 point = RotatePoint2D_CCW(up, GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv());

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.TransformPoint(middle), transform.TransformPoint(middle + point));

        Vector2 mid = middle;

        Gizmos.color = Color.magenta * new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, 0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, 0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, 0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, -0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(-0.5f, -0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, -0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())));
        Gizmos.DrawLine(transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, -0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())), transform.TransformPoint(mid + RotatePoint2D_CCW(new Vector3(0.5f, 0.5f, 0f), GetTextureAngle2Deg() * Mathf.Deg2Rad) * (1f / GetTextureScale2Inv())));

    }
	
	public void DrawGrid() {
		Rect bounds = GetBounds();
		Gizmos.color = gridColor;
		
		if(showCoordinates == ShowCoordinates.World) {
			Vector3 topleft = transform.TransformPoint(new Vector3(bounds.xMin, bounds.yMin));
			Vector3 downright = transform.TransformPoint(new Vector3(bounds.xMax, bounds.yMax));
			
			float sx1 = SnapToGrid(topleft.x - gridSize*gridExpansion, gridSize);
			float sx2 = SnapToGrid(downright.x + gridSize*gridExpansion, gridSize);
			float sy1 = SnapToGrid(topleft.y - gridSize*gridExpansion, gridSize);
			float sy2 = SnapToGrid(downright.y + gridSize*gridExpansion, gridSize);

            if ((sx2 - sx1) / gridSize < 500f && (sy2 - sy1) / gridSize < 500f)
            {
                for (float x = sx1; x < sx2 + gridSize * 0.5f; x += gridSize)
                {
                    Gizmos.DrawLine(new Vector2(x, sy2), new Vector2(x, sy1));
                }

                for (float y = sy1; y < sy2 + gridSize * 0.5f; y += gridSize)
                {
                    Gizmos.DrawLine(new Vector2(sx2, y), new Vector2(sx1, y));
                }
            }
		} else {
		
			float sx1 = SnapToGrid(bounds.xMin, gridSize) - gridSize*gridExpansion;
			float sx2 = SnapToGrid(bounds.xMax, gridSize) + gridSize*gridExpansion;
			float sy1 = SnapToGrid(bounds.yMin, gridSize) - gridSize*gridExpansion;
			float sy2 = SnapToGrid(bounds.yMax, gridSize) + gridSize*gridExpansion;
			
            if((sx2-sx1)/gridSize < 500f && (sy2-sy1)/gridSize < 500f) {
			    for(float x = sx1; x < sx2+gridSize*0.5f; x+=gridSize) {
				    Gizmos.DrawLine(transform.TransformPoint(new Vector2(x, sy2)),transform.TransformPoint(new Vector2(x, sy1)));
			    }	
			
			    for(float y = sy1; y < sy2+gridSize*0.5f; y+=gridSize) {
				    Gizmos.DrawLine(transform.TransformPoint(new Vector2(sx2, y)),transform.TransformPoint(new Vector2(sx1, y)));
			    }
            }

		}
		
	}
	
	
	public bool IsSharpCorner(int index) {
		if(!GetNatural(index)) {
			Vector3 splinePos = GetPosition(index);
			float prevSplinePos = (float)index/(float)GetPointCount() - 1f/(float)GetVertexCount();
			float nextSplinePos = (float)index/(float)GetPointCount() + 1f/(float)GetVertexCount();
			Vector3 prevPos = GetPosition(prevSplinePos);	
			Vector3 nextPos = GetPosition(nextSplinePos);
			Vector3 inVec = splinePos - prevPos;
			Vector3 outVec = nextPos - splinePos;
			if( Vector3.Cross(inVec, outVec).z < 0f) {
				return true;		
			}
		}	
		return false;
	}
	
    public float GetLastSplinePosition()
    {
        if (SplineIsOpenEnded())
        {
            return (float)(GetPointCount() - 1) / (float)(GetPointCount());
        }
        else
        {
            return 1f;
        }
    }

    public bool SplineIsOpenEnded()
    {
        if (GetOutline() == Outline.Free && GetFill() == Fill.None || GetFill() == Fill.Landscape)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // RageSpline API

    public Vector3 GetNormal(float splinePosition)
    {
        return spline.GetNormal(splinePosition * GetLastSplinePosition());
    }
	
	public Vector3 GetNormalInterpolated(float splinePosition) {
        return spline.GetNormalInterpolated(splinePosition * GetLastSplinePosition());
	}

    public Vector3 GetNormal(int index)
    {
        return spline.GetNormal((float)index / (float)GetPointCount());
    }

    public void SetOutControlPosition(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).outCtrl = position - spline.GetRageSplinePoint(index).point;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).inCtrl = spline.GetRageSplinePoint(index).outCtrl * -1f;
        }
    }

    public void SetOutControlPositionPointSpace(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).outCtrl = position;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).inCtrl = spline.GetRageSplinePoint(index).outCtrl * -1f;
        }
    }

    public void SetOutControlPositionWorldSpace(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).outCtrl = transform.InverseTransformPoint(position) - spline.GetRageSplinePoint(index).point;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).inCtrl = spline.GetRageSplinePoint(index).outCtrl * -1f;
        }
    }

    public void SetInControlPosition(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).inCtrl = position - spline.GetRageSplinePoint(index).point;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).outCtrl = spline.GetRageSplinePoint(index).inCtrl * -1f;
        }

    }

    public void SetInControlPositionPointSpace(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).inCtrl = position;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).outCtrl = spline.GetRageSplinePoint(index).inCtrl * -1f;
        }
    }

    public void SetInControlPositionWorldSpace(int index, Vector3 position)
    {
        spline.GetRageSplinePoint(index).inCtrl = transform.InverseTransformPoint(position) - spline.GetRageSplinePoint(index).point;
        if (spline.GetRageSplinePoint(index).natural)
        {
            spline.GetRageSplinePoint(index).outCtrl = spline.GetRageSplinePoint(index).inCtrl * -1f;
        }
    }

    public Vector3 GetOutControlPosition(int index)
    {
        return spline.GetRageSplinePoint(index).point + spline.GetRageSplinePoint(index).outCtrl;
    }

    public Vector3 GetInControlPosition(int index)
    {
        return spline.GetRageSplinePoint(index).point + spline.GetRageSplinePoint(index).inCtrl;
    }

    public Vector3 GetOutControlPositionPointSpace(int index)
    {
        return spline.GetRageSplinePoint(index).outCtrl;
    }

    public Vector3 GetInControlPositionPointSpace(int index)
    {
        return spline.GetRageSplinePoint(index).inCtrl;
    }

    public Vector3 GetOutControlPositionWorldSpace(int index)
    {
        return transform.TransformPoint(spline.GetRageSplinePoint(index).point + spline.GetRageSplinePoint(index).outCtrl);
    }

    public Vector3 GetInControlPositionWorldSpace(int index)
    {
        return transform.TransformPoint(spline.GetRageSplinePoint(index).point + spline.GetRageSplinePoint(index).inCtrl);
    }

    public Vector3 GetPosition(int index)
    {
        return spline.GetRageSplinePoint(index).point;
    }

    public int GetPointCount()
    {
        return spline.points.Length;
    }

    public Vector3 GetPositionWorldSpace(int index)
    {
        return transform.TransformPoint(spline.GetRageSplinePoint(index).point);
    }

    public Vector3 GetPosition(float splinePosition)
    {
        return spline.GetPoint(splinePosition * GetLastSplinePosition());
    }

    public Vector3 GetPositionWorldSpace(float splinePosition)
    {
        return transform.TransformPoint(spline.GetPoint(splinePosition * GetLastSplinePosition()));
    }

    public Vector3 GetMiddle()
    {
        return spline.GetMiddle(100);
    }

    public Rect GetBounds()
    {
        Vector3 max = spline.GetMax(100, 0f, GetLastSplinePosition());
        Vector3 min = spline.GetMin(100, 0f, GetLastSplinePosition());
        return new Rect(min.x, min.y, (max.x - min.x), (max.y - min.y));
    }
	
	public float GetLength()
	{
		return spline.GetLength(128);	
	}

    public float GetNearestSplinePosition(Vector3 position, int accuracy)
    {
        float nearestSqrDist = 99999999999f;
        float nearestPoint = 0f;
        for (int i = 0; i < accuracy; i++)
        {
            Vector3 p = spline.GetPoint(((float)i / (float)accuracy) * GetLastSplinePosition());
            if ((position - p).sqrMagnitude < nearestSqrDist)
            {
                nearestPoint = ((float)i / (float)accuracy);
                nearestSqrDist = (position - p).sqrMagnitude;
            }
        }
        return nearestPoint;
    }

    public float GetNearestSplinePositionWorldSpace(Vector3 position, int accuracy)
    {
        return GetNearestSplinePosition(transform.InverseTransformPoint(position), accuracy);
    }

    public Vector3 GetNearestPosition(Vector3 position)
    {
        return spline.GetPoint(GetNearestSplinePosition(position, 100));
    }

    public Vector3 GetNearestPositionWorldSpace(Vector3 position)
    {
        return transform.TransformPoint(spline.GetPoint(spline.GetNearestSplinePoint(transform.InverseTransformPoint(position), 100)));
    }

    public void ClearPoints()
    {
        spline.ClearPoints();
    }

    public void AddPoint(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width, bool natural)
    {
        spline.AddRageSplinePoint(index, position, inCtrl, outCtrl, width, natural);
    }

    public void AddPoint(int index, Vector3 position, Vector3 outCtrl)
    {
        spline.AddRageSplinePoint(index, position, outCtrl * -1f, outCtrl, 1f, true);
    }

    public void AddPoint(int index, Vector3 position)
    {
        if (GetPointCount() >= 2)
        {
            spline.AddRageSplinePoint(index, position);
        }
        else
        {
            Debug.Log("ERROR: You can only call AddPoint(index, position), when there are 2 or more points in the RageSpline already");
        }
    }
    
    public int AddPoint(float splinePosition)
    {
        return spline.AddRageSplinePoint(splinePosition * GetLastSplinePosition());
    }
    
    public void AddPointWorldSpace(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width, bool natural)
    {
        spline.AddRageSplinePoint(index, transform.InverseTransformPoint(position), inCtrl, outCtrl, width, natural);
    }

    public void AddPointWorldSpace(int index, Vector3 position, Vector3 outCtrl, float width)
    {
        spline.AddRageSplinePoint(index, transform.InverseTransformPoint(position), outCtrl * -1f, outCtrl, width, true);
    }

    public void AddPointWorldSpace(int index, Vector3 position, Vector3 outCtrl)
    {
        spline.AddRageSplinePoint(index, transform.InverseTransformPoint(position), outCtrl * -1f, outCtrl, 1f, true);
    }
    
    public void AddPointWorldSpace(int index, Vector3 position)
    {
        if (GetPointCount() >= 2)
        {
            spline.AddRageSplinePoint(index, transform.InverseTransformPoint(position));
        }
        else
        {
            Debug.Log("ERROR: You can only call AddPoint(index, position), when there are 2 or more points in the RageSpline already");
        }
    } 
    
    public void SetPoint(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width, bool natural)
    {
        spline.points[index].point = position;
        spline.points[index].inCtrl = inCtrl;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].widthMultiplier = width;
        spline.points[index].natural = natural;
    }

    public void SetPoint(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, bool natural)
    {
        spline.points[index].point = position;
        spline.points[index].inCtrl = inCtrl;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].natural = natural;
    }

    public void SetPoint(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl)
    {
        spline.points[index].point = position;
        spline.points[index].inCtrl = inCtrl;
        spline.points[index].outCtrl = outCtrl;
    }

    public void SetPoint(int index, Vector3 position, Vector3 outCtrl)
    {
        spline.points[index].point = position;
        spline.points[index].inCtrl = outCtrl*-1f;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].natural = true;
    }

    public void SetPoint(int index, Vector3 position)
    {
        spline.points[index].point = position;
    }

    public void SetPointWorldSpace(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width, bool natural)
    {
        spline.points[index].point = transform.InverseTransformPoint(position);
        spline.points[index].inCtrl = outCtrl * -1f;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].widthMultiplier = width;
        spline.points[index].natural = natural;
    }

    public void SetPointWorldSpace(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl, float width)
    {
        spline.points[index].point = transform.InverseTransformPoint(position);
        spline.points[index].inCtrl = outCtrl * -1f;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].widthMultiplier = width;
    }

    public void SetPointWorldSpace(int index, Vector3 position, Vector3 inCtrl, Vector3 outCtrl)
    {
        spline.points[index].point = transform.InverseTransformPoint(position);
        spline.points[index].inCtrl = outCtrl * -1f;
        spline.points[index].outCtrl = outCtrl;
    }

    public void SetPointWorldSpace(int index, Vector3 position, Vector3 outCtrl)
    {
        spline.points[index].point = transform.InverseTransformPoint(position);
        spline.points[index].inCtrl = outCtrl * -1f;
        spline.points[index].outCtrl = outCtrl;
        spline.points[index].natural = true;
    }

    public void SetPointWorldSpace(int index, Vector3 position)
    {
        spline.points[index].point = transform.InverseTransformPoint(position);
    }
    
    public void RemovePoint(int index)
    {
        spline.DelPoint(index);
    }

    public bool GetNatural(int index)
    {
        return spline.GetRageSplinePoint(index).natural;
    }

    public void SetNatural(int index, bool natural)
    {
        spline.GetRageSplinePoint(index).natural = natural;
        if (natural)
        {
            spline.GetRageSplinePoint(index).outCtrl = spline.GetRageSplinePoint(index).inCtrl * -1f;
        }
    }

    public float GetOutlineWidth(float splinePosition)
    {
		float edgeWidth = spline.GetWidth(splinePosition) * GetOutlineWidth();
        /*
        float segmentPosition = spline.GetSegmentPosition(splinePosition);
		
		if((segmentPosition < 0.001f || segmentPosition > 0.999f) && corners == Corner.Beak) {
            if (!GetNatural(GetNearestPointIndex(splinePosition)))
            {
                Vector3 splinePos = GetPosition(splinePosition);
                float prevSplinePos = splinePosition * GetLastSplinePosition() - 1f / (float)GetVertexCount();
                float nextSplinePos = splinePosition * GetLastSplinePosition() + 1f / (float)GetVertexCount();
				Vector3 prevPos = GetPosition(prevSplinePos);	
				Vector3 nextPos = GetPosition(nextSplinePos);
				
				Vector3 inVec = splinePos - prevPos;
				Vector3 outVec = nextPos - splinePos;

				if( (Vector3.Cross(inVec, outVec).z < 0f || GetFill() == Fill.None) && Mathf.Abs(Vector3.Cross(inVec, outVec).z)>0.0001f) {
					Vector3 prevNormal = GetNormal(prevSplinePos);
					Vector3 nextNormal = GetNormal(nextSplinePos);
					Vector3 prevVertPos = new Vector3();
					Vector3 nextVertPos = new Vector3();
                    
				    prevVertPos = prevPos + prevNormal * edgeWidth;
	                nextVertPos = nextPos + nextNormal * edgeWidth;

                    edgeWidth = (spline.Intersect(prevVertPos, prevVertPos + inVec, nextVertPos, nextVertPos - outVec) - splinePos).magnitude;
                    edgeWidth = Mathf.Clamp(edgeWidth, 0f, spline.GetWidth(splinePosition) * GetOutlineWidth() * maxBeakLength);
				}
				
			}
		}
        */
        return edgeWidth;
    }
	
	public float GetAntialiasingWidth(float splinePosition)
    {
        float AAWidth = GetAntialiasingWidth();
		/*
		float segmentPosition = spline.GetSegmentPosition(splinePosition);
		
		if((segmentPosition < 0.001f || segmentPosition > 0.999f) && corners == Corner.Beak) {
			if(!GetNatural(GetNearestPointIndex(splinePosition))) {
				Vector3 splinePos = GetPosition(splinePosition);
				float prevSplinePos = splinePosition - 1f/(float)GetVertexCount();
				float nextSplinePos = splinePosition + 1f/(float)GetVertexCount();
				Vector3 prevPos = GetPosition(prevSplinePos);	
				Vector3 nextPos = GetPosition(nextSplinePos);
				
				Vector3 inVec = splinePos - prevPos;
				Vector3 outVec = nextPos - splinePos;

				if( (Vector3.Cross(inVec, outVec).z < 0f || GetFill() == Fill.None) && Mathf.Abs(Vector3.Cross(inVec, outVec).z)>0.01f) {
					Vector3 prevNormal = GetNormal(prevSplinePos);
					Vector3 nextNormal = GetNormal(nextSplinePos);
					Vector3 prevVertPos = new Vector3();
					Vector3 nextVertPos = new Vector3();

					if (GetFill() == Fill.None)
	                {
						prevVertPos = prevPos + prevNormal * spline.GetWidth(splinePosition) * GetOutlineWidth();
	                    nextVertPos = nextPos + nextNormal * spline.GetWidth(splinePosition) * GetOutlineWidth();
	            	}
	                else
	                {
						prevVertPos = prevPos + prevNormal * spline.GetWidth(splinePosition) * GetOutlineWidth();
	                    nextVertPos = nextPos + nextNormal * spline.GetWidth(splinePosition) * GetOutlineWidth();
	                }

					AAWidth = GetAntialiasingWidth() * ((spline.Intersect(prevVertPos, prevVertPos + inVec, nextVertPos, nextVertPos - outVec) - splinePos).magnitude / (spline.GetWidth(splinePosition) * GetOutlineWidth()));			
				}
				
			}
		}
        */
        return AAWidth;
    }

    public Vector3 FindNormal(Vector3 v1, Vector3 v2, Vector3 v3, float outlineWidth) //left, mid, right
    {
        Vector3 n1 = normalRotationQuat * (v1 - v2).normalized * outlineWidth;
        Vector3 n2 = normalRotationQuat * (v2 - v3).normalized * outlineWidth;
        return Crossing(v1 + n1, v2 + n1, v2 + n2, v3 + n2) - v2;
    }

    public Vector3 Crossing(Vector3 p11, Vector3 p12, Vector3 p21, Vector3 p22)
    {
        float Z = (p12.y - p11.y) * (p21.x - p22.x) - (p21.y - p22.y) * (p12.x - p11.x);
        //float Ca = (p12.y - p11.y) * (p21.x - p11.x) - (p21.y - p11.y) * (p12.x - p11.x);
        float Cb = (p21.y - p11.y) * (p21.x - p22.x) - (p21.y - p22.y) * (p21.x - p11.x);
        if (Z > -0.001f && Z < 0.001f || Cb > -0.001f && Cb < 0.001f)
        {
            return p12; //Segments are parallel.
        }
        else
        {
            return new Vector3(p11.x + (p12.x - p11.x) * Cb / Z, p11.y + (p12.y - p11.y) * Cb / Z);
        }
    }

    public int GetIndex(int index, int length)
    {
        if (index >= length)
            return length - index + 1;
        else if (index < 0)
            return length + index - 1;
        else
            return index;
    }


    public float GetOutlineWidth(int index)
    {
        return GetOutlineWidth((float)index/(float)GetPointCount());
    }

    public float GetOutlineWidthMultiplier(int index)
    {
        return spline.GetRageSplinePoint(index).widthMultiplier;
    }

    public void SetOutlineWidthMultiplier(int index, float width)
    {
        spline.GetRageSplinePoint(index).widthMultiplier = width;
    }

    public void SetOutline(Outline outline)
    {
        this.outline = outline;
       
        if(style != null)
        {
            this.style.SetOutline(outline, this);
        }
    }
    public Outline GetOutline()
    {
        if (style == null)
        {
            return this.outline;
        }
        else
        {
            return style.GetOutline();
        }
    }

    public void SetOutlineColor1(Color color)
    {

        this.outlineColor1 = color;
        
        if(style!=null)
        {
            this.style.SetOutlineColor1(color, this);
        }
    }
    public Color GetOutlineColor1()
    {
        if (this.style == null)
        {
            return this.outlineColor1;
        }
        else
        {
            return this.style.GetOutlineColor1();
        }
    }
	
    public Color GetOutlineColor2()
    {
        if (this.style == null)
        {
            return this.outlineColor2;
        }
        else
        {
            return this.style.GetOutlineColor2();
        }
    }
    public void SetOutlineColor2(Color color)
    {

        this.outlineColor2 = color;

        if (style != null)
        {
            this.style.SetOutlineColor2(color, this);
        }
    }
    public OutlineGradient GetOutlineGradient() 
    {
        if (this.style == null)
        {
            return this.outlineGradient;
        }
        else
        {
            return this.style.GetOutlineGradient();
        }
    }
    public void SetOutlineGradient(OutlineGradient outlineGradient)
    {
        this.outlineGradient = outlineGradient;

        if (style != null)
        {
            this.style.SetOutlineGradient(outlineGradient, this);
        }
    }
    public float GetOutlineNormalOffset()
    {
        if (this.style == null)
        {
            return this.outlineNormalOffset;
        }
        else
        {
            return this.style.GetOutlineNormalOffset();
        }
    }
    public void SetOutlineNormalOffset(float outlineNormalOffset)
    {
        this.outlineNormalOffset = outlineNormalOffset;

        if (style != null)
        {
            this.style.SetOutlineNormalOffset(outlineNormalOffset, this);
        }
    }

	public void SetCorners(Corner corners)
    {
        this.corners = corners;

        if (style != null)
        {
            style.SetCorners(corners, this);
        }
    }
    public Corner GetCorners()
    {
        if (style == null)
        {
            return this.corners;
        }
        else
        {
            return style.GetCorners();
        }
    }

	
    public void SetFill(Fill fill)
    {
        this.fill = fill;

        if (style != null)
        {
            style.SetFill(fill, this);
        }
    }
    public Fill GetFill()
    {
        if (style == null)
        {
            return this.fill;
        }
        else
        {
            return style.GetFill();
        }
    }

    public void SetFillColor1(Color color)
    {
        this.fillColor1 = color;

        if (style != null)
        {
            style.SetFillColor1(color, this);
        }
    }
    public Color GetFillColor1()
    {
        if (style == null)
        {
            return this.fillColor1;
        }
        else
        {
            return style.GetFillColor1();
        }
    }

    public void SetFillColor2(Color color)
    {
        this.fillColor2 = color;
        
        if (style != null)
        {
            style.SetFillColor2(color, this);
        }
    }
    public Color GetFillColor2()
    {
        if (style == null)
        {
            return this.fillColor2;
        }
        else
        {
            return style.GetFillColor2();
        }
    }

    public void SetLandscapeBottomDepth(float landscapeBottomDepth)
    {
        this.landscapeBottomDepth = landscapeBottomDepth;

        if (style != null)
        {
            style.SetLandscapeBottomDepth(landscapeBottomDepth, this);
        }
    }
    public float GetLandscapeBottomDepth()
    {
        if (style == null)
        {
            return this.landscapeBottomDepth;
        }
        else
        {
            return style.GetLandscapeBottomDepth();
        }
    }

    public void SetLandscapeOutlineAlign(float landscapeOutlineAlign)
    {
        landscapeOutlineAlign = Mathf.Clamp01(landscapeOutlineAlign);

        this.landscapeOutlineAlign = landscapeOutlineAlign;

        if (style != null)
        {
            style.SetLandscapeOutlineAlign(landscapeOutlineAlign, this);
        }
    }
    public float GetLandscapeOutlineAlign()
    {
        if (style == null)
        {
            return this.landscapeOutlineAlign;
        }
        else
        {
            return style.GetLandscapeOutlineAlign();
        }
    }


    public void SetTexturing1(UVMapping texturing)
    {
        this.UVMapping1 = texturing;

        if (style != null)
        {
            style.SetTexturing1(texturing, this);
        }
    }
    public UVMapping GetTexturing1()
    {
        if (style == null)
        {
            return this.UVMapping1;
        }
        else
        {
            return style.GetTexturing1();
        }
    }

    public void SetTexturing2(UVMapping texturing)
    {
        this.UVMapping2 = texturing;

        if (style != null)
        {
            style.SetTexturing2(texturing, this);
        }
    }
    public UVMapping GetTexturing2()
    {
        if (style == null)
        {
            return this.UVMapping2;
        }
        else
        {
            return style.GetTexturing2();
        }
    }

    public void SetGradientOffset(Vector2 offset)
    {
        this.gradientOffset = offset;

        if (style != null && !styleLocalGradientPositioning)
        {
            style.SetGradientOffset(offset, this);
        }
    }
    public Vector2 GetGradientOffset()
    {
        if (style == null || styleLocalGradientPositioning)
        {
            return this.gradientOffset;
        }
        else
        {
            return style.GetGradientOffset();
        }
    }

    public void SetGradientAngleDeg(float angle)
    {
        this.gradientAngle = Mathf.Clamp(angle, 0f, 360f);

        if (style != null && !styleLocalGradientPositioning)
        {
            style.SetGradientAngleDeg(angle, this);
        }
    }
    public float GetGradientAngleDeg()
    {
        if (style == null || styleLocalGradientPositioning)
        {
            return this.gradientAngle;
        }
        else
        {
            return style.GetGradientAngleDeg();
        }
    }

    public void SetGradientScaleInv(float scale)
    {
        this.gradientScale = Mathf.Clamp(scale, 0.00001f, 100f);

        if (style != null && !styleLocalGradientPositioning)
        {
            style.SetGradientScaleInv(scale, this);
        }
    }
    public float GetGradientScaleInv()
    {
        if (style == null || styleLocalGradientPositioning)
        {
            return this.gradientScale;
        }
        else
        {
            return style.GetGradientScaleInv();
        }
    }

    public void SetTextureOffset(Vector2 offset)
    {
        this.textureOffset = offset;

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureOffset(offset, this);
        }
    }
    public Vector2 GetTextureOffset()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureOffset;
        }
        else
        {
            return style.GetTextureOffset();
        }
    }

    public void SetTextureAngleDeg(float angle)
    {
        this.textureAngle = Mathf.Clamp(angle, 0f, 360f);

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureAngleDeg(angle, this);
        }
    }
    public float GetTextureAngleDeg()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureAngle;
        }
        else
        {
            return style.GetTextureAngleDeg();
        }
    }

    public void SetTextureScaleInv(float scale)
    {
        this.textureScale = Mathf.Clamp(scale, 0.00001f, 100f);

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureScaleInv(scale, this);
        }
    }
    public float GetTextureScaleInv()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureScale;
        }
        else
        {
            return style.GetTextureScaleInv();
        }
    }

    public void SetTextureOffset2(Vector2 offset)
    {
        this.textureOffset2 = offset;

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureOffset2(offset, this);
        }
    }
    public Vector2 GetTextureOffset2()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureOffset2;
        }
        else
        {
            return style.GetTextureOffset2();
        }
    }

    public void SetTextureAngle2Deg(float angle)
    {
        this.textureAngle2 = Mathf.Clamp(angle, 0f, 360f);

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureAngle2Deg(angle, this);
        }
    }
    public float GetTextureAngle2Deg()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureAngle2;
        }
        else
        {
            return style.GetTextureAngle2Deg();
        }
    }

    public void SetTextureScale2Inv(float scale)
    {
        this.textureScale2 = Mathf.Clamp(scale, 0.00001f, 100f);

        if (style != null && !styleLocalTexturePositioning)
        {
            style.SetTextureScale2Inv(scale, this);
        }
    }
    public float GetTextureScale2Inv()
    {
        if (style == null || styleLocalTexturePositioning)
        {
            return this.textureScale2;
        }
        else
        {
            return style.GetTextureScale2Inv();
        }
    }

    public void SetEmboss(Emboss emboss)
    {
        this.emboss = emboss;

        if (style != null)
        {
            style.SetEmboss(emboss, this);
        }
    }
    public Emboss GetEmboss()
    {
        if (style == null)
        {
            return this.emboss;
        }
        else
        {
            return style.GetEmboss();
        }
    }

    public void SetEmbossColor1(Color color)
    {
        this.embossColor1 = color;

        if (style != null)
        {
            style.SetEmbossColor1(color, this);
        }
    }
    public Color GetEmbossColor1()
    {
        if (style == null)
        {
            return this.embossColor1;
        }
        else
        {
            return style.GetEmbossColor1();
        }
    }

    public void SetEmbossColor2(Color color)
    {
        this.embossColor2 = color;

        if (style != null)
        {
            style.SetEmbossColor2(color, this);
        }
    }
    public Color GetEmbossColor2()
    {
        if (style == null)
        {
            return this.embossColor2;
        }
        else
        {
            return style.GetEmbossColor2();
        }
    }

    public void SetEmbossAngleDeg(float angle)
    {
        this.embossAngle = Mathf.Clamp(angle, 0f, 360f);
        if (style != null && !styleLocalEmbossPositioning)
        {
            style.SetEmbossAngle(angle, this);
        }
    }
    public float GetEmbossAngleDeg()
    {
        if (style == null || styleLocalEmbossPositioning)
        {
            return this.embossAngle;
        }
        else
        {
            return style.GetEmbossAngle();
        }
    }

    public void SetEmbossOffset(float offset)
    {
        this.embossOffset = offset;

        if (style != null && !styleLocalEmbossPositioning)
        {
            style.SetEmbossOffset(offset, this);
        }
    }
    public float GetEmbossOffset()
    {
        if (style == null || styleLocalEmbossPositioning)
        {
            return this.embossOffset;
        }
        else
        {
            return style.GetEmbossOffset();
        }
    }

    public void SetEmbossSize(float size)
    {
        this.embossSize = Mathf.Clamp(size, 0.00061f, 1000f);

        if (style != null && !styleLocalEmbossPositioning)
        {
            style.SetEmbossSize(size, this);
        }
    }
    public float GetEmbossSize()
    {
        if (style == null)
        {
            return this.embossSize;
        }
        else
        {
            return style.GetEmbossSize();
        }
    }

    public void SetEmbossSmoothness(float smoothness)
    {
        this.embossCurveSmoothness = Mathf.Clamp(smoothness, 0f, 100f);

        if (style != null)
        {
            style.SetEmbossSmoothness(smoothness, this);
        }
    }
    public float GetEmbossSmoothness()
    {
        if (style == null)
        {
            return this.embossCurveSmoothness;
        }
        else
        {
            return style.GetEmbossSmoothness();
        }
    }

    public void SetPhysics(Physics physics)
    {
        this.physics = physics;

        if (style != null)
        {
            style.SetPhysics(physics, this);
        }
    }
    public Physics GetPhysics()
    {
        if (style == null)
        {
            return this.physics;
        }
        else
        {
            return style.GetPhysics();
        }
    }

    public void SetCreatePhysicsInEditor(bool createInEditor)
    {
        this.createPhysicsInEditor = createInEditor;

        if (style != null)
        {
            style.SetCreatePhysicsInEditor(createInEditor, this);
        }
    }
    public bool GetCreatePhysicsInEditor()
    {
        if (style == null)
        {
            return this.createPhysicsInEditor;
        }
        else
        {
            return style.GetCreatePhysicsInEditor();
        }
    }

    public void SetPhysicsMaterial(PhysicMaterial physicsMaterial)
    {
        this.physicsMaterial = physicsMaterial;

        if (style != null)
        {
            style.SetPhysicsMaterial(physicsMaterial, this);
        }
    }
    public PhysicMaterial GetPhysicsMaterial()
    {
        if (style == null)
        {
            return this.physicsMaterial;
        }
        else
        {
            return style.GetPhysicsMaterial();
        }
    }

    public void SetVertexCount(int count)
    {
        if (!lowQualityRender)
        {
            int vCount = 0;
            int pCountFix = 0;

            if (outline == Outline.Free && (fill == Fill.None))
            {
                pCountFix = -1;
            }

            if (mod(count, GetPointCount() + pCountFix) == 0)
            {
                vCount = count;
            }
            else
            {
                if (mod(count, GetPointCount() + pCountFix) >= (GetPointCount() + pCountFix) / 2)
                {
                    vCount = count + (GetPointCount() + pCountFix - mod(count, GetPointCount() + pCountFix));
                }
                else
                {
                    vCount = count - (mod(count, GetPointCount() + pCountFix));
                }
                
            }

            if (vCount < GetPointCount() + pCountFix)
            {
                vCount = GetPointCount() + pCountFix;
            }
            
            this.vertexCount = vCount;

            if (style != null && !styleLocalVertexCount)
            {
                if (count <= 0)
                {
                    count = 1;
                }
                style.SetVertexCount(count, this);
            }
        }
    }
    public int GetVertexCount()
    {
        if (style == null || lowQualityRender || styleLocalVertexCount)
        {
            if (lowQualityRender)
            {
                return 128;
            }
            else
            {
                if (this.vertexCount >= GetPointCount() || (outline == Outline.Free && fill == Fill.None))
                {
                    return this.vertexCount;
                }
                else
                {
                    return GetPointCount();
                }
            }
        }
        else
        {
            if (mod(style.GetVertexCount(), GetPointCount()) == 0)
            {
                return style.GetVertexCount();
            }
            else
            {
                return style.GetVertexCount() + (GetPointCount() - mod(style.GetVertexCount(), GetPointCount()));
            }
        }
    }

    public void SetPhysicsColliderCount(int count)
    {
        this.physicsColliderCount = count;

        if (style != null && !styleLocalPhysicsColliderCount)
        {
            style.SetPhysicsColliderCount(count, this);
        }
    }
    public int GetPhysicsColliderCount()
    {
        if (style == null || styleLocalPhysicsColliderCount)
        {
            return this.physicsColliderCount;
        }
        else
        {
            return style.GetPhysicsColliderCount();
        }
    }

    public void SetCreateConvexMeshCollider(bool createConvexMeshCollider)
    {
        this.createConvexMeshCollider = createConvexMeshCollider;

        if (style != null)
        {
            style.SetCreateConvexMeshCollider(createConvexMeshCollider, this);
        }
    }
    public bool GetCreateConvexMeshCollider()
    {
        if (style == null)
        {
            return this.createConvexMeshCollider;
        }
        else
        {
            return style.GetCreateConvexMeshCollider();
        }
    }

    public void SetPhysicsZDepth(float depth)
    {
        this.colliderZDepth = Mathf.Clamp(depth, 0.0001f, 10000f);

        if (style != null)
        {
            style.SetPhysicsZDepth(depth, this);
        }
    }
    public float GetPhysicsZDepth()
    {
        if (style == null)
        {
            return this.colliderZDepth;
        }
        else
        {
            return style.GetPhysicsZDepth();
        }
    }

    public void SetPhysicsNormalOffset(float offset)
    {
        this.colliderNormalOffset = Mathf.Clamp(offset, -1000f, 1000f);

        if (style != null)
        {
            style.SetPhysicsNormalOffset(offset, this);
        }
    }
    public float GetPhysicsNormalOffset()
    {
        if (style == null)
        {
            return this.colliderNormalOffset;
        }
        else
        {
            return style.GetPhysicsNormalOffset();
        }
    }

    public void SetBoxColliderDepth(float depth)
    {
        this.boxColliderDepth = Mathf.Clamp(depth, -1000f, 1000f);

        if (style != null)
        {
            style.SetBoxColliderDepth(depth, this);
        }
    }
    public float GetBoxColliderDepth()
    {
        if (style == null)
        {
            return this.boxColliderDepth;
        }
        else
        {
            return style.GetBoxColliderDepth();
        }
    }

    public void SetAntialiasingWidth(float width)
    {
        this.antiAliasingWidth = Mathf.Clamp(width, 0f, 1000f);

        if (style != null && !styleLocalAntialiasing)
        {
            style.SetAntialiasingWidth(width, this);
        }
    }
    public float GetAntialiasingWidth()
    {
        if (style == null || styleLocalAntialiasing)
        {
            return this.antiAliasingWidth;
        }
        else
        {
            return style.GetAntialiasingWidth();
        }
    }

    public void SetOutlineWidth(float width)
    {
        this.OutlineWidth = Mathf.Clamp(width, 0.0001f, 1000f);

        if (style != null)
        {
            style.SetOutlineWidth(width, this);
        }
    }
    public float GetOutlineWidth()
    {
        if (style == null)
        {
            return this.OutlineWidth;
        }
        else
        {
            return style.GetOutlineWidth();
        }
    }

    public void SetOutlineTexturingScaleInv(float scale)
    {
        this.outlineTexturingScale = Mathf.Clamp(scale, 0.001f, 1000f);

        if (style != null)
        {
            style.SetOutlineTexturingScaleInv(scale, this);
        }
    }
    public float GetOutlineTexturingScaleInv()
    {
        if (style == null)
        {
            return this.outlineTexturingScale;
        }
        else
        {
            return style.GetOutlineTexturingScaleInv();
        }
    }

    public void SetOptimizeAngle(float angle)
    {
        this.optimizeAngle = angle;
        if (style != null)
        {
            style.SetOptimizeAngle(angle, this);
        }
    }

    public float GetOptimizeAngle()
    {
        if (style == null)
        {
            return this.optimizeAngle;
        }
        else
        {
            return style.GetOptimizeAngle();
        }
    }

    public void SetOptimize(bool optimize)
    {
        this.optimize = optimize;
        if (style != null)
        {
            style.SetOptimize(optimize, this);
        }
    }
    
    public bool GetOptimize()
    {
        if (style == null)
        {
            return this.optimize;
        }
        else
        {
            return style.GetOptimize();
        }
    }

    public void SetStyle(RageSplineStyle style)
    {
        this.style = style;
    }
    public RageSplineStyle GetStyle()
    {
        return style;
    }
}

