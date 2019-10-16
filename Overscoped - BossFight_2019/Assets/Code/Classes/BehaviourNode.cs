using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/*
 * Description: Runtime behaviour tree class.
 * Author: Nic Van Zuylen
*/

namespace BehaviourTree
{
    public enum ENodeResult
    {
        NODE_FAILURE,
        NODE_SUCCESS
    }

    public abstract class BehaviourNode
    {
        protected BehaviourNode m_parent;
        protected List<BehaviourNode> m_children;

        public BehaviourNode()
        {
            m_children = new List<BehaviourNode>();
        }

        public abstract ENodeResult Run();

        // Add a node of any type to this node as a child of it.
        public void AddNode(BehaviourNode node)
        {
            node.m_parent = this;
            m_children.Add(node);
        }
    }

    public class Action : BehaviourNode
    {
        public delegate ENodeResult ActionFunc();
        private ActionFunc m_action;

        public Action(ActionFunc actionFunc)
        {
            m_action = actionFunc;
        }

        public Action(string actionFuncName, object classInstance)
        {
            try
            {
                // Get action method info.
                MethodInfo function = classInstance.GetType().GetMethod(actionFuncName);

                // Get action function delegate.
                m_action = (ActionFunc)Delegate.CreateDelegate(typeof(ActionFunc), classInstance, function);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not create action function delegate with name: " + actionFuncName + " " + e.Message);
                m_action = null;
            }
        }

        public override ENodeResult Run()
        {
            return m_action();
        }
    }

    public class Condition : BehaviourNode
    {
        public delegate ENodeResult QueryFunc();
        private QueryFunc m_condition;

        public Condition(QueryFunc conditionFunc)
        {
            m_condition = conditionFunc;
        }

        public Condition(string conditionFuncName, object classInstance)
        {
            try
            {
                // Get condition method info.
                MethodInfo function = classInstance.GetType().GetMethod(conditionFuncName);

                // Get condition function delegate.
                m_condition = (QueryFunc)Delegate.CreateDelegate(typeof(QueryFunc), classInstance, function);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not create condition function delegate with name: " + conditionFuncName + " " + e.Message);
                m_condition = null;
            }
        }

        public override ENodeResult Run()
        {
            return m_condition();
        }
    }

    public class CompositeSelector : BehaviourNode
    {
        public override ENodeResult Run()
        {
            // Success if at least one child node returns success.
            ENodeResult result = ENodeResult.NODE_FAILURE;

            // Run all childen, exit if one succeeds.
            for (int i = 0; i < m_children.Count; ++i)
            {
                result = m_children[i].Run();

                // Exit of one result succeeds.
                if (result == ENodeResult.NODE_SUCCESS)
                    return result;
            }

            return result;
        }
    }

    public class CompositeSequence : BehaviourNode
    {
        public override ENodeResult Run()
        {
            // Success if all child nodes return success.
            ENodeResult result = ENodeResult.NODE_SUCCESS;

            // Run all children, exit if one fails.
            for (int i = 0; i < m_children.Count; ++i)
            {
                result = m_children[i].Run();

                // Exit on failure.
                if (result == ENodeResult.NODE_FAILURE)
                    return result;
            }

            return result;
        }
    }

    public class SelectorOffshoot : BehaviourNode
    {
        public override ENodeResult Run()
        {
            // Run all children, exit if one fails.
            for (int i = 0; i < m_children.Count; ++i)
            {

                Debug.Log("K");
                if (m_children[i].Run() == ENodeResult.NODE_SUCCESS)
                    return ENodeResult.NODE_SUCCESS;
            }

            return ENodeResult.NODE_SUCCESS;
        }
    }

    public class SequenceOffshoot : BehaviourNode
    {
        public override ENodeResult Run()
        {
            // Run all children, exit if one fails.
            for (int i = 0; i < m_children.Count; ++i)
            {
                if (m_children[i].Run() == ENodeResult.NODE_FAILURE)
                    return ENodeResult.NODE_SUCCESS;
            }

            return ENodeResult.NODE_SUCCESS;
        }
    }
}