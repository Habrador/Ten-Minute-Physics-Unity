Shader "Custom/DisplayAllParticles"
{
    Properties
    {
        _ParticleTexture ("Particle Texture", 2D) = "white" {}
        _ParticleColor ("Particle Color", Color) = (1,1,1,1)
        //_ParticleRadius ("Particle Radius", Float) = 0.1
        //_ParticleCount ("Particle Count", Int) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _ParticleTexture;
            float4 _ParticleColor;
            float _ParticleRadius;
            int _ParticleCount = 1;
            float2 _PlaneScale;

            struct Particle
            {
                float2 position;
            };

            //In the shader, you define a StructuredBuffer that matches the structure of the data in the ComputeBuffer. This allows the shader to access the data efficiently.
            StructuredBuffer<Particle> _Particles;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = half4(0, 0, 0, 0);

                for (uint index = 0; index < _ParticleCount; index++)
                {
                    float2 particlePos = _Particles[index].position;
                    
                    //Compensate for plane scale
                    float2 scaledUV = i.uv * _PlaneScale;

                    float dist = distance(scaledUV, particlePos * _PlaneScale);
                    
                    if (dist < _ParticleRadius)
                    {
                        color = tex2D(_ParticleTexture, particlePos) * _ParticleColor;
                    }
                }
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
