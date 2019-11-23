using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            Rename();
    }

    void Rename()
    {
        int x = 0;
        foreach (Transform child in transform)
        {
            child.name = (++x).ToString();
        }
    }
}
