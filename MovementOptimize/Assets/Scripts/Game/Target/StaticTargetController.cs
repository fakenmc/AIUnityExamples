/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */

using UnityEngine;

// Controls when to spawn a static target
public class StaticTargetController : MonoBehaviour
{

    // Holds an instance the static target prefab
    [SerializeField] private GameObject target = default;

    // How long between target destruction and target spawn
    [SerializeField] private float delay = 1f;

    // The game area
    private GameArea gameArea;

    // The text mesh where to show the points
    private TextMesh textMesh;

    // The points for the current run
    public float Points { get; private set; } = 0;

    // Called when the script is instantiated
    private void Awake()
    {
        // Get reference to the text mesh
        textMesh = GameObject.Find("Points")?.GetComponent<TextMesh>();
        // Create a game area helper
        gameArea = new GameArea();
    }

    // Called when script is activated, i.e. when a new runs starts
    private void OnEnable()
    {
        // Destroy a possible leftover target from previous run
        GameObject currentTarget = GameObject.FindWithTag("Target");
        if (currentTarget != null)
        {
            Destroy(currentTarget);
        }

        // Reset points
        Points = 0;

        // Target will only be spawned in the next frame
        Invoke("SpawnTarget", 0);
    }

    // Called when a target is destroyed
    public void NotifyDestructionBy(Rigidbody2D destroyer)
    {
        // How well was the destroyer facing the target?
        float angleDiff = Mathf.DeltaAngle(
            SteeringBehaviour.Vec2Deg(destroyer.velocity), destroyer.rotation);

        // Determine points for this destruction based on how well the
        // destroyer was facing the target
        float points = Mathf.Pow(1 - 0.9f * Mathf.Abs(angleDiff) / 180, 5);
        UpdatePoints(points);
        Invoke("SpawnTarget", delay);
    }

    // Update points
    private void UpdatePoints(float morePoints)
    {
        Points += morePoints;
        if (textMesh != null)
            textMesh.text = $"Points: {Points}";
    }

    // Spawn a new target at a random location
    private void SpawnTarget()
    {
        Vector2 pos = gameArea.RandomPosition(0.9f);
        Instantiate(target, pos, Quaternion.identity, transform);
    }
}
