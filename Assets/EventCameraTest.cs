using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventCameraTest : MonoBehaviour
{
    // Start is called before the first frame update
    private GraphicRaycaster m_Raycaster;
    private Canvas curCanvas;
    private Ray ray;
    private RaycastHit hitInfo;
    private Vector3 v3 = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
    private Vector3 hitPoint = Vector3.zero;
    private Camera mainCamera;
    private float screenWidth;

    void Start()
    {
        m_Raycaster = GetComponent<GraphicRaycaster>();
        curCanvas = GetComponent<Canvas>();
        // Debug.Log($"canvasWorldCamera:{m_Raycaster.eventCamera.name}");
        Debug.Log($"canvas targetDisplay:{curCanvas.targetDisplay}");
        mainCamera = Camera.main;
        screenWidth = Screen.width;
    }

    // Update is called once per frame
    void Update()
    {
        v3.x = v3.x > Screen.width ? 0f : v3.x + 1;
        ray = mainCamera.ScreenPointToRay(v3);

        if (Physics.Raycast(ray, out hitInfo, 100f))
        {
            Debug.DrawLine(ray.origin, hitInfo.point, Color.red);
            Debug.Log($"射线检测的物体名称：" + hitInfo.transform.name);
        }
    }
}