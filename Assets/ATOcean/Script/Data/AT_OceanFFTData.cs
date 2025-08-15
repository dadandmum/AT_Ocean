using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{
    public struct SpectrumSettings
    {
        public float scale;
        public float angle;
        public float spreadBlend;
        public float swell;
        public float alpha;
        public float peakOmega;
        public float gamma;
        public float shortWavesFade;
    }

    [System.Serializable]
    public struct DisplaySpectrumSettings
    {
        [Range(0, 1)]
        public float scale;
        public float windSpeed;
        public Vector3 windDirection;
        public float fetch;
        [Range(0, 1)]
        public float spreadBlend;
        [Range(0, 1)]
        public float swell;
        public float peakEnhancement;
        public float shortWavesFade;
    }

    // for more infomation about JSONSWAP Spectrums, See
    // https://www.cg.tuwien.ac.at/research/publications/2018/GAMPER-2018-OSG/GAMPER-2018-OSG-thesis.pdf
    // Gamper T. 2018. Ocean Surface Generation and Rendering, Vienna University of Technology, the Faculty of Informatics.
    // Another paper:
    // Christopher J. Horvath. 2015. Empirical directional wave spectra for computer graphics. In Proceedings of the 2015 Symposium on Digital Production (DigiPro '15). Association for Computing Machinery, New York, NY, USA, 29¨C39. DOI:10.1145/2791261.2791267
    // 
    [CreateAssetMenu(fileName = "ATO_JSONSWAP_SpecData", menuName = "ATOcean/JSONSWAP_SpecData")]
    public class AT_OceanFFTData : ScriptableObject
    {
        [BoxGroup("ATOcean")]
        [BoxGroup("ATOcean/Setting")]
        public float g=9.81f;
        [BoxGroup("ATOcean/Setting")]
        public float depth=500f;
        [BoxGroup("ATOcean/Setting")]
        [Range(0, 1)]
        public float lambda=1f;
        [BoxGroup("ATOcean/Random")]
        public float depthRand = 1000f;
        [BoxGroup("ATOcean/Random")]
        public float windSpeedRand = 10f;
        [BoxGroup("ATOcean/Random")]
        [Range(0,1.0f)]
        public float spreadRand = 0.1f;
        [BoxGroup("ATOcean/Random")]
        [Range(0, 1.0f)]
        public float swellRand = 0.3f;
        [BoxGroup("ATOcean/Random")]
        [Range(0, 1.0f)]
        public float swellIntensityRand = 0.5f;


        [BoxGroup("ATOcean/Setting")]
        public DisplaySpectrumSettings local;

        [BoxGroup("ATOcean/Setting")]
        public DisplaySpectrumSettings swell;

        SpectrumSettings[] spectrums = new SpectrumSettings[2];

        [BoxGroup("ATOcean/Random")]
        [Button]
        public void RandomizeData()
        {
            g = 9.81f;
            depth = Random.Range(0.5f, 1f) * depthRand;
            lambda = 1.0f;
            local.scale = 1.0f;
            local.windSpeed = Random.Range(0.5f, 1.0f) * windSpeedRand;
            {
                var randDir = Random.insideUnitCircle;
                local.windDirection = new Vector3(randDir.x, 0, randDir.y);
            }

            local.fetch = 100000f;
            local.spreadBlend = Random.Range(1.0f - spreadRand, 1.0f);
            local.swell = Random.Range(0 , 1.0f) * swellRand;
            local.peakEnhancement = 3.3f;
            local.shortWavesFade = 0.01f;


            swell.scale = Random.Range(0, 1.0f) * swellIntensityRand;
            swell.windSpeed = Random.Range(0.5f, 1.0f) * windSpeedRand;
            {
                var randDir = Random.insideUnitCircle;
                swell.windDirection = new Vector3(randDir.x, 0, randDir.y);
            }
            swell.fetch = 300000f;
            swell.spreadBlend = Random.Range(1.0f - spreadRand, 1.0f);
            swell.swell = 1.0f;
            local.peakEnhancement = 3.3f;
            local.shortWavesFade = 0.01f;

        }

        public void SetParametersToShader(ComputeShader shader, int kernelIndex, ComputeBuffer paramsBuffer)
        {
            shader.SetFloat(G_PROP, g);
            shader.SetFloat(DEPTH_PROP, depth);

            FillSettingsStruct(local, ref spectrums[0]);
            FillSettingsStruct(swell, ref spectrums[1]);

            paramsBuffer.SetData(spectrums);
            shader.SetBuffer(kernelIndex, SPECTRUMS_PROP, paramsBuffer);
        }

        void FillSettingsStruct(DisplaySpectrumSettings display, ref SpectrumSettings settings)
        {
            settings.scale = display.scale;
            settings.angle = Mathf.Atan2(display.windDirection.z, display.windDirection.x);
            settings.spreadBlend = display.spreadBlend;
            settings.swell = Mathf.Clamp(display.swell, 0.01f, 1);
            settings.alpha = JonswapAlpha(g, display.fetch, display.windSpeed);
            settings.peakOmega = JonswapPeakFrequency(g, display.fetch, display.windSpeed);
            settings.gamma = display.peakEnhancement;
            settings.shortWavesFade = display.shortWavesFade;
        }

        float JonswapAlpha(float g, float fetch, float windSpeed)
        {
            return 0.076f * Mathf.Pow(g * fetch / windSpeed / windSpeed, -0.22f);
        }

        float JonswapPeakFrequency(float g, float fetch, float windSpeed)
        {
            return 22.0f * Mathf.Pow(windSpeed * fetch / g / g, -0.33f);
        }

        readonly int G_PROP = Shader.PropertyToID("GravityAcceleration");
        readonly int DEPTH_PROP = Shader.PropertyToID("Depth");
        readonly int SPECTRUMS_PROP = Shader.PropertyToID("Spectrums");

    }

}