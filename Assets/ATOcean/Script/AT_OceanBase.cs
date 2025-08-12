using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace ATOcean
{
    [ExecuteInEditMode]
    public class AT_OceanBase : MonoBehaviour
    {
        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public MeshFilter filter;

        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public MeshRenderer meshRenderer ;

        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public Mesh mesh;


        [ShowIf("useExtendMesh")]
        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public MeshFilter meshFilterExtended;
        [ShowIf("useExtendMesh")]
        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public Mesh meshExtended;
        [ShowIf("useExtendMesh")]
        [BoxGroup("ATOcean/reference")]
        [ReadOnly]
        public MeshRenderer meshExtendedRenderer;

        [BoxGroup("ATOcean/Settings")]
        [OnValueChanged("Setup")]
        public int resolution = 16;

        [BoxGroup("ATOcean/Settings")]
        public float domainSize = 10.0f;

        [BoxGroup("ATOcean/Settings")]
        [OnValueChanged("Setup")]
        public bool useExtendMesh = false;

        [BoxGroup("ATOcean")]
        public Material material;

        [ShowIf("useExtendMesh")]
        [BoxGroup("ATOcean")]
        public Material materialExtended;

        [BoxGroup("ATOcean/Settings")]
        public bool wireframe;

        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector3[] vertices;
        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public int[] indices;
        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector3[] normals;

        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector3[] vertUpdate;

        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public UnityEngine.Color[] colors;

        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector2[] uvs;


        // Extended
        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector3[] verticesExtended;
        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public int[] indicesExtended;
        [BoxGroup("RealTimeParameters")]
        [HideInInspector]
        public Vector3[] normalsExtended;

        [Button]
        virtual public void Setup()
        { 
            SetupReference();
            InitParameters();
            InitMesh();
        }

        public void OnEnable()
        {
            SetupReference();
            InitParameters();
            InitMesh();

        }

        public virtual void SetupReference()
        {
            filter = GetComponent<MeshFilter>();

            if (filter == null)
                filter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.material = material;

            // if we use extend mesh, then create an sub gameobject for extended mesh
            if (useExtendMesh)
            {
                var children = new List<Transform>(GetComponentsInChildren<Transform>());
                Transform root = null;
                if ( children != null && children.Count > 0 )
                    root = children.Find(x => x.name == "MeshExtended");
                if ( root == null )
                {
                    var go = new GameObject("MeshExtended");
                    go.transform.SetParent(transform);
                    root = go.transform;
                }

                meshFilterExtended = root.GetComponent<MeshFilter>();
                if ( meshFilterExtended == null )
                {
                    meshFilterExtended = root.gameObject.AddComponent<MeshFilter>();
                }

                meshExtendedRenderer = root.GetComponent<MeshRenderer>();
                if ( meshExtendedRenderer == null )
                {
                    meshExtendedRenderer = root.gameObject.AddComponent<MeshRenderer>();
                }
                meshExtendedRenderer.material = materialExtended;
            }
            else
            {
                if ( meshFilterExtended != null)
                {
                    DestroyImmediate(meshFilterExtended.gameObject);
                    meshFilterExtended = null;
                }

                if ( meshExtendedRenderer != null)
                {
                    meshExtendedRenderer = null;
                }

                if ( meshExtended != null)
                {
                    meshExtended = null;
                }

            }

        }

        public virtual void InitParameters()
        {
            vertices = new Vector3[resolution * resolution];
            indices = new int[(resolution - 1) * (resolution - 1) * 6];
            normals = new Vector3[resolution * resolution];
            vertUpdate = new Vector3[resolution * resolution];
            uvs = new Vector2[resolution * resolution];
            colors = new UnityEngine.Color[resolution * resolution];

            if (useExtendMesh)
            {
                verticesExtended = new Vector3[5];
                indicesExtended = new int[12];
                normalsExtended = new Vector3[5];
            }
        }

        virtual public void SetInitData(int i , int j )
        {
            int currentIdx = i * (resolution) + j;
            int halfResolution = resolution / 2;
            float unitWidth = domainSize / ( resolution - 1);
            float horizontalPosition = (i - halfResolution) * unitWidth;
            float verticalPosition = (j - halfResolution) * unitWidth;

            vertices[currentIdx] = new Vector3(horizontalPosition + (resolution % 2 == 0 ? unitWidth / 2f : 0f), 0f, verticalPosition + (resolution % 2 == 0 ? unitWidth / 2f : 0f));
            normals[currentIdx] = new Vector3(0f, 1f, 0f);
            uvs[currentIdx] = new Vector2(i * 1.0f / (resolution - 1), j * 1.0f / (resolution - 1));
            colors[currentIdx] = new UnityEngine.Color(0, 0, 0,0);

            if (j != resolution - 1)
            {
                if (i != resolution - 1)
                {
                    indices[indiceCount++] = currentIdx;
                    indices[indiceCount++] = currentIdx + 1;
                    indices[indiceCount++] = currentIdx + resolution;
                }
                if (i != 0)
                {
                    indices[indiceCount++] = currentIdx;
                    indices[indiceCount++] = currentIdx - resolution + 1;
                    indices[indiceCount++] = currentIdx + 1;
                }
            }

        }


        [HideInInspector]
        int indiceCount = 0;

        public virtual void InitMesh()
        {
            mesh = new Mesh();

            mesh.Clear();
            mesh.name = "AT_Ocean_Mesh";

            indiceCount = 0;
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    SetInitData(i, j);
                }
            }

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.uv = uvs;

            //mesh.UploadMeshData(false);
            mesh.RecalculateNormals();

            filter.mesh = mesh;

            if ( useExtendMesh )
            {
                meshExtended = new Mesh();
                meshExtended.Clear();
                meshExtended.name = "AT_Ocean_ExtendedMesh";

                ComputeExtendedPlane();

                meshExtended.RecalculateBounds();
                meshExtended.RecalculateNormals();

                meshFilterExtended.mesh = meshExtended;
            }

        }

        // Mesh Extend is Used for extend the ocean mesh to cover the whole screen
        // Referencee : https://github.com/speps/GX-EncinoWaves
        Vector3? GetIntersection(Vector3 planeOrigin, Vector3 planeNormal, Vector3 p0, Vector3 p1)
        {
            float den = Vector3.Dot(planeNormal, p1 - p0);
            if (Mathf.Abs(den) < float.Epsilon)
            {
                return null;
            }
            float u = Vector3.Dot(planeNormal, planeOrigin - p0) / den;
            if (u < 0.0f || u > 1.0f)
            {
                return null;
            }
            return p0 + u * (p1 - p0);
        }

        void AddPoint(List<Vector3> points, Vector3? point)
        {
            if (point.HasValue)
            {
                points.Add(point.Value);
            }
        }

        Vector2 To2D(Vector3 n, Vector3 p)
        {
            var v1 = GetPlaneBase(n, 1);
            var v2 = GetPlaneBase(n, 2);
            var v3 = n;

            float denom = v2.y * v3.x * v1.z - v2.x * v3.y * v1.z + v3.z * v2.x * v1.y +
                   v2.z * v3.y * v1.x - v3.x * v2.z * v1.y - v2.y * v3.z * v1.x;
            float x = -(v2.y * v3.z * p.x - v2.y * v3.x * p.z + v3.x * v2.z * p.y +
                      v2.x * v3.y * p.z - v3.z * v2.x * p.y - v2.z * v3.y * p.x) / denom;
            float y = (v1.y * v3.z * p.x - v1.y * v3.x * p.z - v3.y * p.x * v1.z +
                    v3.y * v1.x * p.z + p.y * v3.x * v1.z - p.y * v3.z * v1.x) / denom;

            return new Vector2(x, y);
        }


        Vector3 GetPlaneBase(Vector3 n, int index)
        {
            if (index == 1)
            {
                if (n.x == 0.0f)
                {
                    return Vector3.right;
                }
                else if (n.y == 0.0f)
                {
                    return Vector3.up;
                }
                else if (n.z == 0.0f)
                {
                    return Vector3.forward;
                }
                return new Vector3(-n.y, n.x, 0.0f);
            }
            return Vector3.Cross(n, GetPlaneBase(n, 1));
        }

        bool OrderedPointsCompare(Vector2 center, Vector2 a, Vector2 b)
        {
            if (a.x - center.x >= 0 && b.x - center.x < 0)
                return true;
            if (a.x - center.x < 0 && b.x - center.x >= 0)
                return false;
            if (a.x - center.x == 0 && b.x - center.x == 0)
            {
                if (a.y - center.y >= 0 || b.y - center.y >= 0)
                    return a.y > b.y;
                return b.y > a.y;
            }

            // compute the cross product of vectors (center -> a) x (center -> b)
            float det = (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y);
            if (det < 0)
                return true;
            if (det > 0)
                return false;

            // points a and b are on the same line from the center
            // check which point is closer to the center
            float d1 = (a.x - center.x) * (a.x - center.x) + (a.y - center.y) * (a.y - center.y);
            float d2 = (b.x - center.x) * (b.x - center.x) + (b.y - center.y) * (b.y - center.y);
            return d1 > d2;
        }

        void ComputeExtendedPlane()
        {
            var camera = Camera.main;
            if ( camera == null)
                return;

            var nearTL = camera.ViewportToWorldPoint(new Vector3(1, 0, camera.nearClipPlane));
            var nearTR = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.nearClipPlane));
            var nearBL = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.nearClipPlane));
            var nearBR = camera.ViewportToWorldPoint(new Vector3(0, 1, camera.nearClipPlane));
            var farTL = camera.ViewportToWorldPoint(new Vector3(1, 0, camera.farClipPlane));
            var farTR = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.farClipPlane));
            var farBL = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.farClipPlane));
            var farBR = camera.ViewportToWorldPoint(new Vector3(0, 1, camera.farClipPlane));

            var planeOrigin = new Vector3(camera.transform.position.x, 0.0f, camera.transform.position.z);
            var planeNormal = Vector3.up;

            var points = new List<Vector3>();
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearTL, farTL));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearTR, farTR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearBL, farBL));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearBR, farBR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, farTL, farTR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, farBL, farBR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, farTL, farBL));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, farTR, farBR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearTL, nearTR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearBL, nearBR));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearTL, nearBL));
            AddPoint(points, GetIntersection(planeOrigin, planeNormal, nearTR, nearBR));
            if (points.Count == 0)
            {
                return;
            }

            var center = Vector2.zero;
            var points2D = new List<Vector2>();
            foreach (var p in points)
            {
                var p2D = To2D(planeNormal, p);
                center += p2D;
                points2D.Add(p2D);
            }
            center /= points.Count;

            var v1 = GetPlaneBase(planeNormal, 1);
            var v2 = GetPlaneBase(planeNormal, 2);

            points2D.Sort((a, b) => OrderedPointsCompare(center, a, b) ? -1 : 1);

            if ( verticesExtended.Length != points2D.Count + 1 ) 
                verticesExtended = new Vector3[points2D.Count + 1];

            if ( indicesExtended.Length != points2D.Count * 3 )
                indicesExtended = new int[points2D.Count * 3];

            if ( normalsExtended.Length != points2D.Count + 1)
                normalsExtended = new Vector3[points2D.Count + 1];

            verticesExtended[0] = v1 * center.x + v2 * center.y;
            normalsExtended[0] = new Vector3(0, 1f, 0);
            for (int i = 0; i < points2D.Count; i++)
            {
                verticesExtended[i+1] = v1 * points2D[i].x + v2 * points2D[i].y;
                normalsExtended[i+1] = new Vector3(0,1f,0);
                indicesExtended[i * 3 ] = 0;
                indicesExtended[i * 3 + 1 ] = 1 + (i + 1) % points2D.Count;
                indicesExtended[i * 3 + 2 ] = 1 + i;
            }

            if ( meshExtended == null )
            {
                meshExtended = new Mesh();
                meshExtended.name = "AT_Ocean_ExtendedMesh";
            }

            meshExtended.Clear();
            meshExtended.SetVertices(verticesExtended);
            meshExtended.SetNormals(normalsExtended);
            meshExtended.SetTriangles(indicesExtended, 0);
            meshExtended.RecalculateNormals();

            // Graphics.DrawMesh(meshExtended, Matrix4x4.identity, materialExtended, gameObject.layer);
        }

        void UpdateMaterial(Material m)
        {
            m.SetVector("_ViewOrigin", Camera.main.transform.position);
            m.SetFloat("_DomainSize", domainSize);
            m.SetFloat("_InvDomainSize", 1.0f / domainSize);
            m.SetVector("_WaveWorldPos", transform.position);
        }

        private void LateUpdate()
        {
            UpdateMaterial(material);
            UpdateMaterial(materialExtended);
            ComputeExtendedPlane();
        }


    }
}