Shader "Custom/BlinnPhone"
{
    Properties
    {
        _MainColor ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        [Space(20)]
        [MaterialToggle] _IsAmbient ("Ambient", float) = 0
        [MaterialToggle] _IsDiffuse ("Diffuse", float) = 0
        _Alpha ("Diffuse Alpha", Range(0, 1)) = 0.5
        [MaterialToggle] _IsSpecular ("Specular", float) = 0
        _Specular ("Specular Intensity", float) = 1
        _SpecularPow ("Specular Pow", float) = 256
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        CGINCLUDE

        #include "UnityCG.cginc"

        fixed4 _MainColor;
        sampler2D _MainTex;
        float4 _MainTex_ST;

        float _IsAmbient;
        float _IsDiffuse;
        float _IsSpecular;
        float _Specular;
        float _SpecularPow;

        fixed _Alpha;

        struct Particle
        {
            float3 position;
            float3 velocity;
            float3 force;
            float3 normal;
            float2 uv;
            float isFixed;
        };

        struct v2f
        {
            float4 position : SV_POSITION;
            float4 worldPos : TEXCOORD0;
            float3 normal : TEXCOORD1;
            float2 uv : TEXCOORD2;
        };

        StructuredBuffer<Particle> particleBuffer;

        v2f vert(uint id : SV_VertexID)
        {
            v2f o;
            o.position = UnityObjectToClipPos(float4(particleBuffer[id].position, 1));
            o.worldPos = mul(unity_ObjectToWorld, particleBuffer[id].position);
            o.normal = UnityObjectToWorldNormal(particleBuffer[id].normal);
            o.uv = particleBuffer[id].uv;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            fixed4 color = 0;
            fixed4 light = normalize(_WorldSpaceLightPos0);
            fixed3 view = normalize(WorldSpaceViewDir(i.position));
            i.normal = normalize(i.normal);
            if (_IsAmbient)
            {
                color += unity_AmbientSky;
            }
            if (_IsDiffuse)
            {
                color += tex2D(_MainTex, i.uv) * (_Alpha * saturate(dot(i.normal, light)) + 1 - _Alpha);
            }
            if (_IsSpecular)
            {
                fixed3 h = normalize(view + light);
                color += (2 + _Specular) / (2 * UNITY_PI) * pow(saturate(dot(h, i.normal)), _SpecularPow);
            }
            color.a = 1;
            return color;
        }
        
        ENDCG
 
        Pass
        {
            Cull Off
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            ENDCG
        }
    }
}