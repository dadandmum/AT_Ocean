using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{

    // �򵥵ĸ����ṹ�� (Unity û������)
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

        // ��������� (�����Ҫ�����ӵĲ����������)
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
        [Tooltip("����ֱ��ʣ�ȡֵԽ��Ч��Խ�ͣ���ģ��N*N����")]
        public int N = 8;
        [BoxGroup("Input")]
        public float L = 40.0f;    // ��������߳� (Unity��λ)

        [BoxGroup("Input")]
        public float windSpeedRand = 10f;


        [ReadOnly]
        [Tooltip("����")]
        public float windSpeed = 10f;

        [ReadOnly]
        [Tooltip("����y�������������)")]
        public Vector3 windDirection = new Vector3(1.0f, 0, 1.0f);
        public float A = 1e-7f;                          // Phillips Ƶ�׷��Ȳ���

        // ���ڼ���������Ƶɢ��ϵ
        private const float g = 9.81f; // �������ٶ� (m/s^2)
         
        [ReadOnly]
        public Vector2[] kVectors;
        [ReadOnly]
        public Complex[] h0;
        [ReadOnly]
        public Complex[] h0Conj;

        [ReadOnly]
        public float[] omega;

        [Tooltip("Setup ֮ǰ�ȶ���")]
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

            // Ԥ�������������ƽ�� (���� Phillips Ƶ��)
            float wLengthSq = windSpeed * windSpeed;
            if (wLengthSq < 1e-6f) wLengthSq = 1e-6f; // ���������

            // ��һ���ķ������� (���ڷ�����)
            Vector2 windDirNorm = windDirection.normalized;
            float damping = 0.001f; // С�����ֹ k=0 ʱ����


            // �������в�������
            for (int n = 0; n < N; n++)
            {
                for (int m = 0; m < N; m++)
                {
                    int index = n * N + m;
                    // ���������� k_x, k_y (�������� n, m ӳ�䵽 -��N/L �� ��N/L ��Χ)
                    float kx = (2.0f * Mathf.PI / L) * (m - N / 2);
                    float ky = (2.0f * Mathf.PI / L) * (n - N / 2);
                    Vector2 k = new Vector2(kx, ky);
                    kVectors[index] = k;

                    float kLength = k.magnitude;
                    float kLengthSq = kLength * kLength;
                    if (kLengthSq < 1e-6f)
                    {
                        // k=0 ����Ϊ 0���������
                        h0[index] = Complex.zero;
                        h0Conj[index] = Complex.zero;
                        omega[index] = 0.0f;
                        continue;
                    }

                    // Phillips Ƶ�׹�ʽ: P(k) = A / k^4 * exp(-1/(k^2 * l^2)) * (k . w)^2 / |w|^2
                    // ���� l = w^2 / g �Ƿ��˳��ȳ߶�
                    float l = wLengthSq / g;
                    float phillips = (A / kLengthSq / kLengthSq) *
                                    Mathf.Exp(-1.0f / (kLengthSq * l * l)) *
                                    Mathf.Pow(Vector2.Dot(k, windDirNorm), 2) / wLengthSq;


                    // ����С����
                    phillips *= Mathf.Exp(-kLengthSq * damping * damping);

                    // ���ɸ�˹������� h0(k) = (GaussianRandom() + i * GaussianRandom()) * sqrt(P(k)/2)
                    float randReal = RandomGaussian();
                    float randImag = RandomGaussian();
                    float sqrtPhillipsOver2 = Mathf.Sqrt(phillips * 0.5f);
                    h0[index] = new Complex(randReal * sqrtPhillipsOver2, randImag * sqrtPhillipsOver2);


                    // ������Ƶɢ��ϵ: ��(k) = sqrt(g * |k|)
                    omega[index] = Mathf.Sqrt(g * kLength);
                }
            }

            // ���ù��� h0
            for (int n = 0; n < N; n++)
            {
                for (int m = 0; m < N; m++)
                {
                    if (m == 0 && n == 0)
                        continue;

                    int index = n * N + m;
                    // ���� H0*(-k) = h0(-k).conjugate()
                    // -k ��Ӧ������: (N-n)%N, (N-m)%N
                    int n_neg = (N - n) % N;
                    int m_neg = (N - m) % N;
                    int index_neg = n_neg * N + m_neg;
                    // H0*(-k) Ӧ���� h0[n_neg, m_neg] �Ĺ���
                    h0Conj[index] = new Complex(h0[index_neg].real, -h0[index_neg].imag);

                }

            }

        }

        /// <summary>
        /// ���ɱ�׼��̬�ֲ� (��˹�ֲ�) ������� (Box-Muller �任)
        /// </summary>
        /// <returns>��ֵΪ0����׼��Ϊ1�������</returns>
        private float RandomGaussian()
        {
            float u1 = Random.Range(1e-6f, 1.0f); // ���� log(0)
            float u2 = Random.Range(0.0f, 1.0f);
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        /// <summary>
        /// ����һ�Ի���������ұ�׼��̬�ֲ� (��˹�ֲ�) ������� (Box-Muller ������)
        /// </summary>
        /// <returns>��ֵΪ0����׼��Ϊ1�������</returns>
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