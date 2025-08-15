using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace ATOcean
{
    public class ATO_Visual : MonoBehaviour
    {
        public Transform root;
        public Transform col0;

        public GameObject imageDisplayPrefab;
        [ReadOnly]
        public List<ATO_Visual_ImageDisplay> imageDisplays = new List<ATO_Visual_ImageDisplay>();

        public void Clear()
        {
            foreach (var imageDisplay in imageDisplays)
            {
                if (imageDisplay != null)
                {
                    // if Unity is playing 
                    if (Application.isEditor)
                        DestroyImmediate(imageDisplay.gameObject);
                    else
                    {
                        Destroy(imageDisplay.gameObject);
                    }
                }
            }

            var images = col0.GetComponentsInChildren<ATO_Visual_ImageDisplay>();

            foreach (var image in images)
            {
                // if Unity is playing 
                if ( Application.isEditor )
                    DestroyImmediate(image.gameObject);
                else
                {       
                    Destroy(image.gameObject);
                }
            }

            imageDisplays.Clear();
        }

        public void AddImageRef( RenderTexture rt , string rtName , int lod , int resolution)
        {
            var obj = Instantiate(imageDisplayPrefab, col0);
            
            var imageDisplay = obj.GetComponent<ATO_Visual_ImageDisplay>();
            imageDisplay.Init(rt, rtName, lod, resolution);
            imageDisplays.Add(imageDisplay);
        }

        public void Show()
        {
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
        }

        public void OnDisable()
        {
            Clear();
        }
    }
}
