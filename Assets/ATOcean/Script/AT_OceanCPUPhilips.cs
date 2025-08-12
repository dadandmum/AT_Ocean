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

        public override void Setup()
        {
            recalculateNormal = true;
            base.Setup();
        }


        public Vector2 ComplexMultiply( Vector2 a , Vector2 b)
        {
            return new Vector2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
        }

        public Vector2 ComplexConjugate( Vector2 a)
        {
            return new Vector2(a.x, -a.y);
        }


        public override void EvaluateMesh(int i, int j, float t)
        {
            // 当前顶点索引
            var currentIndex = i * resolution + j;

            var vertex = vertices[currentIndex];
            Vector2 X = new Vector2(vertex.x, vertex.z);

            float dh = 0.0f;
            float dz = 0.0f;
            float dx = 0.0f;
            const float g = 9.81f;

            // Phillips Spectrum 的统计个数
            int N = waveData.N;

            for ( int n = 0 ; n < N ; n++)
            {
                for ( int m = 0 ; m < N ; m++)
                { 
                    int index = n * N + m;

                    // 计算物理波数 k_x, k_y (基于索引 n, m 映射到 -πN/L 到 πN/L 范围)
                    float kx = (2.0f * Mathf.PI / domainSize) * (m - N / 2);
                    float kz = (2.0f * Mathf.PI / domainSize) * (n - N / 2);
                    Vector2 k = new Vector2(kx, kz);
                    float kDotX = Vector2.Dot(k, X);
                    float kLength = k.magnitude;

                    if (kLength < 1e-6f)
                        continue;

                    float omega = Mathf.Sqrt(g * kLength);
                    float omegaT = omega * t;


                    // 计算 h(k, t) = h0(k) * e^(i(k.x - ωt)) + h0*(-k) * e^(-i(k.x + ωt))
                    Vector2 exponent  = new Vector2(Mathf.Cos(kDotX+omegaT), Mathf.Sin(kDotX+omegaT));
                    Vector2 h = ComplexMultiply(waveData.h0[index] , exponent ) + ComplexMultiply(waveData.h0Conj[index], ComplexConjugate(exponent));
                    

                    // 垂直方向的偏移取h的实部
                    dh += h.x;

                    // 水平方向的偏移取h的虚部计算
                    dx += -h.y * kx / kLength * waveData.choppiness;
                    dz += -h.y * kz / kLength * waveData.choppiness;
                }
            }


            vertUpdate[currentIndex] = new Vector3(vertex.x + dx , dh , vertex.z + dz );
        }
    }
}