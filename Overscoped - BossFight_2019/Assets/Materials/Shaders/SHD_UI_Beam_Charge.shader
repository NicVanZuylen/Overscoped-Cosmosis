// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmosis/UI/Beam_Charge"
{
    Properties
    {
		_Resource("Resource %", Range( 0 , 1)) = 0.5013457
		_ColorHigh("Color High", Color) = (0,0.9083636,1,0)
		_ColorLow("Color Low", Color) = (0.1583304,0.06594875,0.3584906,0)
		_ColourIntensity("Colour Intensity", Float) = 5
		_EdgeColour("Edge Colour", Color) = (0,0,0,0)
		_EdgeIntensity("Edge Intensity", Float) = 1
		_Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", Vector) = (1,1,0,0)
		_NoiseOffset("Noise Offset", Vector) = (0.2,1,0,0)
		[Toggle(_USEALPHAMASK_ON)] _UseAlphaMask("Use Alpha Mask", Float) = 0
		_Alpha("Alpha", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
    }

    SubShader
    {
		
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha , SrcAlpha OneMinusSrcAlpha
		Cull Back
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
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEALPHAMASK_ON

        
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
					float4 ase_texcoord1 : TEXCOORD1;
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

				float _Resource;
				sampler2D _Alpha;
				float4 _Alpha_ST;
				
				                
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord1.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = inputMesh.ase_texcoord.xy;
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
					float temp_output_64_0 = ( 1.0 - (-0.1 + (_Resource - 0.0) * (1.1 - -0.1) / (1.0 - 0.0)) );
					float2 uv130 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.5,0 ) );
					float2 uv159 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.2,0 ) );
					float temp_output_49_0 = ( ( ( sin( ( 5.0 * uv130.x ) ) * 0.015 ) + ( sin( ( uv159.x * 7.0 ) ) * 0.015 ) ) + uv130.y );
					float temp_output_82_0 = step( temp_output_64_0 , ( temp_output_49_0 + 0.01 ) );
					float2 uv_Alpha = packedInput.ase_texcoord.zw * _Alpha_ST.xy + _Alpha_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float staticSwitch87 = tex2D( _Alpha, uv_Alpha ).r;
					#else
					float staticSwitch87 = 1.0;
					#endif
					
					surfaceDescription.Alpha = ( temp_output_82_0 * staticSwitch87 );
					surfaceDescription.AlphaClipThreshold =  0;

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
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEALPHAMASK_ON

        
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
					float4 ase_texcoord1 : TEXCOORD1;
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

				float4 _EdgeColour;
				float _EdgeIntensity;
				float _Resource;
				float4 _ColorLow;
				float _ColourIntensity;
				float4 _ColorHigh;
				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				sampler2D _Alpha;
				float4 _Alpha_ST;
				
				                
		            
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord1.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = inputMesh.ase_texcoord.xy;
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
					float temp_output_64_0 = ( 1.0 - (-0.1 + (_Resource - 0.0) * (1.1 - -0.1) / (1.0 - 0.0)) );
					float2 uv130 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.5,0 ) );
					float2 uv159 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.2,0 ) );
					float temp_output_49_0 = ( ( ( sin( ( 5.0 * uv130.x ) ) * 0.015 ) + ( sin( ( uv159.x * 7.0 ) ) * 0.015 ) ) + uv130.y );
					float temp_output_82_0 = step( temp_output_64_0 , ( temp_output_49_0 + 0.01 ) );
					float2 uv05 = packedInput.ase_texcoord.zw * _NoiseTiling + ( _Time.y * _NoiseOffset );
					float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , tex2D( _Noise, uv05 ).r);
					float4 lerpResult84 = lerp( ( _EdgeColour * _EdgeIntensity * temp_output_82_0 ) , lerpResult13 , step( temp_output_64_0 , temp_output_49_0 ));
					
					float2 uv_Alpha = packedInput.ase_texcoord.zw * _Alpha_ST.xy + _Alpha_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float staticSwitch87 = tex2D( _Alpha, uv_Alpha ).r;
					#else
					float staticSwitch87 = 1.0;
					#endif
					
					surfaceDescription.Color =  lerpResult84.rgb;
					surfaceDescription.Alpha = ( temp_output_82_0 * staticSwitch87 );
					surfaceDescription.AlphaClipThreshold =  0;

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
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEALPHAMASK_ON


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
					float4 ase_texcoord1 : TEXCOORD1;
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

				float _Resource;
				sampler2D _Alpha;
				float4 _Alpha_ST;
				
				                
			    
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord1.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = inputMesh.ase_texcoord.xy;
					
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
						float temp_output_64_0 = ( 1.0 - (-0.1 + (_Resource - 0.0) * (1.1 - -0.1) / (1.0 - 0.0)) );
						float2 uv130 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.5,0 ) );
						float2 uv159 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.2,0 ) );
						float temp_output_49_0 = ( ( ( sin( ( 5.0 * uv130.x ) ) * 0.015 ) + ( sin( ( uv159.x * 7.0 ) ) * 0.015 ) ) + uv130.y );
						float temp_output_82_0 = step( temp_output_64_0 , ( temp_output_49_0 + 0.01 ) );
						float2 uv_Alpha = packedInput.ase_texcoord.zw * _Alpha_ST.xy + _Alpha_ST.zw;
						#ifdef _USEALPHAMASK_ON
						float staticSwitch87 = tex2D( _Alpha, uv_Alpha ).r;
						#else
						float staticSwitch87 = 1.0;
						#endif
						
						surfaceDescription.Alpha = ( temp_output_82_0 * staticSwitch87 );
						surfaceDescription.AlphaClipThreshold = 0;

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
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEALPHAMASK_ON

				
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
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				float4 _EdgeColour;
				float _EdgeIntensity;
				float _Resource;
				float4 _ColorLow;
				float _ColourIntensity;
				float4 _ColorHigh;
				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				sampler2D _Alpha;
				float4 _Alpha_ST;
				
				                
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.uv1.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = inputMesh.uv0.xy;
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
					float temp_output_64_0 = ( 1.0 - (-0.1 + (_Resource - 0.0) * (1.1 - -0.1) / (1.0 - 0.0)) );
					float2 uv130 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.5,0 ) );
					float2 uv159 = packedInput.ase_texcoord.xy * float2( 1,1 ) + ( _Time.y * float2( 0.2,0 ) );
					float temp_output_49_0 = ( ( ( sin( ( 5.0 * uv130.x ) ) * 0.015 ) + ( sin( ( uv159.x * 7.0 ) ) * 0.015 ) ) + uv130.y );
					float temp_output_82_0 = step( temp_output_64_0 , ( temp_output_49_0 + 0.01 ) );
					float2 uv05 = packedInput.ase_texcoord.zw * _NoiseTiling + ( _Time.y * _NoiseOffset );
					float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , tex2D( _Noise, uv05 ).r);
					float4 lerpResult84 = lerp( ( _EdgeColour * _EdgeIntensity * temp_output_82_0 ) , lerpResult13 , step( temp_output_64_0 , temp_output_49_0 ));
					
					float2 uv_Alpha = packedInput.ase_texcoord.zw * _Alpha_ST.xy + _Alpha_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float staticSwitch87 = tex2D( _Alpha, uv_Alpha ).r;
					#else
					float staticSwitch87 = 1.0;
					#endif
					
					surfaceDescription.Color =  lerpResult84.rgb;
					surfaceDescription.Alpha = ( temp_output_82_0 * staticSwitch87 );
					surfaceDescription.AlphaClipThreshold =  0;

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
Version=16800
1927;26;1906;993;684.3806;230.7407;1;True;False
Node;AmplifyShaderEditor.SimpleTimeNode;58;-1665.739,476.5952;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;56;-1662.739,550.5952;Float;False;Constant;_Vector1;Vector 1;9;0;Create;True;0;0;False;0;0.2,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;33;-1660.239,280.3452;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;45;-1657.239,354.3453;Float;False;Constant;_Vector0;Vector 0;9;0;Create;True;0;0;False;0;0.5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-1477.739,508.5952;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-1489.239,314.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-1259.239,252.3453;Float;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;False;0;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;-1257.239,572.3452;Float;False;Constant;_Float1;Float 1;9;0;Create;True;0;0;False;0;7;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;30;-1345.239,330.3452;Float;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-1340.739,451.5952;Float;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-1099.239,315.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-1090.239,436.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;36;-948.239,337.3452;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;55;-943.2389,435.3452;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-811.2389,436.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.015;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-814.2389,318.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.015;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-1041.597,211.9769;Float;False;Property;_Resource;Resource %;0;0;Create;True;0;0;False;0;0.5013457;0.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;62;-675.2389,325.3452;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;8;-1486.147,11.60281;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;6;-1484.848,94.30282;Float;False;Property;_NoiseOffset;Noise Offset;8;0;Create;True;0;0;False;0;0.2,1;0.3,-0.15;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;92;-572.4009,449.5602;Float;False;Constant;_EdgeSize;Edge Size;11;0;Create;True;0;0;False;0;0.01;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;9;-1313.943,-65.82446;Float;False;Property;_NoiseTiling;Noise Tiling;7;0;Create;True;0;0;False;0;1,1;1.5,1.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCRemapNode;65;-740.5316,158.4412;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.1;False;4;FLOAT;1.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-1266.947,72.60281;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;49;-532.2389,353.3453;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-768.7217,-304.4261;Float;False;Property;_ColourIntensity;Colour Intensity;3;0;Create;True;0;0;False;0;5;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-774.7217,-223.4261;Float;False;Property;_ColorHigh;Color High;1;0;Create;True;0;0;False;0;0,0.9083636,1,0;0.6091191,0.4199891,0.6226414,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;64;-532.427,280.6825;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;-801.7217,-476.4263;Float;False;Property;_ColorLow;Color Low;2;0;Create;True;0;0;False;0;0.1583304,0.06594875,0.3584906,0;0.01435609,0,0.3679243,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-340.8745,329.5107;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;5;-1106.515,-3.884031;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;10;-1132.944,-194.8244;Float;True;Property;_Noise;Noise;6;0;Create;True;0;0;False;0;cd460ee4ac5c1e746b7a734cc7cc64dd;cd460ee4ac5c1e746b7a734cc7cc64dd;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;4;-842.5148,-35.884;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-65.98389,379.9742;Float;True;Property;_Alpha;Alpha;10;0;Create;True;0;0;False;0;None;a8de9c9c15d9c7e4eaa883c727391bee;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;82;-215.8745,249.5107;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;90;-357.9038,-540.1344;Float;False;Property;_EdgeColour;Edge Colour;4;0;Create;True;0;0;False;0;0,0,0,0;0.3312554,0,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-525.7217,-276.4261;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-348.9038,-377.1344;Float;False;Property;_EdgeIntensity;Edge Intensity;5;0;Create;True;0;0;False;0;1;40;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;88;71.29834,306.7734;Float;False;Constant;_Float2;Float 2;9;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-518.7217,-371.426;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;13;-364.7218,-291.4261;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;89;-131.9037,-421.1344;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;51;-324.9933,423.6982;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;87;220.2983,342.7734;Float;False;Property;_UseAlphaMask;Use Alpha Mask;9;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;84;147.8255,-61.58924;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;492.0251,248.9011;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;META;0;3;META;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;Depth prepass;0;0;Depth prepass;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=DepthForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;-345.2872,178.4751;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;ShadowCaster;0;2;ShadowCaster;1;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;695.1108,147.0187;Float;False;True;2;Float;ASEMaterialInspector;0;4;Cosmosis/UI/Beam_Charge;dfe2f27ac20b08c469b2f95c236be0c3;True;Forward Unlit;0;1;Forward Unlit;5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;5;0;False;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=ForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;4;True;True;True;True;False;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
WireConnection;57;0;58;0
WireConnection;57;1;56;0
WireConnection;46;0;33;0
WireConnection;46;1;45;0
WireConnection;30;1;46;0
WireConnection;59;1;57;0
WireConnection;44;0;35;0
WireConnection;44;1;30;1
WireConnection;53;0;59;1
WireConnection;53;1;54;0
WireConnection;36;0;44;0
WireConnection;55;0;53;0
WireConnection;60;0;55;0
WireConnection;48;0;36;0
WireConnection;62;0;48;0
WireConnection;62;1;60;0
WireConnection;65;0;32;0
WireConnection;7;0;8;0
WireConnection;7;1;6;0
WireConnection;49;0;62;0
WireConnection;49;1;30;2
WireConnection;64;0;65;0
WireConnection;83;0;49;0
WireConnection;83;1;92;0
WireConnection;5;0;9;0
WireConnection;5;1;7;0
WireConnection;4;0;10;0
WireConnection;4;1;5;0
WireConnection;82;0;64;0
WireConnection;82;1;83;0
WireConnection;26;0;17;0
WireConnection;26;1;27;0
WireConnection;28;0;16;0
WireConnection;28;1;27;0
WireConnection;13;0;28;0
WireConnection;13;1;26;0
WireConnection;13;2;4;1
WireConnection;89;0;90;0
WireConnection;89;1;91;0
WireConnection;89;2;82;0
WireConnection;51;0;64;0
WireConnection;51;1;49;0
WireConnection;87;1;88;0
WireConnection;87;0;29;1
WireConnection;84;0;89;0
WireConnection;84;1;13;0
WireConnection;84;2;51;0
WireConnection;63;0;82;0
WireConnection;63;1;87;0
WireConnection;1;0;84;0
WireConnection;1;1;63;0
ASEEND*/
//CHKSM=68CD2AD7D9B84672ADC0B9E572ADC2F72964A37B