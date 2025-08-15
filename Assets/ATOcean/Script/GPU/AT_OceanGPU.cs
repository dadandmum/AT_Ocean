using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

            // ����
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


        [BoxGroup("AT_Ocean/Debug",Order = 1000)]
        public bool syncMaterial;
        [BoxGroup("AT_Ocean/Debug")]
        public bool showLODs;
        [BoxGroup("AT_Ocean/Debug")]
        public bool playInEditor=true;

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
        }

        public void OnEnable()
        {
            InitMesh();

            if (renderCascades == null || renderCascades.Count == 0)
            {
                InitBasicCascadesDefinition();
            }
            InitRender();
        }

        public void OnDisable()
        {
            Dipose();
        }


        public void Update()
        {
            if ( playInEditor || Application.isPlaying)
                UpdateRender();
        }

        virtual public void Dipose()
        {

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

            InitRender();
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




        // ������ɫ��������
        UnityEngine.Color[] commonColors = new UnityEngine.Color[]
        {
                UnityEngine.Color.red,        // ��ɫ
                UnityEngine.Color.green,      // ��ɫ
                UnityEngine.Color.blue,       // ��ɫ
                UnityEngine.Color.white,      // ��ɫ
                UnityEngine.Color.black,      // ��ɫ
                UnityEngine.Color.yellow,     // ��ɫ
                UnityEngine.Color.cyan,       // ��ɫ
                UnityEngine.Color.magenta,    // ���ɫ
                UnityEngine.Color.gray,       // ��ɫ (0.5, 0.5, 0.5)
                UnityEngine.Color.grey,       // ��ɫ��Ӣʽƴд��ͬ gray��
                UnityEngine.Color.clear,      // ͸��ɫ (0,0,0,0)
        };


        virtual public void UpdateRender()
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
        

        public SubMeshRef CreateSubMesh( string name , int lod , Mesh mesh , Material material )
        { 
            var go = new GameObject();
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, 0, 0);

            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();

            return new SubMeshRef(lod , mesh, filter, go, renderer);

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
            float offset = 0.05f;


            //Debug.Log($"CreateFrameMesh: resolution={resolution}, unitScale={unitScale}");
            //Debug.Log($"  Total Size: {totalSize}, Hole Size: {holeSize}, Strip Width: {stripWidth}");

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
                new Vector3(1 + offset , 1, 1 + offset * 2.0f)
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
                new Vector3(1 + offset, 1, 1 + offset * 2.0f)
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
                new Vector3(1 + offset , 1, 1) 
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
                new Vector3(1 + offset , 1, 1)
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

        #endregion


        public static int LENGTH_SCALE_0_PROP = Shader.PropertyToID("_LengthScale0");
        public static int LENGTH_SCALE_1_PROP = Shader.PropertyToID("_LengthScale1");
        public static int LENGTH_SCALE_2_PROP = Shader.PropertyToID("_LengthScale2");



    }

}