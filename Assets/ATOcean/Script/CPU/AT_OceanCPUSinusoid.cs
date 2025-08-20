using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace ATOcean
{


    public class AT_OceanCPUSinusoid : AT_OceanCPU
    {
        [BoxGroup("ATOcean/Data")]
        [InlineEditor]
        public AT_OceanWaveData waveData;

        public override void EvaluateMesh(int i, int j, float t , float dt )
        {
            // get the index of current vertex
            var currentIndex = GetCurrentIndex(i, j);

            // get the vertex position 
            var vertex = vertices[currentIndex];

            float h = 0;
            float nx = 0;
            float nz = 0;
            float sssThickness = 0;

            for (int k = 0; k < waveData.waves.Count; k++)
            {
                var w_data = waveData.waves[k];
                var dir = w_data.direction.normalized;
                var w_k = 2 * Mathf.PI / w_data.wavelength;
                var theta = (vertex.x * dir.x + vertex.z * dir.z) * w_k + t * w_data.phaseFrequency;
                
                var h_k = w_data.amplitude * Mathf.Sin(theta);

                var nx_k = vertex.x * dir.x * w_k * w_data.amplitude * Mathf.Cos(theta);
                var nz_k = vertex.z * dir.z * w_k * w_data.amplitude * Mathf.Cos(theta);
                var n_k = new Vector3( nx_k , 1f , nz_k).normalized;


                h += h_k;

                nx += nx_k;
                nz += nz_k;

                //if (sunLight != null)
                //{
                //    var lightDir = sunLight.transform.forward;
                //    var lightHorizontal = new Vector3(lightDir.x, 0, lightDir.z).normalized;
                //    var lightTheta = Mathf.Atan2(lightDir.z, Mathf.Sqrt(1f - lightDir.z * lightDir.z));
                //    var waveTheta = Mathf.Atan2(n_k.z, Mathf.Sqrt(1f - n_k.z * n_k.z));
                //    var sss_k = w_data.wavelength / Mathf.Max(0.01f, Mathf.Abs(Vector3.Dot(w_data.direction, lightHorizontal)))
                //        * Mathf.Max(0, Mathf.Cos(theta) > 0 ? Mathf.Cos(theta) * 2f : (1f - Mathf.Cos(theta) * 2f))
                //        * Mathf.Lerp(0, 1f, Mathf.Clamp01(lightTheta / waveTheta));

                //    sssThickness += sss_k;
                //}
            }

            vertUpdate[currentIndex] = new Vector3( vertex.x, h, vertex.z );

            normals[currentIndex] = new Vector3(-nx, 1f, -nz).normalized;

            colors[currentIndex] = new Color(sssThickness , 0 , 0, 0 );
        }

    }

}