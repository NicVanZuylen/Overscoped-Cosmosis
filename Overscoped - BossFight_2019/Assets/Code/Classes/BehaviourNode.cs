﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

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
                MethodInfo function = classInstance.GetType().GetMethod(actionFuncName);

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
                MethodInfo function = classInstance.GetType().GetMethod(conditionFuncName);

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

    /*
    public class CompositeNode
    {
        private CompositeNode m_parent;

        protected List<CompositeNode> m_children;
        protected List<Condition> m_conditions;
        protected List<Action> m_actions;

        public CompositeNode()
        {
            m_parent = null;

            m_children = new List<CompositeNode>();
            m_conditions = new List<Condition>();
            m_actions = new List<Action>();
        }

        // Add composite node to the tree.
        public void AddNode(CompositeNode node)
        {
            // A node cannot be added if it already has a parent.
            if (node.m_parent != null)
                return;

            node.m_parent = this;
            m_children.Add(node);
        }

        // Remove an existing composite node.
        public void RemoveNode(CompositeNode node)
        {
            node.m_parent = null;
            m_children.Remove(node);
        }

        // Remove an existing composite node at the given index.
        public void RemoveNodeAt(int nIndex)
        {
            m_children[nIndex].m_parent = null;
            m_children.RemoveAt(nIndex);
        }

        // Add an action this node will perform on success.
        public void AddAction(Action action)
        {
            m_actions.Add(action);
        }

        // Remove and existing action from this node.
        public void RemoveAction(Action action)
        {
            m_actions.Remove(action);
        }

        // Remove an existing action from this node at the given index.
        public void RemoveActionAt(int nIndex)
        {
            m_actions.RemoveAt(nIndex);
        }

        // Add a condition that must be met for this node to return success.
        public void AddCondition(Condition condition)
        {
            m_conditions.Add(condition);
        }

        // Remove an existing condition from this node.
        public void RemoveCondition(Condition condition)
        {
            m_conditions.Remove(condition);
        }

        // Remove an existing condition from this node at the given index.
        public void RemoveConditionAt(int nIndex)
        {
            m_conditions.RemoveAt(nIndex);
        }

        protected void PerformActions()
        {
            for (int i = 0; i < m_actions.Count; ++i)
                m_actions[i].m_action();
        }
    }
    */

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

            /*
            // Query children until one result returns success.
            for (int i = 0; i < m_children.Count; ++i)
            {
                result |= m_children[i].Run();

                // If there is a success perform actions.
                if (result == ENodeResult.NODE_SUCCESS)
                {
                    PerformActions();
                    return ENodeResult.NODE_SUCCESS;
                }
            }

            // If none of those were successful, query conditions...
            for (int i = 0; i < m_conditions.Count; ++i)
            {
                result |= m_conditions[i].m_condition();

                // If there is a success perform actions.
                if (result == ENodeResult.NODE_SUCCESS)
                {
                    PerformActions();
                    return ENodeResult.NODE_SUCCESS;
                }
            }

            // Otherwise actions will still be performed and return success if there is any.
            if (m_actions.Count > 0)
            {
                result = ENodeResult.NODE_SUCCESS;
                PerformActions();
            }
            */

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

            /*
            // Query children until one result returns failure.
            for (int i = 0; i < m_children.Count; ++i)
            {
                result &= m_children[i].Run();

                if (result == ENodeResult.NODE_FAILURE)
                    return ENodeResult.NODE_FAILURE;
            }

            // If no child nodes failed, query conditions...
            for (int i = 0; i < m_conditions.Count; ++i)
            {
                result &= m_conditions[i].m_condition();

                if (result == ENodeResult.NODE_FAILURE)
                    return ENodeResult.NODE_FAILURE;
            }

            // If there is no failiure by this point all conditions/child nodes returned success, perform actions.
            PerformActions();
            */

            return result;
        }
    }
}