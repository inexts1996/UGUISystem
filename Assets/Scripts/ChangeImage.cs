using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeImage : MonoBehaviour
{
    // Start is called before the first frame update
    private Sprite imgSprite;
    private Button changeBtn;
    private Image img;

    private void Awake()
    {
        img = GameObject.Find("Image").transform.GetComponent<Image>();
        var imgRect = img.GetComponent<RectTransform>();
        List<Component> listComponent = new List<Component>();
        imgRect.GetComponents(typeof(Component), listComponent);
        Debug.Log($"componentCount:{listComponent.Count}");
        changeBtn = GetComponent<Button>();
    }

    void Start()
    {
        imgSprite = Resources.Load<Sprite>("Images/2");
        changeBtn.onClick.AddListener(OnClickChangeBtnHandler);
    }

    private void OnClickChangeBtnHandler()
    {
        img.sprite = imgSprite;
    }

    // Update is called once per frame
    void Update()
    {
    }
}