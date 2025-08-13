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

                // �� Material Ӧ�õ� MeshRenderer
                if (renderer != null && material != null)
                {
                    renderer.material = material;
                }
            }

            // ����
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
        /// ����һ����״������
        /// �����СΪ (resolution  * unitScale) * (resolution  * unitScale)��
        /// ������һ����СΪ (resolution * unitScale) * (resolution * unitScale) �Ŀն���
        /// ʹ�� CombineMeshes ����ƴ���ĸ���������ʵ�֡�
        /// </summary>
        /// <param name="resolution">����ÿ�ߵĵ��������ڼ�������Ϳն��ߴ磩</param>
        /// <param name="unitScale">����Ļ�����λ����</param>
        /// <returns>��Ϻ�Ļ�״����</returns>
        Mesh CreateRingMesh(int resolution, float unitScale)
        {
            if ( resolution % 4 != 0 )
            {
                Debug.LogError("Resolution of Ring Mesh should be power of 4 , now is " + resolution);
                return null;
            }
            // 1. ����ؼ��ߴ�
            int totalRes = resolution;
            int holeRes = resolution / 2 ;
            int stripRes = (totalRes - holeRes) / 2;
            float totalSize = totalRes * unitScale; // ������ܵ���߳�
            float holeSize = holeRes * unitScale;      // ���Ŀն��ı߳�
            float stripWidth = stripRes * unitScale; // ÿ�������Ŀ�� (��򵽿ն��ľ���)

            Debug.Log($"CreateFrameMesh: resolution={resolution}, unitScale={unitScale}");
            Debug.Log($"  Total Size: {totalSize}, Hole Size: {holeSize}, Strip Width: {stripWidth}");

            // 2. ���� CombineInstance �б����洢Ҫ�ϲ�������
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            // 3. ��������λ�ĸ����� (Top, Bottom, Left, Right)

            // 3.1 �������� (Top Strip)
            // �ߴ�: ��� = totalSize, �߶� = stripWidth
            Mesh topMesh = CreatePlaneMesh(totalRes, stripRes, unitScale);
            CombineInstance topInstance = new CombineInstance();
            topInstance.mesh = topMesh;
            // λ��: Y=0, X=0, Z= �ӿն�������Ե����ܶ�����Ե������
            topInstance.transform = Matrix4x4.TRS(
                new Vector3(0, 0, (holeSize / 2) + (stripWidth / 2)),
                Quaternion.identity,
                Vector3.one
            );
            combineInstances.Add(topInstance);

            // 3.2 �ײ����� (Bottom Strip)
            // �ߴ�: ��� = totalSize, �߶� = stripWidth
            Mesh bottomMesh = CreatePlaneMesh(totalRes, stripRes, unitScale);
            CombineInstance bottomInstance = new CombineInstance();
            bottomInstance.mesh = bottomMesh;
            // λ��: Y=0, X=0, Z= �ӿ�ܵײ���Ե���ն��ײ���Ե������ (������)
            bottomInstance.transform = Matrix4x4.TRS(
                new Vector3(0, 0, -(holeSize / 2) - (stripWidth / 2)),
                Quaternion.identity,
                Vector3.one
            );
            combineInstances.Add(bottomInstance);

            // 3.3 ������� (Left Strip)
            // �ߴ�: ��� = stripWidth, �߶� = holeSize (��Ϊ�м�ն��߶��� holeSize)
            Mesh leftMesh = CreatePlaneMesh(stripRes, holeRes,unitScale);
            CombineInstance leftInstance = new CombineInstance();
            leftInstance.mesh = leftMesh;
            // λ��: Y=0, X= �ӿ������Ե���ն�����Ե������ (������), Z=0
            // ע��: CreatePlaneMesh Ĭ����XZƽ�棬����ֱ���� stripWidth �����������߶��� stripWidth��
            // ������Ҫ��Z�������������� holeSize �ĳ��ȡ���ͨ������ transform ʵ�֡�
            leftInstance.transform = Matrix4x4.TRS(
                new Vector3(-(holeSize / 2) - (stripWidth / 2), 0, 0),
                Quaternion.identity,
                new Vector3(1, 1, 1) 
            );
            combineInstances.Add(leftInstance);

            // 3.4 �Ҳ����� (Right Strip)
            // �ߴ�: ��� = stripWidth, �߶� = holeSize
            Mesh rightMesh = CreatePlaneMesh(stripRes, holeRes, unitScale);
            CombineInstance rightInstance = new CombineInstance();
            rightInstance.mesh = rightMesh;
            // λ��: Y=0, X= �ӿն��Ҳ��Ե������Ҳ��Ե������, Z=0
            rightInstance.transform = Matrix4x4.TRS(
                new Vector3((holeSize / 2) + (stripWidth / 2), 0, 0),
                Quaternion.identity,
                new Vector3(1, 1, 1)
            );
            combineInstances.Add(rightInstance);

            // 4. �������յ��������
            Mesh combinedMesh = new Mesh();
            // ʹ�� combineInstances ������������
            // �ڶ������� 'true' ��ʾ����ϣ���ϲ������������ʣ�����ֻ��һ��������������ν��
            // ���������� 'true' ��ʾ����ϣ���ϲ�ʱ���Ǳ任����λ�á���ת�����ţ�
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            // 5. ������ʱ�����������Ա����ڴ�й©
            // ע��: �� Unity �У�Mesh ��Դ��Ҫ�ֶ����٣��������ڱ༭����Ƶ������ʱ��
            // ������ʱ�������Щ����û�б������ط����ã�GC �ᴦ������ʽ���ٸ���ȫ��
            // **��Ҫ**: ֻ����ȷ�� CombineMeshes �Ѿ������˶������ݺ�������١�
            // CombineMeshes ͨ���Ḵ�����ݣ����Կ��԰�ȫ����Դ����
            Object.DestroyImmediate(topMesh);
            Object.DestroyImmediate(bottomMesh);
            Object.DestroyImmediate(leftMesh);
            Object.DestroyImmediate(rightMesh);

            // 6. ��ѡ: ���¼��㷨�ߺ����ߣ�ȷ��������ȷ
            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateTangents(); // �����Ҫ������ͼ��
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