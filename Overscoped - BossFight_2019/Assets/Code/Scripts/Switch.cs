using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;

public class Switch : PullObject
{
    [SerializeField]
    private MonoBehaviour m_eventClass = null;

    [SerializeField]
    private string m_eventFuncName = "";

    private float m_fStartAngle;
    private float m_fEndAngle;

    private delegate void EventFunc();

    private EventFunc m_eventFunc;

    void Awake()
    {
        m_fStartAngle = transform.parent.rotation.eulerAngles.x;
        m_fEndAngle = -m_fStartAngle;

        // Look for a callback with a matching name in the provided class.
        MethodInfo callbackFunc = m_eventClass.GetType().GetMethod(m_eventFuncName);
  
        m_eventFunc = (EventFunc)Delegate.CreateDelegate(typeof(EventFunc), m_eventClass, callbackFunc);
    }

    public override void Trigger(Vector3 v3PlayerDirection)
    {
        // Play activation animation.
        transform.parent.rotation = Quaternion.Euler(m_fEndAngle, 0.0f, 0.0f);

        // Run event callback once.
        m_eventFunc();

        // Disable script.
        enabled = false;
    }
}
