using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;

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

        public static string NoiseTexturePath = "Assets/ATOcean/Data/NoiseTexture";
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

            return noise;
        }


        /// <summary>
        /// 生成标准正态分布 (高斯分布) 的随机数 (Box-Muller 变换)
        /// </summary>
        /// <returns>均值为0，标准差为1的随机数</returns>
        static public float RandomGaussian()
        {
            float u1 = Random.Range(1e-6f, 1.0f);  
            float u2 = Random.Range(0.0f, 1.0f);
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        }

        /// <summary>
        /// 生成一对互相独立，且标准正态分布 (高斯分布) 的随机数 (Box-Muller 极坐标)
        /// </summary>
        /// <returns>均值为0，标准差为1的随机数</returns>
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


        public static Texture2D SaveTextureToDisk(RenderTexture rt, string path)
        {
            // Convert RenderTexture to Texture2D
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            return SaveTexture2D2EXR(tex, path);
        }
        public static Texture2D SaveTextureToDisk( RenderTexture rt , string folder , string fileName , int resolution )
        {
            var path = folder + "/" + fileName + "_" + resolution + "x" + resolution;
            return SaveTextureToDisk(rt, path);
        }
    
         /// <summary>
        /// 保存 Texture2D 到指定路径（Assets 开始），如果已存在则覆盖
        /// </summary>
        public static Texture2D SaveTexture2DPNG(Texture2D texture, string path)
        {
            var assetPath = path + ".png";

            // 确保目录存在
            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 将 Texture2D 编码为 PNG
            byte[] pngData = texture.EncodeToPNG();

            // 直接写入文件（覆盖）
            File.WriteAllBytes(fullPath, pngData);
            Debug.Log($"Texture saved to: {fullPath}");

#if UNITY_EDITOR

            // 强制刷新 AssetDatabase
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
            // if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.compressionQuality = 100;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }

            // 获取资源对象（可用于后续选中）
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return savedTex;

#else

            return texture;
#endif
            //if (savedTex != null)
            //{
            //    EditorGUIUtility.PingObject(savedTex);
            //}
        }



        /// <summary>
        /// 将 Texture2D 保存为 EXR 文件到指定路径，如果已存在则覆盖
        /// </summary>
        /// <param name="texture2D">要保存的 Texture2D 对象</param>
        /// <param name="path">保存路径（不包含文件扩展名）</param>
        /// <returns>如果在编辑器模式下，返回保存后的 Texture2D 资源；否则返回 null</returns>
        public static Texture2D SaveTexture2D2EXR(Texture2D texture2D, string path)
        {
            try
            {
                // 拼接完整的 EXR 文件路径
                var assetPath = path + ".exr";
                // 获取文件的完整系统路径
                string fullPath = Path.GetFullPath(assetPath);
                // 获取文件所在的目录路径
                string directory = Path.GetDirectoryName(fullPath);
                // 检查目录是否存在，如果不存在则创建
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 将 Texture2D 编码为 EXR 格式的字节数据，输出为浮点数格式
                byte[] exrData = texture2D.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);

                // 将编码后的字节数据写入文件，覆盖已存在的文件
                File.WriteAllBytes(fullPath, exrData);
                // 打印保存成功的日志信息
                Debug.Log($"Texture saved to: {fullPath}");

#if UNITY_EDITOR
                // 强制刷新 AssetDatabase，确保新导入的资源被识别
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                // 获取 EXR 文件的纹理导入器
                TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
                // if (importer != null)
                {
                    // 设置纹理不使用 sRGB 颜色空间
                    importer.sRGBTexture = false;
                    // 设置纹理压缩质量为最高
                    importer.compressionQuality = 100;
                    // 设置纹理不进行压缩
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                }
                // 从指定路径加载保存后的 Texture2D 资源
                Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                return savedTex;
#endif 
                // 在非编辑器模式下返回 null
                return null;
            }
            catch (System.Exception e)
            {
                // 打印保存失败的错误日志
                Debug.LogError($"Failed to save HDR RenderTexture: {e.Message}");
                return null;
            }
        }
        public static Texture2D SaveTexture2DAsset(Texture2D texture, string path)
        {
            var assetPath = path + ".asset";
            
            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(texture, assetPath);


            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return savedTex;
#endif

            return texture;
        }






        public static string WaveDataPath = "Assets/ATOcean/Data/WaveData";

        // Generate a new wave Data  
        public static AT_OceanWaveData GernateWaveData( string name_prefix = "")
        {
#if UNITY_EDITOR
            AT_OceanWaveData asset = ScriptableObject.CreateInstance<AT_OceanWaveData>();
            asset.SetupWaves();

            var assetPath = WaveDataPath + "/" + "WaveData_" + name_prefix + Random.Range(100, 999) + ".asset";

            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"ScriptableObject saved to: {assetPath}");
            return asset;
#endif 

            return null;
        }


    }

}