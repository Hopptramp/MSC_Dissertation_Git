using UnityEngine;
using System.Collections;

public class BakeMaterial : MonoBehaviour {
    public RenderTexture ResultTexture;
    public int Size = 256;

    public Material ReplaceMaterial;

	// Use this for initialization
	void Awake () {
        if (ResultTexture == null)
        {
            ResultTexture = new RenderTexture(Size, Size, 0);
            ResultTexture.name = "Baked Texture";
        }

        bakeTexture();

        if (ReplaceMaterial != null)
        {
            GetComponent<Renderer>().material = ReplaceMaterial;
            ReplaceMaterial.mainTexture = ResultTexture;
        }
	}

    void bakeTexture()
    {
        var renderer = GetComponent<Renderer>();
        var material = Instantiate(renderer.material);
        Graphics.Blit(material.mainTexture, ResultTexture, material);
    }
}
