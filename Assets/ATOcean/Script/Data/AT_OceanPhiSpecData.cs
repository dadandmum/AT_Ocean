using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{

    // 简单的复数结构体 (Unity 没有内置)
    public struct Complex
    {
        public float real;
        public float imag;

        public Complex(float real, float imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public static Complex zero => new Complex(0, 0);

        // 重载运算符 (如果需要更复杂的操作可以添加)
        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.imag + b.imag);
        }

        public static Complex operator -(Complex a, Complex b)
        {
            return new Complex(a.real - b.real, a.imag - b.imag);
        }

        public static Complex operator *(Complex a, Complex b)
        {
            return new Complex(a.real * b.real - a.imag * b.imag,
                               a.real * b.imag + a.imag * b.real);
        }
    }

    [CreateAssetMenu(fileName = "AT_OceanPhiSpecData", menuName = "ATOcean/AT_OceanPhiSpecData")]
    public class AT_OceanPhiSpecData : ScriptableObject
    {
        [BoxGroup("Input")]
        [Tooltip("网格分辨率，取值越高效率越低，会模拟N*N个波")]
        public int N = 8;
        [BoxGroup("Input")]
        public float L = 40.0f;    // 海面区域边长 (Unity单位)

        [BoxGroup("Input")]
        public float windSpeedRand = 10f;


        [ReadOnly]
        [Tooltip("风速")]
        public float windSpeed = 10f;

        [ReadOnly]
        [Tooltip("风向（y方向分量不考虑)")]
        public Vector3 windDirection = new Vector3(1.0f, 0, 1.0f);
        public float A = 1e-7f;                          // Phillips 频谱幅度参数

        // 用于计算重力波频散关系
        private const float g = 9.81f; // 重力加速度 (m/s^2)
         
        [ReadOnly]
        public Vector2[] kVectors;
        [ReadOnly]
        public Complex[] h0;
        [ReadOnly]
        public Complex[] h0Conj;

        [ReadOnly]
        public float[] omega;

        [Tooltip("Setup 之前先对齐")]
        [Button]
        public void SetupPhillipsSpectrum()
        {
            // set up wind 
            windSpeed = Random.Range(0 , 1.0f ) * windSpeedRand;
            windDirection = Random.onUnitSphere;
            windDirection.y = 0;
            windDirection = windDirection.normalized;

            InitVariables();
            InitSpectrum();
        }

        public void InitVariables()
        {
            kVectors = new Vector2[N * N];
            h0 = new Complex[N * N];
            h0Conj = new Complex[N * N];
            omega = new float[N * N];
        }

        public void InitSpectrum()
        {

            // 预计算风速向量的平方 (用于 Phillips 频谱)
            float wLengthSq = windSpeed * windSpeed;
            if (wLengthSq < 1e-6f) wLengthSq = 1e-6f; // 避免除以零

            // 归一化的风向向量 (用于方向性)
            Vector2 windDirNorm = windDirection.normalized;
            float damping = 0.001f; // 小阻尼防止 k=0 时除零


            // 遍历所有波数索引
            for (int n = 0; n < N; n++)
            {
                for (int m = 0; m < N; m++)
                {
                    int index = n * N + m;
                    // 计算物理波数 k_x, k_y (基于索引 n, m 映射到 -πN/L 到 πN/L 范围)
                    float kx = (2.0f * Mathf.PI / L) * (m - N / 2);
                    float ky = (2.0f * Mathf.PI / L) * (n - N / 2);
                    Vector2 k = new Vector2(kx, ky);
                    kVectors[index] = k;

                    float kLength = k.magnitude;
                    float kLengthSq = kLength * kLength;
                    if (kLengthSq < 1e-6f)
                    {
                        // k=0 处设为 0，避免除零
                        h0[index] = Complex.zero;
                        h0Conj[index] = Complex.zero;
                        omega[index] = 0.0f;
                        continue;
                    }

                    // Phillips 频谱公式: P(k) = A / k^4 * exp(-1/(k^2 * l^2)) * (k . w)^2 / |w|^2
                    // 其中 l = w^2 / g 是风浪长度尺度
                    float l = wLengthSq / g;
                    float phillips = (A / kLengthSq / kLengthSq) *
                                    Mathf.Exp(-1.0f / (kLengthSq * l * l)) *
                                    Mathf.Pow(Vector2.Dot(k, windDirNorm), 2) / wLengthSq;


                    // 加入小阻尼
                    phillips *= Mathf.Exp(-kLengthSq * damping * damping);

                    // 生成高斯随机复数 h0(k) = (GaussianRandom() + i * GaussianRandom()) * sqrt(P(k)/2)
                    float randReal = RandomGaussian();
                    float randImag = RandomGaussian();
                    float sqrtPhillipsOver2 = Mathf.Sqrt(phillips * 0.5f);
                    h0[index] = new Complex(randReal * sqrtPhillipsOver2, randImag * sqrtPhillipsOver2);


                    // 重力波频散关系: ω(k) = sqrt(g * |k|)
                    omega[index] = Mathf.Sqrt(g * kLength);
                }
            }

            // 设置共轭 h0
            for (int n = 0; n < N; n++)
            {
                for (int m = 0; m < N; m++)
                {
                    if (m == 0 && n == 0)
                        continue;

                    int index = n * N + m;
                    // 计算 H0*(-k) = h0(-k).conjugate()
                    // -k 对应的索引: (N-n)%N, (N-m)%N
                    int n_neg = (N - n) % N;
                    int m_neg = (N - m) % N;
                    int index_neg = n_neg * N + m_neg;
                    // H0*(-k) 应该是 h0[n_neg, m_neg] 的共轭
                    h0Conj[index] = new Complex(h0[index_neg].real, -h0[index_neg].imag);

                }

            }

        }

        /// <summary>
        /// 生成标准正态分布 (高斯分布) 的随机数 (Box-Muller 变换)
        /// </summary>
        /// <returns>均值为0，标准差为1的随机数</returns>
        private float RandomGaussian()
        {
            float u1 = Random.Range(1e-6f, 1.0f); // 避免 log(0)
            float u2 = Random.Range(0.0f, 1.0f);
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        /// <summary>
        /// 生成一对互相独立，且标准正态分布 (高斯分布) 的随机数 (Box-Muller 极坐标)
        /// </summary>
        /// <returns>均值为0，标准差为1的随机数</returns>
        private Vector2 RandomGaussianVariablePair()
        {
            float x1, x2, w;
            do
            {
                x1 = 2f * Random.Range(0f, 1f) - 1f;
                x2 = 2f * Random.Range(0f, 1f) - 1f;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1f);
            w = Mathf.Sqrt((-2f * Mathf.Log(w)) / w);
            return new Vector2(x1 * w, x2 * w);
        }

    }
}