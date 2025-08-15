using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ATOcean
{
    public class ATO_Visual_ImageDisplay : MonoBehaviour
    {
        public RawImage image;
        public Text text;

        public void Init(RenderTexture rt , string RTName , int lod , int resolution )
        {
            image.texture = rt;
            text.text = RTName + " C" + lod + " " + resolution + "x" + resolution;
        }


    }
}
