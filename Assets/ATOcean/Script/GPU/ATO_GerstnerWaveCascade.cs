using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{
    public class ATO_GerstnerWaveCascade 
    {
        // setting Data 
        private int resolution;
        private float lengthScale;
        private AT_OceanWaveData waveData;
        
        // Compute Shader
        private ComputeShader gerstnerWaveShader;

        // Global Const 
        const int LOCAL_WORK_GROUPS_X = 8;
        const int LOCAL_WORK_GROUPS_Y = 8;

        // Compute Buffer & Render Texture
        private ComputeBuffer paramsBuffer;
        private RenderTexture displacementRT;
        public RenderTexture DisplacementRT { get { return displacementRT; } }
        private RenderTexture normalRT;
        public RenderTexture NormalRT { get { return normalRT; } }


        // Kernel IDs:
        int KERNEL_CALCULATE_GERSTERN_WAVE;

        public ATO_GerstnerWaveCascade(int resolution, 
            ComputeShader gerstnerWaveShader,
            float lengthScale , 
            AT_OceanWaveData data )
        { 
            this.resolution = resolution;
            this.gerstnerWaveShader = gerstnerWaveShader;
            this.lengthScale = lengthScale;
            this.waveData = data;
        }

        public void RunOnce(
            float time
            )
        {
            Init();
            Run(time);

            paramsBuffer?.Release();
        }

        public void Init( )
        {

            KERNEL_CALCULATE_GERSTERN_WAVE = gerstnerWaveShader.FindKernel("CalculateGerstnerWave");

            this.displacementRT = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);
            this.normalRT = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);

            // setup compute buffer and render texture
            this.paramsBuffer = new ComputeBuffer(waveData.waves.Count, 6 * sizeof(float));
        }

        public void Dispose()
        {
            paramsBuffer?.Release();
            displacementRT?.Release();
            normalRT?.Release();
        }

        public void Run( float time )
        {
            if ( paramsBuffer.count != waveData.waves.Count)
            {
                paramsBuffer.Release();
                paramsBuffer = new ComputeBuffer(waveData.waves.Count, 6 * sizeof(float));
            }

            // Input parameters
            gerstnerWaveShader.SetInt(RESOLUTION_PROP, resolution);
            float unitWidth = lengthScale / (resolution - 1);
            gerstnerWaveShader.SetFloat(UNIT_WIDTH_PROP, unitWidth);
            gerstnerWaveShader.SetFloat(TIME_PROP, time);
            gerstnerWaveShader.SetInt(WAVE_COUNT_PROP, waveData.waves.Count);

            waveData.SetParametersToShader(gerstnerWaveShader, KERNEL_CALCULATE_GERSTERN_WAVE, paramsBuffer);

            // calculate gerstner wave
            gerstnerWaveShader.SetTexture(KERNEL_CALCULATE_GERSTERN_WAVE, DISPLACEMENT_PROP, displacementRT);
            gerstnerWaveShader.SetTexture(KERNEL_CALCULATE_GERSTERN_WAVE, NORMAL_PROP, normalRT);
            gerstnerWaveShader.Dispatch(KERNEL_CALCULATE_GERSTERN_WAVE, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }


        // Property IDs
        public static int RESOLUTION_PROP = Shader.PropertyToID("Resolution");
        public static int UNIT_WIDTH_PROP = Shader.PropertyToID("UnitWidth");
        public static int TIME_PROP = Shader.PropertyToID("Time");
        public static int WAVE_COUNT_PROP = Shader.PropertyToID("WaveCount");
        public static int WAVE_INFOS_PROP = Shader.PropertyToID("WaveInfos");


        readonly int DISPLACEMENT_PROP = Shader.PropertyToID("Displacement");
        readonly int NORMAL_PROP = Shader.PropertyToID("Normal");


    }

}