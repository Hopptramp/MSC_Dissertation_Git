Shader "Custom/BlendTwoTextures" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_SecondTex ("Second (RGB)", 2D) = "white" {}
		_BlendFactor ("Blend", Range(0, 1)) = 0
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            uniform sampler2D _MainTex;
			uniform sampler2D _SecondTex;
			fixed _BlendFactor;

            fixed4 frag(v2f_img i) : SV_Target {
                return lerp(tex2D(_MainTex, i.uv), tex2D(_SecondTex, i.uv), _BlendFactor);
            }
            ENDCG
        }
    }
}