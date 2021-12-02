using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform graphics;
    private Animator anim;

    public float petrolTime = 10f;
    public float aggroRange = 10f;
    public Transform[] wayPoints;
    float walkSpeed = 0.15f, runSpeed = 3f;

    int index;
    float speed, agentSpeed;
    Transform player;

    NavMeshAgent agent;
    bool run = false;
    public bool isDead = false;

    float distanceBetweenHeronEnemy = 0;

    private void Awake()
    {
        anim = graphics.GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        player = GameObject.FindGameObjectWithTag("Player").transform;
        index = Random.Range(0, wayPoints.Length);

        InvokeRepeating("Tick", 0, 0.5f);
        if(wayPoints.Length>0)
        {
            InvokeRepeating("Patrol", 0, petrolTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        distanceBetweenHeronEnemy = Vector3.Distance(transform.position, player.position);

        if (distanceBetweenHeronEnemy >= 1.5)
        {
            if (run)
            {
                agent.speed = runSpeed;
                anim.SetFloat("Speed", 1f);
            }
            else
            {
                agent.speed = walkSpeed;
                anim.SetFloat("Speed", 0.5f);
            }
        }
        else
        {
            if (isDead)
            {
                anim.SetTrigger("Dead");
            }
            else
            {
                anim.SetFloat("Speed", 1.5f);
                anim.SetTrigger("StartAttack");
            }
        }
    }

    void Patrol()
    {
        index = index == wayPoints.Length - 1 ? 0 : index + 1;
    }

    void Tick()
    {
        agent.destination = wayPoints[index].position;
        if (player != null && distanceBetweenHeronEnemy < aggroRange)
        {
            agent.destination = player.position;
            run = true;
        }
        else
            run = false;
    }

}
