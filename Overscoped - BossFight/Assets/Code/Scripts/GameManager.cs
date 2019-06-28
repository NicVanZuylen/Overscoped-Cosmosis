using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }

        if (Input.GetKey("1"))
        {
            SceneManager.LoadScene("Greybox_Tutorial_001");
        }

        if (Input.GetKey("2"))
        {
            SceneManager.LoadScene("Greybox_Tutorial_002");
        }

        if (Input.GetKey("3"))
        {
            SceneManager.LoadScene("Greybox_Boss_Arena_001");
        }
    }
}
