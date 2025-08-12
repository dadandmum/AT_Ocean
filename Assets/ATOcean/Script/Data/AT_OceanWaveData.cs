using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{

    [System.Serializable]
    public class SinusoidWaveInfo
    {
        public Vector3 direction; // only consider (x,0,z)
        public float amplitude;  // A = amplitude
        public float wavelength; // w = 2*pi/wavelength
        public float phase; // time offset speed
        public float steepness; // for Gerstner Wave, should be < 1 / (w * A)

    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "ATOcean/WaveData")]
    public class AT_OceanWaveData : ScriptableObject
    {
        [BoxGroup("Waves")]
        [Min(1)]
        [MaxValue(10)]
        [Tooltip("ģ��Ĳ��������������������Ӱ�����Ч��,����ȡ1-10֮��")]
        public int waveCount = 5;
        [BoxGroup("Waves")]
        [Min(0.001f)]
        [Tooltip("�����A�������Χ�����ȡֵ��ΧΪ(( 1 - randomness) * amplitudeRand, amplitudeRand)����λΪUnity�ڵĳ��ȵ�λ")]
        public float amplitudeRand = 0.1f;
        [BoxGroup("Waves")]
        [Min(0.001f)]
        [Tooltip("����L�������Χ�����ȡֵ��ΧΪ(( 1 - randomness) * wavelengthRand, wavelengthRand)����λΪUnity�ڵĳ��ȵ�λ")]
        public float wavelengthRand = 2.0f;
        [BoxGroup("Waves")]
        [Min(0.001f)]
        [Tooltip("��ʱ����λphi�������Χ�����ȡֵ��ΧΪ(( 1 - randomness) * phaseRand, phaseRand)")]
        public float phaseRand = 3.0f;
        [BoxGroup("Waves")]
        [Min(0.001f)]
        [MaxValue(1.0f)]
        [Tooltip("����Gerster������ʾ����ƫ�Ʒ��ȣ����ȡֵ�ɲ�����������ƣ����ȡֵ��ΧΪ(( 1 - randomness) *  steepnessRand * maxSteepness , steepnessRand * steepnessRand)")]
        public float steepnessRand = 0.5f;

        [BoxGroup("Waves")]
        [Min(0.001f)]
        [MaxValue(1.0f)]
        [Tooltip("������������ԣ�ȡֵ��Χ[0,1]��1Ϊ��ȫ�����0Ϊ��ȫȷ��")]
        public float randomness = 0.5f;

        public List<SinusoidWaveInfo> waves = new List<SinusoidWaveInfo>();


        [BoxGroup("Waves")]
        [Button]
        public void SetupWaves()
        {
            if (waves.Count > 0)
                waves.Clear();

            for (int i = 0; i < waveCount; ++i)
            {
                var info = new SinusoidWaveInfo();
                info.direction = new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f)).normalized;
                info.amplitude = Random.Range( ( 1.0f - randomness), 1.0f ) * amplitudeRand;
                info.wavelength = Random.Range((1.0f - randomness), 1.0f) * wavelengthRand;
                info.phase = Random.Range((1.0f - randomness), 1.0f) * phaseRand;

                float maxSharpness = 1.0f / (2 * Mathf.PI / info.wavelength * info.amplitude);
                info.steepness = Random.Range((1.0f - randomness), 1.0f) * steepnessRand * maxSharpness;


                waves.Add(info);
            }
        }

    }

}