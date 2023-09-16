using NavMeshPlus.Components;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

public class Alias_AI : MonoBehaviour
{
    [Header("NavMesh")]
    public NavMeshAgent agent;
    public NavMeshSurface navMesh;

    [Header("Sprite")]
    public Animator anim;
    public SpriteRenderer sr;
    public CapsuleCollider2D colider;

    [Header("Farms")]
    public Characteristic ch;
    public Inventory inv;

    [Header("SelectManager")]
    public SelectManager selector;
    private void Start()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        navMesh = GetComponentInParent<NavMeshSurface>();

        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        colider = GetComponent<CapsuleCollider2D>();

        ch = GetComponent<Characteristic>();
        inv = GetComponent<Inventory>();

        selector = Camera.main.GetComponent<SelectManager>();
    }

    private void Update()
    {
        GoTo();
    }

    private void FixedUpdate()
    { 
        SetCurrnetObj();
        SetInArea();
    }

    public void GoTo()
    {
        if (selector.IsSelected(gameObject))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                agent.destination = pos;
            }
        }
        float movement = Mathf.Abs(agent.velocity.x) + Mathf.Abs(agent.velocity.y);
        anim.SetFloat("Move", movement);
        anim.SetBool("InArea", InArea);



        if (agent.velocity.x >= 0)
        {
            sr.flipX = false;
        }
        else
        { 
            sr.flipX = true;
        }
    }

    public void SetCurrnetObj()
    {
        foreach(GameObject obj in collisionObjects)
        {
            if(obj.GetComponent<Resourse>().HEALTH > 0)
            {
                currentObj = obj;
            }
        }
    }

    public void SetInArea()
    {
        if (collisionObjects.Count > 0)
        {
            InArea = true;
        }
        else { InArea = false; }
    }

    public void Farm()
    {
        Resourse stats = currentObj.GetComponent<Resourse>();
        if (inv.Add(currentObj, ch.STRENGHT * 20))
        {
            stats.HEALTH -= ch.STRENGHT;
            stats.STORAGE = stats.HEALTH * 20;
            stats.defaultColor.b = stats.defaultColor.b - 0.2f;
            stats.defaultColor.g = stats.defaultColor.g - 0.2f;
        }

        if (stats.HEALTH <= 0)
        {
            Destroy(currentObj);
            navMesh.UpdateNavMesh(navMesh.navMeshData);
        }
    }


    public void Death()
    {
        Destroy(gameObject);
    }

    public bool InArea;
    public GameObject currentObj;
    public List<GameObject> collisionObjects;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Wood")
        {
            collisionObjects.Add(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Wood")
        {
            collisionObjects.Remove(collision.gameObject);
        }
    }
}
