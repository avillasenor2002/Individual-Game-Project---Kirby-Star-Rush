Shader "Unlit/ColorSwaWithLayeredShadow"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _KeyColor("Color to Remove", Color) = (0,1,0,1)
        _Threshold("Color Threshold", Range(0,1)) = 0.1
        _ShadowOffset("Shadow Offset", Vector) = (0.05, -0.05, 0, 0)
        _ShadowColor("Shadow Color", Color) = (0,0,0,0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        // -------------------
        // Shadow Pass
        // -------------------
        Pass
        {
            Name "Shadow"
            Tags { "Queue"="Transparent-1" "LightMode"="Always" } // render before main sprite

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _KeyColor;
            float _Threshold;
            float4 _ShadowOffset;
            fixed4 _ShadowColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Convert vertex to world space first
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                // Apply shadow offset in world space (consistent even when flipped)
                worldPos.xy += _ShadowOffset.xy;

                // Transform back to clip space
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Ignore culled color
                if (distance(col.rgb, _KeyColor.rgb) < _Threshold)
                    discard;

                // Return shadow color
                return _ShadowColor;
            }
            ENDCG
        }

        // -------------------
        // Main Sprite Pass
        // -------------------
        Pass
        {
            Name "MainSprite"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _KeyColor;
            float _Threshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (distance(col.rgb, _KeyColor.rgb) < _Threshold)
                    col.a = 0;

                return col;
            }
            ENDCG
        }
    }
}
