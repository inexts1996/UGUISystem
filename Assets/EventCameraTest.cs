using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventCameraTest : MonoBehaviour
{
    // Start is called before the first frame update
    private GraphicRaycaster m_Raycaster;
    private Canvas curCanvas;
    void Start()
    {
        m_Raycaster = GetComponent<GraphicRaycaster>();
        curCanvas = GetComponent<Canvas>();
        // Debug.Log($"canvasWorldCamera:{m_Raycaster.eventCamera.name}");
        Debug.Log($"canvas targetDisplay:{curCanvas.targetDisplay}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
