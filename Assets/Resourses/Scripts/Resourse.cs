using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Resourse : MonoBehaviour
{
    [Header("Color")]
    public SpriteRenderer sr;
    public Color selectedColor;
    public Color defaultColor = new Color(1f, 1f, 1f, 1f);
    public Color currentColor;
    public bool isClear;

    [Header("Inventory")]
    public Inventory inv;
    public Characteristic ch;



    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        inv = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
        ch = GameObject.FindGameObjectWithTag("Player").GetComponent<Characteristic>();
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

    public void OnClick()
    {
        if (PlayerArea)
        {
            if(inv.Add(gameObject, ch.STRENGHT*20))
            {
                HEALTH -= ch.STRENGHT;
                STORAGE = HEALTH * 20;
                defaultColor.b = defaultColor.b - 0.2f;
                defaultColor.g = defaultColor.g - 0.2f;
            }
        }
        if (HEALTH <= 0)
        {
            Destroy (gameObject);
        }
    }

    public void OnExit()
    {
        isClear = true;
    }

    [Header("Resourses")]
    public bool PlayerArea;
    public int HEALTH = 5;
    public int STORAGE = 100;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            PlayerArea = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            PlayerArea = false;
        }
    }
}
