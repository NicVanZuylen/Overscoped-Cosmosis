using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BehaviourTree;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

#if (UNITY_EDITOR)

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


    public class BTreeEditor : EditorWindow
    {

        static Node m_baseNode = null;
        static NodeData m_data = null;
        static Stack<BTreeEditAction> m_actions = new Stack<BTreeEditAction>();
        static string m_loadedPath;

        const float m_fDefaultScale = 1000.0f;

        // View movement.
        Vector2 m_v2LastMousePos;
        private float m_fZoomFactor = 1.0f;

        public static Vector2 m_v2GlobalViewOffset;

        // Undo
        bool m_bControlDown;

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

        // On enable...
        private void OnEnable()
        {
            if (m_baseNode == null)
            {
                m_baseNode = new Node(ENodeType.NODE_COMPOSITE_SELECTOR, null, "Base");

                RestoreTemp();

                m_v2LastMousePos = Vector2.zero;
                m_v2GlobalViewOffset = Vector2.zero;
            }
        }

        private void OnDisable()
        {
            // Save to temp file.
            if (m_baseNode != null)
                Save(Application.dataPath + "/BTreeTemp.xml", true);
        }

        public static void RestoreTemp()
        {
            Debug.Log("Behaviour Tree Editor: Restoring Temp Data.");
            m_baseNode = new Node(Load(Application.dataPath + "/BTreeTemp.xml", true));
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

            for (int i = 0; i < nHorizontalDiv; ++i)
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

                    if (e.keyCode == KeyCode.LeftControl)
                    {
                        m_bControlDown = true;
                    }
                    else if (m_bControlDown && e.keyCode == KeyCode.Z)
                    {
                        Undo();
                    }
                    else if (m_bControlDown && e.keyCode == KeyCode.S)
                    {
                        Save(m_loadedPath);
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
            if (m_baseNode == null)
                return;

            Vector2 v2BasePos = new Vector2(800.0f, 10.0f) + m_v2GlobalViewOffset;

            m_baseNode.ProcessEvents(Event.current, v2BasePos);

            ProcessEvents(Event.current);

            m_baseNode.Reposition(m_fDefaultScale * m_fZoomFactor, m_fZoomFactor);

            DrawGrid(128.0f, 0.2f, Color.white);
            DrawGrid(32.0f, 0.1f, Color.white);

            Handles.BeginGUI();

            BeginWindows();

            m_baseNode.Draw(v2BasePos);

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

                if (bClear)
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

        private void Save(string path, bool bTempLoad = false)
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

                if (!bTempLoad)
                {
                    Debug.Log("Behaviour Tree Editor: Behaviour Tree Saved at Path: " + path);

                    m_loadedPath = path;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private static NodeData Load(string path, bool bTempLoad = false)
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

                if (!bTempLoad)
                {
                    Debug.Log("Behaviour Tree Editor: Behaviour Tree Loaded at Path: " + path);

                    m_loadedPath = path;
                }

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

#endif