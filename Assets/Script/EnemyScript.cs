using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    EnemyController enemyController;
    PlayerController playerController;
    private void Start()
    {
        enemyController = gameObject.GetComponent<EnemyController>();
        playerController = gameObject.GetComponent<PlayerController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        var otherGameObject = other.gameObject;

        if (otherGameObject.tag == "Sword")
        {
            Debug.Log("collision with sword");

            //Destroy(otherGameObject);

            //var anim = gameObject.GetComponentInChildren<Animator>();
            enemyController.isDead = true;
        }
    }
}
