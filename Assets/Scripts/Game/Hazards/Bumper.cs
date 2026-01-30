using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    public float bumperForce = 15f;
    public AudioClip bumperClip;
    public Animator anim;

    public Movement movement;

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Movement>(out movement))
        {
            Vector3 n = collision.GetContact(0).normal.normalized;

            // component of velocity along the normal
            float normalComponent = Vector3.Dot(movement.marbleVelocity, n);

            // remove it
            movement.marbleVelocity -= normalComponent * n;
            movement.marbleVelocity += n * bumperForce * 2f;

            GameManager.instance.PlayAudioClip(bumperClip);
            anim.SetTrigger("Hit");
        }
    }
}