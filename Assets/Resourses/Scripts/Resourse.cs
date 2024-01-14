using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Resourse : MonoBehaviour
{
    [Header("Color")]
    public SpriteRenderer sr;
    public Color selectedColor;
    public Color defaultColor = new Color(1f, 1f, 1f, 1f);
    public Color currentColor;
    public bool isClear;



    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        currentColor = defaultColor;
    }

    private void FixedUpdate()
    {
        if (isClear)
        {
            currentColor = Color.Lerp(currentColor, defaultColor, Time.deltaTime);
        }
        sr.color = currentColor;
    }

    public void OnEnter()
    {
        currentColor = selectedColor;
        isClear = false;
    }

    public void OnExit()
    {
        isClear = true;
    }

    [Header("Resourses")]
    public bool PlayerArea;
    public int HEALTH = 5;
    public int STORAGE = 100;
    public void ShowStatus()
    {
        UpgradeStatus();
        gameObject.transform.GetChild(0).gameObject.SetActive(true);

    }

    public void UpgradeStatus()
    {
        Transform healthbar = gameObject.transform.GetChild(0).gameObject.transform.GetChild(0);
        Slider slider = healthbar.GetComponentInChildren<Slider>();
        Text text = healthbar.GetComponentInChildren<Text>();
        slider.value = HEALTH / 5f;
        text.text = Convert.ToString(HEALTH);
    }

    public void HideStatus()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }

    [Header("Inventory")]
    public Inventory inv;
    public Characteristic ch;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            ShowStatus();
            PlayerArea = true;
            ch = collision.gameObject.GetComponent<Characteristic>();
            inv = collision.gameObject.GetComponent<Inventory>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            HideStatus();
            PlayerArea = false;
            ch = null;
            inv = null;
        }
    }
}
