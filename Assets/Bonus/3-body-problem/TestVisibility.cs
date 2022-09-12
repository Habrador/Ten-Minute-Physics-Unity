using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVisibility : MonoBehaviour
{
    private void Update()
    {
        if(!GetComponent<Renderer>().isVisible)
        {
            Debug.Log("Cube not visible");
        }
    }
}
