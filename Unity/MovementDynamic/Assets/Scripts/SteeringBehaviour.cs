﻿using UnityEngine;

public abstract class SteeringBehaviour : MonoBehaviour, ISteeringBehaviour {

    public float weight = 1;

    protected DynamicAgent agent;
    protected Rigidbody2D rb;

    public float Weight { get { return weight; } }

    // Use this for initialization
    void Start()
    {
        agent = GetComponent<DynamicAgent>();
        rb = GetComponent<Rigidbody2D>();
    }

    public abstract SteeringOutput GetSteering(GameObject target);

    public static Vector2 Deg2Vec(float angle)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    public static float Vec2Deg(Vector2 vector)
    {
        return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    }

    public static GameObject CreateTarget(Vector3 position, float orientation)
    {
        GameObject target = new GameObject();
        target.transform.position = position;
        target.transform.Rotate(new Vector3(0, 0, orientation), Space.World);
        return target;
    }
}
