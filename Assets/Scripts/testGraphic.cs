using UnityEngine;
using UnityEngine.UI;

public class testGraphic : MonoBehaviour
{
    Graphic m_Graphic;
    Color m_MyColor;

    void Start()
    {
        //Fetch the Graphic from the GameObject
        m_Graphic = GetComponent<Graphic>();
        //Create a new Color that starts as red
        m_MyColor = Color.red;
        //Change the Graphic Color to the new Color
        m_Graphic.color = m_MyColor;
        Debug.Log($"{gameObject.name}, depth:{m_Graphic.depth}");
    }

    // Update is called once per frame
    void Update()
    {
        //When the mouse button is clicked, change the Graphic Color
        if (Input.GetKey(KeyCode.Mouse0))
        {
            //Change the Color over time between blue and red while the mouse button is pressed
            m_MyColor = Color.Lerp(Color.red, Color.blue, Mathf.PingPong(Time.time, 1));
            m_Graphic.raycastTarget = !m_Graphic.raycastTarget;
        }

        //Change the Graphic Color to the new Color
        m_Graphic.color = m_MyColor;
    }
}