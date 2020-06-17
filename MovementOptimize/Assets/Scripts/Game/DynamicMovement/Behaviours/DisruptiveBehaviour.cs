/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */
using UnityEngine;

public class DisruptiveBehaviour : SteeringBehaviour
{
    [SerializeField]
    [Tooltip("Interval between switching disruption forces")]
    private float switchInterval = 0.25f;

    [Header("Linear acceleration disruption")]

    [SerializeField]
    [Tooltip("Linear speed value when disruption starts")]
    private float linX0 = 50f;

    [SerializeField]
    [Tooltip("Slope of increasing disruption")]
    private float linM = 1;

    [Header("Angular acceleration disruption")]

    [SerializeField]
    [Tooltip("Angular rotation value when disruption starts")]
    private float angX0 = 90f;

    [SerializeField]
    [Tooltip("Slope of increasing disruption")]
    private float angM = 1;

    // Last disruption
    private SteeringOutput lastDisruption;

    // Time last disruption was calculated
    private float lastDisruptionTime;

    private void Start()
    {
        lastDisruption = new SteeringOutput(Vector2.zero, 0);
        lastDisruptionTime = 0f;
    }

    // Disruptive behaviour
    public override SteeringOutput GetSteering(GameObject target)
    {
        // Is it time to create a new disruption?
        if (Time.time > lastDisruptionTime + switchInterval)
        {
            // Determine disruptions based on current speed and acceleration
            Vector2 linear = Random.onUnitSphere *
                Linear(Mathf.Pow(MaxSpeed + MaxAccel, 2), linX0, linM);
            float angular = Random.value *
                Linear(Mathf.Pow(MaxRotation + MaxAngularAccel, 2), angX0, angM);
            lastDisruption = new SteeringOutput(linear, angular);
            lastDisruptionTime = Time.time;
        }

        // Output the disruption
        return lastDisruption;
    }

    /// <summary>
    /// Linear function.
    /// </summary>
    /// <param name="x">Input variable x</param>
    /// <param name="x0">x value when disruption starts</param>
    /// <param name="m">The slope</param>
    /// <returns>The y output variable</returns>
    public static float Linear(float x, float x0, float m)
    {
        return x < x0 ? 0 : m * (x - x0);
    }

    // Draw a line showing the linear disruption caused by this behaviour.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position,
            transform.position + (Vector3)lastDisruption.Linear);
    }
}
