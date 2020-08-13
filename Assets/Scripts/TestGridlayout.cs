using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestGridlayout : GridLayoutGroup
{
    protected override void OnDidApplyAnimationProperties()
    {
        Debug.Log(" OnDidApplyAnimationProperties");
        base.OnDidApplyAnimationProperties();
    }

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
    }
}