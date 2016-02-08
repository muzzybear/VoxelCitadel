Shader "Lit/VoxelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        CGPROGRAM
        #pragma surface surf Lambert exclude_path:forward nometa novertexlights nodirlightmap nodynlightmap noforwardadd 

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            float4 color : COLOR;
            INTERNAL_DATA
        };

        sampler2D _MainTex;
        float _VoxelCutY;

        void surf (Input IN, inout SurfaceOutput o) {
            clip (IN.worldPos.y > _VoxelCutY ? -1 : 1);

            // custom mipmap with clamp to avoid texture atlas bleeding
            float2 dx = ddx(IN.uv_MainTex);
            float2 dy = ddy(IN.uv_MainTex);
            float dmax_sqr = max(dot(dx,dx), dot(dy,dy));
            float mip = min(5, 0.5*log2(dmax_sqr)+7.5);

            float4 uvw = float4(IN.uv_MainTex, 0,mip);

            o.Albedo = IN.color * tex2Dlod (_MainTex, uvw).rgb;
        }
        ENDCG

        // includes cut geometry in shadows, something addshadow doesn't do 
        UsePass "VertexLit/SHADOWCASTER"

        // cross section hack
        Pass {
            Cull Front
            Tags {"LightMode"="Deferred" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float4 worldpos : FLOAT4;
            };

            v2f vert(appdata_base v) {
                v2f o = (v2f)0;
                o.worldpos = mul(_Object2World, v.vertex);
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }

            float _VoxelCutY;

            void frag(v2f i,
                out half4 outDiffuse : SV_Target0,                      // RT0: diffuse color (rgb), occlusion (a)
                out half4 outSpecSmoothness : SV_Target1,               // RT1: spec color (rgb), smoothness (a)
                out half4 outNormal : SV_Target2,                       // RT2: normal (rgb), --unused, very low precision-- (a) 
                out half4 outEmission : SV_Target3                      // RT3: emission (rgb), --unused-- (a)
            )
            {
                if(i.worldpos.y > _VoxelCutY)
                    discard;
                // it will turn black due to normals facing downwards anyway
                outDiffuse = half4(0.5,0.5,0.5,0);
                outSpecSmoothness = half4(0,0,0,1);
                outNormal = half4(0.5,0,0.5,0);
                // magic to set color without shadows from backface geometry landing on it...
                float v = 0.98;
                outEmission = half4(v,v,v,0);
            }

            ENDCG
        }
    }	
}
