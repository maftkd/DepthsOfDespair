Shader "Custom/FlatStandardTwoSided"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		Cull Off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf FlatLit fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		uniform fixed4 _AmbientColor;

		half4 LightingFlatLit( SurfaceOutput s, half3 lightDir, half atten){
			half NdotL = dot (s.Normal, lightDir);
			half4 c;
			c.rgb = s.Albedo*_LightColor0.rgb*step(0,NdotL);
			c.rgb*=lerp(fixed3(1,1,1),_AmbientColor.rgb*s.Alpha,1-step(0.1,atten));
			//c.rgb = lerp(c.rgb,_AmbientColor.rgb
			//c.rgb*=step(0.1,atten);
			c.a = s.Alpha;
			return c;
		}


        struct Input
        {
			float foo;
        };

        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            //o.Metallic = _Metallic;
            //o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
