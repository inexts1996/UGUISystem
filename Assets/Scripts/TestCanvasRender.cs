using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCanvasRender : MonoBehaviour
{
    // Start is called before the first frame update
    private CanvasRenderer render;

    private void Awake()
    {
        render = GetComponent<CanvasRenderer>();
        render.cull = true;
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            render.cull = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            render.cull = false;
        }
    }
    
    
}
