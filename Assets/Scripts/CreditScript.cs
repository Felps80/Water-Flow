using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditScript : MonoBehaviour
{
    public float scrollSpeed = 40f;
    private RectTransform rectTransform;
    private Vector2 StartPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {       
        rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
    }

    public void StartCredits()
    {
        rectTransform.anchoredPosition = StartPosition;
    }

}
