using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DTSAnimator : MonoBehaviour
{
    [System.Serializable]
    public class MaterialIndex
    {
        public int index;
        public Texture texture;
    }

    [System.Serializable]
    public class MaterialAnim
    {
        public MaterialIndex[] material;
        public int frames;
    }

    [SerializeField] MaterialAnim[] animationFrames;
    MeshRenderer smr;    

    // Start is called before the first frame update
    void Start()
    {
        smr = GetComponent<MeshRenderer>();
        StartCoroutine(Animate());
    }

    // Update is called once per frame
    IEnumerator Animate()
    {
        while (true)
        {
            foreach (MaterialAnim af in animationFrames)
            {
                foreach (MaterialIndex mi in af.material)
                    smr.materials[mi.index].mainTexture = mi.texture;

                float delayTime = ((float) af.frames) / 30f;
                yield return new WaitForSeconds(delayTime);
            }
        }
    }
}
