// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Diffusion Profile", "Miscellaneous", "Returns Diffusion Profile Hash Id. To be used on Diffusion Profile port on HDRP templates.", KeyCode.None, true, 0, int.MaxValue, typeof( DiffusionProfileSettings ) )]
	public sealed class DiffusionProfileNode : ParentNode
	{
		private const string DiffusionProfileStr = "Diffusion Profile";

		[SerializeField]
		private DiffusionProfileSettings m_diffusionProfile;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.INT, Constants.EmptyPortValue );
			m_textLabelWidth = 95;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			m_diffusionProfile = EditorGUILayoutObjectField( DiffusionProfileStr, m_diffusionProfile, typeof( DiffusionProfileSettings ),true ) as DiffusionProfileSettings;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			uint id = ( m_diffusionProfile != null ) ? m_diffusionProfile.profile.hash : 1074012128;
			return id.ToString();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			string guid = ( m_diffusionProfile != null ) ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_diffusionProfile ) ) : "0";
			IOUtils.AddFieldValueToString( ref nodeInfo, guid );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			string guid = GetCurrentParam( ref nodeParams );
			if( guid.Length > 1 )
			{
				m_diffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>( AssetDatabase.GUIDToAssetPath( guid ) );
			}
		}
	}
}
#endif
