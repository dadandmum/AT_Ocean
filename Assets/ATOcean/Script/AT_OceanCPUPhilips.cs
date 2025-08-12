using ATOcean;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{
    public class AT_OceanCPUPhilips : AT_OceanCPU
    {
        [BoxGroup("ATOcean/PhillipsData")]
        [InlineEditor]
        public AT_OceanPhiSpecData waveData;

        [Button]
        public void UpdatePhillipsSpectrumData()
        {
            if ( waveData != null)
            {
                waveData.L = domainSize;

                waveData.SetupPhillipsSpectrum();
            }
        }


        public override void EvaluateMesh(int i, int j, float t)
        {
            // 当前顶点索引
            var currentIndex = i * resolution + j;

            var vertex = vertices[currentIndex];
            Vector2 x = new Vector2(vertex.x, vertex.z);

            float height = 0.0f;

            // Phillips Spectrum 的统计个数
            int N = waveData.N;

            for ( int n = 0 ; n < N ; n++)
            {
                for ( int m = 0 ; m < N ; m++)
                { 
                    Vector2 k = 

                }
            }

        }
    }
}