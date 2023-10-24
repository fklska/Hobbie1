using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public SpriteRenderer sr;
    private void Awake()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
    }

    public void Status()
    {
        if (interfers.Count > 1 && interfers[0].gameObject.tag != "Ground")
        {
            sr.color = new Color(1, 0.5f, 0.5f, 1);
        }
        else sr.color = new Color(1, 1, 1, 1);
    }

    public List<GameObject> interfers;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        interfers.Add(collision.gameObject);
        Status();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        interfers.Remove(collision.gameObject);
        Status();
    }
}
