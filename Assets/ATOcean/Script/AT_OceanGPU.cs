using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace ATOcean
{
    [ExecuteInEditMode]
    public class AT_OceanGPU : MonoBehaviour
    {
        [BoxGroup("AT_Ocean/Settings")]
        [OnValueChanged("ValidateRes")]
        [OnValueChanged("Setup")]
        public int resolution = 16;

        [BoxGroup("AT_Ocean/Settings")]
        public float domainSize = 10.0f;

        [BoxGroup("AT_Ocean/Settings")]
        [Range(1,10)]
        public int clipLevels = 8;

        [BoxGroup("AT_Ocean")]
        public Material material;

        [BoxGroup("AT_Ocean/Mesh")]
        [ReadOnly]
        public List<SubMeshRef> meshClips;

        [BoxGroup("AT_Ocean/Mesh")]
        public SubMeshRef centerMesh;

        [System.Serializable]
        public class SubMeshRef
        {
            public Mesh mesh;
            public MeshFilter filter;
            public GameObject gameObject;
            public MeshRenderer renderer;
            public Material material;

            public SubMeshRef(Mesh mesh, MeshFilter filter, GameObject gameObject, MeshRenderer renderer, Material material)
            {
                this.mesh = mesh;
                this.filter = filter;
                this.gameObject = gameObject;
                this.renderer = renderer;
                this.material = material;

                // 将 Material 应用到 MeshRenderer
                if (renderer != null && material != null)
                {
                    renderer.material = material;
                }
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

        public void ValidateRes()
        {
            int quarRes = resolution / 4 ;
            quarRes = Mathf.Max(1, quarRes);

            resolution = quarRes * 4;
        }

        #region Init
        [Button]
        public void Setup()
        {
            InitMesh();
        }

        public void OnEnable()
        {
            InitMesh();
        }


        virtual public void InitMesh()
        {
            CleanAll();

            float unitScale = domainSize / resolution;
            centerMesh = CreateSubMesh("Center" ,CreatePlaneMesh(resolution, resolution, domainSize / resolution), material);

            for (int i = 0; i < clipLevels; i++)
            {
                unitScale *= 2f;
                var clip = CreateSubMesh("Clip_" + i, CreateRingMesh(resolution , unitScale), material);
                meshClips.Add(clip);
            }

        }

        public void CleanAll()
        {
            centerMesh.Destroy();

            foreach (var clip in meshClips)
            {
                clip.Destroy();
            }

            meshClips.Clear();
        }
        

        public SubMeshRef CreateSubMesh( string name , Mesh mesh , Material material )
        { 
            var go = new GameObject();
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();

            return new SubMeshRef(mesh, filter, go, renderer,  material);

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

            Debug.Log($"CreateFrameMesh: resolution={resolution}, unitScale={unitScale}");
            Debug.Log($"  Total Size: {totalSize}, Hole Size: {holeSize}, Strip Width: {stripWidth}");

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
                Vector3.one
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
                Vector3.one
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
                new Vector3(1, 1, 1) 
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
                new Vector3(1, 1, 1)
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

        Mesh CreatePlaneMesh(int width, int height, float unitScale, int trianglesShift = 0)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Plane";
            if ((width + 1) * (height + 1) >= 256 * 256)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
            int[] triangles = new int[width * height * 2 * 3];
            Vector3[] normals = new Vector3[(width + 1) * (height + 1)];

            Vector3 offset = new Vector3(width, 0, height) * unitScale * (-0.5f);

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

            int tris = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int k = j + i * (width + 1);
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

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            mesh.RecalculateBounds();

            return mesh;
        }


        #endregion





    }

}