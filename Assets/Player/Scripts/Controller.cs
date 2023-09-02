using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody2D rb;
    public SpriteRenderer spr;
    public Characteristic CH;
    public Animator anim;

    [Header("Selector")]
    public SelectManager selector;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spr = GetComponent<SpriteRenderer>();
        CH = GetComponent<Characteristic>();
        anim = GetComponent<Animator>();

        selector = Camera.main.GetComponent<SelectManager>();
    }

    void Update()
    {
        Attack();
    }

    private void FixedUpdate()
    {
        Movemvent();
        //Attack();
    }


    public Vector2 movement;
    public void Movemvent()
    {
        movement = new Vector2(Input.GetAxis("Horizontal") * CH.AGILITY * CH.RoadBonus, Input.GetAxis("Vertical") * CH.AGILITY * CH.RoadBonus);
        rb.velocity = movement;
        anim.SetFloat("MoveX", Mathf.Abs(movement.x) + Mathf.Abs(movement.y));

        if (movement.x > 0.01)
        {
            spr.flipX = true;
        }
        else
        {
            spr.flipX = false;
        }
    }

    public void Attack()
    {
        if (selector.IsSelected(gameObject))
        {
            anim.SetBool("Attack", Input.GetMouseButtonDown(0));
        }
    }
}
