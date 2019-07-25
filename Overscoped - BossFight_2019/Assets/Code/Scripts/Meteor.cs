using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meteor : MonoBehaviour
{
    private GameObject m_player;
    private Vector3 m_target;
    private bool m_bDelayOver;
    // Start is called before the first frame update
    void Start()
    {
        m_player = GameObject.Find("PlayerBody");
        m_target = m_player.transform.position;
        StartCoroutine(Delay());
    }

    // Update is called once per frame
    void Update()
    {
        //Make meteor move toward player
        if(m_bDelayOver)
            transform.position = Vector3.MoveTowards(transform.position, m_target, Time.deltaTime * 50);
    }

    IEnumerator Delay()
    {
        yield return new WaitForSeconds(1);
        m_bDelayOver = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
