using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSpriteController : MonoBehaviour
{
    private Toggle _toggle;
    private Image backgroundImg;

    public Sprite onSprite;
    public Sprite offSprite;
    public Image backGround;//控制背景图片遮住FakeVideoRender
    // Start is called before the first frame update
    void Start()
    {
        _toggle = transform.GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        _toggle.graphic = null;// 去除勾选图片
        
        backgroundImg = transform.Find("Background").GetComponent<Image>();
        backgroundImg.sprite = offSprite;
    }
    
    private void OnToggleValueChanged(bool value)
    {
        backgroundImg.sprite = value ? onSprite : offSprite;
        backGround.enabled = !value;
    }
}
