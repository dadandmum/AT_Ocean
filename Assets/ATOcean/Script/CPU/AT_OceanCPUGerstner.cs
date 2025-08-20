using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ATOcean
{

    public class AT_OceanCPUGerstner : AT_OceanCPU
    {
        [BoxGroup("ATOcean/Data")]
        [InfoBox("Gerstner波数据,点击")]
        [InlineEditor]
        public AT_OceanWaveData waveData;

        [Button]
        void CreateNewWaveData()
        {
            waveData = AT_OceanUtiliy.GernateWaveData();

        }

        public override void EvaluateMesh(int i, int j, float t, float dt)
        {
            // 当前顶点的索引
            var currentIndex = GetCurrentIndex(i, j);


            // 获取顶点初始坐标
            var vertex = vertices[currentIndex];

            Vector3 p = new Vector3(0, 0, 0); // 位置偏移
            Vector3 n = new Vector3(0, 0, 0); // 法线

            // reference : https://zhuanlan.zhihu.com/p/31670275

            // 遍历每一个波浪
            for (int k = 0; k < waveData.waves.Count; k++)
            {
                var wave_k = waveData.waves[k];
                var dir_k = wave_k.direction.normalized;
                var omega_k = 2 * Mathf.PI / wave_k.wavelength;
                // theta = dot( vertex.xz , dir.xz ) * w_k + t * phase;
                var theta_k = (vertex.x * dir_k.x + vertex.z * dir_k.z) * omega_k + t * wave_k.phaseFrequency;

                var p_y_k = wave_k.amplitude * Mathf.Sin(theta_k);
                var p_x_k = wave_k.steepness * wave_k.amplitude * dir_k.x * Mathf.Cos(theta_k);
                var p_z_k = wave_k.steepness * wave_k.amplitude * dir_k.z * Mathf.Cos(theta_k);

                var n_y_k = wave_k.steepness * omega_k * wave_k.amplitude * Mathf.Sin(theta_k);
                var n_x_k = dir_k.x * omega_k * wave_k.amplitude * Mathf.Cos(theta_k);
                var n_z_k = dir_k.z * omega_k * wave_k.amplitude * Mathf.Cos(theta_k);

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