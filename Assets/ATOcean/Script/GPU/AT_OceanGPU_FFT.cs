using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor;
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

        [BoxGroup("AT_Ocean/Settings")]
        [InlineEditor]
        public AT_OceanFFTData waveSettings;

        [BoxGroup("AT_Ocean/Cascade")]
        [ReadOnly]
        public List<ATO_FFTOceanCascade> waveCascade;


        [BoxGroup("AT_Ocean/NoiseTexture")]
        public Texture2D noiseTexture;

        //[SerializeField]
        //float lengthScale0 = 250;
        //[SerializeField]
        //float lengthScale1 = 17;
        //[SerializeField]
        //float lengthScale2 = 5;


        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader initialSpectrumShader;
        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader timeDependentSpectrumShader;
        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader texturesMergerShader;

        [Button]
        public void GenerateNoiseTexture()
        {
            var noise = AT_OceanUtiliy.GenerateNoiseTexture(renderResolution, true);
            noiseTexture = noise;

            EditorGUIUtility.PingObject(noise);
        }


        public static string TempTexturePath = "Assets/ATOcean/Result/JONSWAP";

        public static string H0TextureName = "H0Texture";
        public static string precomputeTextureName = "PrecomputeTexture";

        [Button]
        public void GenerateH0Texture()
        {
            var gaussianNoise = noiseTexture;
            // init cascade 
            var tempCascade = new ATO_FFTOceanCascade(renderResolution, 
                initialSpectrumShader,
                gaussianNoise);


            // run and save data to rt 

            float lengthScale1 = renderCascades[1].lengthScale;
            float lengthScale0 = renderCascades[0].lengthScale;
            float boundary1 = 2 * Mathf.PI / lengthScale1 * 6f;
            tempCascade.RunInitials(waveSettings, lengthScale0, 0.0001f, boundary1);


            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.InitialSpectrum,
                TempTexturePath,
                H0TextureName,
                renderResolution
                );

            EditorGUIUtility.PingObject(tex2D);


            AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.PrecomputedData,
                TempTexturePath,
                precomputeTextureName,
                renderResolution
                );
        }


        public override void InitMesh()
        {
            base.InitMesh();

            InitRender();
        }


        void InitialiseCascades()
        {
            if (waveCascade.Count < 3)
                return;

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

                float boundaryIn = i == cascadeLevel - 1 ? 0.0001f : 2.0f * Mathf.PI / renderCascades[i].lengthScale * 6f;
                float boundaryOut = i == 0 ? 9999f : 2.0f * Mathf.PI / renderCascades[i + 1].lengthScale * 6f;

                waveCascade[i].CalculateInitials(waveSettings, renderCascades[i].lengthScale, boundaryIn, boundaryOut);
                
                Shader.SetGlobalFloat("LengthScale" + i, renderCascades[i].lengthScale);
            }
        }


        override public void InitRender()
        {
            waveCascade = new List<ATO_FFTOceanCascade>();


            // var gaussianNoise = AT_OceanUtiliy.GetNoiseTexture(resolution);

            var gaussianNoise = noiseTexture;

            for (int i = 0; i < cascadeLevel; ++i)
            {
                var lod = new ATO_FFTOceanCascade(
                    renderResolution,
                    initialSpectrumShader,
                    timeDependentSpectrumShader,
                    texturesMergerShader,
                    gaussianNoise
                    );
                renderCascades[i].material = lod.InitMaterial(material,i);
                waveCascade.Add(lod);
            }

            base.InitRender();
        }



        #region Update




        #endregion

    }

}