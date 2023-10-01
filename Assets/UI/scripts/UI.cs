using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [Header("HealthBar")]
    public Transform healthbar;
    public Slider slider;
    public Text text;

    [Header("Stats")]
    public Transform stats;
    public Text strenght;
    public Text agility;
    public Text bonus;
    public Image avatar;

    [Header("Inventory")]
    public Transform inventory;
    public Transform slot1;
    public Transform slot2;    
    public Transform slot3;
    public Transform slot4;

    private void Start()
    {
        healthbar = gameObject.transform.GetChild(0);
        slider = healthbar.GetComponentInChildren<Slider>();
        text = healthbar.GetComponentInChildren<Text>();

        stats = gameObject.transform.GetChild(1);
        strenght = stats.transform.GetChild(0).gameObject.GetComponent<Text>();
        agility = stats.transform.GetChild(1).gameObject.GetComponent<Text>();
        bonus = stats.transform.GetChild(2).gameObject.GetComponent<Text>();
        avatar = stats.transform.GetChild(3).gameObject.GetComponent<Image>();

        inventory = gameObject.transform.GetChild(2);
        slot1 = inventory.transform.GetChild(0);
        slot2 = inventory.transform.GetChild(1);
        slot3 = inventory.transform.GetChild(2);
        slot4 = inventory.transform.GetChild(3);
    }


    private void FixedUpdate()
    {
        GetSelectedInfo();
    }

    public List<GameObject> selected;
    public Characteristic ch;
    public Inventory inv;
    public void GetSelectedInfo()
    {
        selected = Camera.main.GetComponent<SelectManager>().selectedObjects;
        if (selected.Count != 0)
        {
            ch = selected[0].GetComponent<Characteristic>();
            slider.value = ch.HEALTH / 100f;
            text.text = Convert.ToString(ch.HEALTH);

            strenght.text = Convert.ToString(ch.STRENGHT);
            agility.text = Convert.ToString(ch.AGILITY);
            bonus.text = Convert.ToString(ch.RoadBonus);
            avatar.sprite = selected[0].GetComponent<SpriteRenderer>().sprite;

            bool is_get = selected[0].TryGetComponent<Inventory>(out inv);

            if (is_get)
            {
                foreach (var item in inv.inventory)
                {
                    slot1.gameObject.GetComponentInChildren<Text>().text = Convert.ToString(item.Value);
                    slot1.gameObject.GetComponentInChildren<Image>().sprite = item.Key.GetComponent<SpriteRenderer>().sprite;
                }
            }
        }
    }
}
