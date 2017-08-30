Shader "Unlit/ObjectSpaceBuffer"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_SpecColor ("Specular Material Color", Color) = (1,1,1,1)
		_Shininess ("Shininess", Float) = 10
	}
	SubShader
	{
		LOD 100
		Pass
		{
			Tags { "RenderType"="Opaque" "LightMode" = "ForwardBase" }

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma target 5.0
			
			#include "UnityCG.cginc"

			uniform float4 _LightColor0;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Output
			{
				float4 target0 : SV_Target0;
				float4 target1 : SV_Target1;
			};

			//StructuredBuffer<CustomAppdata> vertexData;
			//RWStructuredBuffer<float4> pixelData : register(u1);
			RWTexture2D <float4> outTex : register(u1);

			struct v2f
			{			
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalDir : TEXCOORD1;
				float3 posWorld : TEXCOORD2;
				int id : INT;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _SpecColor;
			float _Shininess;
			float4 _Color;
			
			v2f vert (appdata input, uint id : SV_VertexID)
			{
				v2f output;

				// calculate normal & view direction
				output.normalDir = normalize(mul(float4(input.normal, 0.0), unity_WorldToObject).xyz);
				output.vertex = UnityObjectToClipPos(input.vertex);

				
				output.uv = input.uv;
				input.vertex = mul(unity_WorldToObject, float4(input.uv,0,1));
				output.posWorld = mul(unity_ObjectToWorld, input.vertex);
				output.id = id;
				return output;
			}

			
			Output frag (v2f input) 
			{				
				float3 normalDirection = normalize(input.normalDir);
				float3 viewDirection = normalize(_WorldSpaceCameraPos - input.posWorld.xyz);

				// calculate lightDirection and light dropoff
				float3 lightDirection;
				float attenuation;
				float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - input.posWorld.xyz;

				float One_Over_Distance = 1 / length(vertexToLightSource);
				attenuation = lerp(1.0, One_Over_Distance, _WorldSpaceLightPos0.w);
				lightDirection = lerp(normalize(_WorldSpaceLightPos0.xyz), normalize(vertexToLightSource), _WorldSpaceLightPos0.w);

				// calculate ambientLighting
				float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
				
				// calculate diffuseReflection
				float3 diffuseReflection = attenuation * _LightColor0.rgb * _Color.rgb * max(0.0, dot(normalDirection, lightDirection));
				
				// calculate specularReflection
				float3 specularReflection;
				specularReflection = lerp(float3(0.0, 0.0, 0.0), attenuation * _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0, dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess), step(0.0, dot(normalDirection, lightDirection)));

				float4 output = float4(ambientLighting + diffuseReflection * tex2D(_MainTex, input.uv.xy) + specularReflection, 1.0);
				
				Output outputStruct;
				outputStruct.target0 = output;
				outputStruct.target1 = output;

				return outputStruct;

			}
			ENDCG
		}
	}
}
