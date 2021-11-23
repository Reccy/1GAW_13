using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMover : MonoBehaviour
{
    private Rigidbody2D m_rb;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        float hoz = Input.GetAxis("Horizontal");

        m_rb.velocity = new Vector2(hoz, -1) * Time.deltaTime;
    }
}
