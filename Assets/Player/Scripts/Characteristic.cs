using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UI;

public class Characteristic : MonoBehaviour
{
    [Header("HeroStats")]
    [SerializeField] public int STRENGHT = 1;
    [SerializeField] public int AGILITY = 1;
    [SerializeField] public int HEALTH = 1;
    public float RoadBonus;


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Road")
        {
            RoadBonus = 1.30f;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        RoadBonus = 1f;
    }
}
