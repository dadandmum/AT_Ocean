using JetBrains.Annotations;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
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


        public override void InitParameters()
        {
            timer = 0;
            base.InitParameters();
        }


        public void Update()
        {
            timer += Time.deltaTime / tDivision;
            EvalulateWave(timer);
        }


        virtual public void EvalulateWave( float t)
        {
    
            // This is the main loop
            // evaluate the wave position offset
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    EvaluateMesh(i, j , t );
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);

        }

        // edit this function to implement different wave model
        virtual public void EvaluateMesh( int i , int j , float t )
        {
            // Here is an example of single sinusoid wave 
            var currentIndex = i * resolution + j;

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
