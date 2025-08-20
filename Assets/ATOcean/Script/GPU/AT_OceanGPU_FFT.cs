using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

namespace ATOcean
{
    public class AT_OceanGPU_FFT : AT_OceanGPU
    {

        //[BoxGroup("AT_Ocean")]
        //[BoxGroup("AT_Ocean/Settings")]
        //public int renderResolution = 256;


        //[BoxGroup("AT_Ocean")]
        //[BoxGroup("AT_Ocean/Settings")]
        //[ReadOnly]
        //public int cascadeLevel = 3;

        [BoxGroup("AT_Ocean/FFT")]
        [InlineEditor]
        public AT_OceanFFTData waveSettings;

        [BoxGroup("AT_Ocean/FFT")]
        [ReadOnly]
        public List<ATO_FFTOceanCascade> waveCascade;


        [BoxGroup("AT_Ocean/FFT/Noise")]
        public Texture2D noiseTexture;

        //[SerializeField]
        //float lengthScale0 = 250;
        //[SerializeField]
        //float lengthScale1 = 17;
        //[SerializeField]
        //float lengthScale2 = 5;


        [BoxGroup("AT_Ocean/FFT/ComputeShader")]
        [SerializeField]
        ComputeShader initialSpectrumShader;
        [BoxGroup("AT_Ocean/FFT/ComputeShader")]
        [SerializeField]
        ComputeShader timeDependentSpectrumShader;
        [BoxGroup("AT_Ocean/FFT/ComputeShader")]
        [SerializeField]
        ComputeShader texturesMergerShader;
        [BoxGroup("AT_Ocean/FFT/ComputeShader")]
        [SerializeField]
        ComputeShader fftShader;

        [BoxGroup("AT_Ocean/FFT/ComputeShader")]
        [HideInInspector]
        ATO_FastFourierTransform fft;


        [BoxGroup("AT_Ocean/FFT/Noise")]
        [Button("Generate Noise Texture" , ButtonSizes.Large)]
        [GUIColor(0.8f,0.8f,0.2f)]
        public void GenerateNoiseTexture()
        {
            var noise = AT_OceanUtiliy.GenerateNoiseTexture(renderResolution, true);
            noiseTexture = noise;

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(noise);
#endif

        }


        [BoxGroup("AT_Ocean/FFT/Output")]
        public string TempTexturePath = "Assets/ATOcean/Output/FFT";

        public static string H0TextureName = "H0Texture";
        public static string specPrecomputeTextureName = "SpecPrecomputeTexture";
        public static string FFTPrecomputeTextureName = "FFTPrecomputeTexture";

        [BoxGroup("AT_Ocean/FFT/Output")]
        public int outputCascadeLevel = 0;


        [BoxGroup("AT_Ocean/FFT/Output")]
        [Button("Generate H0 Texture" , ButtonSizes.Large)]
        [GUIColor(0.8f,0.8f,0.2f)]
        public void GenerateH0Texture()
        {
            var gaussianNoise = noiseTexture;
            // init cascade 
            var tempCascade = new ATO_FFTOceanCascade(renderResolution, 
                initialSpectrumShader,
                gaussianNoise);


            // run and save data to rt 

            float lengthScale = renderCascades[outputCascadeLevel].lengthScale;
            float boundaryIn = 0;
            float boundaryOut = 0;
            CalculateBoundary(outputCascadeLevel, out boundaryIn, out boundaryOut);

            tempCascade.RunInitials(waveSettings, lengthScale, boundaryIn, boundaryOut);


            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.InitialSpectrum,
                TempTexturePath,
                H0TextureName,
                renderResolution
                );

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(tex2D);
#endif

        }

        [BoxGroup("AT_Ocean/FFT/Output")]
        [Button("Generate Specctrum Precompute Texture" , ButtonSizes.Large)]
        [GUIColor(0.8f,0.8f,0.2f)] 
        public void GeneratePrecomputeTexture()
        {
            var gaussianNoise = noiseTexture;
            // init cascade 
            var tempCascade = new ATO_FFTOceanCascade(renderResolution, 
                initialSpectrumShader,
                gaussianNoise);
            // run and save data to rt 

            float lengthScale = renderCascades[outputCascadeLevel].lengthScale;
            float boundaryIn = 0;
            float boundaryOut = 0;
            CalculateBoundary(outputCascadeLevel, out boundaryIn, out boundaryOut);
            tempCascade.RunInitials(waveSettings, lengthScale, boundaryIn, boundaryOut);

            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.PrecomputedData,
                TempTexturePath,
                specPrecomputeTextureName,
                renderResolution
                );

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(tex2D);
#endif

        }



        [BoxGroup("AT_Ocean/FFT/Output")]
        [Button("Generate FFT Precompute Texture" , ButtonSizes.Large)]
        [GUIColor(0.8f,0.8f,0.2f)]
        public void GenerateFFTPrecomputeTexture()
        {
            fft = new ATO_FastFourierTransform(renderResolution, fftShader);
        
            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                fft.PrecomputedData,
                TempTexturePath,
                FFTPrecomputeTextureName,
                renderResolution
                );

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(tex2D);
#endif
        }



        public void CalculateBoundary( int level, out float boundaryIn, out float boundaryOut ) 
        {
            level = Mathf.Clamp(level, 0, cascadeLevel - 1);

            boundaryIn = level == cascadeLevel - 1 ? 0.0001f : 2.0f * Mathf.PI / renderCascades[level].lengthScale * 12f;
            boundaryOut = level == 0 ? 9999f : 2.0f * Mathf.PI / renderCascades[level - 1].lengthScale * 12f;

        }


        public void InitialiseCascades()
        {
            //float boundary1 = 2 * Mathf.PI / lengthScale1 * 6f;
            //float boundary2 = 2 * Mathf.PI / lengthScale2 * 6f;
            //waveCascade[0].CalculateInitials(waveSettings, lengthScale0, 0.0001f, boundary1);
            //waveCascade[1].CalculateInitials(waveSettings, lengthScale1, boundary1, boundary2);
            //waveCascade[2].CalculateInitials(waveSettings, lengthScale2, boundary2, 9999);

            //Shader.SetGlobalFloat("LengthScale0", lengthScale0);
            //Shader.SetGlobalFloat("LengthScale1", lengthScale1);
            //Shader.SetGlobalFloat("LengthScale2", lengthScale2);


            for ( int i = 0; i < cascadeLevel; ++i)
            {
                //float boundaryIn = i == 0 ? 0.0001f: 2.0f * Mathf.PI / renderCascades[i].lengthScale * 6f;
                //float boundaryOut = i == cascadeLevel - 1 ? 9999f : 2.0f * Mathf.PI / renderCascades[i+1].lengthScale * 6f;

                // float boundaryIn = i == cascadeLevel - 1 ? 0.0001f : 2.0f * Mathf.PI / renderCascades[i].lengthScale * 6f;
                // float boundaryOut = i == 0 ? 9999f : 2.0f * Mathf.PI / renderCascades[i + 1].lengthScale * 6f;
                float boundaryIn = 0;
                float boundaryOut = 0;
                CalculateBoundary(i, out boundaryIn, out boundaryOut);

                waveCascade[i].CalculateInitials(waveSettings, renderCascades[i].lengthScale, boundaryIn, boundaryOut);
                
                Shader.SetGlobalFloat("LengthScale" + i, renderCascades[i].lengthScale);
            }
        }

        public void LinkMaterialWithCascade()
        {
            for (int i = 0; i < renderCascades.Count; i++)
            {
                for (int d = 0; d < waveCascade.Count; ++d)
                {
                    renderCascades[i].material.SetTexture("_Displacement_c" + d , waveCascade[d].Displacement);
                    renderCascades[i].material.SetTexture("_Derivatives_c" + d , waveCascade[d].Derivatives);
                    renderCascades[i].material.SetTexture("_Turbulence_c" + d , waveCascade[d].Turbulence);
                }
            }


        }



        override public void InitRender()
        {
            base.InitRender();

            fft = new ATO_FastFourierTransform(renderResolution, fftShader);

            waveCascade = new List<ATO_FFTOceanCascade>();
            // var gaussianNoise = AT_OceanUtiliy.GetNoiseTexture(resolution);
            var gaussianNoise = noiseTexture;

            for (int i = 0; i < cascadeLevel; ++i)
            {
                var cascade = new ATO_FFTOceanCascade(
                    renderResolution,
                    initialSpectrumShader,
                    timeDependentSpectrumShader,
                    texturesMergerShader,
                    fft,
                    gaussianNoise
                    );
                cascade.Init();
                renderCascades[i].material = cascade.InitMaterial(material,i);
                waveCascade.Add(cascade);
            }

            InitialiseCascades();
            LinkMaterialWithCascade();

        }


        #region Visual

        public override void InitVisual()
        {
            base.InitVisual();

            if ( visual != null )
            {
                for ( int i = 0; i < waveCascade.Count; i++)
                {
                    visual.AddImageRef(
                        waveCascade[i].Displacement,
                        "Displacement",
                        i,
                        renderCascades[i].renderResolution
                        );
                    visual.AddImageRef(
                        waveCascade[i].Derivatives,
                        "Derivatives",
                        i,
                        renderCascades[i].renderResolution
                        );
                    visual.AddImageRef(
                        waveCascade[i].Turbulence,
                        "Turbulence",
                        i,
                        renderCascades[i].renderResolution
                        );

                }
            }


        }

        #endregion Visual 



        #region Update

        public override void UpdateRender(float t , float dt)
        {
            base.UpdateRender(t, dt);

            for (int i = 0; i < cascadeLevel; ++i)
            {
                waveCascade[i].CalculateWavesAtTime(t);
            }


        }


        #endregion Update

    }

}