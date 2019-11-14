// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmosis/UI/Health"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_Resource("Resource", Range( 0 , 1)) = 0
		_Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", Vector) = (1,1,0,0)
		_NoiseOffset("Noise Offset", Vector) = (0.2,1,0,0)
		_ColorHigh("Color High", Color) = (0.8962264,0.0549573,0.0549573,0)
		_ColorLow("Color Low", Color) = (0.3867925,0.06337899,0.06020827,0)
		_ColourIntensity("Colour Intensity", Float) = 5
		_AlphaMask("Alpha Mask", 2D) = "white" {}
		_EdgeColour("Edge Colour", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			CompFront [_StencilComp]
			PassFront [_StencilOp]
			FailFront Keep
			ZFailFront Keep
			CompBack Always
			PassBack Keep
			FailBack Keep
			ZFailBack Keep
		}


		Cull Off
		Lighting Off
		ZWrite Off
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		
		Pass
		{
			Name "Default"
		CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			#include "UnityShaderVariables.cginc"

			
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
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;
			uniform float4 _ColorLow;
			uniform float _ColourIntensity;
			uniform float4 _ColorHigh;
			uniform sampler2D _Noise;
			uniform float2 _NoiseTiling;
			uniform float2 _NoiseOffset;
			uniform float4 _EdgeColour;
			uniform float _Resource;
			uniform sampler2D _AlphaMask;
			uniform float4 _AlphaMask_ST;
			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				
				
				OUT.worldPosition.xyz +=  float3( 0, 0, 0 ) ;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN  ) : SV_Target
			{
				float2 uv05 = IN.texcoord.xy * _NoiseTiling + ( _Time.y * _NoiseOffset );
				float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , tex2D( _Noise, uv05 ));
				float2 uv0102 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_100_0 = (0.2 + (_Resource - 0.0) * (1.0 - 0.2) / (1.0 - 0.0));
				float temp_output_104_0 = step( uv0102.x , temp_output_100_0 );
				float4 break91 = ( lerpResult13 + ( _EdgeColour * ( temp_output_104_0 - step( uv0102.x , ( temp_output_100_0 - 0.0075 ) ) ) ) );
				float2 uv_AlphaMask = IN.texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
				float4 appendResult93 = (float4(break91.r , break91.g , break91.b , ( temp_output_104_0 * tex2D( _AlphaMask, uv_AlphaMask ).r )));
				
				half4 color = appendResult93;
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=16900
7;20;1906;999;850.0201;138.459;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;99;-409.2308,459.8783;Float;False;Property;_Resource;Resource;0;0;Create;True;0;0;False;0;0;0.716;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;8;-786.5678,193.1142;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;6;-785.2689,275.814;Float;False;Property;_NoiseOffset;Noise Offset;3;0;Create;True;0;0;False;0;0.2,1;-0.3,-0.075;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCRemapNode;100;-143.7101,466.0203;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.2;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-608.368,240.1141;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;101;71.02543,345.1805;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0075;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;9;-622.364,114.6869;Float;False;Property;_NoiseTiling;Noise Tiling;2;0;Create;True;0;0;False;0;1,1;3,3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;102;-188.2592,345.1245;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;17;-94.19016,-57.10935;Float;False;Property;_ColorHigh;Color High;4;0;Create;True;0;0;False;0;0.8962264,0.0549573,0.0549573,0;1,0.06249998,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;5;-435.9353,175.6273;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;16;-121.1902,-310.1096;Float;False;Property;_ColorLow;Color Low;5;0;Create;True;0;0;False;0;0.3867925,0.06337899,0.06020827,0;0.3018867,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;104;84.94523,442.9514;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-88.19016,-138.1096;Float;False;Property;_ColourIntensity;Colour Intensity;6;0;Create;True;0;0;False;0;5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;10;-458.8,-17.09519;Float;True;Property;_Noise;Noise;1;0;Create;True;0;0;False;0;cd460ee4ac5c1e746b7a734cc7cc64dd;cd460ee4ac5c1e746b7a734cc7cc64dd;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.StepOpNode;103;221.0255,348.1805;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;161.81,-205.1096;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;4;-173.2354,115.0273;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;154.81,-110.1095;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;106;348.0254,358.1805;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;105;237.0255,180.1805;Float;False;Property;_EdgeColour;Edge Colour;8;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;13;358.0099,-159.3096;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;107;491.0254,268.1805;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;108;41.98103,542.5108;Float;True;Property;_AlphaMask;Alpha Mask;7;0;Create;True;0;0;False;0;None;3b9330e8fddc73248ba39dc5c64f8949;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;110;690.4203,33.79474;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;349.5705,455.0542;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;91;801.2539,36.61289;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;93;1037.754,37.3129;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;90;1171.073,44.58465;Float;False;True;2;Float;ASEMaterialInspector;0;7;Cosmosis/UI/Health;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;True;2;False;-1;True;True;True;True;True;0;True;-9;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;0;False;-1;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;0;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;100;0;99;0
WireConnection;7;0;8;0
WireConnection;7;1;6;0
WireConnection;101;0;100;0
WireConnection;5;0;9;0
WireConnection;5;1;7;0
WireConnection;104;0;102;1
WireConnection;104;1;100;0
WireConnection;103;0;102;1
WireConnection;103;1;101;0
WireConnection;28;0;16;0
WireConnection;28;1;27;0
WireConnection;4;0;10;0
WireConnection;4;1;5;0
WireConnection;26;0;17;0
WireConnection;26;1;27;0
WireConnection;106;0;104;0
WireConnection;106;1;103;0
WireConnection;13;0;28;0
WireConnection;13;1;26;0
WireConnection;13;2;4;0
WireConnection;107;0;105;0
WireConnection;107;1;106;0
WireConnection;110;0;13;0
WireConnection;110;1;107;0
WireConnection;109;0;104;0
WireConnection;109;1;108;1
WireConnection;91;0;110;0
WireConnection;93;0;91;0
WireConnection;93;1;91;1
WireConnection;93;2;91;2
WireConnection;93;3;109;0
WireConnection;90;0;93;0
ASEEND*/
//CHKSM=E5FE86D4F4A89F2415B21FA4486D7F2FC663FEE6