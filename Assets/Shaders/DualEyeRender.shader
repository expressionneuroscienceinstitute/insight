Shader "Custom/DualEyeVisibilityUI"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _VisibilityMode ("Visibility Mode", Range(0,2)) = 0 // 0: Both, 1: Left, 2: Right
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [HideInInspector] _CanvasTex ("Canvas Texture", 2D) = "black" {}
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-10000,-10000,10000,10000)
        [HideInInspector] _UIMaskSoftness ("UI Mask Softness", Vector) = (0.5,0.5,0,0)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "true"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "true"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "DualEyeVisibilityUI"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float eyeIndex: TEXCOORD2;
                float4 clipRect : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _UIMaskSoftness;
            sampler2D _MainTex;
            float _VisibilityMode;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);

                o.color = v.color;
                o.texcoord = v.texcoord;
                o.clipRect = _ClipRect;
                o.eyeIndex = unity_StereoEyeIndex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                 // Sample the texture
                fixed4 textureColor = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;
                fixed4 baseColor = textureColor * i.color;
                
                // Apply tint to the base color
                fixed4 tintedColor = baseColor * _Color;


                // Apply visibility logic
                if (_VisibilityMode == 1.0) // Left eye only
                {
                    if (i.eyeIndex != 0.0) // If not left eye
                    {
                        tintedColor.a = 0.0; // Set alpha to zero
                    }
                }
                else if (_VisibilityMode == 2.0) // Right eye only
                {
                    if (i.eyeIndex != 1.0) // If not right eye
                    {
                        tintedColor.a = 0.0; // Set alpha to zero
                    }
                }
                

                #ifdef UNITY_UI_CLIP_RECT
                    if (i.worldPosition.x < i.clipRect.x || i.worldPosition.x > i.clipRect.z || i.worldPosition.y < i.clipRect.y || i.worldPosition.y > i.clipRect.w) {
                        discard;
                    }

                    float2 mask = (i.worldPosition.xy - i.clipRect.xy) * float2(1.0 / i.clipRect.zw);
                    float clipSoftness = fwidth(mask.x) + fwidth(mask.y);
                    float alpha = 1.0;
                    alpha = saturate(alpha);
                    clip(alpha - 0.0001);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (tintedColor.a - 0.001);
                #endif

                return tintedColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
