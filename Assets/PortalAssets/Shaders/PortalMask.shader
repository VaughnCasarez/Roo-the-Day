Shader "Portals/PortalMask"
{
    Properties
    {
        _LeftEyeTex("Left Eye Texture", 2D) = "white" {}
        _RightEyeTex("Right Eye Texture", 2D) = "white" {}
        _ForcedEye("Forced Eye (-1 = auto)", Float) = -1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            Name "Mask"

            Stencil
            {
                Ref 1
                Pass replace
            }

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                struct appdata
                {
                    float4 vertex : POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float4 screenPos : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                TEXTURE2D(_LeftEyeTex);  SAMPLER(sampler_LeftEyeTex);
                TEXTURE2D(_RightEyeTex); SAMPLER(sampler_RightEyeTex);
                float _ForcedEye;

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = TransformObjectToHClip(v.vertex.xyz);
                    o.screenPos = ComputeScreenPos(o.vertex);
                    return o;
                }

                float4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    float2 uv = i.screenPos.xy / i.screenPos.w;
                    int eye = _ForcedEye >= 0 ? (int)_ForcedEye : unity_StereoEyeIndex;

                    return eye == 0
                        ? SAMPLE_TEXTURE2D(_LeftEyeTex,  sampler_LeftEyeTex,  uv)
                        : SAMPLE_TEXTURE2D(_RightEyeTex, sampler_RightEyeTex, uv);
                }
            ENDHLSL
        }
    }
}