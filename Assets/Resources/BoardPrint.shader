Shader "Cashflow/Printed Board"
{
    Properties { _MainTex ("Printed artwork",2D)="white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        struct Input { float2 uv_MainTex; float4 tint; };
        void vert(inout appdata_full v,out Input o){ UNITY_INITIALIZE_OUTPUT(Input,o);o.tint=v.color; }
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            fixed4 ink=tex2D(_MainTex,i.uv_MainTex)*i.tint;
            o.Albedo=ink.rgb;o.Alpha=ink.a;o.Smoothness=.12;o.Metallic=0;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
