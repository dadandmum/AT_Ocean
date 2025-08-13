using UnityEngine;
using System.IO;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ATOcean
{
    static public class AT_OceanUtiliy
    {
        public static Texture2D GetNoiseTexture(int size)
        {
            string filename = GaussianNoiseName + "_" + size.ToString() + "x" + size.ToString();
            Texture2D noise = Resources.Load<Texture2D>(NoiseTexturePath + "/" + filename);
            return noise ? noise : GenerateNoiseTexture(size, true);
        }

        public static string NoiseTexturePath = "Assets/ATOcean/PreCompute";
        public static string GaussianNoiseName = "GaussianNoise";

        public static Texture2D GenerateNoiseTexture(int size, bool saveIntoAssetFile)
        {
            Texture2D noise = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
            noise.filterMode = FilterMode.Point;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    // noise.SetPixel(i, j, new Vector4(RandomGaussian(), RandomGaussian() , 0 , 0 ));
                    var randPair = RandomGaussianVariablePair();
                    noise.SetPixel(i, j, new Vector4(randPair.x, randPair.y));
                }
            }
            noise.Apply();

            if (saveIntoAssetFile)
            {
                string filename = GaussianNoiseName + "_" + size.ToString() + "x" + size.ToString();
                string path =  NoiseTexturePath + "/" + filename ;
                SaveTexture2DAsset(noise, path);

            }

//#if UNITY_EDITOR
//            if (saveIntoAssetFile)
//            {
//                string filename = GaussianNoiseName + "_" + size.ToString() + "x" + size.ToString();
//                string path = "Assets/"+ NoiseTexturePath+ "/";
//                AssetDatabase.CreateAsset(noise, path + filename + ".asset");
//                Debug.Log("Texture \"" + filename + "\" was created at path \"" + path + "\".");
//            }
//#endif
            return noise;
        }

        /// <summary>
        /// ���ɱ�׼��̬�ֲ� (��˹�ֲ�) ������� (Box-Muller �任)
        /// </summary>
        /// <returns>��ֵΪ0����׼��Ϊ1�������</returns>
        static public float RandomGaussian()
        {
            float u1 = Random.Range(1e-6f, 1.0f); // ���� log(0)
            float u2 = Random.Range(0.0f, 1.0f);
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        /// <summary>
        /// ����һ�Ի���������ұ�׼��̬�ֲ� (��˹�ֲ�) ������� (Box-Muller ������)
        /// </summary>
        /// <returns>��ֵΪ0����׼��Ϊ1�������</returns>
        static public Vector2 RandomGaussianVariablePair()
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


        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.RGFloat, bool useMips = false)
        {
            RenderTexture rt = new RenderTexture(size, size, 0,
                format, RenderTextureReadWrite.Linear);
            rt.useMipMap = useMips;
            rt.autoGenerateMips = false;
            rt.anisoLevel = 6;
            rt.filterMode = FilterMode.Trilinear;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }


        public static string TempTexturePath = "Assets/ATOcean/Temp";

        public static string H0TextureName = "H0Texture";
        public static string precomputeTextureName = "PrecomputeTexture";


        public static Texture2D SaveTextureToDisk( RenderTexture rt , string folder , string fileName , int resolution )
        {
            var path = folder + "/" + fileName + "_" + resolution + "x" + resolution;
            return SaveTextureToDisk(rt, path);
        }

        /// <summary>
        /// ���� Texture2D ��ָ��·����Assets ��ʼ��������Ѵ����򸲸�
        /// </summary>
        public static Texture2D SaveTexture2DPNG(Texture2D texture, string path)
        {
            var assetPath = path + ".png";

            // ȷ��Ŀ¼����
            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // �� Texture2D ����Ϊ PNG
            byte[] pngData = texture.EncodeToPNG();

            // ֱ��д���ļ������ǣ�
            File.WriteAllBytes(fullPath, pngData);
            Debug.Log($"Texture saved to: {fullPath}");

#if UNITY_EDITOR
            TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false;
            }

            // ǿ��ˢ�� AssetDatabase
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // ��ȡ��Դ���󣨿����ں���ѡ�У�
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return savedTex;

#endif

            return texture;

            //if (savedTex != null)
            //{
            //    EditorGUIUtility.PingObject(savedTex);
            //}
        }

        public static Texture2D SaveTexture2DAsset(Texture2D texture, string path)
        {
            var assetPath = path + ".asset";
            // ȷ��Ŀ¼����
            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(texture, assetPath);


            // ��ȡ��Դ���󣨿����ں���ѡ�У�
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return savedTex;
#endif

            return texture;
        }



        public static Texture2D SaveTextureToDisk(RenderTexture rt, string path)
        {
            // Convert RenderTexture to Texture2D
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            return SaveTexture2DPNG(tex, path);
        }


    }

}