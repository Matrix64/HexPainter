using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MatrixToolBox.Editor
{
    public class HexPainter : EditorWindow
    {
        [Range(0f, 2f)]
        public float hexSpacing = 1f;
        [Range(0f, 360f)]
        public float rotationAngle = 0f;
        private bool isDrawing = false; // 是否正在绘制
        
        private List<GameObject> brushPrefabs = new List<GameObject>(); // 存储所有的笔刷预制体
        private GameObject selectedPrefab; // 当前选中的预制体
        private Vector2 scrollPosition; // 滚动视图的位置
        private int replaceMode = 0; // 当前的替换模式
        
        private bool showNotification = true;

        [MenuItem("ArtTools/Hex Painter")]
        public static void ShowWindow()
        {
            GetWindow<HexPainter>("Hex Painter");
        }

        private void OnGUI()
        {
            // 创建一个可折叠的公告区域
            showNotification = EditorGUILayout.Foldout(showNotification, "Paint Info");
            if (showNotification)
            {
                EditorGUILayout.LabelField("1. 点击鼠标左键：在当前位置创建一个新的hex对象");
                EditorGUILayout.LabelField("2. 按住Shift键并点击鼠标左键：删除绘制的hex对象");
                EditorGUILayout.LabelField("3. 按住Control键并点击鼠标左键：将当前选中的预制体替换为已存在的hex对象的预制体资源");
            }
            GUILayout.Label("Paint Settings", EditorStyles.boldLabel);
            // 添加一个单选按钮组用于切换替换模式
            replaceMode = GUILayout.SelectionGrid(replaceMode, new string[] { "Replace Mode", "Recreate Mode" }, 2);
            hexSpacing = EditorGUILayout.FloatField("Spacing", hexSpacing);
            rotationAngle = EditorGUILayout.FloatField("Rotation Angle", rotationAngle);
            if (LayerMask.NameToLayer("HexTile") == -1)
            {
                EditorGUILayout.HelpBox("The layer 'HexTile' does not exist. Please create a new layer named 'HexTile' in the Layers settings before drawing.", MessageType.Error);
            }
            
            if (GUILayout.Button(isDrawing ? "Stop Drawing" : "Start Drawing"))
            {
                isDrawing = !isDrawing;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < brushPrefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");

                // 显示预制体的缩略图
                Texture2D previewImage = AssetPreview.GetAssetPreview(brushPrefabs[i]);
                GUIStyle style = new GUIStyle(GUI.skin.button);
                if (brushPrefabs[i] == selectedPrefab)
                {
                    style.normal.background = MakeTex(2, 2, new Color(0f, 0f, 1f, 0.5f));
                }
                if (GUILayout.Button(previewImage, style, GUILayout.Width(50), GUILayout.Height(50)))
                {
                    // 点击缩略图时，将此预制体设置为当前选中的预制体
                    selectedPrefab = brushPrefabs[i];
                }

                // 显示预制体的名称
                GUILayout.Label(brushPrefabs[i].name);

                // 添加一个按钮用于删除此预制体
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (brushPrefabs[i] == selectedPrefab) selectedPrefab = null;
                    brushPrefabs.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            
            // 添加一个区域用于拖拽预制体到窗口
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop new brushes here");

            // 处理拖拽操作
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            GameObject go = draggedObject as GameObject;
                            if (go != null && !brushPrefabs.Contains(go))
                            {
                                // 如果拖入的预制体不是asset prefab，弹出一个错误提示框
                                if (!PrefabUtility.IsPartOfPrefabAsset(go))
                                {
                                    EditorUtility.DisplayDialog("Error", "Only asset prefabs can be used as brushes.", "OK");
                                }
                                else
                                {
                                    brushPrefabs.Add(go);
                                }
                            }
                        }
                    }
                    break;
            }
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private const string GroupNameSuffix = "_HexGroup";
        void OnSceneGUI(SceneView obj)
        {
            if(selectedPrefab==null) return;
            if (LayerMask.NameToLayer("HexTile") == -1) return;
            if (!isDrawing) return;

            GameObject parentObject = null;
            string groupName = selectedPrefab.name + GroupNameSuffix;
            
            GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in sceneRoots)
            {
                if (root.name == groupName)
                {
                    parentObject = root;
                    break;
                }
            }

            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float distanceToDrawPlane = 0;
            new Plane(Vector3.up, 0).Raycast(ray, out distanceToDrawPlane);
            Vector3 mousePositionInWorld = ray.GetPoint(distanceToDrawPlane);

            // 计算最近的六边形中心点
            Vector3 nearestHexCenter = CalculateNearestHexCenter(mousePositionInWorld, hexSpacing, rotationAngle);
            
            // 绘制x轴和z轴
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(-1000, 0, 0), new Vector3(1000, 0, 0));
            Handles.color = Color.blue;
            Handles.DrawLine(new Vector3(0, 0, -1000), new Vector3(0, 0, 1000));
            

            // 在最近的六边形中心点位置显示一个六边形提示框
            Handles.color = Color.green;
            Handles.DrawWireDisc(nearestHexCenter, Vector3.up, hexSpacing);

            // 点击鼠标左键在当前位置创建一个新的hex对象
            // 按住鼠标左键进行持续绘制
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !(e.alt || e.shift || e.control))
            {
                if (parentObject == null)
                {
                    parentObject = new GameObject(groupName);
                }
                
                Collider[] hitColliders = Physics.OverlapSphere(nearestHexCenter, hexSpacing, LayerMask.GetMask("HexTile"));
                if (hitColliders.Length>0)
                {
                    Transform existHexTransform = null;
                    foreach (var hitCollider in hitColliders)
                    {
                        if(hitCollider==null) continue;
                        existHexTransform = hitCollider.transform;
                        while (existHexTransform.parent != null &&
                               !existHexTransform.parent.gameObject.name.Contains(GroupNameSuffix))
                        {
                            existHexTransform = existHexTransform.parent;
                        }
                    }

                    if (existHexTransform != null)
                    {
                        if (existHexTransform.gameObject.name != selectedPrefab.name)
                        {
                            //执行prefab替换/重建
                            switch (replaceMode)
                            {
                                case 0:
                                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(existHexTransform.gameObject, selectedPrefab, InteractionMode.AutomatedAction);
                                    existHexTransform.parent = parentObject.transform;
                                    break;
                                case 1:
                                    DestroyImmediate(existHexTransform.gameObject);
                                    CreateHexTile(parentObject, nearestHexCenter);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    CreateHexTile(parentObject, nearestHexCenter);
                }
            }

            // 按住Shift键并点击鼠标左键删除绘制的hex对象
            if (e.shift && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Transform existHexTransform = hit.collider.transform;
                    while (existHexTransform.parent != null &&
                           !existHexTransform.parent.gameObject.name.Contains(GroupNameSuffix))
                    {
                        existHexTransform = existHexTransform.parent;
                    }

                    if (existHexTransform.parent != null && existHexTransform.parent.gameObject.name.Contains(GroupNameSuffix))
                    {
                        DestroyImmediate(existHexTransform.gameObject);
                    }
                }
            }

            // 按住Control键并点击鼠标左键将当前选中的预制体替换为已存在的hex对象的预制体资源
            if (e.control && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Transform existHexTransform = hit.collider.transform;
                    while (existHexTransform.parent != null &&
                           !existHexTransform.parent.gameObject.name.Contains(GroupNameSuffix))
                    {
                        existHexTransform = existHexTransform.parent;
                    }

                    if (existHexTransform.parent != null && existHexTransform.parent.gameObject.name.Contains(GroupNameSuffix))
                    {
                        GameObject existPrefab = PrefabUtility.GetCorrespondingObjectFromSource(existHexTransform.gameObject);
                        if (!brushPrefabs.Contains(existPrefab))
                        {
                            
                            brushPrefabs.Add(existPrefab);
                        }
                        selectedPrefab = brushPrefabs[brushPrefabs.IndexOf(existPrefab)];
                    }
                }
            }

            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }
        }

        private void CreateHexTile(GameObject parentObject, Vector3 nearestHexCenter)
        {
            GameObject hex = (GameObject) PrefabUtility.InstantiatePrefab(selectedPrefab, parentObject.transform);
            hex.transform.position = nearestHexCenter;
            hex.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
            SetLayerRecursively(hex, LayerMask.NameToLayer("HexTile"));
        }
        
        void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null)
            {
                return;
            }

            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (child == null)
                {
                    continue;
                }

                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        Vector3 CalculateNearestHexCenter(Vector3 point, float hexRadius, float hexRotate)
        {
            // 创建旋转矩阵
            float radian = (hexRotate+30) * Mathf.Deg2Rad;
            float cosTheta = Mathf.Cos(radian);
            float sinTheta = Mathf.Sin(radian);
            Matrix4x4 rotationMatrix = new Matrix4x4(
                new Vector4(cosTheta, 0, sinTheta, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(-sinTheta, 0, cosTheta, 0),
                new Vector4(0, 0, 0, 1)
            );
            
            // 使用旋转矩阵旋转鼠标的位置
            point = rotationMatrix.inverse.MultiplyPoint3x4(point);
            
            // 计算六边形的宽度和高度
            float hexWidth = 2f * hexRadius;
            float hexHeight = Mathf.Sqrt(3f) * hexRadius;
            // 计算六边形的行和列
            int col = Mathf.RoundToInt(point.x / hexWidth);
            int row = Mathf.RoundToInt(point.z / hexHeight);

            // 计算六边形的中心点
            Vector3 hexCenter = new Vector3(col * hexWidth, 0, row * hexHeight);

            // 如果行是奇数，则向右移动半个六边形的宽度
            if (row % 2 != 0)
            {
                hexCenter.x += hexWidth / 2f;
            }

            // 使用旋转矩阵旋转六边形的中心点
            hexCenter = rotationMatrix.MultiplyPoint3x4(hexCenter);

            return hexCenter;
        }
    }
}