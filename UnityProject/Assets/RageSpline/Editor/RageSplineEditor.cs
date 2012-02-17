using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

[CustomEditor(typeof(RageSpline))]
public class RageSplineEditor : Editor
{
    // Since Unity 3.4 this is deprecated.
    public bool showMeshColliderWarning = false;
    public bool showLocalStyleOptions = false;
    public int[] selectedControlPoints = new int[0];
    
    public override void OnInspectorGUI()
    {
		RageSpline rageSpline = target as RageSpline;

		int labelWidth = 166;

		EditorGUIUtility.LookLikeControls();
		
		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		rageSpline.SetOutline((RageSpline.Outline)EditorGUILayout.EnumPopup(" Outline:", rageSpline.GetOutline(), GUILayout.Width(labelWidth)));
		if (rageSpline.GetOutline() != RageSpline.Outline.None)
		{
			rageSpline.SetOutlineColor1(EditorGUILayout.ColorField(rageSpline.GetOutlineColor1(), GUILayout.Width(40)));
			if (rageSpline.GetOutlineGradient() != RageSpline.OutlineGradient.None)
			{
				rageSpline.SetOutlineColor2(EditorGUILayout.ColorField(rageSpline.GetOutlineColor2(), GUILayout.Width(40)));
			}
		}
		EditorGUILayout.EndHorizontal();
		if (rageSpline.GetOutline() != RageSpline.Outline.None)
		{
			EditorGUIUtility.LookLikeInspector();
			rageSpline.SetOutlineWidth(EditorGUILayout.FloatField("      Outline width", rageSpline.GetOutlineWidth()));
			rageSpline.SetOutlineNormalOffset(EditorGUILayout.FloatField("      Outline offset", rageSpline.GetOutlineNormalOffset()));
			//EditorGUIUtility.LookLikeControls();
			rageSpline.SetCorners((RageSpline.Corner)EditorGUILayout.EnumPopup("      Outline corners:", rageSpline.GetCorners()));
			EditorGUIUtility.LookLikeInspector();
			
			if (rageSpline.GetTexturing1() == RageSpline.UVMapping.Outline || rageSpline.GetTexturing2() == RageSpline.UVMapping.Outline)
			{
				rageSpline.SetOutlineTexturingScaleInv(EditorGUILayout.FloatField("      Texturing scale", rageSpline.GetOutlineTexturingScaleInv()));
			}
			rageSpline.SetOutlineGradient((RageSpline.OutlineGradient)EditorGUILayout.EnumPopup("      Outline gradient:", rageSpline.GetOutlineGradient()));
			EditorGUILayout.Separator();
		}

		EditorGUIUtility.LookLikeControls();

		
		EditorGUILayout.BeginHorizontal();
		rageSpline.SetFill((RageSpline.Fill)EditorGUILayout.EnumPopup(" Fill:", rageSpline.GetFill(), GUILayout.Width(labelWidth)));
		switch (rageSpline.fill)
		{
			case RageSpline.Fill.None:
				break;
			case RageSpline.Fill.Solid:
				rageSpline.SetFillColor1(EditorGUILayout.ColorField(rageSpline.GetFillColor1(), GUILayout.Width(40)));
				break;
			case RageSpline.Fill.Gradient:
				rageSpline.SetFillColor1(EditorGUILayout.ColorField(rageSpline.GetFillColor1(), GUILayout.Width(40)));
				rageSpline.SetFillColor2(EditorGUILayout.ColorField(rageSpline.GetFillColor2(), GUILayout.Width(40)));
				break;
			case RageSpline.Fill.Landscape:
				rageSpline.SetFillColor1(EditorGUILayout.ColorField(rageSpline.GetFillColor1(), GUILayout.Width(40)));
				rageSpline.SetFillColor2(EditorGUILayout.ColorField(rageSpline.GetFillColor2(), GUILayout.Width(40)));
			   break;
		}
		EditorGUILayout.EndHorizontal();

		if (rageSpline.GetFill() == RageSpline.Fill.Landscape)
		{
			EditorGUIUtility.LookLikeInspector();
			rageSpline.SetLandscapeBottomDepth(EditorGUILayout.FloatField("      Landscape bottom depth", rageSpline.GetLandscapeBottomDepth()));
			rageSpline.SetLandscapeOutlineAlign(EditorGUILayout.FloatField("      Landscape Outline align", rageSpline.GetLandscapeOutlineAlign()));
			EditorGUILayout.Separator();
		}
		
		EditorGUIUtility.LookLikeControls();
				
		EditorGUILayout.BeginHorizontal();
		rageSpline.SetEmboss((RageSpline.Emboss)EditorGUILayout.EnumPopup(" Emboss:", rageSpline.GetEmboss(), GUILayout.Width(labelWidth)));
		switch (rageSpline.GetEmboss())
		{
			case RageSpline.Emboss.None:
				break;
			case RageSpline.Emboss.Sharp:
			case RageSpline.Emboss.Blurry:
				rageSpline.SetEmbossColor1(EditorGUILayout.ColorField(rageSpline.GetEmbossColor1(), GUILayout.Width(40)));
				rageSpline.SetEmbossColor2(EditorGUILayout.ColorField(rageSpline.GetEmbossColor2(), GUILayout.Width(40)));
				break;
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUIUtility.LookLikeControls();

		if (rageSpline.GetEmboss() != RageSpline.Emboss.None)
		{
			EditorGUIUtility.LookLikeInspector();
			rageSpline.SetEmbossOffset(EditorGUILayout.FloatField("      Emboss offset", rageSpline.GetEmbossOffset()));
			rageSpline.SetEmbossSmoothness(EditorGUILayout.FloatField("      Emboss smoothness", rageSpline.GetEmbossSmoothness()));
			EditorGUILayout.Separator();
		}

		EditorGUIUtility.LookLikeControls();

		EditorGUILayout.BeginHorizontal();
		rageSpline.SetTexturing1((RageSpline.UVMapping)EditorGUILayout.EnumPopup(" Texturing:", rageSpline.GetTexturing1(), GUILayout.Width(labelWidth)));
		rageSpline.SetTexturing2((RageSpline.UVMapping)EditorGUILayout.EnumPopup(rageSpline.GetTexturing2(), GUILayout.Width(75)));
		EditorGUILayout.EndHorizontal();
		
		EditorGUIUtility.LookLikeControls();

		RageSpline.Physics oldPhysics = rageSpline.GetPhysics();
		bool oldCreatePhysicsInEditor = rageSpline.GetCreatePhysicsInEditor();

		rageSpline.SetPhysics((RageSpline.Physics)EditorGUILayout.EnumPopup(" Physics:", rageSpline.GetPhysics(), GUILayout.Width(labelWidth)));
		EditorGUIUtility.LookLikeInspector();

		switch (rageSpline.GetPhysics())
		{
			case RageSpline.Physics.None:
				break;
			case RageSpline.Physics.MeshCollider:
            case RageSpline.Physics.OutlineMeshCollider:
                rageSpline.SetPhysicsColliderCount(EditorGUILayout.IntField("      Physics split count", rageSpline.GetPhysicsColliderCount()));
				rageSpline.SetPhysicsZDepth(EditorGUILayout.FloatField("      Physics z-depth", rageSpline.GetPhysicsZDepth()));
                if (rageSpline.GetPhysics() != RageSpline.Physics.OutlineMeshCollider)
                {
                    rageSpline.SetPhysicsNormalOffset(EditorGUILayout.FloatField("      Physics normal offset", rageSpline.GetPhysicsNormalOffset()));
                }
                #if UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
                rageSpline.SetPhysicsMaterial(EditorGUILayout.ObjectField("      Physics material", rageSpline.GetPhysicsMaterial(), typeof(PhysicMaterial), false) as PhysicMaterial);
                #else
                rageSpline.SetPhysicsMaterial(EditorGUILayout.ObjectField("      Physics material", rageSpline.GetPhysicsMaterial(), typeof(PhysicMaterial)) as PhysicMaterial);
                #endif

                if (rageSpline.GetPhysics() != RageSpline.Physics.OutlineMeshCollider)
                {
                    rageSpline.SetCreateConvexMeshCollider(EditorGUILayout.Toggle("      Physics convex", rageSpline.GetCreateConvexMeshCollider()));
                }
                rageSpline.SetCreatePhysicsInEditor(EditorGUILayout.Toggle("      Physics created in editor", rageSpline.GetCreatePhysicsInEditor()));
			
				EditorGUILayout.Separator();
				break;
			case RageSpline.Physics.Boxed:
				rageSpline.SetPhysicsColliderCount(EditorGUILayout.IntField("      Physics split count", rageSpline.GetPhysicsColliderCount()));
				rageSpline.SetPhysicsZDepth(EditorGUILayout.FloatField("      Physics z-depth", rageSpline.GetPhysicsZDepth()));
                rageSpline.SetPhysicsNormalOffset(EditorGUILayout.FloatField("      Physics offset", rageSpline.GetPhysicsNormalOffset()));
				rageSpline.SetBoxColliderDepth(EditorGUILayout.FloatField("      Physics normal depth", rageSpline.GetBoxColliderDepth()));
                
                #if UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
                rageSpline.SetPhysicsMaterial(EditorGUILayout.ObjectField("      Physics material", rageSpline.GetPhysicsMaterial(), typeof(PhysicMaterial), false) as PhysicMaterial);
				#else
                rageSpline.SetPhysicsMaterial(EditorGUILayout.ObjectField("      Physics material", rageSpline.GetPhysicsMaterial(), typeof(PhysicMaterial)) as PhysicMaterial);
				#endif

                rageSpline.SetCreatePhysicsInEditor(EditorGUILayout.Toggle("      Physics create in editor", rageSpline.GetCreatePhysicsInEditor()));
			
				EditorGUILayout.Separator();
				break;
		}

		if (showMeshColliderWarning)
		{
			if (rageSpline.GetPhysics() != RageSpline.Physics.MeshCollider && oldPhysics == RageSpline.Physics.MeshCollider && rageSpline.GetCreatePhysicsInEditor() ||
				rageSpline.GetPhysics() == RageSpline.Physics.MeshCollider && oldCreatePhysicsInEditor && !rageSpline.GetCreatePhysicsInEditor())
			{
				MeshCollider meshCollider = rageSpline.gameObject.GetComponent(typeof(MeshCollider)) as MeshCollider;
				if (meshCollider != null)
				{
					Debug.Log("RageSpline warning: Please remove the Mesh Collider component from the GameObject '" + rageSpline.gameObject.name + "' manually. This warning can be disabled from the RageSpline/Code/RageSplineEditor.cs line 13.");
				}
			}
		}
				
		EditorGUILayout.Separator();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("   General:", "");
		EditorGUILayout.EndHorizontal();
		EditorGUIUtility.LookLikeInspector();

        if (!rageSpline.lowQualityRender)
        {
            rageSpline.SetVertexCount(EditorGUILayout.IntField("      Vertex count", rageSpline.GetVertexCount()));
        }
        else
        {
            rageSpline.SetVertexCount(EditorGUILayout.IntField("      Vertex count", rageSpline.vertexCount));
        }

		rageSpline.SetAntialiasingWidth(EditorGUILayout.FloatField("      Anti-aliasing width", rageSpline.GetAntialiasingWidth()));
		rageSpline.showSplineGizmos = EditorGUILayout.Toggle("      Show spline gizmos", rageSpline.showSplineGizmos);
		rageSpline.showOtherGizmos = EditorGUILayout.Toggle("      Show other gizmos", rageSpline.showOtherGizmos);
        rageSpline.showWireFrameInEditor = EditorGUILayout.Toggle("      Show wireframe", rageSpline.showWireFrameInEditor);
        rageSpline.hideHandles = EditorGUILayout.Toggle("      Hide handles", rageSpline.hideHandles);
		
		rageSpline.SetOptimize(EditorGUILayout.Toggle("      Optimize", rageSpline.GetOptimize()));
        if (rageSpline.GetOptimize())
        {
            rageSpline.SetOptimizeAngle(EditorGUILayout.FloatField("      Optimize Angle", rageSpline.GetOptimizeAngle()));
        }

		EditorGUILayout.Separator();
		EditorGUIUtility.LookLikeControls();

		EditorGUILayout.BeginHorizontal();
		rageSpline.showCoordinates = ((RageSpline.ShowCoordinates)EditorGUILayout.EnumPopup("  Snapping:", rageSpline.showCoordinates, GUILayout.Width(labelWidth)));
		EditorGUILayout.EndHorizontal();
		
		EditorGUIUtility.LookLikeInspector();
		if ((rageSpline.showCoordinates != RageSpline.ShowCoordinates.None))
		{
			rageSpline.showGrid = EditorGUILayout.Toggle("      Show grid", rageSpline.showGrid);
            if (rageSpline.showGrid)
            {
                rageSpline.gridColor = EditorGUILayout.ColorField("      Grid color", rageSpline.gridColor);
                rageSpline.gridExpansion = EditorGUILayout.IntField("      Grid expand", rageSpline.gridExpansion);
            }
            rageSpline.gridSize = Mathf.Clamp(EditorGUILayout.FloatField("      Grid size", rageSpline.gridSize), 0.01f, 100f);
            rageSpline.snapToGrid = EditorGUILayout.Toggle("      Snap points to grid", rageSpline.snapToGrid);
			rageSpline.snapHandlesToGrid = EditorGUILayout.Toggle("      Snap handles to grid", rageSpline.snapHandlesToGrid);
			rageSpline.showNumberInputs = EditorGUILayout.Toggle("      Coordinate inputs", rageSpline.showNumberInputs);
			
		}
		
			
		EditorGUIUtility.LookLikeControls();


		if (rageSpline.showCoordinates != RageSpline.ShowCoordinates.None && rageSpline.showNumberInputs)
		{
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Point  X|Y", "", GUILayout.MinWidth(40f));
			EditorGUILayout.LabelField("Handle In  X|Y", "", GUILayout.MinWidth(40f));
			EditorGUILayout.LabelField("Handle Out  X|Y", "", GUILayout.MinWidth(40f));
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.LookLikeControls();
			
						
			for (int p = 0; p < selectedControlPoints.Length; p++)
			{
                int i = selectedControlPoints[p];			
				switch (rageSpline.showCoordinates)
				{
					case RageSpline.ShowCoordinates.Local:
						EditorGUILayout.BeginHorizontal();
						Vector2 newPos = new Vector2();

						newPos.x = EditorGUILayout.FloatField(rageSpline.GetPosition(i).x, GUILayout.MinWidth(30f));
						newPos.y = EditorGUILayout.FloatField(rageSpline.GetPosition(i).y, GUILayout.MinWidth(30f));
						
						rageSpline.SetPoint(i, newPos);
				
						Vector2 newIn = new Vector2();
						Vector2 newOut = new Vector2();

						newIn.x = EditorGUILayout.FloatField(rageSpline.GetInControlPositionPointSpace(i).x, GUILayout.MinWidth(30f));
						newIn.y = EditorGUILayout.FloatField(rageSpline.GetInControlPositionPointSpace(i).y, GUILayout.MinWidth(30f));
						newOut.x = EditorGUILayout.FloatField(rageSpline.GetOutControlPositionPointSpace(i).x, GUILayout.MinWidth(30f));
						newOut.y = EditorGUILayout.FloatField(rageSpline.GetOutControlPositionPointSpace(i).y, GUILayout.MinWidth(30f));
						
						rageSpline.SetInControlPositionPointSpace(i, newIn);                       
						rageSpline.SetOutControlPositionPointSpace(i, newOut);
					
						EditorGUILayout.EndHorizontal();
						break;
					case RageSpline.ShowCoordinates.World:
						EditorGUILayout.BeginHorizontal();
						Vector2 newPos2 = new Vector2();

						newPos2.x = EditorGUILayout.FloatField(rageSpline.GetPositionWorldSpace(i).x, GUILayout.MinWidth(30f));
						newPos2.y = EditorGUILayout.FloatField(rageSpline.GetPositionWorldSpace(i).y, GUILayout.MinWidth(30f));
						
						rageSpline.SetPointWorldSpace(i, newPos2);

						Vector2 newIn2 = new Vector2();
						Vector2 newOut2 = new Vector2();

						newIn2.x = EditorGUILayout.FloatField(rageSpline.GetInControlPositionWorldSpace(i).x, GUILayout.MinWidth(30f));
						newIn2.y = EditorGUILayout.FloatField(rageSpline.GetInControlPositionWorldSpace(i).y, GUILayout.MinWidth(30f));
						newOut2.x = EditorGUILayout.FloatField(rageSpline.GetOutControlPositionWorldSpace(i).x, GUILayout.MinWidth(30f));
						newOut2.y = EditorGUILayout.FloatField(rageSpline.GetOutControlPositionWorldSpace(i).y, GUILayout.MinWidth(30f));							
						
						rageSpline.SetInControlPositionWorldSpace(i, newIn2);
						rageSpline.SetOutControlPositionWorldSpace(i, newOut2);
						
						EditorGUILayout.EndHorizontal();
						break;
				}
				
			}
		}

		EditorGUILayout.Separator();

		EditorGUIUtility.LookLikeControls();

		EditorGUILayout.BeginHorizontal();

        #if UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
            rageSpline.style = EditorGUILayout.ObjectField(" Style", rageSpline.style, typeof(RageSplineStyle), false) as RageSplineStyle;
        #else
            rageSpline.style = EditorGUILayout.ObjectField(" Style", rageSpline.style, typeof(RageSplineStyle)) as RageSplineStyle;
        #endif
        EditorGUILayout.EndHorizontal();
		

		if (rageSpline.style == null)
		{
			EditorGUILayout.BeginHorizontal();

			rageSpline.defaultStyleName = EditorGUILayout.TextField("      New style", rageSpline.defaultStyleName);
			if (GUILayout.Button("Create", GUILayout.Height(15f), GUILayout.Width(60f)))
			{
				RageSplineStyle style = ScriptableObject.CreateInstance(typeof(RageSplineStyle)) as RageSplineStyle;
				style.GetStyleFromRageSpline(rageSpline);
				AssetDatabase.CreateAsset(style, "Assets/" + rageSpline.defaultStyleName + ".asset");
				EditorUtility.SetDirty(style);
				AssetDatabase.SaveAssets();
				rageSpline.SetStyle(style);
			}
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUIUtility.LookLikeInspector();

            showLocalStyleOptions = EditorGUILayout.Foldout(showLocalStyleOptions, "      Style options"); 
            if (showLocalStyleOptions)
            {
                rageSpline.styleLocalGradientPositioning = EditorGUILayout.Toggle("      Local gradient gizmos", rageSpline.styleLocalGradientPositioning);
                rageSpline.styleLocalTexturePositioning = EditorGUILayout.Toggle("      Local texture gizmos", rageSpline.styleLocalTexturePositioning);
                rageSpline.styleLocalEmbossPositioning = EditorGUILayout.Toggle("      Local emboss gizmos", rageSpline.styleLocalEmbossPositioning);
                rageSpline.styleLocalAntialiasing = EditorGUILayout.Toggle("      Local anti-aliasing", rageSpline.styleLocalAntialiasing);
                rageSpline.styleLocalVertexCount = EditorGUILayout.Toggle("      Local vertex count", rageSpline.styleLocalVertexCount);
                rageSpline.styleLocalPhysicsColliderCount = EditorGUILayout.Toggle("      Local physics collider count", rageSpline.styleLocalPhysicsColliderCount);
            }
        }

		EditorGUILayout.Separator();

		if (Event.current.type == EventType.ExecuteCommand)
		{
			Undo.CreateSnapshot();
			Undo.RegisterSnapshot();
		}
		
		if (Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.KeyUp || Event.current.type == EventType.used)
		{
			RageSpline spMesh = target as RageSpline;
			if (rageSpline.style != null)
			{
				EditorUtility.SetDirty(rageSpline.style);
				AssetDatabase.SaveAssets();
			}
			spMesh.RefreshMeshInEditor(true, true, true);
		}
    }

	
    public void OnSceneGUI()
    {
        bool mouseDrag = false;
        bool mouseUp = false;
        bool keyUp = false;
        KeyCode keyCode = KeyCode.None;
        if (Event.current.type == EventType.keyUp)
        {
            keyUp = true;
            keyCode = Event.current.keyCode;
        }
        if (Event.current.type == EventType.mouseUp)
        {
            mouseUp = true;
        }
        if (Event.current.type == EventType.mouseDrag)
        {
            mouseDrag = true;
        }
		Undo.SetSnapshotTarget(target, "Modified object");
        
		RageSpline rageSpline = target as RageSpline;

        
        if (rageSpline.renderer != null)
        {
            if (!rageSpline.showWireFrameInEditor)
            {
                EditorUtility.SetSelectedWireframeHidden(rageSpline.renderer, true);
            }
            else
            {
                EditorUtility.SetSelectedWireframeHidden(rageSpline.renderer, false);
            }
        }

		if(Mathf.Abs(rageSpline.transform.localScale.x)>0f && Mathf.Abs(rageSpline.transform.localScale.y)>0f && Mathf.Abs(rageSpline.transform.localScale.z)>0f)
        {
			
			if (rageSpline.showOtherGizmos && rageSpline.GetFill() == RageSpline.Fill.Gradient)
			{
				Handles.color = Color.green;
				rageSpline.SetGradientOffset(
					rageSpline.transform.InverseTransformPoint(
					Handles.FreeMoveHandle(
						rageSpline.transform.TransformPoint(rageSpline.GetGradientOffset()),
						Quaternion.identity,
						HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(rageSpline.GetGradientOffset())) * 0.2f,
						Vector3.zero,
						Handles.CircleCap
					))
				);
				
				Handles.color = Color.green;
				Vector3 up = new Vector3(0f, 1f, 0f);
				Vector3 point = rageSpline.RotatePoint2D_CCW(up, rageSpline.GetGradientAngleDeg() * Mathf.Deg2Rad);
				Vector3 middle = rageSpline.GetGradientOffset();

				Vector3 pos =
					rageSpline.transform.InverseTransformPoint(
						Handles.FreeMoveHandle(
							rageSpline.transform.TransformPoint(middle + point * (1f / rageSpline.GetGradientScaleInv())),
							Quaternion.identity,
							HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(point)) * 0.1f,
							Vector3.zero,
							Handles.CircleCap
						)
					);

				Vector2 pos2d = pos;
				rageSpline.SetGradientScaleInv(1f / (pos2d - rageSpline.GetGradientOffset()).magnitude);
				
				Vector3 dir = (pos - middle).normalized;
				rageSpline.SetGradientAngleDeg(rageSpline.Vector2Angle_CCW(dir));
			}

			if (rageSpline.showOtherGizmos && rageSpline.GetFill() != RageSpline.Fill.None && rageSpline.GetTexturing1() == RageSpline.UVMapping.Fill)
			{
				Handles.color = Color.magenta;
				rageSpline.SetTextureOffset(
					rageSpline.transform.InverseTransformPoint(
					Handles.FreeMoveHandle(
						rageSpline.transform.TransformPoint(rageSpline.GetTextureOffset()),
						Quaternion.identity,
						HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(rageSpline.GetTextureOffset())) * 0.2f,
						Vector3.zero,
						Handles.CircleCap
					))
				);

				Vector3 up = new Vector3(0f, 1f, 0f);
				Vector3 point = rageSpline.RotatePoint2D_CCW(up, rageSpline.GetTextureAngleDeg() * Mathf.Deg2Rad);
				Vector3 middle = rageSpline.GetTextureOffset();

				Vector3 pos =
					rageSpline.transform.InverseTransformPoint(
						Handles.FreeMoveHandle(
							rageSpline.transform.TransformPoint(middle + point * (1f / rageSpline.GetTextureScaleInv())),
							Quaternion.identity,
							HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(point)) * 0.1f,
							Vector3.zero,
							Handles.CircleCap
						)
					);

				Vector2 pos2d = pos;
				rageSpline.SetTextureScaleInv(1f/(pos2d - rageSpline.GetTextureOffset()).magnitude);
				Vector3 dir = (pos - middle).normalized;
				rageSpline.SetTextureAngleDeg(rageSpline.Vector2Angle_CCW(dir));
			}

			if (rageSpline.showOtherGizmos && rageSpline.GetFill() != RageSpline.Fill.None && rageSpline.GetTexturing2() == RageSpline.UVMapping.Fill)
			{

				Handles.color = Color.magenta;
				rageSpline.SetTextureOffset2(
					rageSpline.transform.InverseTransformPoint(
					Handles.FreeMoveHandle(
						rageSpline.transform.TransformPoint(rageSpline.GetTextureOffset2()),
						Quaternion.identity,
						HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(rageSpline.GetTextureOffset2())) * 0.2f,
						Vector3.zero,
						Handles.CircleCap
						)
					)
				);

				Vector3 up = new Vector3(0f, 1f, 0f);
				Vector3 point = rageSpline.RotatePoint2D_CCW(up, rageSpline.GetTextureAngle2Deg() * Mathf.Deg2Rad);
				Vector3 middle = rageSpline.GetTextureOffset2();

				Vector3 pos =
					rageSpline.transform.InverseTransformPoint(
						Handles.FreeMoveHandle(
							rageSpline.transform.TransformPoint(middle + point * (1f / rageSpline.GetTextureScale2Inv())),
							Quaternion.identity,
							HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(point)) * 0.1f,
							Vector3.zero,
							Handles.CircleCap
						)
					);

				Vector2 pos2d = pos;
				rageSpline.SetTextureScale2Inv(1f / (pos2d - rageSpline.GetTextureOffset2()).magnitude);
				Vector3 dir = (pos - middle).normalized;
				rageSpline.SetTextureAngle2Deg(rageSpline.Vector2Angle_CCW(dir));
			}


			if (rageSpline.showOtherGizmos && rageSpline.GetEmboss() != RageSpline.Emboss.None)
			{

				Handles.color = Color.blue;

				Vector3 up = new Vector3(0f, 1f, 0f);
				Vector3 point = rageSpline.RotatePoint2D_CCW(up, rageSpline.GetEmbossAngleDeg() * Mathf.Deg2Rad);
				Vector3 middle = rageSpline.GetMiddle();

				Vector3 pos =
					rageSpline.transform.InverseTransformPoint(
						Handles.FreeMoveHandle(
							rageSpline.transform.TransformPoint(middle + point * (rageSpline.GetEmbossSize() * 4f)),
							Quaternion.identity,
							HandleUtility.GetHandleSize(rageSpline.transform.TransformPoint(point)) * 0.2f,
							Vector3.zero,
							Handles.ConeCap
						)
					);

				Vector2 pos2d = pos - middle;
				rageSpline.SetEmbossSize(pos2d.magnitude * 0.25f);
				Vector2 dir = pos2d.normalized;
				rageSpline.SetEmbossAngleDeg(rageSpline.Vector2Angle_CCW(dir));
			}

			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.I)
				{
					Color oldOutlineColor1 = rageSpline.GetOutlineColor1();
					Color oldOutlineColor2 = rageSpline.GetOutlineColor2();
					rageSpline.SetOutlineColor1(rageSpline.GetFillColor1());
					if (rageSpline.GetFill() == RageSpline.Fill.Gradient)
					{
						rageSpline.SetOutlineColor2(rageSpline.GetFillColor2());
					}
					rageSpline.SetFillColor1(oldOutlineColor1);
					if (rageSpline.GetOutlineGradient() != RageSpline.OutlineGradient.None)
					{
						rageSpline.SetFillColor2(oldOutlineColor2);
					}
					
					GUI.changed = true;
					EditorUtility.SetDirty(target);
					Event.current.Use();
				}
				if (Event.current.keyCode == KeyCode.O)
				{
					rageSpline.SetOutlineColor1(Color.black);
					rageSpline.SetOutlineColor2(Color.black);
					rageSpline.SetFillColor1(Color.white);
					rageSpline.SetFillColor2(Color.white);
					rageSpline.SetOutlineGradient(RageSpline.OutlineGradient.None);
					rageSpline.SetFill(RageSpline.Fill.Solid);
					GUI.changed = true;
					EditorUtility.SetDirty(target);
					Event.current.Use();
				}
			}

			if (rageSpline.showSplineGizmos)
			{
				if (Event.current.type == EventType.mouseDown)
				{
                    if (Event.current.control || Event.current.clickCount > 1)
                    {
                        Vector3 localPosition = rageSpline.transform.InverseTransformPoint(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint((rageSpline.transform.position - Camera.current.transform.position).magnitude));
                        localPosition.z = rageSpline.transform.position.z;
                        float nearest = rageSpline.GetNearestSplinePosition(localPosition, 1000);
                        rageSpline.AddPoint(nearest);
                        rageSpline.RefreshMeshInEditor(true, true, true);
                        Event.current.Use();
                        GUI.changed = true;
                    }
                    else
                    {
                        Vector3 localPosition = rageSpline.transform.InverseTransformPoint(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint((rageSpline.transform.position - Camera.current.transform.position).magnitude));

                        int nearest = rageSpline.GetNearestPointIndex(localPosition);
                        if (Event.current.shift)
                        {
                            SelectPoint(nearest);
                        }
                        else
                        {
                            selectedControlPoints = new int[0];
                            SelectPoint(nearest);
                        }
                    
                    }
				}

				if (Event.current.type == EventType.KeyDown)
				{
                    
					if (Event.current.keyCode == KeyCode.N && selectedControlPoints.Length>0)
					{
                        for(int i=0; i<selectedControlPoints.Length; i++) {
						    if (rageSpline.GetNatural(selectedControlPoints[i]))
						    {
							    rageSpline.SetNatural(selectedControlPoints[i], false);
						    }
						    else
						    {
							    rageSpline.SetNatural(selectedControlPoints[i], true);
						    }
                        }
						GUI.changed = true;
						EditorUtility.SetDirty(target);
						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.L && selectedControlPoints.Length > 0)
					{
						for(int i=0; i<selectedControlPoints.Length; i++) {
						    rageSpline.SetOutlineWidthMultiplier(selectedControlPoints[i], Mathf.Max(rageSpline.GetOutlineWidthMultiplier(selectedControlPoints[i])*1.15f, 0.01f));
						}
                        GUI.changed = true;
						EditorUtility.SetDirty(target);
						Event.current.Use();  
                        
					}
					else if (Event.current.keyCode == KeyCode.K && selectedControlPoints.Length > 0)
					{
						for(int i=0; i<selectedControlPoints.Length; i++) {
                            rageSpline.SetOutlineWidthMultiplier(selectedControlPoints[i], Mathf.Max(rageSpline.GetOutlineWidthMultiplier(selectedControlPoints[i])*0.85f, 0.01f));
						}
                        GUI.changed = true;
						EditorUtility.SetDirty(target);
						Event.current.Use();
					}
                    else if (Event.current.keyCode == KeyCode.L && selectedControlPoints.Length > 0 && Event.current.shift)
                    {
                        rageSpline.scaleHandles(1.1f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.K && selectedControlPoints.Length > 0 && Event.current.shift)
                    {
                        rageSpline.scaleHandles(0.9f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.L && selectedControlPoints.Length > 0 && (Event.current.control || Event.current.command) )
                    {
                        rageSpline.scalePoints(rageSpline.GetMiddle(), 1.1f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.K && selectedControlPoints.Length > 0 && (Event.current.control || Event.current.command) )
                    {
                        rageSpline.scalePoints(rageSpline.GetMiddle(), 0.9f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.L && selectedControlPoints.Length > 0)
                    {
                        rageSpline.scalePoints(rageSpline.GetMiddle(), 1.1f);
                        rageSpline.scaleHandles(1.1f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.K && selectedControlPoints.Length > 0)
                    {
                        rageSpline.scalePoints(rageSpline.GetMiddle(), 0.9f);
                        rageSpline.scaleHandles(0.9f);
                        GUI.changed = true;
                        EditorUtility.SetDirty(target);
                        Event.current.Use();
                    }
            
					else if ((Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) && selectedControlPoints.Length > 0)
					{
                        for(int i=0; i<selectedControlPoints.Length; i++) {
						    rageSpline.RemovePoint(selectedControlPoints[i]);
                        }
                        selectedControlPoints = new int[0];
                        rageSpline.RefreshMeshInEditor(true, true, true);
						GUI.changed = true;
						Event.current.Use();
					}
                    else if ((Event.current.keyCode == KeyCode.C))
                    {
                        rageSpline.setPivotCenter();
                    }
					
				}
									
				Handles.color = Color.white;

				for (int s = 0; s < rageSpline.GetPointCount(); s++)
				{
					
					Handles.DrawCapFunction capFunc;
					float capSizeMultiplier = 1f;
					if (rageSpline.GetNatural(s))
					{
						capFunc = Handles.SphereCap;
						capSizeMultiplier = 2.5f;
					}
					else
					{
						capFunc = Handles.RectangleCap;
						capSizeMultiplier = 1f;
					}
					
					bool guiChanged = GUI.changed;
                    Vector3 oldPosition = rageSpline.GetPosition(s);

                    Handles.color = Color.white;
                    if (selectedControlPoints.Length > 1)
                    {
                        for (int i = 0; i < selectedControlPoints.Length; i++)
                        {
                            if (s == selectedControlPoints[i])
                            {
                                Handles.color = Color.yellow;
                            }
                        }
                    }

					rageSpline.SetPointWorldSpace(s, 
						Handles.FreeMoveHandle(
							rageSpline.GetPositionWorldSpace(s),
							Quaternion.identity,
							HandleUtility.GetHandleSize(rageSpline.GetPositionWorldSpace(s)) * 0.075f * capSizeMultiplier,
							Vector3.zero,
							capFunc
						)
					);
                    if(!guiChanged && GUI.changed) {
                        Vector3 change = oldPosition - rageSpline.GetPosition(s);
                        //selectedControlPoints = s;
                        for (int i = 0; i < selectedControlPoints.Length; i++)
                        {
                            if (selectedControlPoints[i] != s)
                            {
                                rageSpline.SetPoint(selectedControlPoints[i], rageSpline.GetPosition(selectedControlPoints[i]) - change);
                            }
                            if (rageSpline.snapToGrid && rageSpline.showCoordinates != RageSpline.ShowCoordinates.None)
                            {
                                if (rageSpline.showCoordinates == RageSpline.ShowCoordinates.Local)
                                {
                                    rageSpline.SetPoint(s, SnapToGrid(rageSpline.GetPosition(s), rageSpline.gridSize));
                                }
                                else
                                {
                                    rageSpline.SetPointWorldSpace(s, SnapToGrid(rageSpline.GetPositionWorldSpace(s), rageSpline.gridSize));
                                }
                            }
                        }
					}

                    Handles.color = Color.white;
                    if (!rageSpline.hideHandles)
                    {
                        guiChanged = GUI.changed;
                        rageSpline.SetInControlPositionWorldSpace(s,
                            Handles.FreeMoveHandle(
                                rageSpline.GetInControlPositionWorldSpace(s),
                                Quaternion.identity,
                                HandleUtility.GetHandleSize(rageSpline.GetPositionWorldSpace(s)) * 0.1f,
                                Vector3.zero,
                                Handles.SphereCap
                            )
                        );
                        if (!guiChanged && GUI.changed)
                        {
                            //selectedControlPoints = s;
                            if (rageSpline.snapHandlesToGrid && rageSpline.showCoordinates != RageSpline.ShowCoordinates.None)
                            {
                                if (rageSpline.showCoordinates == RageSpline.ShowCoordinates.Local)
                                {
                                    rageSpline.SetInControlPosition(s, SnapToGrid(rageSpline.GetInControlPosition(s), rageSpline.gridSize));
                                }
                                else
                                {
                                    rageSpline.SetInControlPositionWorldSpace(s, SnapToGrid(rageSpline.GetInControlPositionWorldSpace(s), rageSpline.gridSize));
                                }
                            }
                        }

                        guiChanged = GUI.changed;
                        rageSpline.SetOutControlPositionWorldSpace(s,
                            Handles.FreeMoveHandle(
                                rageSpline.GetOutControlPositionWorldSpace(s),
                                Quaternion.identity,
                                HandleUtility.GetHandleSize(rageSpline.GetPositionWorldSpace(s)) * 0.1f,
                                Vector3.zero,
                                Handles.SphereCap
                            )
                        );
                    }
					if(!guiChanged && GUI.changed) {
						//selectedControlPoints = s;
						if(rageSpline.snapHandlesToGrid && rageSpline.showCoordinates != RageSpline.ShowCoordinates.None) {
							if(rageSpline.showCoordinates == RageSpline.ShowCoordinates.Local) {
								rageSpline.SetOutControlPosition(s, SnapToGrid(rageSpline.GetOutControlPosition(s), rageSpline.gridSize));
							} else {
								rageSpline.SetOutControlPositionWorldSpace(s, SnapToGrid(rageSpline.GetOutControlPositionWorldSpace(s), rageSpline.gridSize));
							}
						}	
					}
					
					
				}
	
			}
			
			if (Event.current.type == EventType.mouseDown)
			{
				Undo.CreateSnapshot();
				Undo.RegisterSnapshot();
			}

            if (mouseUp || (keyUp && keyCode != KeyCode.Delete) || mouseDrag)
			{
                if (mouseUp || keyUp)
                {
                    rageSpline.RefreshMeshInEditor(true, true, true);
                }
                else
                {
                    rageSpline.RefreshMeshInEditor(false, true, true);
                }
				EditorUtility.SetDirty(target);
			}

		}
    }

    private void SelectPoint(int index) {
        bool exists = false;
        for(int i=0; i<selectedControlPoints.Length; i++) {
            if(selectedControlPoints[i]==index) {
                exists = true;
            }
        }

        // do nothing is exists, add if not
        if(exists) {
            //noop
        } 
        else 
        {
            int[] tmp = new int[selectedControlPoints.Length+1];
            selectedControlPoints.CopyTo(tmp, 0);
            tmp[tmp.Length-1] = index;
            selectedControlPoints = tmp;
        }
    }
    
    public float SnapToGrid(float val, float gridsize) {
		if(mod(val, gridsize) < gridsize*0.5f) {
			return val - mod(val, gridsize);
		} else {
			return val + (gridsize - mod(val, gridsize));
		}
	}
	
	public Vector3 SnapToGrid(Vector3 val, float gridsize) {
        if (selectedControlPoints.Length == 1)
        {
            return new Vector3(SnapToGrid(val.x, gridsize), SnapToGrid(val.y, gridsize));
        }
        else
        {
            return new Vector3(val.x, val.y);
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
}