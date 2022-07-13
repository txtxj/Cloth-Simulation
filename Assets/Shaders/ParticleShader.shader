Shader "Unlit/ParticleShader"
{
    Properties
    {
        _MainColor ("Main Color", color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
 
            #include "UnityCG.cginc"

            fixed4 _MainColor;
 
            struct Particle
            {
                float3 position;
                float3 velocity;
            };
 
            StructuredBuffer<Particle> particleBuffer;
 
            float4 vert(uint id : SV_VertexID) : SV_POSITION
            {
                return UnityObjectToClipPos(float4(particleBuffer[id].position, 1));
            }
 
            fixed4 frag() : SV_Target
            {
                return _MainColor;
            }
            ENDCG
        }
    }
}