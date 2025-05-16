Shader "Custom/UIScanEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RadarCenterUIPos ("Radar Center UI Pos", Vector) = (0,0,0,0) // xy: center, zw: unused
        _ScanCurrentAngleRad ("Scan Current Angle (Rad)", Float) = 0
        _ScanArcWidthRad ("Scan Arc Width (Rad)", Float) = 0.523599 // Default to 30 degrees in radians
        _FadeRangeRad ("Scan Fade Range (Rad)", Float) = 0.0872665 // Default to 5 degrees in radians

        // Properties for UI system integration
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane" // Important for UI shaders
            "CanUseSpriteAtlas"="True" // Important for UI shaders
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
        ZTest [unity_GUIZTestMode] // From Unity UI Shader
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc" // For UnityUI specific functions/variables

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1; // For UI elements, this might be the canvas position
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            // CanvasRenderer related properties
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;


            // Our custom properties
            float4 _RadarCenterUIPos;
            float _ScanCurrentAngleRad;
            float _ScanArcWidthRad;
            float _FadeRangeRad;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex; // In UI, vertex is often in local space relative to the element
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 originalColor = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                fixed alpha = originalColor.a; // 최종 알파 값, 기본은 원래 알파

                // 1. Calculate vector from radar center to current pixel's UI position.
                //    i.worldPosition.xy 는 버텍스 셰이더에서 v.vertex.xy (로컬 좌표)를 그대로 넘긴 값.
                //    _RadarCenterUIPos.xy 는 C#에서 설정한 레이더 중심의 UI 좌표.
                //    두 좌표가 동일한 공간에 있다고 가정한다 (예: 마커들이 레이더의 자식으로 있고, _RadarCenterUIPos가 (0,0)인 경우).
                //    만약 _RadarCenterUIPos가 스크린 좌표이거나 다른 기준이라면, i.worldPosition.xy도 해당 공간으로 변환 필요.
                //    여기서는 i.worldPosition.xy가 _RadarCenterUIPos와 같은 로컬 UI 공간 내의 좌표라고 가정한다.
                float2 pixelPosRelativeToElementOrigin = i.worldPosition.xy;
                float2 dirToPixel = pixelPosRelativeToElementOrigin - _RadarCenterUIPos.xy;

                // 2. Calculate angle of this vector.
                // atan2(y, x)는 -PI ~ PI 범위의 라디안 값을 반환.
                // Unity UI는 보통 위쪽이 Y+ 이므로, dirToPixel.y, dirToPixel.x 순서가 맞음.
                float pixelAngleRad = atan2(dirToPixel.y, dirToPixel.x);

                // 3. Calculate normalized delta angle between pixelAngle and scanCenterAngle.
                //    결과를 -PI ~ PI 범위로 정규화하면 비교가 용이.
                float deltaAngleRad = pixelAngleRad - _ScanCurrentAngleRad;
                // Normalize deltaAngle to the range [-PI, PI]
                while (deltaAngleRad > UNITY_PI) deltaAngleRad -= 2.0 * UNITY_PI;
                while (deltaAngleRad < -UNITY_PI) deltaAngleRad += 2.0 * UNITY_PI;
                
                float absDeltaAngleRad = abs(deltaAngleRad);
                float halfArcWidthRad = _ScanArcWidthRad * 0.5;

                // 4. Adjust alpha based on scan range and fade.
                if (absDeltaAngleRad < halfArcWidthRad)
                {
                    // 픽셀이 주 스캔 범위 내에 있음
                    if (_FadeRangeRad > 0.001 && absDeltaAngleRad > (halfArcWidthRad - _FadeRangeRad))
                    {
                        // 페이드 아웃 구간 (가장자리)
                        // smoothstep(edge0, edge1, x)는 x가 edge0에서 edge1로 변할 때 0에서 1로 부드럽게 변하는 값을 반환.
                        // 여기서는 가장자리에서 안쪽으로 들어올수록 알파가 1에 가까워지도록 함.
                        // (halfArcWidthRad - absDeltaAngleRad) 값이 0에서 _FadeRangeRad로 변할 때, 알파가 0에서 1로.
                        alpha = smoothstep(0.0, _FadeRangeRad, halfArcWidthRad - absDeltaAngleRad);
                    }
                    else
                    {
                        // 완전한 스캔 범위 내부 (페이드 구간 아님)
                        alpha = 1.0;
                    }
                }
                else
                {
                    // 스캔 범위 완전 외부
                    // C# 스크립트에서 DOFade로 알파를 제어하므로, 여기서는 1.0을 곱해서 스크립트 값을 존중.
                    alpha = 1.0; 
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함 (텍스처 자체의 투명도 유지)
                originalColor.a *= alpha;

                // Apply clipping (from UnityUI.cginc)
                #ifdef UNITY_UI_CLIP_RECT
                // originalColor.a *= UnityGet2DClipping(i.vertex.xy, _ClipRect); // SASHA: URP 호환성 문제로 일단 주석 처리
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (originalColor.a - 0.001);
                #endif
                
                return originalColor;
            }
            ENDCG
        }
    }
    Fallback "UI/Default" // Fallback to default UI shader
} 