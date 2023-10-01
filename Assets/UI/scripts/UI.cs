using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{

    public Transform healthbar;
    public Slider slider;
    public Text text;

    private void Start()
    {
        healthbar = gameObject.transform.GetChild(0);
        slider = healthbar.GetComponentInChildren<Slider>();
        text = healthbar.GetComponentInChildren<Text>();
    }


    private void FixedUpdate()
    {
        GetSelectedInfo();
    }

    public List<GameObject> selected;
    public Characteristic ch;
    public void GetSelectedInfo()
    {
        selected = Camera.main.GetComponent<SelectManager>().selectedObjects;
        if (selected.Count != 0)
        {
            ch = selected[0].GetComponent<Characteristic>();
            slider.value = ch.HEALTH / 100f;
            text.text = Convert.ToString(ch.HEALTH);
        }
    }
}
