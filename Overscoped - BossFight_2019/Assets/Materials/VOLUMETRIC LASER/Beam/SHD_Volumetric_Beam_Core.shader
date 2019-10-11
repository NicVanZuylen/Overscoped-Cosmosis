// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmosis/Burt/Volumetric_Beam_Core"
{
    Properties
    {
		_LineOrigin("LineOrigin", Vector) = (-244,-52,105,0)
		_LineLength("LineLength", Float) = 35
		_ColourTexture("Colour Texture", 2D) = "white" {}
		_ColourOffset("Colour Offset", Vector) = (0,0,0,0)
		_ColourTiling("Colour Tiling", Vector) = (1,1,0,0)
		[HDR]_Color1("Color 1", Color) = (0,0,0,0)
		[HDR]_Color0("Color 0", Color) = (0,0,0,0)
		_ColourVertexStrength("Colour Vertex Strength", Float) = 0
		_VertexTexture("Vertex Texture", 2D) = "white" {}
		_VertexStrength("Vertex Strength", Float) = 0
		_VertexTiling("Vertex Tiling", Vector) = (1,1,0,0)
		_VertexOffset("Vertex Offset", Vector) = (0,0,0,0)
		_BeamTaper("Beam Taper", Range( 0 , 50)) = 10
    }

    SubShader
    {
		
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		Blend One Zero
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
					
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif
				};

				sampler2D _ColourTexture;
				float3 _LineOrigin;
				float _LineLength;
				float2 _ColourTiling;
				float2 _ColourOffset;
				float _ColourVertexStrength;
				float _BeamTaper;
				sampler2D _VertexTexture;
				float2 _VertexTiling;
				float2 _VertexOffset;
				float _VertexStrength;
				
				                
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

					float2 uv067 = inputMesh.ase_texcoord * float2( 1,1 ) + float2( 0,0 );
					float temp_output_8_0 = ( distance( inputMesh.positionOS , _LineOrigin ) / _LineLength );
					float temp_output_57_0 = ( 1.0 - temp_output_8_0 );
					float2 appendResult66 = (float2(uv067.y , temp_output_57_0));
					float4 tex2DNode74 = tex2Dlod( _ColourTexture, float4( (appendResult66*_ColourTiling + ( _Time.y * _ColourOffset )), 0, 0.0) );
					float temp_output_124_0 = min( ( 1.0 - pow( ( 1.0 - temp_output_57_0 ) , _BeamTaper ) ) , ( 1.0 - pow( ( 1.0 - temp_output_8_0 ) , _BeamTaper ) ) );
					
					float3 vertexValue =  ( ( tex2DNode74.r * _ColourVertexStrength * temp_output_124_0 * inputMesh.normalOS.xyz ) + ( temp_output_124_0 * inputMesh.normalOS.xyz * tex2Dlod( _VertexTexture, float4( (appendResult66*_VertexTiling + ( _Time.y * _VertexOffset )), 0, 0.0) ).r * _VertexStrength ) );
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
					
					surfaceDescription.Alpha = 1;
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
					float4 ase_color : COLOR;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				struct PackedVaryingsMeshToPS 
				{
					float4 positionCS : SV_Position;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif 
				};

				sampler2D _ColourTexture;
				float3 _LineOrigin;
				float _LineLength;
				float2 _ColourTiling;
				float2 _ColourOffset;
				float _ColourVertexStrength;
				float _BeamTaper;
				sampler2D _VertexTexture;
				float2 _VertexTiling;
				float2 _VertexOffset;
				float _VertexStrength;
				float4 _Color0;
				float4 _Color1;
				
				                
		            
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

					float2 uv067 = inputMesh.ase_texcoord * float2( 1,1 ) + float2( 0,0 );
					float temp_output_8_0 = ( distance( inputMesh.positionOS , _LineOrigin ) / _LineLength );
					float temp_output_57_0 = ( 1.0 - temp_output_8_0 );
					float2 appendResult66 = (float2(uv067.y , temp_output_57_0));
					float4 tex2DNode74 = tex2Dlod( _ColourTexture, float4( (appendResult66*_ColourTiling + ( _Time.y * _ColourOffset )), 0, 0.0) );
					float temp_output_124_0 = min( ( 1.0 - pow( ( 1.0 - temp_output_57_0 ) , _BeamTaper ) ) , ( 1.0 - pow( ( 1.0 - temp_output_8_0 ) , _BeamTaper ) ) );
					
					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord3.xyz = ase_worldNormal;
					
					outputPackedVaryingsMeshToPS.ase_color = inputMesh.ase_color;
					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord3.w = 0;
					float3 vertexValue = ( ( tex2DNode74.r * _ColourVertexStrength * temp_output_124_0 * inputMesh.normalOS.xyz ) + ( temp_output_124_0 * inputMesh.normalOS.xyz * tex2Dlod( _VertexTexture, float4( (appendResult66*_VertexTiling + ( _Time.y * _VertexOffset )), 0, 0.0) ).r * _VertexStrength ) );
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
					float2 uv067 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float temp_output_8_0 = ( distance( packedInput.ase_texcoord1.xyz , _LineOrigin ) / _LineLength );
					float temp_output_57_0 = ( 1.0 - temp_output_8_0 );
					float2 appendResult66 = (float2(uv067.y , temp_output_57_0));
					float4 tex2DNode74 = tex2D( _ColourTexture, (appendResult66*_ColourTiling + ( _Time.y * _ColourOffset )) );
					float3 ase_worldPos = packedInput.ase_texcoord2.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = packedInput.ase_texcoord3.xyz;
					float fresnelNdotV112 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode112 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV112, 5.0 ) );
					float clampResult114 = clamp( fresnelNode112 , 0.0 , 1.0 );
					float4 lerpResult84 = lerp( _Color0 , _Color1 , ( tex2DNode74.r + clampResult114 ));
					
					surfaceDescription.Color =  ( packedInput.ase_color * lerpResult84 ).rgb;
					surfaceDescription.Alpha = 1;
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
					
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};
        
				struct PackedVaryingsMeshToPS
				{
					float4 positionCS : SV_Position;
					
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

								
				                
			    
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
						
						surfaceDescription.Alpha = 1;
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
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				sampler2D _ColourTexture;
				float3 _LineOrigin;
				float _LineLength;
				float2 _ColourTiling;
				float2 _ColourOffset;
				float _ColourVertexStrength;
				float _BeamTaper;
				sampler2D _VertexTexture;
				float2 _VertexTiling;
				float2 _VertexOffset;
				float _VertexStrength;
				float4 _Color0;
				float4 _Color1;
				
				                
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

					float2 uv067 = inputMesh.uv0.xy * float2( 1,1 ) + float2( 0,0 );
					float temp_output_8_0 = ( distance( inputMesh.positionOS , _LineOrigin ) / _LineLength );
					float temp_output_57_0 = ( 1.0 - temp_output_8_0 );
					float2 appendResult66 = (float2(uv067.y , temp_output_57_0));
					float4 tex2DNode74 = tex2Dlod( _ColourTexture, float4( (appendResult66*_ColourTiling + ( _Time.y * _ColourOffset )), 0, 0.0) );
					float temp_output_124_0 = min( ( 1.0 - pow( ( 1.0 - temp_output_57_0 ) , _BeamTaper ) ) , ( 1.0 - pow( ( 1.0 - temp_output_8_0 ) , _BeamTaper ) ) );
					
					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS);
					outputPackedVaryingsMeshToPS.ase_texcoord3.xyz = ase_worldNormal;
					
					outputPackedVaryingsMeshToPS.ase_color = inputMesh.color;
					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.uv0.xy;
					outputPackedVaryingsMeshToPS.ase_texcoord1 = float4(inputMesh.positionOS,0);
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord3.w = 0;
					float3 vertexValue = ( ( tex2DNode74.r * _ColourVertexStrength * temp_output_124_0 * inputMesh.normalOS ) + ( temp_output_124_0 * inputMesh.normalOS * tex2Dlod( _VertexTexture, float4( (appendResult66*_VertexTiling + ( _Time.y * _VertexOffset )), 0, 0.0) ).r * _VertexStrength ) );
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
					float2 uv067 = packedInput.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float temp_output_8_0 = ( distance( packedInput.ase_texcoord1.xyz , _LineOrigin ) / _LineLength );
					float temp_output_57_0 = ( 1.0 - temp_output_8_0 );
					float2 appendResult66 = (float2(uv067.y , temp_output_57_0));
					float4 tex2DNode74 = tex2D( _ColourTexture, (appendResult66*_ColourTiling + ( _Time.y * _ColourOffset )) );
					float3 ase_worldPos = packedInput.ase_texcoord2.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = packedInput.ase_texcoord3.xyz;
					float fresnelNdotV112 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode112 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV112, 5.0 ) );
					float clampResult114 = clamp( fresnelNode112 , 0.0 , 1.0 );
					float4 lerpResult84 = lerp( _Color0 , _Color1 , ( tex2DNode74.r + clampResult114 ));
					
					surfaceDescription.Color =  ( packedInput.ase_color * lerpResult84 ).rgb;
					surfaceDescription.Alpha = 1;
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
Version=16900
-1;2;1906;1011;954.8654;406.235;1.51634;True;False
Node;AmplifyShaderEditor.CommentaryNode;72;-1374.909,397.1762;Float;False;878.4163;467.8072;0-1 Gradient + UVs;11;2;126;66;57;67;125;8;9;5;4;6;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;6;-1361.909,638.5586;Float;False;Property;_LineOrigin;LineOrigin;0;0;Create;True;0;0;False;0;-244,-52,105;-242.109,-51.79,105.99;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PosVertexDataNode;4;-1332.909,495.5586;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DistanceOpNode;5;-1117.909,562.5585;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-1122.909,663.5583;Float;False;Property;_LineLength;LineLength;1;0;Create;True;0;0;False;0;35;33;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;8;-962.9103,591.5585;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;125;-674.8363,734.5558;Float;False;161.2848;112.8145;B to W;1;123;;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;57;-837.955,594.3547;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;126;-672.9425,600.886;Float;False;152.7418;125.8279;W to B;1;73;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;129;-433.3461,-245.2927;Float;False;1527.739;768.952;Colour;16;62;64;68;63;65;74;113;84;112;86;87;85;114;88;0;3;;0.9056604,0.7905427,0.4912781,1;0;0
Node;AmplifyShaderEditor.Vector2Node;62;-367.446,278.468;Float;False;Property;_ColourOffset;Colour Offset;3;0;Create;True;0;0;False;0;0,0;-0.2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;67;-910.1428,447.1762;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;128;-333.8665,639.8025;Float;False;721.731;273.6603;Beam Taper;8;124;145;143;139;144;140;146;142;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RelayNode;73;-651.4189,642.343;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;127;-285.0656,938.6874;Float;False;1039.657;352.0366;Spiral Vertex Offset;8;105;108;110;109;106;104;107;102;;0.4700465,0.4145159,0.6509434,1;0;0
Node;AmplifyShaderEditor.SimpleTimeNode;64;-383.3461,203.3679;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;123;-653.0146,773.8142;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;142;-205.2926,824.5241;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;146;-198.7487,675.8215;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;68;-229.5565,64.59512;Float;False;Property;_ColourTiling;Colour Tiling;4;0;Create;True;0;0;False;0;1,1;1,3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;140;-308.5808,748.3777;Float;False;Property;_BeamTaper;Beam Taper;12;0;Create;True;0;0;False;0;10;10;0;50;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;-180.7459,222.9681;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;108;-271.5539,1086.646;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;107;-260.5539,1154.646;Float;False;Property;_VertexOffset;Vertex Offset;11;0;Create;True;0;0;False;0;0,0;0,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;66;-674.1425,493.1761;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;-83.8255,1116.765;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;106;-114.8254,991.1891;Float;False;Property;_VertexTiling;Vertex Tiling;10;0;Create;True;0;0;False;0;1,1;1,3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ScaleAndOffsetNode;65;-32.6939,152.8378;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;139;-38.59171,785.0518;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;144;-38.47063,689.7122;Float;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;112;93.90419,321.6594;Float;False;Standard;WorldNormal;ViewDir;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;145;112.1906,710.014;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;143;113.5575,780.9464;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;114;326.9042,320.6594;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;74;178.6283,126.2543;Float;True;Property;_ColourTexture;Colour Texture;2;0;Create;True;0;0;False;0;None;1524804b94e1abd4dabefcde29b6b7fd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;105;95.44585,1004.646;Float;False;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;86;461.3932,-19.29263;Float;False;Property;_Color1;Color 1;5;1;[HDR];Create;True;0;0;False;0;0,0,0,0;0,0.0627451,1.498039,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;85;461.3932,-195.2927;Float;False;Property;_Color0;Color 0;6;1;[HDR];Create;True;0;0;False;0;0,0,0,0;0,0.003921569,0.2,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMinOpNode;124;265.4754,730.2255;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;361.0354,1205.737;Float;False;Property;_VertexStrength;Vertex Strength;9;0;Create;True;0;0;False;0;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;113;531.9042,156.6594;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;103;404.8704,734.4489;Float;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;117;343.8461,558.3987;Float;False;Property;_ColourVertexStrength;Colour Vertex Strength;7;0;Create;True;0;0;False;0;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;104;302.6542,1005.292;Float;True;Property;_VertexTexture;Vertex Texture;8;0;Create;True;0;0;False;0;None;f7389f5ab6a087a4ba093dd442286f11;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;87;717.3926,-163.2927;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;84;717.3926,-3.292632;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;624.3403,978.464;Float;False;4;4;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;116;615.8032,544.8491;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;920.1269,-52.64388;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;115;824.9925,770.9299;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;META;0;3;META;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;-552.4761,525.5453;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;ShadowCaster;0;2;ShadowCaster;1;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;1451.182,539.3654;Float;False;True;2;Float;ASEMaterialInspector;0;4;Cosmosis/Burt/Volumetric_Beam_Core;dfe2f27ac20b08c469b2f95c236be0c3;True;Forward Unlit;0;1;Forward Unlit;5;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=ForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;4;True;True;True;True;False;5;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;2;Float;ASEMaterialInspector;0;4;Hidden/Templates/HDSRPUnlit;dfe2f27ac20b08c469b2f95c236be0c3;True;Depth prepass;0;0;Depth prepass;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;0;False;False;False;False;True;False;False;False;False;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;True;1;LightMode=DepthForwardOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
WireConnection;5;0;4;0
WireConnection;5;1;6;0
WireConnection;8;0;5;0
WireConnection;8;1;9;0
WireConnection;57;0;8;0
WireConnection;73;0;57;0
WireConnection;123;0;8;0
WireConnection;142;0;123;0
WireConnection;146;0;73;0
WireConnection;63;0;64;0
WireConnection;63;1;62;0
WireConnection;66;0;67;2
WireConnection;66;1;57;0
WireConnection;109;0;108;0
WireConnection;109;1;107;0
WireConnection;65;0;66;0
WireConnection;65;1;68;0
WireConnection;65;2;63;0
WireConnection;139;0;142;0
WireConnection;139;1;140;0
WireConnection;144;0;146;0
WireConnection;144;1;140;0
WireConnection;145;0;144;0
WireConnection;143;0;139;0
WireConnection;114;0;112;0
WireConnection;74;1;65;0
WireConnection;105;0;66;0
WireConnection;105;1;106;0
WireConnection;105;2;109;0
WireConnection;124;0;145;0
WireConnection;124;1;143;0
WireConnection;113;0;74;1
WireConnection;113;1;114;0
WireConnection;104;1;105;0
WireConnection;84;0;85;0
WireConnection;84;1;86;0
WireConnection;84;2;113;0
WireConnection;102;0;124;0
WireConnection;102;1;103;0
WireConnection;102;2;104;1
WireConnection;102;3;110;0
WireConnection;116;0;74;1
WireConnection;116;1;117;0
WireConnection;116;2;124;0
WireConnection;116;3;103;0
WireConnection;88;0;87;0
WireConnection;88;1;84;0
WireConnection;115;0;116;0
WireConnection;115;1;102;0
WireConnection;1;0;88;0
WireConnection;1;3;115;0
ASEEND*/
//CHKSM=B250CC7438E5157BDD8DBBB36A09919EC95993E6