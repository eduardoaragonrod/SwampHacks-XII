Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness (Pixels)", Range(0, 10)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
{
    fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

    // Solid sprite pixel
    if (c.a > 0.35)
    {
        c.rgb *= c.a;
        return c;
    }

    // Calculate exact texel size for crisp pixel-art borders
    float2 texel = _MainTex_TexelSize.xy * round(_OutlineThickness);
    float alphaSum = 0;

    // Direct 4-directional cardinal pixel sampling
    alphaSum += step(0.35, tex2D(_MainTex, IN.texcoord + float2(texel.x, 0)).a);
    alphaSum += step(0.35, tex2D(_MainTex, IN.texcoord + float2(-texel.x, 0)).a);
    alphaSum += step(0.35, tex2D(_MainTex, IN.texcoord + float2(0, texel.y)).a);
    alphaSum += step(0.35, tex2D(_MainTex, IN.texcoord + float2(0, -texel.y)).a);

    if (alphaSum > 0.0)
    {
        fixed4 outline = _OutlineColor;
        outline.rgb *= outline.a;
        return outline;
    }

    return fixed4(0, 0, 0, 0);
}
        ENDCG
        }
    }
}