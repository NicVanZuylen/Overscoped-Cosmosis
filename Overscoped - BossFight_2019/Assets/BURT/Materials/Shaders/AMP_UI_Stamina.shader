// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Shaders_Burt/UI_Stamina"
{
    Properties
    {
		_Resource("Resource %", Range( 0 , 1)) = 0.5
		_Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", Vector) = (1,1,0,0)
		_NoiseOffset("Noise Offset", Vector) = (0.2,1,0,0)
		_ColorHigh("Color High", Color) = (1,0.5436541,0,0)
		_ColorLow("Color Low", Color) = (0.3568628,0.06942323,0.06666666,0)
		_ColourIntensity("Colour Intensity", Float) = 5
		[Toggle(_USEALPHAMASK_ON)] _UseAlphaMask("Use Alpha Mask", Float) = 0
		_AlphaMask("Alpha Mask", 2D) = "white" {}
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
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif
				};

				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				float _Resource;
				sampler2D _AlphaMask;
				float4 _AlphaMask_ST;
				
				                
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
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
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
					float2 uv05 = packedInput.ase_texcoord.xy * _NoiseTiling + ( _Time.y * _NoiseOffset );
					float4 tex2DNode4 = tex2D( _Noise, uv05 );
					float temp_output_65_0 = (-0.125 + (_Resource - 0.0) * (0.1 - -0.125) / (1.0 - 0.0));
					float smoothstepResult138 = smoothstep( ( temp_output_65_0 + 0.03 ) , temp_output_65_0 , packedInput.ase_texcoord1.xyz.z);
					float temp_output_142_0 = ( tex2DNode4.r * smoothstepResult138 );
					float smoothstepResult90 = smoothstep( ( temp_output_65_0 + 0.015 ) , ( temp_output_65_0 - 0.015 ) , packedInput.ase_texcoord1.xyz.z);
					float clampResult149 = clamp( ( temp_output_142_0 + smoothstepResult90 ) , 0.0 , 1.0 );
					float4 temp_cast_0 = (1.0).xxxx;
					float2 uv_AlphaMask = packedInput.ase_texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float4 staticSwitch88 = tex2D( _AlphaMask, uv_AlphaMask );
					#else
					float4 staticSwitch88 = temp_cast_0;
					#endif
					
					surfaceDescription.Alpha = ( clampResult149 * staticSwitch88 ).r;
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
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif 
				};

				float4 _ColorLow;
				float _ColourIntensity;
				float4 _ColorHigh;
				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				float _Resource;
				sampler2D _AlphaMask;
				float4 _AlphaMask_ST;
				
				                
		            
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
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
					float2 uv05 = packedInput.ase_texcoord.xy * _NoiseTiling + ( _Time.y * _NoiseOffset );
					float4 tex2DNode4 = tex2D( _Noise, uv05 );
					float temp_output_65_0 = (-0.125 + (_Resource - 0.0) * (0.1 - -0.125) / (1.0 - 0.0));
					float smoothstepResult90 = smoothstep( ( temp_output_65_0 + 0.015 ) , ( temp_output_65_0 - 0.015 ) , packedInput.ase_texcoord1.xyz.z);
					float smoothstepResult138 = smoothstep( ( temp_output_65_0 + 0.03 ) , temp_output_65_0 , packedInput.ase_texcoord1.xyz.z);
					float temp_output_142_0 = ( tex2DNode4.r * smoothstepResult138 );
					float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , ( ( tex2DNode4.r * smoothstepResult90 ) + temp_output_142_0 ));
					
					float clampResult149 = clamp( ( temp_output_142_0 + smoothstepResult90 ) , 0.0 , 1.0 );
					float4 temp_cast_1 = (1.0).xxxx;
					float2 uv_AlphaMask = packedInput.ase_texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float4 staticSwitch88 = tex2D( _AlphaMask, uv_AlphaMask );
					#else
					float4 staticSwitch88 = temp_cast_1;
					#endif
					
					surfaceDescription.Color =  lerpResult13.rgb;
					surfaceDescription.Alpha = ( clampResult149 * staticSwitch88 ).r;
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
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				float _Resource;
				sampler2D _AlphaMask;
				float4 _AlphaMask_ST;
				
				                
			    
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
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
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
						float2 uv05 = packedInput.ase_texcoord.xy * _NoiseTiling + ( _Time.y * _NoiseOffset );
						float4 tex2DNode4 = tex2D( _Noise, uv05 );
						float temp_output_65_0 = (-0.125 + (_Resource - 0.0) * (0.1 - -0.125) / (1.0 - 0.0));
						float smoothstepResult138 = smoothstep( ( temp_output_65_0 + 0.03 ) , temp_output_65_0 , packedInput.ase_texcoord1.xyz.z);
						float temp_output_142_0 = ( tex2DNode4.r * smoothstepResult138 );
						float smoothstepResult90 = smoothstep( ( temp_output_65_0 + 0.015 ) , ( temp_output_65_0 - 0.015 ) , packedInput.ase_texcoord1.xyz.z);
						float clampResult149 = clamp( ( temp_output_142_0 + smoothstepResult90 ) , 0.0 , 1.0 );
						float4 temp_cast_0 = (1.0).xxxx;
						float2 uv_AlphaMask = packedInput.ase_texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
						#ifdef _USEALPHAMASK_ON
						float4 staticSwitch88 = tex2D( _AlphaMask, uv_AlphaMask );
						#else
						float4 staticSwitch88 = temp_cast_0;
						#endif
						
						surfaceDescription.Alpha = ( clampResult149 * staticSwitch88 ).r;
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
					float4 ase_texcoord1 : TEXCOORD1;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				float4 _ColorLow;
				float _ColourIntensity;
				float4 _ColorHigh;
				sampler2D _Noise;
				float2 _NoiseTiling;
				float2 _NoiseOffset;
				float _Resource;
				sampler2D _AlphaMask;
				float4 _AlphaMask_ST;
				
				                
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

					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.uv0.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
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
					float2 uv05 = packedInput.ase_texcoord.xy * _NoiseTiling + ( _Time.y * _NoiseOffset );
					float4 tex2DNode4 = tex2D( _Noise, uv05 );
					float temp_output_65_0 = (-0.125 + (_Resource - 0.0) * (0.1 - -0.125) / (1.0 - 0.0));
					float smoothstepResult90 = smoothstep( ( temp_output_65_0 + 0.015 ) , ( temp_output_65_0 - 0.015 ) , packedInput.ase_texcoord1.xyz.z);
					float smoothstepResult138 = smoothstep( ( temp_output_65_0 + 0.03 ) , temp_output_65_0 , packedInput.ase_texcoord1.xyz.z);
					float temp_output_142_0 = ( tex2DNode4.r * smoothstepResult138 );
					float4 lerpResult13 = lerp( ( _ColorLow * _ColourIntensity ) , ( _ColorHigh * _ColourIntensity ) , ( ( tex2DNode4.r * smoothstepResult90 ) + temp_output_142_0 ));
					
					float clampResult149 = clamp( ( temp_output_142_0 + smoothstepResult90 ) , 0.0 , 1.0 );
					float4 temp_cast_1 = (1.0).xxxx;
					float2 uv_AlphaMask = packedInput.ase_texcoord.xy * _AlphaMask_ST.xy + _AlphaMask_ST.zw;
					#ifdef _USEALPHAMASK_ON
					float4 staticSwitch88 = tex2D( _AlphaMask, uv_AlphaMask );
					#else
					float4 staticSwitch88 = temp_cast_1;
					#endif
					
					surfaceDescription.Color =  lerpResult13.rgb;
					surfaceDescription.Alpha = ( clampResult149 * staticSwitch88 ).r;
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
1921;1;1918;1017;1259.215;312.5914;1;True;False
Node;AmplifyShaderEditor.Vector2Node;6;-1281.576,114.9255;Float;False;Property;_NoiseOffset;Noise Offset;3;0;Create;True;0;0;False;0;0.2,1;0.2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;32;-1301.505,246.0881;Float;False;Property;_Resource;Resource %;0;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;8;-1282.875,32.22549;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;65;-1016.24,290.6524;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.125;False;4;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-1104.675,79.22549;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;9;-1118.671,-46.20181;Float;False;Property;_NoiseTiling;Noise Tiling;2;0;Create;True;0;0;False;0;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;141;-1019.791,597.8727;Float;False;Constant;_FlameGradientSize;Flame Gradient Size;9;0;Create;True;0;0;False;0;0.03;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;5;-932.2423,14.73864;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;10;-958.6713,-176.2017;Float;True;Property;_Noise;Noise;1;0;Create;True;0;0;False;0;cd460ee4ac5c1e746b7a734cc7cc64dd;cd460ee4ac5c1e746b7a734cc7cc64dd;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.PosVertexDataNode;86;-705.0936,341.6786;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;150;-734.2155,508.4086;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;93;-1031.449,185.3511;Float;False;Constant;_PrimaryGradientSize;Primary Gradient Size;9;0;Create;True;0;0;False;0;0.015;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;139;-782.7925,554.1677;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;138;-500.3827,493.6322;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;91;-773.9407,147.8127;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;137;-776.1179,245.6519;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-669.5424,-45.86137;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;90;-488.741,231.9127;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;142;-243.9569,244.2199;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;148;-102.9855,296.8483;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;-121.1902,-310.1096;Float;False;Property;_ColorLow;Color Low;5;0;Create;True;0;0;False;0;0.3568628,0.06942323,0.06666666,0;1,0.1058823,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-229.1374,479.002;Float;True;Property;_AlphaMask;Alpha Mask;8;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;17;-94.19016,-57.10935;Float;False;Property;_ColorHigh;Color High;4;0;Create;True;0;0;False;0;1,0.5436541,0,0;1,0.772549,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;89;-108.4528,399.4725;Float;False;Constant;_FullOpacity;Full Opacity;9;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;87;-249.7621,149.7193;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-88.19016,-138.1096;Float;False;Property;_ColourIntensity;Colour Intensity;6;0;Create;True;0;0;False;0;5;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;161.81,-205.1096;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;149;192.4864,296.8599;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;88;81.54719,419.4724;Float;False;Property;_UseAlphaMask;Use Alpha Mask;7;0;Create;True;0;0;False;0;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;147;-101.2052,125.7859;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;154.81,-110.1095;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;357.2715,339.5289;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;13;358.0099,-159.3096;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;636.1108,45.01869;Float;False;True;2;Float;ASEMaterialInspector;0;4;Shaders_Burt/UI_Stamina;dfe2f27ac20b08c469b2f95c236be0c3;True;Forward Unlit;0;1;Forward Unlit;5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;5;0;False;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=ForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;4;True;True;True;True;False;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;-377,-48.09999;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;ShadowCaster;0;2;ShadowCaster;1;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;Depth prepass;0;0;Depth prepass;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=DepthForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;META;0;3;META;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
WireConnection;65;0;32;0
WireConnection;7;0;8;0
WireConnection;7;1;6;0
WireConnection;5;0;9;0
WireConnection;5;1;7;0
WireConnection;150;0;65;0
WireConnection;139;0;65;0
WireConnection;139;1;141;0
WireConnection;138;0;86;3
WireConnection;138;1;139;0
WireConnection;138;2;150;0
WireConnection;91;0;65;0
WireConnection;91;1;93;0
WireConnection;137;0;65;0
WireConnection;137;1;93;0
WireConnection;4;0;10;0
WireConnection;4;1;5;0
WireConnection;90;0;86;3
WireConnection;90;1;91;0
WireConnection;90;2;137;0
WireConnection;142;0;4;1
WireConnection;142;1;138;0
WireConnection;148;0;142;0
WireConnection;148;1;90;0
WireConnection;87;0;4;1
WireConnection;87;1;90;0
WireConnection;28;0;16;0
WireConnection;28;1;27;0
WireConnection;149;0;148;0
WireConnection;88;1;89;0
WireConnection;88;0;29;0
WireConnection;147;0;87;0
WireConnection;147;1;142;0
WireConnection;26;0;17;0
WireConnection;26;1;27;0
WireConnection;63;0;149;0
WireConnection;63;1;88;0
WireConnection;13;0;28;0
WireConnection;13;1;26;0
WireConnection;13;2;147;0
WireConnection;1;0;13;0
WireConnection;1;1;63;0
ASEEND*/
//CHKSM=633F985A35CEF127E987528F772453C8A11D5C90