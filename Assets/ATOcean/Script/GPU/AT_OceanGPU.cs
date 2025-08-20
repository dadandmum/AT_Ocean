using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ATOcean
{

    [System.Serializable]
    public class RenderCascades
    {
        public Material material;
        public int lod;
        public float lengthScale;
        public int renderResolution;
    };

    [ExecuteInEditMode]
    public class AT_OceanGPU : MonoBehaviour
    {
        [BoxGroup("AT_Ocean")]
        [BoxGroup("AT_Ocean/Mesh", CenterLabel = true)]
        [OnValueChanged("ValidateRes")]
        [InfoBox("Mesh Resolution of LOD 0, all mesh clips share the same resolution, but each size is 2x of last level"
            ,InfoMessageType.Info)]
        [OnValueChanged("Setup")]
        public int resolution = 16;

        [BoxGroup("AT_Ocean/Mesh")]
        [InfoBox("The length of plane of LOD 0 in Unity unit, the size of LOD 1 is 2x, LOD 2 is 4x , etc. ")]
        public float domainSize = 10.0f;

        [BoxGroup("AT_Ocean/Mesh")]
        [Range(1, 10)]
        [InfoBox("The total level of mesh")]
        public int clipLevels = 8;

        [BoxGroup("AT_Ocean/Mesh")]
        [Title("Real Time Variables")]
        [ReadOnly]
        public List<SubMeshRef> meshClips;

        [BoxGroup("AT_Ocean/Mesh")]
        [ReadOnly]
        public SubMeshRef centerMesh;

        [System.Serializable]
        public class SubMeshRef
        {
            public int lod;
            public Mesh mesh;
            public MeshFilter filter;
            public GameObject gameObject;
            public MeshRenderer renderer;

            public SubMeshRef(int lod, Mesh mesh, MeshFilter filter, GameObject gameObject, MeshRenderer renderer)
            {
                this.lod = lod;
                this.mesh = mesh;
                this.filter = filter;
                this.gameObject = gameObject;
                this.renderer = renderer;

            }

            // 销毁
             public void Destroy()
            {
                if (gameObject != null)
                {
                    DestroyImmediate(gameObject);
                }
            }

        }

        [BoxGroup("AT_Ocean/Render", CenterLabel = true)]
        [InfoBox("Resolution of render RT")]
        public int renderResolution = 1024;


        [BoxGroup("AT_Ocean/Render")]
        [InfoBox("Suggestion: Do not change the cascades level",InfoMessageType.Warning)]
        [Range(1, 4)]
        public int cascadeLevel = 3;

        [BoxGroup("AT_Ocean/Render")]
        public List<RenderCascades> renderCascades;

        [BoxGroup("AT_Ocean")]
        [BoxGroup("AT_Ocean/Render")]
        public Material material;

        [BoxGroup("AT_Ocean/Render")]
        [ReadOnly]
        public float timer;


        [BoxGroup("AT_Ocean/Debug")]
        public bool visualizeRT;

        [BoxGroup("AT_Ocean/Visual")]
        [InfoBox("Gerstner波可视化，用于调试")]

        public ATO_Visual visual;


        [BoxGroup("AT_Ocean/Debug",Order = 1000)]
        public bool syncMaterial;
        [BoxGroup("AT_Ocean/Debug")]
        public bool showLODs;
        [BoxGroup("AT_Ocean/Debug")]
        public bool simulateInEditor=true;

        public void ValidateRes()
        {
            int quarRes = resolution / 4;
            quarRes = Mathf.Max(1, quarRes);

            resolution = quarRes * 4;
        }

        #region Init
        [Button("Setup All" , ButtonSizes.Large)]
        [GUIColor(0.8f,0.2f,0.2f)]
        public void Setup()
        {
            InitMesh();

            InitBasicCascadesDefinition();

            InitRender();

            InitVisual();
        }

        public void OnEnable()
        {
            InitMesh();

            if (renderCascades == null || renderCascades.Count == 0)
            {
                InitBasicCascadesDefinition();
            }

            InitRender();

            InitVisual();
        }

        public void OnDisable()
        {
            Dipose();
        }

        public void Update()
        {
            if (simulateInEditor || Application.isPlaying)
            {
                timer += Time.deltaTime;
                UpdateRender(timer, Time.deltaTime);
                
                UpdateVisual();
            }
        }

        virtual public void Dipose()
        {
            CleanVisual();

        }

        

        #region Render 
        public int ClipLevelToCascadesLevel(int _clipLevel)
        {
            int level = Mathf.FloorToInt(Mathf.Lerp(0, cascadeLevel, 1f * _clipLevel / clipLevels));

            return level;
        }

        public void SetupMaterialLODVariables()
        {
            for (int i = 0; i < renderCascades.Count; i++)
            {
                var mat = renderCascades[i].material;
                mat.SetFloat(LENGTH_SCALE_0_PROP, renderCascades[0].lengthScale);

                if (renderCascades.Count > 1)
                    mat.SetFloat(LENGTH_SCALE_1_PROP, renderCascades[1].lengthScale);
                if (renderCascades.Count > 2)
                    mat.SetFloat(LENGTH_SCALE_2_PROP, renderCascades[2].lengthScale);
            }

        }

        virtual public void InitRender()
        {
            for (int i = 0; i < renderCascades.Count; i++)
            {
                renderCascades[i].material = new Material(material);

                renderCascades[i].material.name = "ATOceanMaterial_Cascades_" + renderCascades[i].lod;

            }

            SetupMaterialLODVariables();
            SetupMaterailLODKeys();

            AssignMehsMaterialByCascadeLevel();

            Debug.Log("Finish Init Render cascades Count : " + renderCascades.Count);
        }

        public void AssignMehsMaterialByCascadeLevel()
        {
            centerMesh.renderer.material = renderCascades[0].material;
            for (int i = 0; i < meshClips.Count; ++i)
            {
                meshClips[i].renderer.material = renderCascades[ClipLevelToCascadesLevel(i)].material;
            }

        }

        [BoxGroup("AT_Ocean/Render")]
        [GUIColor(0.8f, 0.2f, 0.5f)]
        [Button("SetupRender",ButtonSizes.Large)]
        virtual public void InitBasicCascadesDefinition()
        {
            List<int> CascadeLevelToClipLevel = new List<int>();

            int cascades = 0;
            for (int i = 0; i < clipLevels; i++)
            {
                var tempCas = ClipLevelToCascadesLevel(i);
                if (tempCas > cascades)
                {
                    CascadeLevelToClipLevel.Add(i - 1);
                    cascades = tempCas;
                }
            }

            if (CascadeLevelToClipLevel.Count < cascadeLevel)
            {
                CascadeLevelToClipLevel.Add(clipLevels - 1);
            }


            renderCascades = new List<RenderCascades>();

            for (int i = 0; i < cascadeLevel; i++)
            {
                var rc =
                    new RenderCascades()
                    {
                        material = null,
                        lod = i,
                        lengthScale = domainSize * Mathf.Pow(2.0f, CascadeLevelToClipLevel[i] + 1),
                        renderResolution = renderResolution,

                    };
                renderCascades.Add(rc);

            }
        }

        public void SetupMaterailLODKeys()
        {
            renderCascades[0].material.EnableKeyword("LOD0");
            renderCascades[0].material.EnableKeyword("LOD1");
            renderCascades[0].material.EnableKeyword("LOD2");

            if (renderCascades.Count > 1)
            {
                renderCascades[1].material.DisableKeyword("LOD0");
                renderCascades[1].material.EnableKeyword("LOD1");
                renderCascades[1].material.EnableKeyword("LOD2");
            }
            if (renderCascades.Count > 2)
            {
                renderCascades[2].material.DisableKeyword("LOD0");
                renderCascades[2].material.DisableKeyword("LOD1");
                renderCascades[2].material.EnableKeyword("LOD2");
            }

        }




        // 常用颜色常量数组
        UnityEngine.Color[] commonColors = new UnityEngine.Color[]
        {
                UnityEngine.Color.red,        // 红色
                UnityEngine.Color.green,      // 绿色
                UnityEngine.Color.blue,       // 蓝色
                UnityEngine.Color.white,      // 白色
                UnityEngine.Color.black,      // 黑色
                UnityEngine.Color.yellow,     // 黄色
                UnityEngine.Color.cyan,       // 青色
                UnityEngine.Color.magenta,    // 洋红色
                UnityEngine.Color.gray,       // 灰色 (0.5, 0.5, 0.5)
                UnityEngine.Color.grey,       // 灰色（英式拼写，同 gray）
                UnityEngine.Color.clear,      // 透明色 (0,0,0,0)
        };


        virtual public void UpdateRender(float t , float dt)
        {
            if ( syncMaterial)
            {
                for (int i = 0; i < renderCascades.Count; ++i)
                {
                    renderCascades[i].material.CopyPropertiesFromMaterial(material);
                }

                SetupMaterialLODVariables();
                SetupMaterailLODKeys();
            }
            
            if (showLODs)
            {
                for (int i = 0; i < renderCascades.Count; ++i)
                {
                    renderCascades[i].material.SetColor("_TipsColor", commonColors[i]);
                }
            }
            else
            {
                for (int i = 0; i < renderCascades.Count; ++i)
                {
                    renderCascades[i].material.SetColor("_TipsColor", new Color(0,0,0,0));
                }

            }

        }
        #endregion


        #region Mesh

        [BoxGroup("AT_Ocean/Mesh")]
        [Button("Setup Mesh", ButtonSizes.Large)]
        [GUIColor(0.2f, 1.0f, 0.5f)]
        virtual public void InitMesh()
        {
            CleanAllMesh();

            float unitScale = domainSize / resolution;
            centerMesh = CreateSubMesh("Center" , 0 , CreatePlaneMesh(resolution, resolution, domainSize / resolution), material);

            for (int i = 0; i < clipLevels; i++)
            {
                unitScale *= 2f;
                var clip = CreateSubMesh("Clip_" + i, i + 1, CreateRingMesh(resolution , unitScale), material);
                meshClips.Add(clip);
            }

            Debug.Log("Finish Init Mesh (Total Count:" + meshClips.Count + ")");

        }

        public void CleanAllMesh()
        {
            centerMesh.Destroy();

            foreach (var clip in meshClips)
            {
                clip.Destroy();
            }

            meshClips.Clear();
        }
        

        /// <summary>
        /// 创建一个子网格引用对象，包含一个新的游戏对象及其相关组件
        /// </summary>
        /// <param name="name">新游戏对象的名称</param>
        /// <param name="lod">该子网格的LOD级别</param>
        /// <param name="mesh">要赋值给网格过滤器的网格数据</param>
        /// <param name="material">要使用的材质（当前方法未使用，可后续扩展）</param>
        /// <returns>返回一个包含新创建游戏对象及其组件的子网格引用对象</returns>
        public SubMeshRef CreateSubMesh( string name , int lod , Mesh mesh , Material material )
        { 
            // 创建一个新的游戏对象
            var go = new GameObject();
            // 设置游戏对象的名称
            go.name = name;
            // 将新游戏对象设置为当前对象的子对象
            go.transform.SetParent(transform);
            // 将新游戏对象的本地位置设置为原点
            go.transform.localPosition = new Vector3(0, 0, 0);

            // 为游戏对象添加网格过滤器组件
            var filter = go.AddComponent<MeshFilter>();
            // 将传入的网格数据赋值给网格过滤器
            filter.mesh = mesh;
            // 为游戏对象添加网格渲染器组件
            var renderer = go.AddComponent<MeshRenderer>();

            // 创建并返回一个新的子网格引用对象
            return new SubMeshRef(lod , mesh, filter, go, renderer);

        }

        /// <summary>
        /// 创建一个环状的网格。
        /// 整体大小为 (resolution  * unitScale) * (resolution  * unitScale)。
        /// 中心有一个大小为 (resolution * unitScale) * (resolution * unitScale) 的空洞。
        /// 使用 CombineMeshes 技术拼接四个矩形条带实现。
        /// </summary>
        /// <param name="resolution">网格每边的点数（用于计算整体和空洞尺寸）</param>
        /// <param name="unitScale">网格的基本单位长度</param>
        /// <returns>组合后的环状网格</returns>
        Mesh CreateRingMesh(int resolution, float unitScale)
        {
            if ( resolution % 4 != 0 )
            {
                Debug.LogError("Resolution of Ring Mesh should be power of 4 , now is " + resolution);
                return null;
            }
            // 1. 计算关键尺寸
            int totalRes = resolution;
            int holeRes = resolution / 2 ;
            int stripRes = (totalRes - holeRes) / 2;
            float totalSize = totalRes * unitScale; // 整个框架的外边长
            float holeSize = holeRes * unitScale;      // 中心空洞的边长
            float stripWidth = stripRes * unitScale; // 每个条带的宽度 (外框到空洞的距离)
            float offset = 0.05f;


            //Debug.Log($"CreateFrameMesh: resolution={resolution}, unitScale={unitScale}");
            //Debug.Log($"  Total Size: {totalSize}, Hole Size: {holeSize}, Strip Width: {stripWidth}");

            // 2. 创建 CombineInstance 列表来存储要合并的网格
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            // 3. 创建并定位四个条带 (Top, Bottom, Left, Right)

            // 3.1 顶部条带 (Top Strip)
            // 尺寸: 宽度 = totalSize, 高度 = stripWidth
            Mesh topMesh = CreatePlaneMesh(totalRes, stripRes, unitScale);
            CombineInstance topInstance = new CombineInstance();
            topInstance.mesh = topMesh;
            // 位置: Y=0, X=0, Z= 从空洞顶部边缘到框架顶部边缘的中心
            topInstance.transform = Matrix4x4.TRS(
                new Vector3(0, 0, (holeSize / 2) + (stripWidth / 2)),
                Quaternion.identity,
                new Vector3(1 + offset , 1, 1 + offset * 2.0f)
            );
            combineInstances.Add(topInstance);

            // 3.2 底部条带 (Bottom Strip)
            // 尺寸: 宽度 = totalSize, 高度 = stripWidth
            Mesh bottomMesh = CreatePlaneMesh(totalRes, stripRes, unitScale);
            CombineInstance bottomInstance = new CombineInstance();
            bottomInstance.mesh = bottomMesh;
            // 位置: Y=0, X=0, Z= 从框架底部边缘到空洞底部边缘的中心 (负方向)
            bottomInstance.transform = Matrix4x4.TRS(
                new Vector3(0, 0, -(holeSize / 2) - (stripWidth / 2)),
                Quaternion.identity,
                new Vector3(1 + offset, 1, 1 + offset * 2.0f)
            );
            combineInstances.Add(bottomInstance);

            // 3.3 左侧条带 (Left Strip)
            // 尺寸: 宽度 = stripWidth, 高度 = holeSize (因为中间空洞高度是 holeSize)
            Mesh leftMesh = CreatePlaneMesh(stripRes, holeRes,unitScale);
            CombineInstance leftInstance = new CombineInstance();
            leftInstance.mesh = leftMesh;
            // 位置: Y=0, X= 从框架左侧边缘到空洞左侧边缘的中心 (负方向), Z=0
            // 注意: CreatePlaneMesh 默认在XZ平面，所以直接用 stripWidth 创建的网格宽高都是 stripWidth。
            // 我们需要在Z方向上拉伸它到 holeSize 的长度。这通过缩放 transform 实现。
            leftInstance.transform = Matrix4x4.TRS(
                new Vector3(-(holeSize / 2) - (stripWidth / 2), 0, 0),
                Quaternion.identity,
                new Vector3(1 + offset , 1, 1) 
            );
            combineInstances.Add(leftInstance);

            // 3.4 右侧条带 (Right Strip)
            // 尺寸: 宽度 = stripWidth, 高度 = holeSize
            Mesh rightMesh = CreatePlaneMesh(stripRes, holeRes, unitScale);
            CombineInstance rightInstance = new CombineInstance();
            rightInstance.mesh = rightMesh;
            // 位置: Y=0, X= 从空洞右侧边缘到框架右侧边缘的中心, Z=0
            rightInstance.transform = Matrix4x4.TRS(
                new Vector3((holeSize / 2) + (stripWidth / 2), 0, 0),
                Quaternion.identity,
                new Vector3(1 + offset , 1, 1)
            );
            combineInstances.Add(rightInstance);

            // 4. 创建最终的组合网格
            Mesh combinedMesh = new Mesh();
            // 使用 combineInstances 构造最终网格
            // 第二个参数 'true' 表示我们希望合并后的网格共享材质（这里只有一个网格，所以无所谓）
            // 第三个参数 'true' 表示我们希望合并时考虑变换矩阵（位置、旋转、缩放）
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            // 5. 清理临时创建的网格以避免内存泄漏
            // 注意: 在 Unity 中，Mesh 资源需要手动销毁，尤其是在编辑器中频繁调用时。
            // 在运行时，如果这些网格没有被其他地方引用，GC 会处理，但显式销毁更安全。
            // **重要**: 只有在确认 CombineMeshes 已经复制了顶点数据后才能销毁。
            // CombineMeshes 通常会复制数据，所以可以安全销毁源网格。
            Object.DestroyImmediate(topMesh);
            Object.DestroyImmediate(bottomMesh);
            Object.DestroyImmediate(leftMesh);
            Object.DestroyImmediate(rightMesh);

            // 6. 可选: 重新计算法线和切线，确保光照正确
            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateTangents(); // 如果需要法线贴图等
            combinedMesh.RecalculateBounds();

            return combinedMesh;
        }

        /// <summary>
        /// 创建一个平面网格
        /// </summary>
        /// <param name="width">宽度正整数</param>
        /// <param name="height">高度正整数</param>
        /// <param name="unitScale">单位缩放</param>
        /// <param name="trianglesShift">三角形偏移</param>
        /// <returns>创建好的平面网格</returns>
        Mesh CreatePlaneMesh(int width, int height, float unitScale, int trianglesShift = 0)
        {
            // 创建一个新的网格对象并设置其名称
            Mesh mesh = new Mesh();
            mesh.name = "Plane";
            // 如果顶点数量超过 256 * 256，使用 UInt32 索引格式
            if ((width + 1) * (height + 1) >= 256 * 256)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            // 初始化顶点、三角形和法线数组
            Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
            int[] triangles = new int[width * height * 2 * 3];
            Vector3[] normals = new Vector3[(width + 1) * (height + 1)];

            // 计算平面的偏移量，使平面中心位于原点
            Vector3 offset = new Vector3(width, 0, height) * unitScale * (-0.5f);

            // 遍历所有顶点，设置顶点位置和法线
            for (int i = 0; i < height + 1; i++)
            {
                for (int j = 0; j < width + 1; j++)
                {
                    int x = j;
                    int z = i;

                    vertices[j + i * (width + 1)] = new Vector3(x, 0, z) * unitScale + offset;
                    normals[j + i * (width + 1)] = Vector3.up;
                }
            }

            // 用于记录当前三角形索引的位置
            int tris = 0;
            // 遍历所有四边形，生成三角形
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // 当前四边形的起始顶点索引
                    int k = j + i * (width + 1);
                    // 根据偏移量决定三角形的绘制顺序
                    if ((i + j + trianglesShift) % 2 == 0)
                    {
                        triangles[tris++] = k;
                        triangles[tris++] = k + width + 1;
                        triangles[tris++] = k + width + 2;

                        triangles[tris++] = k;
                        triangles[tris++] = k + width + 2;
                        triangles[tris++] = k + 1;
                    }
                    else
                    {
                        triangles[tris++] = k;
                        triangles[tris++] = k + width + 1;
                        triangles[tris++] = k + 1;

                        triangles[tris++] = k + 1;
                        triangles[tris++] = k + width + 1;
                        triangles[tris++] = k + width + 2;
                    }
                }
            }

            // 将生成的顶点、三角形和法线数据赋值给网格
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            // 重新计算网格的边界
            mesh.RecalculateBounds();

            return mesh;
        }
        #endregion


        #region Visual 


        public void UpdateVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    visualizeRT  = !visualizeRT;
                }


                if (visualizeRT)
                {
                    visual.Show();
                }
                else
                {
                    visual.Hide();
                }
            }

        }

        virtual public void InitVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
            {
                CleanVisual();
            }


        }

        public void CleanVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
                visual.Clear();

        }
        #endregion

        #endregion


        public static int LENGTH_SCALE_0_PROP = Shader.PropertyToID("_LengthScale0");
        public static int LENGTH_SCALE_1_PROP = Shader.PropertyToID("_LengthScale1");
        public static int LENGTH_SCALE_2_PROP = Shader.PropertyToID("_LengthScale2");



    }

}