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



        private RenderTexture DxDz;
        private RenderTexture DyDxz;
        private RenderTexture DyxDyz;
        private RenderTexture DxxDzz;

        private ATO_FastFourierTransform fft;

        private RenderTexture buffer;

        // Kernel IDs:
        int KERNEL_INITIAL_SPECTRUM;
        int KERNEL_CONJUGATE_SPECTRUM;
        int KERNEL_TIME_DEPENDENT_SPECTRUMS;
        int KERNEL_RESULT_TEXTURES;

        private RenderTexture displacement;
        public RenderTexture Displacement { get { return displacement; } }
        private RenderTexture derivatives;
        public RenderTexture Derivatives { get { return derivatives; } }
        private RenderTexture turbulence;
        public RenderTexture Turbulence { get { return turbulence; } }


        float lambda;

        public ATO_FFTOceanCascade(int resolution,
                        ComputeShader initialSpectrumShader,
                        ComputeShader timeDependentSpectrumShader,
                        ComputeShader texturesMergerShader,
                        ATO_FastFourierTransform fft,

                        Texture2D gaussianNoise
            )
        {
            this.resolution = resolution;
            this.initialSpectrumShader = initialSpectrumShader;
            this.gaussianNoise = gaussianNoise;
            this.timeDependentSpectrumShader = timeDependentSpectrumShader;
            this.texturesMergerShader = texturesMergerShader;
            this.fft = fft;
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
            this.displacement = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);
            this.derivatives = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat, true);
            this.turbulence = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat, true);

            this.paramsBuffer = new ComputeBuffer(2, 8 * sizeof(float));

            this.buffer = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.RGFloat);
            this.DxDz = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.RGFloat);
            this.DyDxz = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.RGFloat);
            this.DyxDyz = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.RGFloat);
            this.DxxDzz = AT_OceanUtiliy.CreateRenderTexture(resolution, RenderTextureFormat.RGFloat);

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
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, PRECOMPUTED_DATA_PROP, precomputedData);
            initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, NOISE_PROP, gaussianNoise);
            initialSpectrumShader.Dispatch(KERNEL_INITIAL_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            // WavesData is (kx, 1 / kLength, ky , omega)
            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0_PROP, initialSpectrum);
            initialSpectrumShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0K_PROP, buffer);
            initialSpectrumShader.Dispatch(KERNEL_CONJUGATE_SPECTRUM, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            this.paramsBuffer?.Release();

        }

        public void CalculateWavesAtTime(float time)
        {
            // Calculating complex amplitudes
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, Dx_Dz_PROP, DxDz);
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, Dy_Dxz_PROP, DyDxz);
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, Dyx_Dyz_PROP, DyxDyz);
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, Dxx_Dzz_PROP, DxxDzz);
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, H0_PROP, initialSpectrum);
            timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUMS, PRECOMPUTED_DATA_PROP, precomputedData);
            timeDependentSpectrumShader.SetFloat(TIME_PROP, time);
            timeDependentSpectrumShader.Dispatch(KERNEL_TIME_DEPENDENT_SPECTRUMS, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            // Calculating IFFTs of complex amplitudes
            fft.IFFT2D(DxDz, buffer, true, false, true);
            fft.IFFT2D(DyDxz, buffer, true, false, true);
            fft.IFFT2D(DyxDyz, buffer, true, false, true);
            fft.IFFT2D(DxxDzz, buffer, true, false, true);


            // Filling displacement and normals textures
            texturesMergerShader.SetFloat("DeltaTime", Time.deltaTime);

            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, Dx_Dz_PROP, DxDz);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, Dy_Dxz_PROP, DyDxz);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, Dyx_Dyz_PROP, DyxDyz);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, Dxx_Dzz_PROP, DxxDzz);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, DISPLACEMENT_PROP, displacement);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, DERIVATIVES_PROP, derivatives);
            texturesMergerShader.SetTexture(KERNEL_RESULT_TEXTURES, TURBULENCE_PROP, turbulence);
            texturesMergerShader.SetFloat(LAMBDA_PROP, lambda);
            texturesMergerShader.Dispatch(KERNEL_RESULT_TEXTURES, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);

            derivatives.GenerateMips();
            turbulence.GenerateMips();

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
                                      float cutoffLow, float cutoffHigh )
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

        readonly int Dx_Dz_PROP = Shader.PropertyToID("Dx_Dz");
        readonly int Dy_Dxz_PROP = Shader.PropertyToID("Dy_Dxz");
        readonly int Dyx_Dyz_PROP = Shader.PropertyToID("Dyx_Dyz");
        readonly int Dxx_Dzz_PROP = Shader.PropertyToID("Dxx_Dzz");
        readonly int LAMBDA_PROP = Shader.PropertyToID("Lambda");


        readonly int DISPLACEMENT_PROP = Shader.PropertyToID("Displacement");
        readonly int DERIVATIVES_PROP = Shader.PropertyToID("Derivatives");
        readonly int TURBULENCE_PROP = Shader.PropertyToID("Turbulence"); 
    
    }

}