Shader "Custom/VoxelTriplanar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tiling ("Tiling", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Tiling;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);

                float3 blend = abs(n);
                blend /= (blend.x + blend.y + blend.z);

                float2 uvX = i.worldPos.yz * _Tiling;
                float2 uvY = i.worldPos.xz * _Tiling;
                float2 uvZ = i.worldPos.xy * _Tiling;

                float4 texX = tex2D(_MainTex, uvX);
                float4 texY = tex2D(_MainTex, uvY);
                float4 texZ = tex2D(_MainTex, uvZ);

                float4 tex = texX * blend.x +
                             texY * blend.y +
                             texZ * blend.z;

                return tex * i.color;
            }
            ENDCG
        }
    }
}