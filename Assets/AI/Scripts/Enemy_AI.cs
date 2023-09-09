using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        Characteristic ch = target.GetComponent<Characteristic>();
        Animator anim = target.GetComponent<Animator>();

        if(ch.HEALTH > 0 )
        {
            anim.SetBool("Hit", true);
            ch.HEALTH -= selfCH.STRENGHT;
        }
    }


    public void PostAttack()
    {
        Animator anim = target.GetComponent<Animator>();
        anim.SetBool("Hit", false);
    }

    public void Death()
    {
        Destroy(gameObject);
    }

    public GameObject target;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            target = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            target = null;
        }
    }

    public bool InArea;
    private void OnCollisionEnter2D(Collision2D collision)
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
    }
}
