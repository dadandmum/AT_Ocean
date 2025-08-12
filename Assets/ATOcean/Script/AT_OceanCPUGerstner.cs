using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{

    public class AT_OceanCPUGerstner : AT_OceanCPU
    {

        [BoxGroup("ATOcean/Data")]
        [InlineEditor]
        public AT_OceanWaveData waveData;


        public override void EvaluateMesh(int i, int j, float t)
        {
            // ��ǰ���������
            var currentIndex = i * resolution + j;

            // ��ȡ�����ʼ����
            var vertex = vertices[currentIndex];

            Vector3 p = new Vector3(0, 0, 0); // λ��ƫ��
            Vector3 n = new Vector3(0, 0, 0); // ����

            // reference : https://zhuanlan.zhihu.com/p/31670275

            // ����ÿһ������
            for (int k = 0; k < waveData.waves.Count; k++)
            {
                var wave_k = waveData.waves[k];
                var dir_k = wave_k.direction.normalized;
                var omega_k = 2 * Mathf.PI / wave_k.wavelength;
                // theta = dot( vertex.xz , dir.xz ) * w_k + t * phase;
                var theta_k = (vertex.x * dir_k.x + vertex.z * dir_k.z) * omega_k + t * wave_k.phase;

                var p_y_k = wave_k.amplitude * Mathf.Sin(theta_k);
                var p_x_k = wave_k.steepness * wave_k.amplitude * dir_k.x * Mathf.Cos(theta_k);
                var p_z_k = wave_k.steepness * wave_k.amplitude * dir_k.z * Mathf.Cos(theta_k);

                var n_y_k = wave_k.steepness * omega_k * wave_k.amplitude * Mathf.Sin(theta_k);
                var n_x_k = vertex.x * dir_k.x * omega_k * wave_k.amplitude * Mathf.Cos(theta_k);
                var n_z_k = vertex.z * dir_k.z * omega_k * wave_k.amplitude * Mathf.Cos(theta_k);

                p.y += p_y_k;
                p.x += p_x_k;
                p.z += p_z_k;

                n.y += n_y_k;
                n.x += n_x_k;
                n.z += n_z_k;
            }

            vertUpdate[currentIndex] = new Vector3(vertex.x + p.x, p.y, vertex.z + p.z );

            normals[currentIndex] = new Vector3(-n.x, 1f - n.y, -n.z).normalized;
            // normals[currentIndex] = new Vector3( 0, 1f, 0).normalized;

            colors[currentIndex] = new Color(0, 0, 0, 0);
        }
    }

}