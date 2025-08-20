using JetBrains.Annotations;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



namespace ATOcean
{

    public class AT_OceanCPU : AT_OceanBase
    {

        #region Update

        [BoxGroup("RealTimeParameters")]
        [ReadOnly] 
        public float timer;

        [BoxGroup("ATOcean")]
        public float tDivision = 1f;

        [BoxGroup("ATOcean")]
        public bool recalculateNormal = false;
        [BoxGroup("ATOcean")]
        public bool simulateInEditor = true;
        [BoxGroup("ATOcean")]
        [GUIColor(0.8f,0.8f,0.2f)]
        [Button(ButtonSizes.Large)]

        public void CalculateNextStep()
        {
            float dt = 0.033f;
            timer += dt;

            simulateInEditor = false;

            // calculate time elapse by .NET timer
            var startTime = System.DateTime.Now;

            EvalulateWave(timer, dt);

            // calculate time elapse
            var endTime =  System.DateTime.Now;
            var timeElapse = endTime - startTime;
            Debug.Log("Time Elapse: " + timeElapse.TotalMilliseconds + " ms");
        }

        public override void InitMesh()
        {
            timer = 0;
            base.InitMesh();
        }


        public void Update()
        {
            if (simulateInEditor || Application.isPlaying)

            {
                timer += Time.deltaTime / tDivision;
                EvalulateWave(timer, Time.deltaTime / tDivision);
            }
        }


        virtual public void EvalulateWave( float t , float dt )
        {
            // This is the main loop
            // evaluate the wave position offset
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    EvaluateMesh(i, j , t , dt );
                }
            }

            mesh.SetVertices(vertUpdate);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);

            if ( recalculateNormal )
                mesh.RecalculateNormals();

            //mesh.vertices = vertUpdate;
            //mesh.normals = normals;
            //mesh.colors = colors;
        }



        public int GetCurrentIndex(int i, int j)
        {
            return i * resolution + j;
        }


        // edit this function to implement different wave model
        virtual public void EvaluateMesh( int i , int j , float t , float dt  )
        {
            // Here is an example of single sinusoid wave 
            var currentIndex = GetCurrentIndex(i, j);


            // get the vertex from vertices
            var tempVertex = vertices[currentIndex];
            tempVertex.y = Mathf.Sin( i * 0.5f + t * 2f) * 0.5f;


            // save the result to vertUpdate to update the vertex of mesh
            vertUpdate[currentIndex] = tempVertex;
            // save the result to normals to update the normal of mesh
            normals[currentIndex] = Vector3.up;
            // save the result to colors to update the vertex color of mesh
            colors[currentIndex] = new Color(1, 1, 1, 1);
        }



        #endregion

    }

}
