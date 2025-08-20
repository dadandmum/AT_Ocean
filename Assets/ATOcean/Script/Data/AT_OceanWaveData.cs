using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace ATOcean
{

    [System.Serializable]
    public class SinusoidWaveInfo
    {
        public Vector3 direction; // only consider (x,0,z)
        public float amplitude;  // A = amplitude
        public float wavelength; // omega = 2*pi/wavelength
        public float phaseFrequency; // time offset speed
        public float steepness; // for Gerstner Wave, should be < 1 / (w * A)

    }


    public struct SinusoidWaveInfoBuffer
    {
        public float dirX; 
        public float dirZ;
        public float amplitude;  // A = amplitude
        public float wavelength; // omega = 2*pi/wavelength
        public float phaseFrequency; // time offset speed
        public float steepness; // for Gerstner Wave, should be < 1 / (w * A)

    }

    [CreateAssetMenu(fileName = "ATO_WaveData", menuName = "ATOcean/WaveData")]
    public class AT_OceanWaveData : ScriptableObject
    {
        [BoxGroup("Waves")]
        [Range(1, 20)]
        [InfoBox("模拟的波的数量，波数量过多会影响计算效率,建议取1-10之间")]
        public int waveCount = 5; // the number of waves to simulate
        [BoxGroup("Waves/Amplitude",centerLabel:true)]
        [Title("Amplitude")]
        [Min(0.001f)]
        [InfoBox("波振幅A的随机范围，随机取值范围为(( 1 - randomness) * amplitudeRand, amplitudeRand)，单位为Unity内的长度单位")]
        public float amplitudeRand = 0.1f; // the random range of the wave amplitude
        [BoxGroup("Waves/waveLength", centerLabel: true)]
        [Title("WaveLength")]
        [Min(0.001f)]
        [InfoBox("波长L的随机范围，随机取值范围为(( 1 - randomness) * wavelengthRand, wavelengthRand)，单位为Unity内的长度单位")]
        public float wavelengthRand = 2.0f; // the random range of the wave wavelength


        [BoxGroup("Waves/PhaseFrequency", centerLabel: true)]
        [Title("PhaseFrequency")]
        [Min(0.001f)]
        [InfoBox("波时间相位变化速率phi的随机范围，随机取值范围为(( 1 - randomness) * phaseRand, phaseRand)")]
        public float phaseRand = 3.0f; // the random range of the wave phase
        [BoxGroup("Waves")]
        [Range(0.001f, 1.0f)]
        [InfoBox("用于Gerster波，表示横向偏移幅度，最大取值由波长和振幅限制，随机取值范围为(( 1 - randomness) *  steepnessRand * maxSteepness , steepnessRand * steepnessRand)")]
        public float steepnessRand = 0.5f;  // the random range of the wave steepness

        [BoxGroup("Waves")]
        [Range(0.001f, 1.0f)]
        [InfoBox("波参数的随机性，取值范围[0,1]，1为完全随机，0为完全确定")]
        public float randomness = 0.5f; // the randomness of the wave parameters, range [0,1], 1 is completely random, 0 is completely determined


        [BoxGroup("Waves/Amplitude")]
        [InfoBox("是否使用振幅因子，若使用，振幅会根据振幅因子自动变小（第i个波的振幅会乘上振幅因子的第i次方）")]
        public bool useAmplitudeFactor = true;
        [BoxGroup("Waves/Amplitude")]
        [ShowIf("useAmplitudeFactor")]
        [InfoBox("振幅因子，用于调整波的振幅，取值范围[0,1]，1为不调整，0为完全调整")]
        [Range(0.001f, 1.0f)]
        public float amplitudeFactor = 0.8f;

        [BoxGroup("Waves/waveLength")]
        [InfoBox("是否使用频率因子，若使用，波长会根据波长因子自动变小（第i个波的波长会乘上波长因子的第i次方）")]
        public bool useWaveLengthFactor = true;
        [BoxGroup("Waves/waveLength")]
        [ShowIf("useWaveLengthFactor")]
        [InfoBox("波长因子，用于调整波的波长，取值范围[0,1]，1为不调整，0为完全调整")]
        [Range(0.001f, 1.0f)]
        public float waveLengthFactor = 0.8f;

        [BoxGroup("Waves/PhaseFrequency")]
        [InfoBox("是否使用相位因子，若使用，相位会根据相位因子自动变小（第i个波的相位会乘上相位因子的第i次方）")]
        public bool usePhaseFrequencyFactor = true;
        [BoxGroup("Waves/PhaseFrequency")]
        [ShowIf("usePhaseFrequencyFactor")]
        [InfoBox("相位因子，用于调整波的相位，取值范围[0,1]，1为不调整，0为完全调整")]
        [Range(0.001f, 1.0f)]
        public float phaseFrequencyFactor = 0.8f;

        [BoxGroup("Waves")]
        public List<SinusoidWaveInfo> waves = new List<SinusoidWaveInfo>();


        [BoxGroup("Waves")]
        [Tooltip("根据配置随机生成波参数")]
        [Button]
        // generate waves according to the configuration and save them to the waves list
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
                info.phaseFrequency = Random.Range((1.0f - randomness), 1.0f) * phaseRand;
                
                if ( useAmplitudeFactor)
                {
                    info.amplitude *= Mathf.Pow(amplitudeFactor, i);
                }

                if (useWaveLengthFactor)
                {
                    info.wavelength *= Mathf.Pow(waveLengthFactor, i);
                }

                if (usePhaseFrequencyFactor)
                {
                    info.phaseFrequency *= Mathf.Pow(phaseFrequencyFactor, i);
                }


                float maxSharpness = 1.0f / (2 * Mathf.PI / info.wavelength * info.amplitude);
                info.steepness = Random.Range((1.0f - randomness), 1.0f) * steepnessRand * maxSharpness;


                waves.Add(info);
            }
        }

        void ConvertData( SinusoidWaveInfo input , ref SinusoidWaveInfoBuffer output )
        {

            output.dirX = input.direction.x;
            output.dirZ = input.direction.z;
            output.amplitude = input.amplitude;
            output.wavelength = input.wavelength;
            output.phaseFrequency = input.phaseFrequency;
            output.steepness = input.steepness;

        }

        public void SetParametersToShader(ComputeShader shader, int kernelIndex, ComputeBuffer paramsBuffer)
        {
            shader.SetInt(WAVE_COUNT_PROP, waves.Count);
          

            SinusoidWaveInfoBuffer[] wavesInfo = new SinusoidWaveInfoBuffer[waves.Count];

            for (int i = 0; i < waves.Count; ++i)
            {
                ConvertData(waves[i], ref wavesInfo[i]);
            }

            paramsBuffer.SetData(wavesInfo);
            shader.SetBuffer(kernelIndex, WAVE_INFOS_PROP, paramsBuffer);
        }

        readonly int WAVE_COUNT_PROP = Shader.PropertyToID("WaveCount");
        readonly int WAVE_INFOS_PROP = Shader.PropertyToID("WaveInfos");

    }

}