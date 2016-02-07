Shader "Lit/VoxelShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque"}
		Tags {"LightMode"="ForwardBase"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				centroid float2 uv : TEXCOORD0;
				SHADOW_COORDS(1)
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				TRANSFER_SHADOW(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 dx = ddx(i.uv);
				float2 dy = ddy(i.uv);
				float dmax_sqr = max(dot(dx,dx), dot(dy,dy));
				float mip = min(5, 0.5*log2(dmax_sqr)+7.5);

				// sample the texture
				float4 uvw = float4(i.uv, 0,mip);
				fixed4 t = tex2Dlod(_MainTex, uvw);
				fixed4 col = i.color * t;
				fixed shadow = SHADOW_ATTENUATION(i);
				col.rgb *= shadow;
				return col;
			}
			ENDCG
		}

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
