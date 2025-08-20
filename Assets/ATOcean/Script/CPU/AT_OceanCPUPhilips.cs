using ATOcean;
using Sirenix.OdinInspector;
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


        public override void EvaluateMesh(int i, int j, float t, float dt)
        {
            
            var currentIndex = GetCurrentIndex(i, j); 


            var vertex = vertices[currentIndex];
            Vector2 X = new Vector2(vertex.x, vertex.z);

            float dh = 0.0f;
            float dz = 0.0f;
            float dx = 0.0f;
            const float g = 9.81f;

            // Phillips Spectrum
            int N = waveData.N;

            for ( int n = 0 ; n < N ; n++)
            {
                for ( int m = 0 ; m < N ; m++)
                { 
                    int index = n * N + m;
                    
                    // Calculate the x and z components of the wave number vector k, mapping n and m to the range from -πN/L to πN/L
                    // 计算波数向量 k 的 x 和 z 分量，将 n, m 映射到 -πN/L 到 πN/L 范围内
                    float kx = (2.0f * Mathf.PI / domainSize) * (m - N / 2);
                    float kz = (2.0f * Mathf.PI / domainSize) * (n - N / 2);
                    Vector2 k = new Vector2(kx, kz);
                    float kDotX = Vector2.Dot(k, X);
                    float kLength = k.magnitude;

                    if (kLength < 1e-6f)
                        continue;

                    // 计算角频率 ω，公式为 ω = √(g * |k|)
                    // Calculate the angular frequency ω using the formula ω = √(g * |k|)
                    float omega = Mathf.Sqrt(g * kLength);
                    // 计算角频率与时间的乘积 ωt
                    // Calculate the product of angular frequency and time ωt
                    float omegaT = omega * t;


                    // 计算复指数项，使用欧拉公式 e^(iθ) = cosθ + i*sinθ，其中 θ = k·X + ωt
                    // Calculate the complex exponential term using Euler's formula e^(iθ) = cosθ + i*sinθ, where θ = k·X + ωt
                    Vector2 exponent  = new Vector2(Mathf.Cos(kDotX+omegaT), Mathf.Sin(kDotX+omegaT));
                    // 计算波面高度 h，结合初始波幅 h0 和其共轭 h0Conj 与复指数项相乘
                    // 计算公式为 h = h0 * e^(i(k·X + ωt)) + h0Conj * e^(-i(k·X + ωt))
                    // 这里h用Vector2表示，x表示实部，y表示虚部
                    // Calculate the wave surface height h by combining the initial amplitude h0 and its conjugate h0Conj multiplied by the complex exponential term
                    Vector2 h = ComplexMultiply(waveData.h0[index] , exponent ) + ComplexMultiply(waveData.h0Conj[index], ComplexConjugate(exponent));

                    // 用h的实部计算波高
                    // Calculate the wave height using the real part of h
                    dh += h.x;

                    // 用h的虚部计算水平方向的偏移
                    // Calculate the horizontal offset using the imaginary part of h
                    // 公式为 dx = -h.y * kx / |k| * 波切系数
                    // 这里h用Vector2表示，x表示实部，y表示虚部
                    // 公式为 dx = -h.y * kx / |k| * 波切系数
                    // 这里h用Vector2表示，x表示实部，y表示虚部
                    dx += -h.y * kx / kLength * waveData.choppiness;
                    dz += -h.y * kz / kLength * waveData.choppiness;
                }
            }


            vertUpdate[currentIndex] = new Vector3(vertex.x + dx , dh , vertex.z + dz );
        }
    }
}