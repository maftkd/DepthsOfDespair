Shader "ImageEffect/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Threshold ("Threshold", Range(0.0001,.5)) = 0.1
		_EdgeColor ("Edge Color", Color) = (1,1,1,1)
		_Debug ("Debug depth", Range(0,1))=0
		_Vignette ("Vignette intensity", Float) = 0
		_Fade ("Fade Intensity", Range(0,1)) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			sampler2D _CameraDepthNormalsTexture;
			uniform fixed4 _Screen;
			fixed _Threshold;
			fixed4 _EdgeColor;
			fixed _Debug;
			fixed _Vignette;
			fixed _Fade;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

				fixed4 norms = tex2D(_CameraDepthNormalsTexture, i.uv);
				fixed dep = (norms.r+norms.g+norms.b)*.33333;

				//kernal samples
				//left
				fixed2 aa = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(-_Screen.z,_Screen.w));
				fixed2 ba = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(-_Screen.z,0));
				fixed2 ca = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(-_Screen.z,-_Screen.w));

				//top/bottom
				fixed2 ab = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(0,_Screen.w));
				fixed2 cb = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(0,-_Screen.w));

				//right
				fixed2 ac = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(_Screen.z,_Screen.w));
				fixed2 bc = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(_Screen.z,0));
				fixed2 cc = tex2D(_CameraDepthNormalsTexture, i.uv+fixed2(_Screen.z,-_Screen.w));

				fixed gx = abs(-aa.x-ba.x*4-ca.x+ac.x+bc.x*4+cc.x);
				fixed gy = abs(-ca.y-cb.y*4-cc.y+aa.y+ab.y*4+ac.y);
				fixed g = gx*gx+gy*gy;
				fixed edge = step(_Threshold,g);
				col = col*(1-edge)+edge*_EdgeColor;

				fixed2 diff = i.uv-fixed2(0.5,0.5);
				fixed rs = dot(diff,diff);
				//rs = pow(rs,_Vignette);
				rs*=_Vignette;
				//col = fixed4(rs,0,0,1);
				//return (rs)*col;
				//
				col = rs*_EdgeColor + (1-rs)*col;
				col*=_Fade;
				//col = lerp(col,_EdgeColor,rs);
				return col;
				//return 1-col;
				//return edge;
            }
            ENDCG
        }
    }
}
