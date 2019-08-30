using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorPull : PullObject
{
    private BossBehaviour m_bossScript;

    new void Awake()
    {
        base.Awake();

        m_bossScript = GetComponentInParent<BossBehaviour>();
    }

    public override void Trigger(Vector3 playerDirection)
    {
        // Decouple.
        base.Trigger(playerDirection);

        // Progress boss stage.
        m_bossScript.ProgressStage();
    }
}
