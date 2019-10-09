using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BehaviourTree;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

/*
 * Description: Editor node class for the Behaviour Tree Editor.
 * Author: Nic Van Zuylen
*/

namespace BTreeEditor
{
    public enum ENodeType
    {
        NODE_COMPOSITE_SELECTOR,
        NODE_COMPOSITE_SEQUENCE,
        NODE_ACTION,
        NODE_CONDITION
    }

    public class NodeData
    {
        // Data
        public ENodeType m_eType;
        public string m_name;
        public string m_funcName;
        public string m_description;
        public NodeData[] m_children;

        public NodeData()
        {
            m_eType = ENodeType.NODE_COMPOSITE_SELECTOR;
            m_children = null;
            m_name = "Node";
            m_funcName = "NO_FUNCTION";
            m_description = "";
        }

#if (UNITY_EDITOR)
        public NodeData(Node node)
        {
            m_eType = node.GetNodeType();
            m_name = node.GetName();
            m_funcName = node.GetFuncName();
            m_description = node.GetDescription();

            m_children = new NodeData[node.m_children.Count];

            // Add all children of the provided node to this node data object's children.
            // These children will add thier children, until the tree is complete.
            for (int i = 0; i < node.m_children.Count; ++i)
            {
                m_children[i] = new NodeData(node.m_children[i]);
            }
        }
#endif

        public static NodeData Load(string path, bool bTempLoad = false)
        {
            try
            {
                // Create an XML reader.
                XmlSerializer reader = new XmlSerializer(typeof(NodeData));

                // Open file...
                FileStream file = File.Open(path, FileMode.Open);

                // Load into data buffer.
                NodeData data = (NodeData)reader.Deserialize(file);

                // Close file.
                file.Close();

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);

                return null;
            }
        }

        public static BehaviourNode LoadTree(string path, object classInstance)
        {
            NodeData data = Load(path);
            BehaviourNode baseNode = null;

            // Initialize base node, it can be either a selector or sequence.
            switch (data.m_eType)
            {
                case ENodeType.NODE_COMPOSITE_SELECTOR:
                    baseNode = new CompositeSelector();
                    break;

                case ENodeType.NODE_COMPOSITE_SEQUENCE:
                    baseNode = new CompositeSequence();
                    break;
            }

            // Create and add children.
            ConstructNode(baseNode, data, classInstance);

            return baseNode;
        }

        private static void ConstructNode(BehaviourNode parent, NodeData data, object classInstance)
        {
            for (int i = 0; i < data.m_children.Length; ++i)
            {
                NodeData childData = data.m_children[i];

                switch (childData.m_eType)
                {
                    case ENodeType.NODE_COMPOSITE_SELECTOR:
                        CompositeSelector newSelector = new CompositeSelector();
                        ConstructNode(newSelector, childData, classInstance); // Create and add children.

                        parent.AddNode(newSelector);
                        break;

                    case ENodeType.NODE_COMPOSITE_SEQUENCE:
                        CompositeSequence newSequence = new CompositeSequence();
                        ConstructNode(newSequence, childData, classInstance); // Create and add children.

                        parent.AddNode(newSequence);
                        break;

                    case ENodeType.NODE_ACTION:
                        BehaviourNode newAction = new BehaviourTree.Action(childData.m_funcName, classInstance);

                        parent.AddNode(newAction); // Create and add action function.
                        break;

                    case ENodeType.NODE_CONDITION:

                        BehaviourNode newCondition = new Condition(childData.m_funcName, classInstance);

                        parent.AddNode(newCondition); // Create and add condition function.
                        break;
                }
            }
        }
    }
}

#if (UNITY_EDITOR)

namespace BTreeEditor
{

    public class Node
    {
        public static bool m_bLayoutChange = false;

        public const float fNodeWidth = 128.0f + 32.0f;
        public const float fNodeHeight = 128.0f + 64.0f;
        public const float fNodeSpacing = 20.0f;

        public List<Node> m_children;
        public Stack<Node> m_deletedChildren;

        static NodeData m_nodeClipboard = null;

        ENodeType m_eType;
        Node m_parent;
        float m_fBoundingRange;
        Vector2 m_v2LocalPosition;
        string m_name;
        string m_funcName;
        string m_description;

        // Provides unique indices for node windows when drawing.
        private static int nWindowIndex = 0;

        public Node(ENodeType eType, Node parent, string name = "Node")
        {
            m_eType = eType;
            m_parent = parent;
            m_children = new List<Node>();
            m_deletedChildren = new Stack<Node>();
            m_name = name;
            m_funcName = "NO_FUNCTION";
            m_description = "";
        }

        public Node(ENodeType eType, Node parent, string name, string funcName = "NO_FUNCTION")
        {
            m_eType = eType;
            m_parent = parent;
            m_children = new List<Node>();
            m_deletedChildren = new Stack<Node>();
            m_name = name;
            m_funcName = funcName;
            m_description = "";
        }

        // Construct a node tree from node save data, this node will act as the base node.
        public Node(NodeData data)
        {
            m_eType = data.m_eType;
            m_name = data.m_name;
            m_funcName = data.m_funcName;
            m_description = data.m_description;

            m_parent = null;
            m_children = new List<Node>();
            m_deletedChildren = new Stack<Node>();

            for (int i = 0; i < data.m_children.Length; ++i)
            {
                AddChild(new Node(data.m_children[i]));
            }
        }

        // ---------------------------------------------------------------------------------+
        // Events and menu callbacks

        private static void ProcessContextMenu(Node node)
        {
            GenericMenu contextMenu = new GenericMenu();
            if (node.m_eType < ENodeType.NODE_ACTION)
            {
                // Adding nodes...
                contextMenu.AddItem(new GUIContent("Add Node/Selector"), false, node.AddSelectorCallback);
                contextMenu.AddItem(new GUIContent("Add Node/Sequence"), false, node.AddSequenceCallback);
                contextMenu.AddSeparator("Add Node/");
                contextMenu.AddItem(new GUIContent("Add Node/Action"), false, node.AddActionCallback);
                contextMenu.AddItem(new GUIContent("Add Node/Condition"), false, node.AddConditionCallback);

                contextMenu.AddSeparator("");

                // Change type...
                contextMenu.AddItem(new GUIContent("Change Type/Selector"), false, node.ChangeTypeToSelectorCallback);
                contextMenu.AddItem(new GUIContent("Change Type/Sequence"), false, node.ChangeTypeToSequenceCallback);
            }

            // Option to delete the node if it has a parent, if it doesn't have a parent it must be the base node and cannot be deleted.
            if (node.m_parent != null)
            {
                // Copy...
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent("Copy"), false, node.NodeCopyCallback);
            }

            // Paste...
            if (m_nodeClipboard != null)
            {
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent("Paste"), false, node.NodePasteCallback);
            }

            if (node.m_parent != null)
            {
                // Delete...
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent("Delete Node"), false, node.DeleteCallback);
            }

            contextMenu.ShowAsContext();
        }

        public void AddSelectorCallback()
        {
            AddChild(new Node(ENodeType.NODE_COMPOSITE_SELECTOR, this, "New Node"));
        }

        public void AddSequenceCallback()
        {
            AddChild(new Node(ENodeType.NODE_COMPOSITE_SEQUENCE, this, "New Node"));
        }

        public void AddActionCallback()
        {
            AddChild(new Node(ENodeType.NODE_ACTION, this, "New Node"));
        }

        public void AddConditionCallback()
        {
            InsertChild(0, new Node(ENodeType.NODE_CONDITION, this, "New Node"));
        }

        public void ChangeTypeToSelectorCallback()
        {
            BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_NODE_TYPE_CHANGE_TO_SELECTOR, this));
            m_eType = ENodeType.NODE_COMPOSITE_SELECTOR;
        }

        public void ChangeTypeToSequenceCallback()
        {
            BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_NODE_TYPE_CHANGE_TO_SEQUENCE, this));
            m_eType = ENodeType.NODE_COMPOSITE_SEQUENCE;
        }

        public void RenameCallback()
        {
            //EditorUtility.DisplayPopupMenu();

            BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_RENAME_NODE, this));
            EditorWindow.GetWindow<BTreeEditor>().ShowPopup();
        }

        public void NodeCopyCallback()
        {
            m_nodeClipboard = new NodeData(this);
        }

        public void NodePasteCallback()
        {
            BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_PASTE_NODE, this));
            AddChild(new Node(m_nodeClipboard));
        }

        public void DeleteCallback()
        {
            BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_DELETE_NODE, this, m_parent));
            m_parent.m_deletedChildren.Push(this);
            m_parent.RemoveChild(this);
        }

        // Process events for this node and all of it's children...
        public void ProcessEvents(Event e, Vector2 v2ParentPos)
        {
            // Process all children...
            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].ProcessEvents(e, m_v2LocalPosition + v2ParentPos);

            // Process this node...
            Rect nodeRect = new Rect(m_v2LocalPosition + v2ParentPos, new Vector2(fNodeWidth, fNodeHeight));

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 1 && nodeRect.Contains(e.mousePosition))
                    {
                        ProcessContextMenu(this);
                    }

                    break;
            }
            
        }

        // ---------------------------------------------------------------------------------
        // Positioning

        // Reposition this node and all it's children to neaten the tree structure.
        public float Reposition(float fReferenceWidth, float fScale)
        {
            if (m_children.Count > 0)
            {
                List<float> boundingRanges = new List<float>();
                float fTotalBoundingRange = 0.0f;

                // Reposition all children...
                for (int i = 0; i < m_children.Count; ++i)
                {
                    float fBoundingRange = m_children[i].Reposition(fReferenceWidth, fScale);
                    fTotalBoundingRange += fBoundingRange;

                    boundingRanges.Add(fBoundingRange);
                }

                float fCurrentTotalBoundRange = 0.0f;

                for (int i = 0; i < boundingRanges.Count; ++i)
                {
                    Vector2 v2ChildLocalPos = Vector2.zero;

                    v2ChildLocalPos.x = fCurrentTotalBoundRange;

                    v2ChildLocalPos.y = fNodeHeight * 1.5f;

                    m_children[i].m_v2LocalPosition = v2ChildLocalPos;

                    fCurrentTotalBoundRange += boundingRanges[i];
                }

                // Set final bounding range of this sub-tree.
                m_fBoundingRange = fTotalBoundingRange;
            }
            else
            {
                // Bounding range of this node and it's children, in this case it's just the with of the node since there are no children.
                m_fBoundingRange = fNodeWidth + fNodeSpacing;
            }

            return m_fBoundingRange;
        }

        public Node GetParent()
        {
            return m_parent;
        }

        public List<Node> GetAllBelow()
        {
            List<Node> nodeList = new List<Node>();

            nodeList.Add(this);

            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].AddToList(ref nodeList);

            return nodeList;
        }

        public List<int> GetAllChildCountsBelow()
        {
            List<int> indices = new List<int>();

            indices.Add(m_children.Count);

            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].AddToChildCount(ref indices);

            return indices;
        }

        private void AddToList(ref List<Node> nodeList)
        {
            nodeList.Add(this);

            // Increment number for all children.
            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].AddToList(ref nodeList);
        }

        private void AddToChildCount(ref List<int> indices)
        {
            indices.Add(m_children.Count);

            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].AddToChildCount(ref indices);
        }

        public ENodeType GetNodeType()
        {
            return m_eType;
        }

        public string GetName()
        {
            return m_name;
        }

        public string GetFuncName()
        {
            return m_funcName;
        }

        public string GetDescription()
        {
            return m_description;
        }

        // ---------------------------------------------------------------------------------
        // Children

        public void AddChild(Node node)
        {
            node.m_parent = this;
            m_children.Add(node);
            m_bLayoutChange = true;
        }

        public void InsertChild(int nIndex, Node node)
        {
            node.m_parent = this;
            m_children.Insert(nIndex, node);
            m_bLayoutChange = true;
        }

        public void RemoveChild(Node childNode)
        {
            if (m_children.Contains(childNode))
            {
                childNode.m_parent = null;
                m_children.Remove(childNode);
                m_bLayoutChange = true;
            }
        }

        public void RemoveLastChild()
        {
            if (m_children.Count > 0)
            {
                int childIndex = m_children.Count - 1;

                m_children[childIndex].m_parent = null;
                m_children.RemoveAt(childIndex);
                m_bLayoutChange = true;
            }
        }

        public void RestoreLastDeletedChild()
        {
            if (m_deletedChildren.Count > 0)
                AddChild(m_deletedChildren.Pop());
        }

        public void ShiftChildLeft(Node child)
        {
            int index = m_children.IndexOf(child);
            m_children.Remove(child);
            m_children.Insert(index - 1, child);
            m_bLayoutChange = true;
        }

        public void ShiftChildRight(Node child)
        {
            int index = m_children.IndexOf(child);
            m_children.Remove(child);
            m_children.Insert(index + 1, child);
            m_bLayoutChange = true;
        }

        // ---------------------------------------------------------------------------------
        // Drawing

        public void Draw(Vector2 v2ParentPos)
        {
            // Get correct window index...
            if (m_parent == null)
                nWindowIndex = 0;

            // Draw this node's window...

            Vector2 v2FinalPos = v2ParentPos + m_v2LocalPosition;

            Rect nodeRect = new Rect(v2FinalPos, new Vector2(fNodeWidth, fNodeHeight));

            string nameTypeExtension = "!";

            switch (m_eType)
            {
                case ENodeType.NODE_COMPOSITE_SELECTOR:
                    nameTypeExtension = "| Selector |";
                    break;

                case ENodeType.NODE_COMPOSITE_SEQUENCE:
                    nameTypeExtension = "& Sequence &";
                    break;

                case ENodeType.NODE_ACTION:
                    nameTypeExtension = "> Action >";
                    break;

                case ENodeType.NODE_CONDITION:
                    nameTypeExtension = "? Condition ?";
                    break;
            }

            GUI.Window(nWindowIndex, nodeRect, WindowFunction, nWindowIndex + " - " + m_name + " - " + nameTypeExtension);

            nWindowIndex++;

            // Draw line between this node and it's parent.
            if (m_parent != null)
            {
                // Start segment
                Vector2 v2StartPos = v2ParentPos + new Vector2(fNodeWidth * 0.5f, fNodeHeight);
                Vector2 v2EndPos = v2ParentPos + new Vector2(fNodeWidth * 0.5f, fNodeHeight * 1.25f);

                Handles.DrawLine(v2StartPos, v2EndPos);

                // Mid segment.
                v2StartPos = v2ParentPos + new Vector2(fNodeWidth * 0.5f, fNodeHeight * 1.25f);
                v2EndPos = v2FinalPos + new Vector2(fNodeWidth * 0.5f, fNodeHeight * -0.25f);

                Handles.DrawLine(v2StartPos, v2EndPos);

                // End segment.
                v2StartPos = v2FinalPos + new Vector2(fNodeWidth * 0.5f, fNodeHeight * -0.25f);
                v2EndPos = v2FinalPos + new Vector2(fNodeWidth * 0.5f, 0.0f);

                Handles.DrawLine(v2StartPos, v2EndPos);
            }

            for (int i = 0; i < m_children.Count; ++i)
            {
                // Draw children.
                m_children[i].Draw(v2ParentPos + m_v2LocalPosition);
            }
        }

        private void WindowFunction(int nWindowID)
        {
            if (m_parent != null)
            {
                float fButtonWidth = Mathf.Max(fNodeWidth * 0.1f, 28.0f);
                float fButtonX = fNodeWidth - fButtonWidth - 2.0f;

                // Move left button
                int nIndex = m_parent.m_children.IndexOf(this);

                if (GUI.Button(new Rect(new Vector2(fButtonX - fButtonWidth - 2.0f, 18.0f), new Vector2(fButtonWidth, 16.0f)), "<") && nIndex > 0)
                {
                    BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_MOVE_NODE_LEFT, this, m_parent));
                    m_parent.ShiftChildLeft(this);
                }

                // Move right button
                if (GUI.Button(new Rect(new Vector2(fButtonX, 18.0f), new Vector2(fButtonWidth, 16.0f)), ">") && nIndex < m_parent.m_children.Count - 1)
                {
                    BTreeEditor.AddAction(new BTreeEditAction(EActionType.ACTION_MOVE_NODE_RIGHT, this, m_parent));
                    m_parent.ShiftChildRight(this);
                }

                float fElementWidth = fNodeWidth - (2.5f * fButtonWidth);
                float fElementHeight = fNodeHeight * 0.6f;

                GUILayout.BeginVertical();

                GUILayout.Label("Name");
                m_name = GUILayout.TextField(m_name, 512);

                // Function field for action and condition nodes.
                if (m_eType >= ENodeType.NODE_ACTION)
                {
                    GUILayout.Label("Function");
                    m_funcName = GUILayout.TextField(m_funcName);
                }

                GUILayout.Label("Description");

                m_description = GUILayout.TextArea(m_description, 512, GUILayout.Height(fElementHeight));

                GUILayout.EndVertical();
            }
        }

        // ---------------------------------------------------------------------------------
    }
}

#endif
