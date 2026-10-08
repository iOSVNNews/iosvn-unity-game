Shader "iOSVN/UI/CharacterAppearance"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HairTint ("Hair", Color) = (1,1,1,1)
        _RobeTint ("Robe", Color) = (1,1,1,1)
        _SkinTint ("Skin", Color) = (1,1,1,1)
        _EyeTint ("Eyes", Color) = (1,1,1,1)
        _Face ("Face region", Vector) = (.5,.85,.08,.06)
        _HairStart ("Hair lower bound", Float) = .75
        _RobeEnd ("Robe upper bound", Float) = .74
        _EyeY ("Eye height", Float) = .89
        _EyeSpacing ("Eye spacing", Float) = .017
        _FullBody ("Full body", Float) = 1
        _Female ("Female", Float) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd, _HairTint, _RobeTint, _SkinTint, _EyeTint;
            float4 _ClipRect, _Face;
            float _HairStart, _RobeEnd, _EyeY, _EyeSpacing, _FullBody, _Female;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(o.world);
                o.uv = v.uv; o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float2 p = float2(frac(i.uv.x * 2), i.uv.y);
                float2 d = (p - _Face.xy) / _Face.zw;
                float face = 1 - smoothstep(.65, 1.1, dot(d,d));
                float luminance = dot(c.rgb, float3(.299,.587,.114));
                // Keep eyes, lips, jewellery and engraved light/shadow details.
                float skin = face * smoothstep(.35,.65,luminance);
                float hair = smoothstep(_HairStart,_HairStart+.07,p.y) * (1-face);
                float gold = smoothstep(.035,.13,c.r-c.b) * smoothstep(.32,.65,c.g);
                float strandEdge = lerp(.58,.45,_Female) + (.83-p.y)*lerp(.55,.70,_Female);
                float strands = smoothstep(strandEdge-.015,strandEdge+.015,p.x) * smoothstep(.51,.60,p.y) * (1-smoothstep(.82,.87,p.y));
                float neutral = 1-smoothstep(.07,.19,abs(c.r-c.g)+abs(c.g-c.b));
                hair = max(hair,strands*_FullBody) * (1-gold) * neutral;
                float robe = (1-smoothstep(_RobeEnd-.025,_RobeEnd+.025,p.y)) * (1-gold*.85);
                robe *= 1-hair;
                float warmSkin = smoothstep(.10,.20,c.r-c.b) * smoothstep(.02,.10,c.r-c.g) * smoothstep(.50,.72,luminance);
                robe *= 1-warmSkin;
                c.rgb = lerp(c.rgb, _RobeTint.rgb * (.35+.85*luminance), robe*.48);
                c.rgb = lerp(c.rgb, _HairTint.rgb * (.25+.9*luminance), hair*.88);
                c.rgb = lerp(c.rgb, _SkinTint.rgb * (.55+.55*luminance), skin*.65);
                float eyeX = abs(abs(p.x-_Face.x)-_EyeSpacing);
                float eye = (1-smoothstep(.002,.008,eyeX)) * (1-smoothstep(.002,.005,abs(p.y-_EyeY))) * (1-smoothstep(.25,.55,luminance));
                c.rgb = lerp(c.rgb,_EyeTint.rgb*(.4+.5*luminance),eye*.75);
                c *= i.color;
                float edgeFade = smoothstep(0,.028,p.x) * (1-smoothstep(.972,1,p.x)) * smoothstep(0,.035,p.y) * (1-smoothstep(.995,1,p.y));
                c.a *= lerp(edgeFade,1,_FullBody);
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
