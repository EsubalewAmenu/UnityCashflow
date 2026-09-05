Shader "Cashflow/Printed Board"
{
 Properties { _MainTex ("Artwork",2D)="white" {} }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="ForwardBase" }
   Blend SrcAlpha OneMinusSrcAlpha
   Cull Off
   ZWrite Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fwdbase
   #include "UnityCG.cginc"
   #include "AutoLight.cginc"
   sampler2D _MainTex;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; SHADOW_COORDS(1) };
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;TRANSFER_SHADOW(o);return o; }
   fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.rgb*=lerp(.65,1,SHADOW_ATTENUATION(i));return c; }
   ENDCG
  }
 }
 Fallback Off
}
