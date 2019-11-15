// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmosis/UI/Beam_Charge"
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
		_AlphaMask("Alpha Mask", 2D) = "white" {}
		_Resource("Resource", Range( 0 , 1)) = 0
		[HDR]_ColorHigh("Color High", Color) = (0,0.9083636,1,0)
		[HDR]_ColorLow("Color Low", Color) = (0.1583304,0.06594875,0.3584906,0)
		_ColourIntensity("Colour Intensity", Float) = 5
		_EdgeColour("Edge Colour", Color) = (0,0,0,0)
		_Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", Vector) = (1,1,0,0)
		_NoiseOffset("Noise Offset", Vector) = (0.2,1,0,0)
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
				float2 uv0102 = IN.texcoord.xy * float2( 2,2 ) + float2( 0.65,0 );
				float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , tex2D( _Noise, (uv0102*_NoiseTiling + ( _Time.y * _NoiseOffset )) ).r);
				float2 uv098 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_101_0 = (0.35 + (_Resource - 0.0) * (0.675 - 0.35) / (1.0 - 0.0));
				float temp_output_99_0 = step( uv098.y , temp_output_101_0 );
				float4 break95 = ( lerpResult13 + ( _EdgeColour * ( temp_output_99_0 - step( uv098.y , ( temp_output_101_0 - 0.0066 ) ) ) ) );
				float2 uv_AlphaMask = IN.texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
				float4 appendResult97 = (float4(break95.r , break95.g , break95.b , ( temp_output_99_0 * tex2D( _AlphaMask, uv_AlphaMask ).r )));
				
				half4 color = appendResult97;
				
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
1;1;1918;1017;1172.347;599.4451;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;100;-912.9006,197.0767;Float;False;Property;_Resource;Resource;1;0;Create;True;0;0;False;0;0;0.743;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;8;-1270.206,-165.6541;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;6;-1268.907,-82.9541;Float;False;Property;_NoiseOffset;Noise Offset;8;0;Create;True;0;0;False;0;0.2,1;0.1,-0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCRemapNode;101;-635.6426,197.4722;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.35;False;4;FLOAT;0.675;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-1078.006,-117.6541;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;9;-1098.002,-243.0813;Float;False;Property;_NoiseTiling;Noise Tiling;7;0;Create;True;0;0;False;0;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;102;-1162.943,-362.6587;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;2,2;False;1;FLOAT2;0.65,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;106;-380.76,71.96332;Float;False;2;0;FLOAT;0;False;1;FLOAT;0.0066;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;98;-681.8007,74.1767;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;99;-378.8007,165.1767;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-551.7813,-528.6829;Float;False;Property;_ColorHigh;Color High;2;1;[HDR];Create;True;0;0;False;0;0,0.9083636,1,0;0.4159594,0,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;107;-233.76,20.96332;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;105;-907.1641,-265.387;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;16;-578.7813,-781.6832;Float;False;Property;_ColorLow;Color Low;3;1;[HDR];Create;True;0;0;False;0;0.1583304,0.06594875,0.3584906,0;0.0218857,0,0.2352941,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;10;-910.0036,-500.0812;Float;True;Property;_Noise;Noise;6;0;Create;True;0;0;False;0;cd460ee4ac5c1e746b7a734cc7cc64dd;e451e4a7415133e499b571c26159bd98;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-545.7813,-609.6829;Float;False;Property;_ColourIntensity;Colour Intensity;4;0;Create;True;0;0;False;0;5;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;108;-102.76,63.96332;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-295.7813,-676.6829;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;4;-619.5744,-341.1409;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-302.7813,-581.6829;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;112;-211.6537,-298.9053;Float;False;Property;_EdgeColour;Edge Colour;5;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;13;-141.7814,-596.6829;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;111;19.34624,-233.4053;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;29;-459.2587,259.3413;Float;True;Property;_AlphaMask;Alpha Mask;0;0;Create;True;0;0;False;0;None;1539606a70ccd1f4998d7799b2c12c89;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;109;227.24,-459.0367;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;-158.2497,164.2682;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;95;354.883,-456.7635;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;97;520.801,-241.9366;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;94;704.2524,-243.564;Float;False;True;2;Float;ASEMaterialInspector;0;7;Cosmosis/UI/Beam_Charge;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;True;2;False;-1;True;True;True;True;True;0;True;-9;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;0;False;-1;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;0;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;101;0;100;0
WireConnection;7;0;8;0
WireConnection;7;1;6;0
WireConnection;106;0;101;0
WireConnection;99;0;98;2
WireConnection;99;1;101;0
WireConnection;107;0;98;2
WireConnection;107;1;106;0
WireConnection;105;0;102;0
WireConnection;105;1;9;0
WireConnection;105;2;7;0
WireConnection;108;0;99;0
WireConnection;108;1;107;0
WireConnection;28;0;16;0
WireConnection;28;1;27;0
WireConnection;4;0;10;0
WireConnection;4;1;105;0
WireConnection;26;0;17;0
WireConnection;26;1;27;0
WireConnection;13;0;28;0
WireConnection;13;1;26;0
WireConnection;13;2;4;1
WireConnection;111;0;112;0
WireConnection;111;1;108;0
WireConnection;109;0;13;0
WireConnection;109;1;111;0
WireConnection;63;0;99;0
WireConnection;63;1;29;1
WireConnection;95;0;109;0
WireConnection;97;0;95;0
WireConnection;97;1;95;1
WireConnection;97;2;95;2
WireConnection;97;3;63;0
WireConnection;94;0;97;0
ASEEND*/
//CHKSM=720F221F02AE61A08098406D192040C124E492C0