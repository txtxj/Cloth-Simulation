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
 
            struct v2f
            {
                float4 position : SV_POSITION;
            };
 
            struct Particle
            {
                float3 position;
                float4 velocity;
            };
 
            StructuredBuffer<Particle> particleBuffer;
 
            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                o.position = UnityObjectToClipPos(float4(particleBuffer[id].position, 0));
                return o;
            }
 
            fixed4 frag(v2f i) : SV_Target
            {
                return _MainColor;
            }
            ENDCG
        }
    }
}