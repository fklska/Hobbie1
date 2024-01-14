using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Inventory;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
using NavMeshPlus.Components;
using System.Threading;

public class Controller : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody2D rb;
    public SpriteRenderer spr;
    public Animator anim;

    [Header("Stats")]
    public Characteristic CH;
    public Inventory inv;

    [Header("Selector")]
    public SelectManager selector;
    public NavMeshSurface navMesh;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        CH = GetComponent<Characteristic>();
        inv = GetComponent<Inventory>();

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
        if (movement.x < 0)
        {
            spr.flipX = false;
        }
    }

    public void Attack()
    {
        if (selector.IsSelected(gameObject))
        {
            anim.SetBool("Attack", Input.GetMouseButton(0));
        }
    }


    public void Hit()
    {
        if (target != null)
        {
            Characteristic targetCh = target.GetComponent<Characteristic>();
            Animator targetAnim = target.GetComponent<Animator>();
            if (targetCh.HEALTH > 0)
            {
                targetCh.HEALTH -= CH.STRENGHT;
            }
            targetAnim.SetBool("Hit", true);
        }

        else
        {
            if (resourse.Count != 0)
            {
                Resourse stats = resourse[0].GetComponent<Resourse>();
                if (inv.Add(resourse[0], CH.STRENGHT * 2))
                {
                    stats.UpgradeStatus();
                    stats.HEALTH -= CH.STRENGHT;
                    stats.STORAGE = stats.HEALTH * 2;
                    stats.defaultColor.b = stats.defaultColor.b - 0.2f;
                    stats.defaultColor.g = stats.defaultColor.g - 0.2f;
                }

                if (stats.HEALTH <= 0)
                {
                    Destroy(resourse[0]);
                    navMesh.UpdateNavMesh(navMesh.navMeshData);
                }
            }
        }
    }

    public void PostHit()
    {
        if (target != null)
        {
            Characteristic targetCh = target.GetComponent<Characteristic>();
            Animator targetAnim = target.GetComponent<Animator>();
            targetAnim.SetBool("Hit", false);

            if (targetCh.HEALTH <= 0)
            {
                targetAnim.SetBool("Death", true);
            }
        }
    }

    public void Death()
    {
        Destroy(gameObject);
    }


    public GameObject target = null;
    public List<GameObject> resourse;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Skeleton")
        {
            target = collision.gameObject;
        }

        if (collision.gameObject.layer == 6)
        {
            resourse.Add(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Skeleton")
        {
            target = null;
        }

        if (collision.gameObject.layer == 6)
        {
            resourse.Remove(collision.gameObject);
        }
    }
}
