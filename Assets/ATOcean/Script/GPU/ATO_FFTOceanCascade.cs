using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace ATOcean
{
    public class ATO_FFTOceanCascade
    {

        private int resolution;
        private ComputeShader initialSpectrumShader;
        private ComputeShader timeDependentSpectrumShader;
        private ComputeShader texturesMergerShader;

        const int LOCAL_WORK_GROUPS_X = 8;
        const int LOCAL_WORK_GROUPS_Y = 8;

        private Texture2D gaussianNoise;
        private ComputeBuffer paramsBuffer;
        private RenderTexture initialSpectrum;
        public RenderTexture InitialSpectrum { get { return initialSpectrum; } }
        private RenderTexture precomputedData;
        public RenderTexture PrecomputedData { get { return precomputedData; } }



        private RenderTexture buffer;

        // Kernel IDs:
        int KERNEL_INITIAL_SPECTRUM;
        int KERNEL_CONJUGATE_SPECTRUM;
        int KERNEL_TIME_DEPENDENT_SPECTRUMS;
        int KERNEL_RESULT_TEXTURES;

        private RenderTexture displacement;
        private RenderTexture derivatives;
        private RenderTexture turbulence;


        float lambda;

        public ATO_FFTOceanCascade(int resolution,
                        ComputeShader initialSpectrumShader,
                        ComputeShader timeDependentSpectrumShader,
                        ComputeShader texturesMergerShader,
                        Texture2D gaussianNoise
            )
        {
            this.resolution = resolution;
            this.initialSpectrumShader = initialSpectrumShader;
            this.gaussianNoise = gaussianNoise;
            this.timeDependentSpectrumShader = timeDependentSpectrumShader;
            this.texturesMergerShader = texturesMergerShader;

        }


        public ATO_FFTOceanCascade(int resolution,
                        ComputeShader initialSpectrumShader,
                        Texture2D gaussianNoise
            )
        {
            this.resolution = resolution;
            this.initialSpectrumShader = initialSpectrumShader;
            this.gaussianNoise = gaussianNoise;
            this.timeDependentSpectrumShader = null;
            this.texturesMergerShader = null;

        }

        public void Init()
        {
            KERNEL_INITIAL_SPECTRUM = initialSpectrumShader.FindKernel("CalculateInitialSpectrum");
            KERNEL_CONJUGATE_SPECTRUM = initialSpectrumShader.FindKernel("CalculateConjugatedSpectrum");

            this.initialSpectrum = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);
            this.precomputedData = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);

            this.paramsBuffer = new ComputeBuffer(2, 8 * sizeof(float));

            this.buffer = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);

        }

        public void RunInitials(AT_OceanFFTData wavesSettings, float lengthScale,
                                      float cutoffLow, float cutoffHigh)
        {
            lambda = wavesSettings.lambda;

            KERNEL_INITIAL_SPECTRUM = initialSpectrumShader.FindKernel("CalculateInitialSpectrum");
            // KERNEL_INITIAL_SPECTRUM = initialSpectrumShader.FindKernel("CalculateInitialPhillipsSpectrum");
            KERNEL_CONJUGATE_SPECTRUM = initialSpectrumShader.FindKernel("CalculateConjugatedSpectrum");

            // set up temperary RT and buffer 
            this.initialSpectrum = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);
            this.precomputedData = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);

            this.paramsBuffer = new ComputeBuffer(2, 8 * sizeof(float));

            this.buffer = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);


            // Input parametes 
            initialSpectrumShader.SetInt(RESOLUTION, resolution);
            initialSpectrumShader.SetFloat(LENGTH_SCALE_PROP, lengthScale);
            initialSpectrumShader.SetFloat(CUTOFF_HIGH_PROP, cutoffHigh);
            initialSpectrumShader.SetFloat(CUTOFF_LOW_PROP, cutoffLow);
            wavesSettings.SetParametersToShader(initialSpectrumShader, KERNEL_INITIAL_SPECTRUM, paramsBuffer);

            // calculate H0K and  WavesData
            // H0K is the real and image of H0
            // WavesData is (kx, 1 / kLength, ky , omega)
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, PRECOMPUTED_DATA_PROP, precomputedData);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, NOISE_PROP, gaussianNoise);
            initialSpectrumShader.Dispatch(KERNEL_INITIAL_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0_PROP, initialSpectrum);
            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.Dispatch(KERNEL_CONJUGATE_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            this.paramsBuffer?.Release();

        }

        public Material material;

        public Material InitMaterial( Material refMat , int lod )
        {
            material = new Material(refMat);
            material.name = "OceanGPU_FFT_" + lod;

            return material;
        }

        public void Dispose()
        {
            paramsBuffer?.Release();
        }
        // Update 



        public void CalculateInitials(AT_OceanFFTData wavesSettings, float lengthScale,
                                      float cutoffLow, float cutoffHigh)
        {
            lambda = wavesSettings.lambda;

            // Input parametes 
            initialSpectrumShader.SetInt(RESOLUTION, resolution);
            initialSpectrumShader.SetFloat(LENGTH_SCALE_PROP, lengthScale);
            initialSpectrumShader.SetFloat(CUTOFF_HIGH_PROP, cutoffHigh);
            initialSpectrumShader.SetFloat(CUTOFF_LOW_PROP, cutoffLow);
            wavesSettings.SetParametersToShader(initialSpectrumShader, KERNEL_INITIAL_SPECTRUM, paramsBuffer);

            // calculate H0K and  WavesData
            // H0K is the real and image of H0
            // WavesData is (kx, 1 / kLength, ky , omega)
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, PRECOMPUTED_DATA_PROP, precomputedData);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, NOISE_PROP, gaussianNoise);
            initialSpectrumShader.Dispatch(KERNEL_INITIAL_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0_PROP, initialSpectrum);
            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.Dispatch(KERNEL_CONJUGATE_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }






        // Property IDs
        public static int RESOLUTION = Shader.PropertyToID("Resolution");
        public static int LENGTH_SCALE_PROP = Shader.PropertyToID("LengthScale");
        public static int CUTOFF_HIGH_PROP = Shader.PropertyToID("CutoffHigh");
        public static int CUTOFF_LOW_PROP = Shader.PropertyToID("CutoffLow");


        readonly int NOISE_PROP = Shader.PropertyToID("Noise");
        readonly int H0_PROP = Shader.PropertyToID("H0");
        readonly int H0K_PROP = Shader.PropertyToID("H0K");
        readonly int PRECOMPUTED_DATA_PROP = Shader.PropertyToID("WavesData");
        readonly int TIME_PROP = Shader.PropertyToID("Time");

    }

}