//using Sirenix.OdinInspector;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UIElements;


//namespace ATOcean
//{
//    public class AT_OceanBase : MonoBehaviour
//    {

//        public MeshFilter filter;
//        public Mesh mesh;

//        [BoxGroup("Data")]
//        public int resolution = 8;
//        [BoxGroup("Data")]
//        public float unitWidth = 1f;

//        [BoxGroup("Data")]
//        public float choppiness = 0.1f;



//        public float timer;
//        public float tDivision = 10f;

//        public const float PI = 3.1415926f;
//        public const float EPSILON = 1e-6f;


//        #region MonoBehaviours

//        private void Update()
//        {
//            timer += Time.deltaTime / tDivision;
//            EvaluateWaves(timer);
//        }

//        private void Awake()
//        {
//            filter = GetComponent<MeshFilter>();
//            mesh = new Mesh();
//            filter.mesh = mesh;
//            SetParams();
//            GenerateMesh();
//        }

//        #endregion

//        #region Init



//        public Vector3[] vertices;
//        public int[] indices;
//        public Vector3[] normals;
//        public Vector2[] vertConj;
//        public Vector2[] verttilde;
//        public Vector3[] vertMeow;
//        public Vector2[] uvs;



//        private void SetParams()
//        {
//            vertices = new Vector3[resolution * resolution];
//            indices = new int[(resolution - 1) * (resolution - 1) * 6];
//            normals = new Vector3[resolution * resolution];
//            vertConj = new Vector2[resolution * resolution];
//            verttilde = new Vector2[resolution * resolution];
//            vertMeow = new Vector3[resolution * resolution];//Meow ~ 
//            uvs = new Vector2[resolution * resolution];
//        }


//        private void GenerateMesh()
//        {
//            int indiceCount = 0;
//            int halfResolution = resolution / 2;

//            for (int i = 0; i < resolution; i++)
//            {
//                float horizontalPosition = (i - halfResolution) * unitWidth;
//                for (int j = 0; j < resolution; j++)
//                {
//                    int currentIdx = i * (resolution) + j;
//                    float verticalPosition = (j - halfResolution) * unitWidth;
//                    vertices[currentIdx] = new Vector3(horizontalPosition + (resolution % 2 == 0 ? unitWidth / 2f : 0f), 0f, verticalPosition + (resolution % 2 == 0 ? unitWidth / 2f : 0f));
//                    normals[currentIdx] = new Vector3(0f, 1f, 0f);
//                    verttilde[currentIdx] = htilde0(i, j);
//                    Vector2 temp = htilde0(resolution - i, resolution - j);
//                    vertConj[currentIdx] = new Vector2(temp.x, -temp.y);
//                    uvs[currentIdx] = new Vector2(i * 1.0f / (resolution - 1), j * 1.0f / (resolution - 1));

//                    if (j == resolution - 1)
//                        continue;

//                    if (i != resolution - 1)
//                    {
//                        indices[indiceCount++] = currentIdx;
//                        indices[indiceCount++] = currentIdx + 1;
//                        indices[indiceCount++] = currentIdx + resolution;
//                    }
//                    if (i != 0)
//                    {
//                        indices[indiceCount++] = currentIdx;
//                        indices[indiceCount++] = currentIdx - resolution + 1;
//                        indices[indiceCount++] = currentIdx + 1;
//                    }
//                }
//            }
//            mesh.vertices = vertices;
//            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
//            mesh.normals = normals;
//            mesh.uv = uvs;
//            filter.mesh = mesh;
//        }

//        public Vector2 htilde0( int i , int j )
//        {
//            return new Vector2(0, 0);
//        }





//        #endregion



//        #region Update


//        private Vector3 Displacement(Vector2 x, float t, out Vector3 nor)
//        {
//            Vector2 h = new Vector2(0f, 0f);
//            Vector2 d = new Vector2(0f, 0f);
//            Vector3 n = Vector3.zero;
//            Vector2 c, htilde_c, k;
//            float kx, kz, k_length, kDotX;
//            for (int i = 0; i < resolution; i++)
//            {
//                kx = 2 * PI * (i - resolution / 2.0f) / length;
//                for (int j = 0; j < resolution; j++)
//                {
//                    kz = 2 * PI * (j - resolution / 2.0f) / length;
//                    k = new Vector2(kx, kz);
//                    k_length = k.magnitude;
//                    kDotX = Vector2.Dot(k, x);
//                    c = new Vector2(Mathf.Cos(kDotX), Mathf.Sin(kDotX));
//                    Vector2 temp = htilde(t, i, j);
//                    htilde_c = new Vector2(temp.x * c.x - temp.y * c.y, temp.x * c.y + temp.y * c.x);
//                    h += htilde_c;
//                    n += new Vector3(-kx * htilde_c.y, 0f, -kz * htilde_c.y);
//                    if (k_length < EPSILON)
//                        continue;
//                    d += new Vector2(kx / k_length * htilde_c.y, -kz / k_length * htilde_c.y);
//                }
//            }
//            nor = Vector3.Normalize(Vector3.up - n);
//            return new Vector3(d.x, h.x, d.y);
//        }

//        private Vector2[] hds;

//        private void EvaluateWaves(float t)
//        {
//            hds = new Vector2[resolution * resolution];

//            for (int i = 0; i < resolution; i++)
//            {
//                for (int j = 0; j < resolution; j++)
//                {
//                    int index = i * resolution + j;
//                    vertMeow[index] = vertices[index];
//                }
//            }

//            for (int i = 0; i < resolution; i++)
//            {
//                for (int j = 0; j < resolution; j++)
//                {
//                    int index = i * resolution + j;
//                    Vector3 nor = new Vector3(0f, 0f, 0f);
//                    Vector3 hd = Displacement(new Vector2(vertMeow[index].x, vertMeow[index].z), t, out nor);
//                    vertMeow[index].y = hd.y;
//                    vertMeow[index].z = vertices[index].z - hd.z * choppiness;
//                    vertMeow[index].x = vertices[index].x - hd.x * choppiness;
//                    normals[index] = nor;
//                    hds[index] = new Vector2(hd.x, hd.z);
//                }
//            }

//            Color[] colors = new Color[resolution * resolution];

//            for (int i = 0; i < resolution; i++)//写得并不正确,
//            {
//                for (int j = 0; j < resolution; j++)
//                {
//                    int index = i * resolution + j;
//                    Vector2 dDdx = Vector2.zero;
//                    Vector2 dDdy = Vector2.zero;
//                    if (i != resolution - 1)
//                    {
//                        dDdx = 0.5f * (hds[index] - hds[index + resolution]);
//                    }
//                    if (j != resolution - 1)
//                    {
//                        dDdy = 0.5f * (hds[index] - hds[index + 1]);
//                    }
//                    float jacobian = (1 + dDdx.x) * (1 + dDdy.y) - dDdx.y * dDdy.x;
//                    Vector2 noise = new Vector2(Mathf.Abs(normals[index].x), Mathf.Abs(normals[index].z)) * 0.3f;
//                    float turb = Mathf.Max(1f - jacobian + noise.magnitude, 0f);
//                    float xx = 1f + 3f * Mathf.SmoothStep(1.2f, 1.8f, turb);
//                    xx = Mathf.Min(turb, 1.0f);
//                    xx = Mathf.SmoothStep(0f, 1f, turb);
//                    colors[index] = new Color(xx, xx, xx, xx);
//                }
//            }
//            mesh.vertices = vertMeow;
//            mesh.normals = normals;
//            mesh.colors = colors;
//        }





//        #endregion



//    }

//}