Shader "GuitarKing/HighwaySurface"
{
    Properties
    {
        _BaseColor ("Color", Color) = (.1,.1,.1,.7)
        _EmissionColor ("Glow", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            fixed4 _BaseColor, _EmissionColor;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float light=.55+.45*saturate(dot(normalize(i.normal),normalize(float3(-.35,1,-.3))));
                float grain=frac(sin(dot(floor(i.world.xz*190),float2(12.9898,78.233)))*43758.5453);
                float3 col=_BaseColor.rgb*light*(.94+grain*.06)+_EmissionColor.rgb*.15;
                return fixed4(col,_BaseColor.a);
            }
            ENDCG
        }
    }
}
