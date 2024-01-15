using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour
{
    public int Cost;
    public GameObject resourseCost;
    public SpriteRenderer sr;
    private void Awake()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
    }

    public bool Status()
    {
        if (interfers.Count > 1)
        {
            sr.color = new Color(1, 0.5f, 0.5f, 1);
            return false;
        }
        else 
        {
            sr.color = new Color(1, 1, 1, 1);
            return true;
        }
    }

    public int[] layers;
    public List<GameObject> interfers;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (layers.Contains(collision.gameObject.layer))
        {
            interfers.Add(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (layers.Contains(collision.gameObject.layer))
        {
            interfers.Remove(collision.gameObject);
        }
    }
}
