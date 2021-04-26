using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageEffect : MonoBehaviour
{
	Monolith _m;
	void Start(){
		_m = FindObjectOfType<Monolith>();
		_mat.SetFloat("_Vignette",0);
		_mat.SetFloat("_Fade",1);
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
	}
	public Material _mat;
	void OnRenderImage(RenderTexture src, RenderTexture dst){
		//vignette
		if(_m!=null)
		{
			_mat.SetFloat("_Vignette",_m._hitTimer);
			_mat.SetFloat("_Fade",_m._fade);
		}
		//outline
		Graphics.Blit(src,dst,_mat);
	}

}
