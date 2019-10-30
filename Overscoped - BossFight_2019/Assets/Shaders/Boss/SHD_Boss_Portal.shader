// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmosis/Boss/Portal"
{
    Properties
    {
		_Albedo("Albedo", 2D) = "white" {}
		_PanningSpeed("Panning Speed", Vector) = (1,1,0,0)
		_Circle("Circle", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", Vector) = (2,1,0,0)
		_NoisePanning("Noise Panning", Vector) = (0.225,-0.5,0,0)
		[HDR]_EdgeColour("Edge Colour", Color) = (36.75834,0,21.33426,0)
		_Opacity("Opacity", Range( 0 , 1)) = 1
		_AlphaClipThreshold("Alpha Clip Threshold", Range( 0 , 1)) = 0
		_AlphaStep("Alpha Step", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
    }

    SubShader
    {
		
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="TransparentCutout" "Queue"="Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha , SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZTest LEqual
		ZWrite On
		Offset 0 , 0

		HLSLINCLUDE
		#pragma target 4.5
		#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
		#pragma multi_compile_instancing
		ENDHLSL

		
        Pass
        {
			
            Name "Depth prepass"
            Tags { "LightMode"="DepthForwardOnly" }
            ColorMask 0
			
        
            HLSLPROGRAM
        
				
        
				#pragma vertex Vert
				#pragma fragment Frag
        
				#define ASE_SRP_VERSION 50702

        
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
                #define SHADERPASS SHADERPASS_DEPTH_ONLY
        
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
				
				struct AttributesMesh 
				{
					float3 positionOS : POSITION;
					float4 normalOS : NORMAL;
					float4 ase_texcoord : TEXCOORD0;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif 
				};
        
				struct PackedVaryingsMeshToPS 
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif
				};

				float _AlphaStep;
				sampler2D _Circle;
				float4 _Circle_ST;
				float _Opacity;
				sampler2D _Noise;
				uniform sampler2D _Sampler6082;
				float2 _NoiseTiling;
				float2 _NoisePanning;
				float _AlphaClipThreshold;
				
				                
                struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };

				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{ 
				#if _ALPHATEST_ON
					DoAlphaTest ( surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold );
				#endif

					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData);
					builtinData.opacity =  surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0);
					builtinData.distortionBlur =0.0;
				}

				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh  )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;

					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					float3 vertexValue =   float3( 0, 0, 0 ) ;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
					inputMesh.positionOS.xyz = vertexValue;
					#else
					inputMesh.positionOS.xyz += vertexValue;
					#endif

					inputMesh.normalOS =  inputMesh.normalOS ;

					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					outputPackedVaryingsMeshToPS.positionCS = TransformWorldToHClip(positionRWS);  
					return outputPackedVaryingsMeshToPS;
				}

				void Frag(  PackedVaryingsMeshToPS packedInput
							#ifdef WRITE_NORMAL_BUFFER
							, out float4 outNormalBuffer : SV_Target0
								#ifdef WRITE_MSAA_DEPTH
							, out float1 depthColor : SV_Target1
								#endif
							#else
							, out float4 outColor : SV_Target0
							#endif

							#ifdef _DEPTHOFFSET_ON
							, out float outputDepth : SV_Depth
							#endif
							
						)
				{
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;

					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0

					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float4 temp_cast_0 = (_AlphaStep).xxxx;
					float2 uv_Circle = packedInput.ase_texcoord.xy * _Circle_ST.xy + _Circle_ST.zw;
					float4 temp_output_125_0 = ( tex2D( _Circle, uv_Circle ) * _Opacity );
					float2 temp_output_1_0_g4 = float2( 0,0 );
					float2 uv080_g4 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult10_g4 = (float2(( (temp_output_1_0_g4).x * uv080_g4.x ) , ( uv080_g4.y * (temp_output_1_0_g4).y )));
					float2 temp_output_11_0_g4 = float2( 0,0 );
					float2 uv081_g4 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner18_g4 = ( ( (temp_output_11_0_g4).x * _Time.y ) * float2( 1,0 ) + uv081_g4);
					float2 panner19_g4 = ( ( _Time.y * (temp_output_11_0_g4).y ) * float2( 0,1 ) + uv081_g4);
					float2 appendResult24_g4 = (float2((panner18_g4).x , (panner19_g4).y));
					float2 temp_output_47_0_g4 = _NoisePanning;
					float2 uv078_g4 = packedInput.ase_texcoord.xy * float2( 2,2 ) + float2( 0,0 );
					float2 temp_output_31_0_g4 = ( uv078_g4 - float2( 1,1 ) );
					float2 appendResult39_g4 = (float2(frac( ( atan2( (temp_output_31_0_g4).x , (temp_output_31_0_g4).y ) / TWO_PI ) ) , length( temp_output_31_0_g4 )));
					float2 panner54_g4 = ( ( (temp_output_47_0_g4).x * _Time.y ) * float2( 1,0 ) + appendResult39_g4);
					float2 panner55_g4 = ( ( _Time.y * (temp_output_47_0_g4).y ) * float2( 0,1 ) + appendResult39_g4);
					float2 appendResult58_g4 = (float2((panner54_g4).x , (panner55_g4).y));
					float4 temp_cast_1 = (0.0).xxxx;
					float4 temp_cast_2 = (1.0).xxxx;
					float4 clampResult116 = clamp( ( temp_output_125_0 + ( pow( temp_output_125_0 , 0.8 ) - tex2D( _Noise, ( ( (tex2D( _Sampler6082, ( appendResult10_g4 + appendResult24_g4 ) )).rg * 1.0 ) + ( _NoiseTiling * appendResult58_g4 ) ) ) ) ) , temp_cast_1 , temp_cast_2 );
					
					surfaceDescription.Alpha = step( temp_cast_0 , clampResult116 ).r;
					surfaceDescription.AlphaClipThreshold =  _AlphaClipThreshold;

					GetSurfaceAndBuiltinData(surfaceDescription, input, V, posInput, surfaceData, builtinData);

				#ifdef _DEPTHOFFSET_ON
					outputDepth = posInput.deviceDepth;
				#endif

				#ifdef WRITE_NORMAL_BUFFER
					EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, outNormalBuffer);
					#ifdef WRITE_MSAA_DEPTH
					// In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
					depthColor = packedInput.positionCS.z;
					#endif
				#elif defined(SCENESELECTIONPASS)
					// We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
					outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
				#else
					outColor = float4(0.0, 0.0, 0.0, 0.0);
				#endif
				}
        
            ENDHLSL
        }
		
        Pass
        {
			
            Name "Forward Unlit"
            Tags { "LightMode"="ForwardOnly" }
        
            ColorMask RGBA
			
            HLSLPROGRAM
        
				
        
				#pragma vertex Vert
				#pragma fragment Frag

				#define ASE_SRP_VERSION 50702

        
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

                #define SHADERPASS SHADERPASS_FORWARD_UNLIT
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
		        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
	        

				struct AttributesMesh 
				{
					float3 positionOS : POSITION;
					float4 normalOS : NORMAL;
					float4 ase_tangent : TANGENT;
					float4 ase_texcoord : TEXCOORD0;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				struct PackedVaryingsMeshToPS 
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif 
				};

				sampler2D _Albedo;
				float2 _PanningSpeed;
				float4 _EdgeColour;
				sampler2D _Circle;
				float4 _Circle_ST;
				float _Opacity;
				sampler2D _Noise;
				uniform sampler2D _Sampler6082;
				float2 _NoiseTiling;
				float2 _NoisePanning;
				float _AlphaStep;
				float _AlphaClipThreshold;
				
				                
		            
				struct SurfaceDescription
				{
					float3 Color;
					float Alpha;
					float AlphaClipThreshold;
				};
        
		
				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
					surfaceData.color = surfaceDescription.Color;
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription , FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{
				#if _ALPHATEST_ON
					DoAlphaTest ( surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold );
				#endif
					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData); 
					builtinData.opacity = surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0); 
					builtinData.distortionBlur = 0.0;
				}
        
         
				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh  )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;
					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					float3 ase_worldTangent = TransformObjectToWorldDir(inputMesh.ase_tangent.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord.xyz = ase_worldTangent;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord1.xyz = ase_worldNormal;
					float ase_vertexTangentSign = inputMesh.ase_tangent.w * unity_WorldTransformParams.w;
					float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldBitangent;
					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord3.xyz = ase_worldPos;
					
					outputPackedVaryingsMeshToPS.ase_texcoord4.xy = inputMesh.ase_texcoord.xy;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord1.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord3.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord4.zw = 0;
					float3 vertexValue =  float3( 0, 0, 0 ) ;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
					inputMesh.positionOS.xyz = vertexValue;
					#else
					inputMesh.positionOS.xyz += vertexValue;
					#endif

					inputMesh.normalOS =  inputMesh.normalOS ;

					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					outputPackedVaryingsMeshToPS.positionCS = TransformWorldToHClip(positionRWS);
					return outputPackedVaryingsMeshToPS;
				}

				float4 Frag(PackedVaryingsMeshToPS packedInput ) : SV_Target
				{
					
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;
				
					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0);

					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float3 ase_worldTangent = packedInput.ase_texcoord.xyz;
					float3 ase_worldNormal = packedInput.ase_texcoord1.xyz;
					float3 ase_worldBitangent = packedInput.ase_texcoord2.xyz;
					float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
					float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
					float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
					float3 ase_worldPos = packedInput.ase_texcoord3.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_tanViewDir =  tanToWorld0 * ase_worldViewDir.x + tanToWorld1 * ase_worldViewDir.y  + tanToWorld2 * ase_worldViewDir.z;
					ase_tanViewDir = normalize(ase_tanViewDir);
					float3 normalizeResult10 = normalize( ase_tanViewDir );
					float2 temp_output_45_0 = ( _Time.y * _PanningSpeed );
					float2 uv_Circle = packedInput.ase_texcoord4.xy * _Circle_ST.xy + _Circle_ST.zw;
					float4 temp_output_125_0 = ( tex2D( _Circle, uv_Circle ) * _Opacity );
					float2 temp_output_1_0_g4 = float2( 0,0 );
					float2 uv080_g4 = packedInput.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult10_g4 = (float2(( (temp_output_1_0_g4).x * uv080_g4.x ) , ( uv080_g4.y * (temp_output_1_0_g4).y )));
					float2 temp_output_11_0_g4 = float2( 0,0 );
					float2 uv081_g4 = packedInput.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner18_g4 = ( ( (temp_output_11_0_g4).x * _Time.y ) * float2( 1,0 ) + uv081_g4);
					float2 panner19_g4 = ( ( _Time.y * (temp_output_11_0_g4).y ) * float2( 0,1 ) + uv081_g4);
					float2 appendResult24_g4 = (float2((panner18_g4).x , (panner19_g4).y));
					float2 temp_output_47_0_g4 = _NoisePanning;
					float2 uv078_g4 = packedInput.ase_texcoord4.xy * float2( 2,2 ) + float2( 0,0 );
					float2 temp_output_31_0_g4 = ( uv078_g4 - float2( 1,1 ) );
					float2 appendResult39_g4 = (float2(frac( ( atan2( (temp_output_31_0_g4).x , (temp_output_31_0_g4).y ) / TWO_PI ) ) , length( temp_output_31_0_g4 )));
					float2 panner54_g4 = ( ( (temp_output_47_0_g4).x * _Time.y ) * float2( 1,0 ) + appendResult39_g4);
					float2 panner55_g4 = ( ( _Time.y * (temp_output_47_0_g4).y ) * float2( 0,1 ) + appendResult39_g4);
					float2 appendResult58_g4 = (float2((panner54_g4).x , (panner55_g4).y));
					float4 temp_cast_6 = (0.0).xxxx;
					float4 temp_cast_7 = (1.0).xxxx;
					float4 clampResult116 = clamp( ( temp_output_125_0 + ( pow( temp_output_125_0 , 0.8 ) - tex2D( _Noise, ( ( (tex2D( _Sampler6082, ( appendResult10_g4 + appendResult24_g4 ) )).rg * 1.0 ) + ( _NoiseTiling * appendResult58_g4 ) ) ) ) ) , temp_cast_6 , temp_cast_7 );
					
					float4 temp_cast_9 = (_AlphaStep).xxxx;
					
					surfaceDescription.Color =  ( ( ( tex2D( _Albedo, ( ( -0.3 * normalizeResult10 ) + float3( packedInput.ase_texcoord4.xy ,  0.0 ) + 0.3 + float3( ( temp_output_45_0 * ( 1.0 - abs( -0.3 ) ) ) ,  0.0 ) ).xy ) + tex2D( _Albedo, ( ( -0.9 * normalizeResult10 ) + float3( packedInput.ase_texcoord4.xy ,  0.0 ) + 1.0 + float3( ( temp_output_45_0 * ( 1.0 - abs( -0.9 ) ) ) ,  0.0 ) ).xy ) ) * 3.0 ) + ( _EdgeColour * pow( ( 1.0 - clampResult116 ) , 0.4 ) ) ).rgb;
					surfaceDescription.Alpha = step( temp_cast_9 , clampResult116 ).r;
					surfaceDescription.AlphaClipThreshold =  _AlphaClipThreshold;

					GetSurfaceAndBuiltinData(surfaceDescription, input, V, posInput, surfaceData, builtinData);

					BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

					float4 outColor = ApplyBlendMode(bsdfData.color + builtinData.emissiveColor, builtinData.opacity);
					outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

					return outColor;
				}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            ZClip [_ZClip]
            ColorMask 0
        
            HLSLPROGRAM
        
				
        
				#pragma instancing_options renderinglayer
                #pragma multi_compile _ LOD_FADE_CROSSFADE

				#pragma vertex Vert
				#pragma fragment Frag
        
				#define ASE_SRP_VERSION 50702


				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
				#define SHADERPASS SHADERPASS_SHADOWS
				#define USE_LEGACY_UNITY_MATRIX_VARIABLES
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"    
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
				struct AttributesMesh 
				{
					float3 positionOS : POSITION;
					float3 normalOS : NORMAL;
					float4 ase_texcoord : TEXCOORD0;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};
        
				struct PackedVaryingsMeshToPS
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				float _AlphaStep;
				sampler2D _Circle;
				float4 _Circle_ST;
				float _Opacity;
				sampler2D _Noise;
				uniform sampler2D _Sampler6082;
				float2 _NoiseTiling;
				float2 _NoisePanning;
				float _AlphaClipThreshold;
				
				                
			    
				struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                    
            
				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
					surfaceData.ambientOcclusion =      1.0f;
					surfaceData.subsurfaceMask =        1.0f;
					surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
			#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
					surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
			#endif
					float3 normalTS = float3(0.0f, 0.0f, 1.0f);
					float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
					GetNormalWS(fragInputs, normalTS, surfaceData.normalWS,doubleSidedConstants);
					surfaceData.tangentWS = normalize(fragInputs.worldToTangent[0].xyz);    // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
					surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
					surfaceData.anisotropy = 0;
					surfaceData.coatMask = 0.0f;
					surfaceData.iridescenceThickness = 0.0;
					surfaceData.iridescenceMask = 1.0;
					surfaceData.ior = 1.0;
					surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
					surfaceData.atDistance = 1000000.0;
					surfaceData.transmittanceMask = 0.0;
					surfaceData.specularOcclusion = 1.0;
			#if defined(_BENTNORMALMAP) && defined(_ENABLESPECULAROCCLUSION)
					// If we have bent normal and ambient occlusion, process a specular occlusion
					surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData);
			#elif defined(_MASKMAP)
					surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(NdotV, surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
			#endif
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{
					#if _ALPHATEST_ON
						DoAlphaTest(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
					#endif
					DoAlphaTest(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
        
					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
        
					InitBuiltinData(surfaceDescription.Alpha, surfaceData.normalWS, -fragInputs.worldToTangent[2], fragInputs.positionRWS, fragInputs.texCoord1, fragInputs.texCoord2, builtinData);
					builtinData.distortion =                float2(0.0, 0.0);
					builtinData.distortionBlur =            0.0;
					builtinData.depthOffset =               0.0;
        
					PostInitBuiltinData(V, posInput, surfaceData, builtinData);
				}
        
				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh  )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;
				
					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					
					float3 vertexValue =  float3( 0, 0, 0 ) ;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
					inputMesh.positionOS.xyz = vertexValue;
					#else
					inputMesh.positionOS.xyz += vertexValue;
					#endif

					inputMesh.normalOS =  inputMesh.normalOS ;

					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					outputPackedVaryingsMeshToPS.positionCS = TransformWorldToHClip(positionRWS);
				
					return outputPackedVaryingsMeshToPS;
				}

				void Frag(  PackedVaryingsMeshToPS packedInput
							#ifdef WRITE_NORMAL_BUFFER
							, out float4 outNormalBuffer : SV_Target0
								#ifdef WRITE_MSAA_DEPTH
							, out float1 depthColor : SV_Target1
								#endif
							#else
							, out float4 outColor : SV_Target0
							#endif

							#ifdef _DEPTHOFFSET_ON
							, out float outputDepth : SV_Depth
							#endif
							 
						)
				{
						FragInputs input;
						ZERO_INITIALIZE(FragInputs, input);
						input.worldToTangent = k_identity3x3;
						input.positionSS = packedInput.positionCS;       // input.positionCS is SV_Position

						// input.positionSS is SV_Position
						PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

						float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0

						SurfaceData surfaceData;
						BuiltinData builtinData;
						SurfaceDescription surfaceDescription = (SurfaceDescription)0;
						float4 temp_cast_0 = (_AlphaStep).xxxx;
						float2 uv_Circle = packedInput.ase_texcoord.xy * _Circle_ST.xy + _Circle_ST.zw;
						float4 temp_output_125_0 = ( tex2D( _Circle, uv_Circle ) * _Opacity );
						float2 temp_output_1_0_g4 = float2( 0,0 );
						float2 uv080_g4 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
						float2 appendResult10_g4 = (float2(( (temp_output_1_0_g4).x * uv080_g4.x ) , ( uv080_g4.y * (temp_output_1_0_g4).y )));
						float2 temp_output_11_0_g4 = float2( 0,0 );
						float2 uv081_g4 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
						float2 panner18_g4 = ( ( (temp_output_11_0_g4).x * _Time.y ) * float2( 1,0 ) + uv081_g4);
						float2 panner19_g4 = ( ( _Time.y * (temp_output_11_0_g4).y ) * float2( 0,1 ) + uv081_g4);
						float2 appendResult24_g4 = (float2((panner18_g4).x , (panner19_g4).y));
						float2 temp_output_47_0_g4 = _NoisePanning;
						float2 uv078_g4 = packedInput.ase_texcoord.xy * float2( 2,2 ) + float2( 0,0 );
						float2 temp_output_31_0_g4 = ( uv078_g4 - float2( 1,1 ) );
						float2 appendResult39_g4 = (float2(frac( ( atan2( (temp_output_31_0_g4).x , (temp_output_31_0_g4).y ) / TWO_PI ) ) , length( temp_output_31_0_g4 )));
						float2 panner54_g4 = ( ( (temp_output_47_0_g4).x * _Time.y ) * float2( 1,0 ) + appendResult39_g4);
						float2 panner55_g4 = ( ( _Time.y * (temp_output_47_0_g4).y ) * float2( 0,1 ) + appendResult39_g4);
						float2 appendResult58_g4 = (float2((panner54_g4).x , (panner55_g4).y));
						float4 temp_cast_1 = (0.0).xxxx;
						float4 temp_cast_2 = (1.0).xxxx;
						float4 clampResult116 = clamp( ( temp_output_125_0 + ( pow( temp_output_125_0 , 0.8 ) - tex2D( _Noise, ( ( (tex2D( _Sampler6082, ( appendResult10_g4 + appendResult24_g4 ) )).rg * 1.0 ) + ( _NoiseTiling * appendResult58_g4 ) ) ) ) ) , temp_cast_1 , temp_cast_2 );
						
						surfaceDescription.Alpha = step( temp_cast_0 , clampResult116 ).r;
						surfaceDescription.AlphaClipThreshold = _AlphaClipThreshold;

						GetSurfaceAndBuiltinData(surfaceDescription,input, V, posInput, surfaceData, builtinData);

					#ifdef _DEPTHOFFSET_ON
						outputDepth = posInput.deviceDepth;
					#endif

					#ifdef WRITE_NORMAL_BUFFER
						EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, outNormalBuffer);
						#ifdef WRITE_MSAA_DEPTH
						// In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
						depthColor = packedInput.positionCS.z;
						#endif
					#elif defined(SCENESELECTIONPASS)
						// We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
						outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
					#else
						outColor = float4(0.0, 0.0, 0.0, 0.0);
					#endif
				}

            ENDHLSL
        }

		
		Pass
		{
			
            Name "META"
            Tags { "LightMode"="Meta" }
        
            Cull Off
        
            HLSLPROGRAM
        
				
        
				#pragma vertex Vert
				#pragma fragment Frag
        
				#define ASE_SRP_VERSION 50702

				
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
                #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
        
                #define ATTRIBUTES_NEED_NORMAL
                #define ATTRIBUTES_NEED_TANGENT
                #define ATTRIBUTES_NEED_TEXCOORD0
                #define ATTRIBUTES_NEED_TEXCOORD1
                #define ATTRIBUTES_NEED_TEXCOORD2
                #define ATTRIBUTES_NEED_COLOR
        
        
			    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"

				struct AttributesMesh
				{
					float3 positionOS : POSITION;
					float3 normalOS : NORMAL;
					float4 tangentOS : TANGENT;
					float4 uv0 : TEXCOORD0;
					float4 uv1 : TEXCOORD1;
					float4 uv2 : TEXCOORD2;
					float4 color : COLOR;
					
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};
        
				struct PackedVaryingsMeshToPS
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				sampler2D _Albedo;
				float2 _PanningSpeed;
				float4 _EdgeColour;
				sampler2D _Circle;
				float4 _Circle_ST;
				float _Opacity;
				sampler2D _Noise;
				uniform sampler2D _Sampler6082;
				float2 _NoiseTiling;
				float2 _NoisePanning;
				float _AlphaStep;
				float _AlphaClipThreshold;
				
				                
                struct SurfaceDescription
                {
                    float3 Color;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                    
				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
					surfaceData.color = surfaceDescription.Color;
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{
       
				#if _ALPHATEST_ON
					DoAlphaTest(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
				#endif

					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData);
					builtinData.opacity = surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0);
					builtinData.distortionBlur = 0.0;
				}
       
				CBUFFER_START(UnityMetaPass)
				bool4 unity_MetaVertexControl;
				bool4 unity_MetaFragmentControl;
				CBUFFER_END

				float unity_OneOverOutputBoost;
				float unity_MaxOutputValue;

				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh  )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;

					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					float3 ase_worldTangent = TransformObjectToWorldDir(inputMesh.tangentOS.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord.xyz = ase_worldTangent;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS);
					outputPackedVaryingsMeshToPS.ase_texcoord1.xyz = ase_worldNormal;
					float ase_vertexTangentSign = inputMesh.tangentOS.w * unity_WorldTransformParams.w;
					float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldBitangent;
					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord3.xyz = ase_worldPos;
					
					outputPackedVaryingsMeshToPS.ase_texcoord4.xy = inputMesh.uv0.xy;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord1.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord3.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord4.zw = 0;
					float3 vertexValue =  float3( 0, 0, 0 ) ;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
					inputMesh.positionOS.xyz = vertexValue; 
					#else
					inputMesh.positionOS.xyz += vertexValue;
					#endif
					
					inputMesh.normalOS =  inputMesh.normalOS ;

					float2 uv;

					if (unity_MetaVertexControl.x)
					{
						uv = inputMesh.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					}
					else if (unity_MetaVertexControl.y)
					{
						uv = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
					}

					outputPackedVaryingsMeshToPS.positionCS = float4(uv * 2.0 - 1.0, inputMesh.positionOS.z > 0 ? 1.0e-4 : 0.0, 1.0);
					return outputPackedVaryingsMeshToPS;
				}

				float4 Frag( PackedVaryingsMeshToPS packedInput  ) : SV_Target
				{			
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;
                
				
					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
		
					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float3 ase_worldTangent = packedInput.ase_texcoord.xyz;
					float3 ase_worldNormal = packedInput.ase_texcoord1.xyz;
					float3 ase_worldBitangent = packedInput.ase_texcoord2.xyz;
					float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
					float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
					float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
					float3 ase_worldPos = packedInput.ase_texcoord3.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_tanViewDir =  tanToWorld0 * ase_worldViewDir.x + tanToWorld1 * ase_worldViewDir.y  + tanToWorld2 * ase_worldViewDir.z;
					ase_tanViewDir = normalize(ase_tanViewDir);
					float3 normalizeResult10 = normalize( ase_tanViewDir );
					float2 temp_output_45_0 = ( _Time.y * _PanningSpeed );
					float2 uv_Circle = packedInput.ase_texcoord4.xy * _Circle_ST.xy + _Circle_ST.zw;
					float4 temp_output_125_0 = ( tex2D( _Circle, uv_Circle ) * _Opacity );
					float2 temp_output_1_0_g4 = float2( 0,0 );
					float2 uv080_g4 = packedInput.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult10_g4 = (float2(( (temp_output_1_0_g4).x * uv080_g4.x ) , ( uv080_g4.y * (temp_output_1_0_g4).y )));
					float2 temp_output_11_0_g4 = float2( 0,0 );
					float2 uv081_g4 = packedInput.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner18_g4 = ( ( (temp_output_11_0_g4).x * _Time.y ) * float2( 1,0 ) + uv081_g4);
					float2 panner19_g4 = ( ( _Time.y * (temp_output_11_0_g4).y ) * float2( 0,1 ) + uv081_g4);
					float2 appendResult24_g4 = (float2((panner18_g4).x , (panner19_g4).y));
					float2 temp_output_47_0_g4 = _NoisePanning;
					float2 uv078_g4 = packedInput.ase_texcoord4.xy * float2( 2,2 ) + float2( 0,0 );
					float2 temp_output_31_0_g4 = ( uv078_g4 - float2( 1,1 ) );
					float2 appendResult39_g4 = (float2(frac( ( atan2( (temp_output_31_0_g4).x , (temp_output_31_0_g4).y ) / TWO_PI ) ) , length( temp_output_31_0_g4 )));
					float2 panner54_g4 = ( ( (temp_output_47_0_g4).x * _Time.y ) * float2( 1,0 ) + appendResult39_g4);
					float2 panner55_g4 = ( ( _Time.y * (temp_output_47_0_g4).y ) * float2( 0,1 ) + appendResult39_g4);
					float2 appendResult58_g4 = (float2((panner54_g4).x , (panner55_g4).y));
					float4 temp_cast_6 = (0.0).xxxx;
					float4 temp_cast_7 = (1.0).xxxx;
					float4 clampResult116 = clamp( ( temp_output_125_0 + ( pow( temp_output_125_0 , 0.8 ) - tex2D( _Noise, ( ( (tex2D( _Sampler6082, ( appendResult10_g4 + appendResult24_g4 ) )).rg * 1.0 ) + ( _NoiseTiling * appendResult58_g4 ) ) ) ) ) , temp_cast_6 , temp_cast_7 );
					
					float4 temp_cast_9 = (_AlphaStep).xxxx;
					
					surfaceDescription.Color =  ( ( ( tex2D( _Albedo, ( ( -0.3 * normalizeResult10 ) + float3( packedInput.ase_texcoord4.xy ,  0.0 ) + 0.3 + float3( ( temp_output_45_0 * ( 1.0 - abs( -0.3 ) ) ) ,  0.0 ) ).xy ) + tex2D( _Albedo, ( ( -0.9 * normalizeResult10 ) + float3( packedInput.ase_texcoord4.xy ,  0.0 ) + 1.0 + float3( ( temp_output_45_0 * ( 1.0 - abs( -0.9 ) ) ) ,  0.0 ) ).xy ) ) * 3.0 ) + ( _EdgeColour * pow( ( 1.0 - clampResult116 ) , 0.4 ) ) ).rgb;
					surfaceDescription.Alpha = step( temp_cast_9 , clampResult116 ).r;
					surfaceDescription.AlphaClipThreshold =  _AlphaClipThreshold;

					GetSurfaceAndBuiltinData(surfaceDescription,input, V, posInput, surfaceData, builtinData);
					BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
					LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);
					float4 res = float4(0.0, 0.0, 0.0, 1.0);
					if (unity_MetaFragmentControl.x)
					{
						res.rgb = clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
					}

					if (unity_MetaFragmentControl.y)
					{
						res.rgb = lightTransportData.emissiveColor;
					}

					return res;
				}

            ENDHLSL
		}
		
    }
    Fallback "Hidden/InternalErrorShader"
	CustomEditor "ASEMaterialInspector"
	
}
/*ASEBEGIN
Version=16900
1920;1;1906;1011;-562.2964;-1988.766;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;120;-189.1415,2327.545;Float;False;1533.677;577.7604;Comment;13;69;82;73;71;116;118;117;115;110;113;63;125;126;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector2Node;71;-138.1415,2624.305;Float;False;Property;_NoiseTiling;Noise Tiling;4;0;Create;True;0;0;False;0;2,1;3,1.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;73;-160.1415,2750.305;Float;False;Property;_NoisePanning;Noise Panning;5;0;Create;True;0;0;False;0;0.225,-0.5;-0.125,-0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;126;180.9685,2474.027;Float;False;Property;_Opacity;Opacity;7;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;119;-499.0415,1721.039;Float;False;1835.758;572.7183;Comment;26;52;30;53;4;6;20;21;5;13;48;14;27;49;24;26;10;45;56;57;44;47;9;59;60;16;17;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;63;-115.9264,2387.345;Float;True;Property;_Circle;Circle;2;0;Create;True;0;0;False;0;5228a04ef529d2641937cab585cc1a02;5228a04ef529d2641937cab585cc1a02;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-451.6835,1795.25;Float;False;Constant;_Float0;Float 0;1;0;Create;True;0;0;False;0;-0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;82;36.85852,2594.305;Float;False;RadialUVDistortion;-1;;4;051d65e7699b41a4c800363fd0e822b2;0;7;60;SAMPLER2D;_Sampler6082;False;1;FLOAT2;0,0;False;11;FLOAT2;0,0;False;65;FLOAT;1;False;68;FLOAT2;1,1;False;47;FLOAT2;0,-0.5;False;29;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;466.5276,2393.79;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-450.5604,2213.984;Float;False;Constant;_Float1;Float 1;1;0;Create;True;0;0;False;0;-0.9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;60;-279.2754,2213.689;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;44;-460.5935,1873.41;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;9;-468.2234,2072.167;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;47;-476.8655,1944.685;Float;False;Property;_PanningSpeed;Panning Speed;1;0;Create;True;0;0;False;0;1,1;-0.02,0.02;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PowerNode;113;608.5367,2488.188;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.8;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;59;-290.7095,1765.69;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;69;462.8585,2588.305;Float;True;Property;_Noise;Noise;3;0;Create;True;0;0;False;0;cd460ee4ac5c1e746b7a734cc7cc64dd;e28dc97a9541e3642a48c0e3886688c5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;56;-149.7424,2212.408;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-256.0924,1922.21;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;110;774.4363,2574.387;Float;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;10;-267.2224,2052.167;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;57;-164.0774,1767.087;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;226.7466,1788.915;Float;False;Constant;_Float4;Float 4;1;0;Create;True;0;0;False;0;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;11.58752,1894.048;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;27;263.7476,2192.122;Float;False;Constant;_Float5;Float 5;1;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;8.113525,2083.01;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;118;1002.536,2559.988;Float;False;Constant;_Float11;Float 11;5;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;115;998.5364,2384.989;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;24;146.2855,1975.404;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;0.1865234,1792.169;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;8.208496,2191.022;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;117;1001.536,2482.989;Float;False;Constant;_Float10;Float 10;5;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;116;1169.537,2452.989;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;453.8466,1799.503;Float;False;4;4;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;5;382.2416,1939.728;Float;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;False;0;4e5f38b4b873cd14c9d7005ff9f54166;03349b297528f144c90f6ac7484be0d7;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;461.6466,2148.044;Float;False;4;4;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;4;670.5135,1807.707;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;674.9017,2084.861;Float;True;Property;_TextureSample1;Texture Sample 1;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;124;1356.832,2313.406;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;30;1035.256,2053.948;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;53;1021,2158.579;Float;False;Constant;_Float9;Float 9;2;0;Create;True;0;0;False;0;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;127;1527.175,2308.073;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.4;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;122;1425.895,2132.143;Float;False;Property;_EdgeColour;Edge Colour;6;1;[HDR];Create;True;0;0;False;0;36.75834,0,21.33426,0;0,0,95.87451,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;1183.713,2057.591;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;121;1685.063,2198.737;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;130;1368.296,2419.766;Float;False;Property;_AlphaStep;Alpha Step;9;0;Create;True;0;0;False;0;0;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;123;1881.318,2059.032;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;128;1780.789,2521.429;Float;False;Property;_AlphaClipThreshold;Alpha Clip Threshold;8;0;Create;True;0;0;False;0;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;129;1529.296,2453.766;Float;False;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;Depth prepass;0;0;Depth prepass;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=DepthForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;META;0;3;META;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;1578.519,1574.481;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;ShadowCaster;0;2;ShadowCaster;1;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;2057.803,2428.246;Float;False;True;2;Float;ASEMaterialInspector;0;4;Cosmosis/Boss/Portal;dfe2f27ac20b08c469b2f95c236be0c3;True;Forward Unlit;0;1;Forward Unlit;5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;False;False;True;2;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=TransparentCutout=RenderType;Queue=Transparent=Queue=0;True;5;0;False;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=ForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;4;True;True;True;True;False;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
WireConnection;82;68;71;0
WireConnection;82;47;73;0
WireConnection;125;0;63;0
WireConnection;125;1;126;0
WireConnection;60;0;17;0
WireConnection;113;0;125;0
WireConnection;59;0;16;0
WireConnection;69;1;82;0
WireConnection;56;0;60;0
WireConnection;45;0;44;0
WireConnection;45;1;47;0
WireConnection;110;0;113;0
WireConnection;110;1;69;0
WireConnection;10;0;9;0
WireConnection;57;0;59;0
WireConnection;13;0;16;0
WireConnection;13;1;10;0
WireConnection;14;0;17;0
WireConnection;14;1;10;0
WireConnection;115;0;125;0
WireConnection;115;1;110;0
WireConnection;48;0;45;0
WireConnection;48;1;57;0
WireConnection;49;0;45;0
WireConnection;49;1;56;0
WireConnection;116;0;115;0
WireConnection;116;1;117;0
WireConnection;116;2;118;0
WireConnection;20;0;13;0
WireConnection;20;1;24;0
WireConnection;20;2;26;0
WireConnection;20;3;48;0
WireConnection;21;0;14;0
WireConnection;21;1;24;0
WireConnection;21;2;27;0
WireConnection;21;3;49;0
WireConnection;4;0;5;0
WireConnection;4;1;20;0
WireConnection;6;0;5;0
WireConnection;6;1;21;0
WireConnection;124;0;116;0
WireConnection;30;0;4;0
WireConnection;30;1;6;0
WireConnection;127;0;124;0
WireConnection;52;0;30;0
WireConnection;52;1;53;0
WireConnection;121;0;122;0
WireConnection;121;1;127;0
WireConnection;123;0;52;0
WireConnection;123;1;121;0
WireConnection;129;0;130;0
WireConnection;129;1;116;0
WireConnection;1;0;123;0
WireConnection;1;1;129;0
WireConnection;1;2;128;0
ASEEND*/
//CHKSM=EC90D4C8351A4045FD8AF86167D70A8C14853800