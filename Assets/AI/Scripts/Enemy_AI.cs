using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class Enemy_AI : MonoBehaviour
{
    [Header("NavMesh")]
    public NavMeshAgent agent;
    public Characteristic selfCH;

    [Header("Sprite")]
    public Animator anim;

    private void Start()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        selfCH = GetComponent<Characteristic>();

        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        MoveToTarger();
    }

    public void MoveToTarger()
    {
        if(target != null && !InArea)
        {
            agent.destination = target.transform.position;
        }
        float movement = Mathf.Abs(agent.velocity.x) + Mathf.Abs(agent.velocity.y);
        anim.SetFloat("Move", movement);
        anim.SetBool("Attack", InArea);
    }


    public void Attack()
    {
        if (target != null)
        {
            Characteristic ch = target.GetComponent<Characteristic>();
            Animator anim = target.GetComponent<Animator>();

            if (ch.HEALTH > 0)
            {
                anim.SetBool("Hit", true);
                ch.HEALTH -= selfCH.STRENGHT;
            }
        }
    }


    public void PostAttack()
    {
        if (target != null)
        {
            Characteristic ch = target.GetComponent<Characteristic>();
            Animator anim = target.GetComponent<Animator>();
            anim.SetBool("Hit", false);
            if (ch.HEALTH <= 0)
            {
                anim.SetBool("Death", true);
                target = null;
            }

            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            targetRb.AddForce(new Vector2(-1, -0.3f));
        }
    }

    public void Death()
    {
        Destroy(gameObject);
    }

    public GameObject target;
    public bool InArea;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            target = collision.gameObject;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            InArea = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            target = null;
            InArea = false;
        }
    }

    /*
     * private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            InArea = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            InArea = false;
        }
    }*/
}
