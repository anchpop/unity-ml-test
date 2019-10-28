using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltController : MonoBehaviour
{
    public float range = 5;
    Vector3 initialPos;
    public GameObject target;
    public ArenaController arena;
    // Start is called before the first frame update
    void Start()
    {
        initialPos = transform.position;
        arena.bolts.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, initialPos) > range)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        arena.bolts.Remove(this);
    }


}
