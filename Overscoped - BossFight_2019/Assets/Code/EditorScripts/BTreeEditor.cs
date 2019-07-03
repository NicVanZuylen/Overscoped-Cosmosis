using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BehaviourTree;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace BTreeEditor
{
    public enum EActionType
    {
        ACTION_ADD_NODE,
        ACTION_DELETE_NODE,
        ACTION_MOVE_NODE_LEFT,
        ACTION_MOVE_NODE_RIGHT,
        ACTION_NODE_TYPE_CHANGE_TO_SELECTOR,
        ACTION_NODE_TYPE_CHANGE_TO_SEQUENCE,
        ACTION_RENAME_NODE,
        ACTION_PASTE_NODE
    }

    public struct BTreeEditAction
    {
        public BTreeEditAction(EActionType action, Node associatedNode, Node parent = null)
        {
            m_action = action;
            m_node = associatedNode;
            m_nodeParent = parent;
        }

        public delegate void UndoAction();

        public EActionType m_action;
        public Node m_node;
        public Node m_nodeParent;
    }

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

        public NodeData(Node node)
        {
            m_eType = node.GetNodeType();
            m_name = node.GetName();
            m_funcName = node.GetFuncName();
            m_description = node.GetDescription();

            m_children = new NodeData[node.m_children.Count];

            // Add all children of the provided node to this node data object's children.
            // These children will add thier children, until the tree is complete.
            for(int i = 0; i < node.m_children.Count; ++i)
            {
                m_children[i] = new NodeData(node.m_children[i]);
            }
        }
    }

    public class BTreeData
    {
        public BTreeData()
        {
            m_nodes = null;
            m_nChildCounts = null;
        }

        public NodeData[] m_nodes; // Array containing all nodes in sequence from base to leaf.
        public int[] m_nChildCounts; // Data on the of children present on the node relating to the provided index.
    }

    public class Node
    {
        public static bool m_bLayoutChange = false;

        public const float fNodeWidth = 256.0f;
        public const float fNodeHeight = 300.0f;
        public const float fTopOffset = 64.0f;

        public List<Node> m_children;
        public Stack<Node> m_deletedChildren;

        static NodeData m_nodeClipboard = null;

        ENodeType m_eType;
        Node m_parent;
        Rect m_rect; // Bounds are used for positioning child nodes...
        Vector2 m_v2VisualDimensions;
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
            m_rect = new Rect();
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
            m_rect = new Rect();
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
            m_rect = new Rect();

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
            if(node.m_eType < ENodeType.NODE_ACTION)
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
        public void ProcessEvents(Event e)
        {
            // Process all children...
            for (int i = 0; i < m_children.Count; ++i)
                m_children[i].ProcessEvents(e);

            // Process this node...
            Vector2 v2ScaleOffset = new Vector2((fNodeWidth - m_v2VisualDimensions.x) * -0.5f, 0.0f);
            Rect nodeRect = new Rect(m_rect.position - v2ScaleOffset + BTreeEditor.m_v2GlobalViewOffset, m_v2VisualDimensions);
            switch (e.type)
            {
                case EventType.MouseDown:
                    if(e.button == 1 && nodeRect.Contains(e.mousePosition))
                    {
                        ProcessContextMenu(this);
                    }

                    break;
            }
        }

        // ---------------------------------------------------------------------------------
        // Positioning

        // Reposition this node and all it's children to neaten the tree structure.
        public void Reposition(float fReferenceWidth, float fScale)
        {
            if(m_parent != null)
            {
                int nChildCount = m_parent.m_children.Count;
                Vector2 v2ParentBounds = new Vector2(m_parent.m_rect.width, m_parent.m_rect.height);

                float fNodeArea = v2ParentBounds.x / nChildCount;
                int nThisChildIndex = m_parent.m_children.IndexOf(this);

                // Node visual dimensions
                m_v2VisualDimensions.x = fNodeArea * 0.75f;
                m_v2VisualDimensions.y = fNodeHeight * fScale * 0.5f;

                if (m_v2VisualDimensions.x > fNodeWidth)
                    m_v2VisualDimensions.x = fNodeWidth;

                float fLeftOffset = fNodeArea * 0.5f;
                m_rect.x = (m_parent.m_rect.x - v2ParentBounds.x * 0.5f) + fLeftOffset + (nThisChildIndex * fNodeArea);
                m_rect.y = m_parent.m_rect.y + (m_parent.m_rect.height * 0.5f) + fTopOffset;

                m_rect.width = fNodeArea; // Bounds of this node are the area it covers.
                m_rect.height = fNodeHeight * 2.0f * fScale; // Constant height for all nodes.
            }
            else
            {
                // This node is the base node.
                m_rect.x = (fReferenceWidth * 0.5f) - (fNodeWidth * 0.5f);
                m_rect.y = fTopOffset;
                m_rect.width = fReferenceWidth;
                m_rect.height = fNodeHeight * 2.0f * fScale;

                // Visual dimensions
                m_v2VisualDimensions = new Vector2(fNodeWidth, 80.0f * fScale);
            }

            // Reposition all children...
            for(int i = 0; i < m_children.Count; ++i)
            {
                m_children[i].Reposition(fReferenceWidth, fScale);
            }
        }

        // Used for positioning base nodes only.
        public void RepositionFree(float fReferenceWidth, float fScale)
        {
            if (m_parent == null)
            {
                // This node is the base node.
                m_rect.width = fReferenceWidth;
                m_rect.height = fNodeHeight * 2.0f * fScale;

                // Visual dimensions
                m_v2VisualDimensions = new Vector2(fNodeWidth, 80.0f * fScale);
            }
            else
            {
                Reposition(fReferenceWidth, fScale);
                return;
            }

            // Reposition all children...
            for (int i = 0; i < m_children.Count; ++i)
            {
                m_children[i].Reposition(fReferenceWidth, fScale);
            }
        }

        public Node GetParent()
        {
            return m_parent;
        }

        public Rect GetRect()
        {
            return m_rect;
        }

        public Vector2 GetDimensions()
        {
            return m_v2VisualDimensions;
        }

        public void SetPosition(Vector2 v2Position)
        {
            m_rect.position = v2Position;
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
            if(m_children.Contains(childNode))
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
            if(m_deletedChildren.Count > 0)
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

        public void Draw()
        {
            // Get correct window index...
            if (m_parent == null)
                nWindowIndex = 0;

            // Draw this node's window...
            Vector2 v2ScaleOffset = new Vector2((fNodeWidth - m_v2VisualDimensions.x) * -0.5f, 0.0f);
            Vector2 v2FinalPos = m_rect.position - v2ScaleOffset + BTreeEditor.m_v2GlobalViewOffset;

            Rect nodeRect = new Rect(v2FinalPos, m_v2VisualDimensions);

            string nameTypeExtension = "!";

            switch(m_eType)
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
                    nameTypeExtension = "= Condition =";
                    break;
            }

            GUI.Window(nWindowIndex, nodeRect, WindowFunction, nWindowIndex + " - " + m_name + " - " + nameTypeExtension);

            nWindowIndex++;

            // Draw curve between this node and it's parent.
            if(m_parent != null)
            {
                Vector2 v2StartPos = m_parent.m_rect.position - new Vector2((fNodeWidth - m_parent.m_v2VisualDimensions.x) * -0.5f, 0.0f)
                    + new Vector2(m_parent.m_v2VisualDimensions.x * 0.5f, m_parent.m_v2VisualDimensions.y);

                Vector2 v2EndPos = v2FinalPos + new Vector2(m_v2VisualDimensions.x * 0.5f, 0.0f);

                Handles.DrawLine(v2StartPos + BTreeEditor.m_v2GlobalViewOffset, v2EndPos);
            }

            // Draw children...
            for (int i = 0; i < m_children.Count; ++i)
            {
                m_children[i].Draw();
            }
        }

        private void WindowFunction(int nWindowID)
        {
            if (m_parent != null)
            {
                float fButtonWidth = Mathf.Max(m_v2VisualDimensions.x * 0.1f, 28.0f);
                float fButtonX = m_v2VisualDimensions.x - fButtonWidth - 2.0f;

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

                float fElementWidth = m_v2VisualDimensions.x - (2.5f * fButtonWidth);
                float fElementHeight = m_v2VisualDimensions.y * 0.6f;

                GUILayout.BeginVertical();

                GUILayout.Label("Name");
                m_name = GUILayout.TextField(m_name, GUILayout.Width(fElementWidth));

                // Function field for action and condition nodes.
                if(m_eType >= ENodeType.NODE_ACTION)
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

    public class BTreeEditor : EditorWindow
    {
        static Node m_baseNode = null;
        static NodeData m_data = null;
        static Stack<BTreeEditAction> m_actions = new Stack<BTreeEditAction>();

        const float m_fDefaultScale = 1000.0f;

        // View movement.
        Vector2 m_v2LastMousePos;
        private float m_fZoomFactor = 1.0f;

        public static Vector2 m_v2GlobalViewOffset;

        // Undo
        bool m_bControlDown;

        public static CompositeNode LoadTree(string path, object classInstance)
        {
            NodeData data = Load(path);
            CompositeNode baseNode = null;

            // Initialize base node, it can be either a selector or sequence.
            switch(data.m_eType)
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

        private static void ConstructNode(CompositeNode parent, NodeData data, object classInstance)
        {
            for(int i = 0; i < data.m_children.Length; ++i)
            {
                NodeData childData = data.m_children[i];

                switch(childData.m_eType)
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
                        parent.AddAction(new BehaviourTree.Action(childData.m_funcName, classInstance)); // Create and add action function.
                        break;

                    case ENodeType.NODE_CONDITION:
                        parent.AddCondition(new BehaviourTree.Condition(childData.m_funcName, classInstance)); // Create and add condition function.
                        break;

                        
                }
            }
        }

        // On enable...
        private void OnEnable()
        {
            if(m_baseNode == null)
            {
                m_baseNode = new Node(ENodeType.NODE_COMPOSITE_SELECTOR, null, "Base");
                m_v2LastMousePos = Vector2.zero;
                m_v2GlobalViewOffset = Vector2.zero;
            }
        }

        // Show window...
        [MenuItem("Window/BehaviourTree Editor")]
        private static void OpenWindow()
        {
            BTreeEditor window = GetWindow<BTreeEditor>();
            window.titleContent = new GUIContent("BehaviourTree Editor");
        }

        private void DrawGrid(float fSpacing, float fOpacity, Color gridColor)
        {
            // Find amount of time window width and height can be divided by grid spacing.
            int nHorizontalDiv = Mathf.CeilToInt(position.width / fSpacing);
            int nVerticalDiv = Mathf.CeilToInt(position.height / fSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, fOpacity);

            // Draw lines for all divisions...

            for(int i = 0; i < nHorizontalDiv; ++i)
            {
                Handles.DrawLine(new Vector3(fSpacing * i, 0.0f, 0.0f), new Vector3(fSpacing * i, position.height, 0.0f));
            }

            for (int i = 0; i < nVerticalDiv; ++i)
            {
                Handles.DrawLine(new Vector3(0.0f, fSpacing * i, 0.0f), new Vector3(position.width, fSpacing * i, 0.0f));
            }

            // Reset color.
            Handles.color = Color.white;

            Handles.EndGUI();
        }

        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.KeyDown:

                    if(e.keyCode == KeyCode.LeftControl)
                    {
                        m_bControlDown = true;
                    }
                    else if(m_bControlDown && e.keyCode == KeyCode.Z)
                    {
                        Undo();
                    }

                    break;

                case EventType.KeyUp:

                    if (e.keyCode == KeyCode.LeftControl)
                    {
                        m_bControlDown = false;
                    }

                    break;

                case EventType.MouseUp:
                    
                    break;

                case EventType.MouseDown:
                    m_v2LastMousePos = e.mousePosition; // Track mouse position.
                    break;

                case EventType.MouseDrag:

                    Vector2 v2MouseDelta = e.mousePosition - m_v2LastMousePos;
                    m_v2LastMousePos = e.mousePosition;

                    if (e.button == 2) // Drag view vector.
                    {
                        m_v2GlobalViewOffset += v2MouseDelta;
                        Node.m_bLayoutChange = true;
                    }
                    break;

                case EventType.ScrollWheel:

                    float fLastZoomFactor = m_fZoomFactor;
                    m_fZoomFactor -= e.delta.y * 0.1f;

                    // Clamp zoom factor.
                    m_fZoomFactor = Mathf.Clamp(m_fZoomFactor, 0.5f, 5.0f);

                    // Adjust view position to account for scale difference.
                    m_v2GlobalViewOffset.x += (fLastZoomFactor - m_fZoomFactor) * (0.5f * m_fDefaultScale);
                    m_v2GlobalViewOffset.y += (fLastZoomFactor - m_fZoomFactor) * (Node.fNodeHeight);

                    Node.m_bLayoutChange = true;

                    break;
            }
        }

        public static void AddAction(BTreeEditAction action)
        {
            m_actions.Push(action);
        }

        private void Undo()
        {
            // No actions to undo.
            if (m_actions.Count == 0)
                return;

            // Get most recent action.
            BTreeEditAction action = m_actions.Pop();
            
            // Take appropriate action to undo the change.
            switch (action.m_action)
            {
                case EActionType.ACTION_ADD_NODE:
                    action.m_node.RemoveLastChild();
                    break;

                case EActionType.ACTION_DELETE_NODE:
                    action.m_nodeParent.RestoreLastDeletedChild();
                    break;

                case EActionType.ACTION_MOVE_NODE_LEFT:
                    action.m_nodeParent.ShiftChildRight(action.m_node);
                    break;

                case EActionType.ACTION_MOVE_NODE_RIGHT:
                    action.m_nodeParent.ShiftChildLeft(action.m_node);
                    break;

                case EActionType.ACTION_NODE_TYPE_CHANGE_TO_SELECTOR:
                    action.m_node.ChangeTypeToSequenceCallback();
                    break;

                case EActionType.ACTION_NODE_TYPE_CHANGE_TO_SEQUENCE:
                    action.m_node.ChangeTypeToSelectorCallback();
                    break;

                case EActionType.ACTION_PASTE_NODE:
                    action.m_node.RemoveLastChild();
                    break;
            }

            Node.m_bLayoutChange = true;
        }

        private void OnGUI()
        {
            m_baseNode.ProcessEvents(Event.current);

            ProcessEvents(Event.current);

            m_baseNode.Reposition(m_fDefaultScale * m_fZoomFactor, m_fZoomFactor);

            DrawGrid(128.0f, 0.2f, Color.white);
            DrawGrid(32.0f, 0.1f, Color.white);

            Handles.BeginGUI();

            BeginWindows();

            m_baseNode.Draw();

            EndWindows();

            Handles.EndGUI();

            // Save/Load buttons...

            // Save
            if (GUI.Button(new Rect(10.0f, 10.0f, 100.0f, 45.0f), "Save"))
            {
                string savePath = EditorUtility.SaveFilePanel("Save", Application.dataPath, "NewBehaviourTree", "xml");

                Save(savePath);
            }

            // Load
            if (GUI.Button(new Rect(10.0f, 60.0f, 100.0f, 45.0f), "Open"))
            {
                string filePath = EditorUtility.OpenFilePanel("Save", Application.dataPath, "xml");

                m_baseNode = new Node(Load(filePath));
            }

            if (GUI.Button(new Rect(10.0f, 110.0f, 100.0f, 45.0f), "Clear"))
            {
                bool bClear = EditorUtility.DisplayDialog("Delete entire tree?", "Are you sure you want to delete the entire tree?", "Yes");

                if(bClear)
                {
                    m_baseNode = new Node(ENodeType.NODE_COMPOSITE_SELECTOR, null, "Base");
                }
            }

            if (Node.m_bLayoutChange)
            {
                Repaint();
                Node.m_bLayoutChange = false;
            }
        }

        private void Save(string path)
        {
            m_data = new NodeData(m_baseNode);

            // Create an XML writer.
            XmlSerializer writer = new XmlSerializer(typeof(NodeData));

            try
            {
                // Create file at the specified path...
                FileStream file = File.Create(path);

                // Write to file buffer.
                writer.Serialize(file, m_data);

                // Close file.
                file.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private static NodeData Load(string path)
        {
            // Create an XML reader.
            XmlSerializer reader = new XmlSerializer(typeof(NodeData));

            try
            {
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
    }
}
