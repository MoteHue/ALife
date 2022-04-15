Shader "Custom/CellSurfaceShader"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma target 4.5

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 colour;

        float _Step;
        int _Resolution;

        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<float> _Values;
        #endif

        void ConfigureProcedural() {
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                float value = _Values[unity_InstanceID];

                if (value <= 0 || value == 10) {
                    unity_ObjectToWorld = 0.0;
                }
                else {
                    unity_ObjectToWorld = 0.0;
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(unity_InstanceID % _Resolution, (unity_InstanceID / _Resolution) % _Resolution, (unity_InstanceID / (_Resolution * _Resolution)) % _Resolution, 1.0);
                    unity_ObjectToWorld._m00_m11_m22 = _Step;

                    if (value == 1) {
                        colour = fixed4(1, 1, 0, 1);
                    } else if (value == 2) {
                        colour = fixed4(0, 1, 1, 1);
                    }
                }
            #endif
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * colour;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
