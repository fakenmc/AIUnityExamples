﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibGameAI.PathFinding;

public class World : MonoBehaviour
{

    // These should contain the prefabs that represent the game objects
    [SerializeField] private GameObject tile = null;
    [SerializeField] private GameObject player = null;
    [SerializeField] private GameObject goal = null;

    // The world size
    [SerializeField] private Vector2Int worldSize = new Vector2Int(10, 10);

    // Ratio of passages in each column
    [SerializeField] [Range(0f, 1f)] private float passageRatio = 0.1f;

    // How long between each movement
    [SerializeField] private float moveDuration = 0.5f;

    // Path finding algorithm to use
    [SerializeField] private PathFindingType pathFindingType = default;

    // Enumeration that represents the differerent path finding strategies
    private enum PathFindingType { Dijkstra = 0, AStar = 1}

    // Known path finders
    private IPathFinder[] knownPathFinders;

    // Player and goal positions
    public Vector2Int PlayerPos { get; private set; }
    public Vector2Int GoalPos { get; private set; }

    // Goal reached?
    public bool GoalReached { get; private set; }

    // Does a valid path exists between player and goal?
    public bool ValidPath { get; private set; } = true;

    // Current path
    private IEnumerable<IConnection> path = null;

    // Matrix of game world state
    private TileBehaviour[,] world;

    // Offset for all tiles
    private Vector2 offset;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Instantiate known path finders
        knownPathFinders = new IPathFinder[2];
        knownPathFinders[(int)PathFindingType.Dijkstra] =
            new DijkstraPathFinder();
        knownPathFinders[(int)PathFindingType.AStar] =
            new AStarPathFinder(HeuristicForAStar);

        // References to camera game object and camera component
        GameObject cameraGameObj = GameObject.FindWithTag("MainCamera");
        Camera cameraComponent = cameraGameObj.GetComponent<Camera>();

        // Width must be odd
        if (worldSize.x % 2 == 0) worldSize.x++;

        // Adjust camera to game world
        cameraComponent.orthographicSize =
            Mathf.Max(worldSize.x, worldSize.y) / 2;

        // Initialize matrix of game world elements (by default will be
        // all empties)
        world = new TileBehaviour[worldSize.x, worldSize.y];

        // Determine tile offsets
        offset = new Vector2(worldSize.x / 2f - 0.5f, worldSize.y / 2f - 0.5f);
    }

    // Start is called before the first frame update
    private void Start()
    {
        // Goal hasn't been reached yet
        GoalReached = false;

        // Generate level
        GenerateLevel();

        // Start path finding process
        StartCoroutine(FindPath());
    }

    // Restart path finding
    internal void Restart()
    {
        // Delete world tiles
        for (int i = 0; i < worldSize.x; i++)
        {
            for (int j = 0; j < worldSize.y; j++)
            {
                Destroy(world[i, j].gameObject);
            }
        }
        // Delete player and goal
        Destroy(player);
        Destroy(goal);

        // Start again
        Start();
    }

    // Procedurally generate level
    private void GenerateLevel()
    {
        // Array of game objects to clone for each column
        TileTypeEnum[] columnToCreate = new TileTypeEnum[worldSize.y];

        // Cycle through columns
        for (int i = 0; i < worldSize.x; i++)
        {
            // Number of passages in current column (by default all)
            int numEmpty = worldSize.y;

            // For current column determine which tiles are empty and which are
            // blocked
            for (int j = 0; j < worldSize.y; j++)
            {
                // By default, current position in column is empty
                columnToCreate[j] = TileTypeEnum.Empty;

                // However, if column is odd...
                if (i % 2 != 0)
                {
                    // ...and of by chance it's supposed to have a blocked tile
                    if (Random.value > passageRatio)
                    {
                        // ...then set a blocked tile
                        columnToCreate[j] = TileTypeEnum.Blocked;

                        // Now we have on less empty space
                        numEmpty--;
                    }
                }
            }

            // Are there any empty tiles in current column?
            if (numEmpty == 0)
            {
                // If not, then randomly define one empty tile
                columnToCreate[Random.Range(0, worldSize.y)] =
                    TileTypeEnum.Empty;
            }

            // Instantiate tiles for current column
            for (int j = 0; j < worldSize.y; j++)
            {
                GameObject currTile = Instantiate(tile, transform);
                currTile.name = $"Tile({i},{j})";
                currTile.transform.position =
                    new Vector3(i - offset.x, j - offset.y, 0);
                world[i, j] = currTile.GetComponent<TileBehaviour>();
                world[i, j].SetAs(columnToCreate[j]);
                world[i, j].Pos = new Vector2Int(i, j);
            }
        }

        // Place player anywhere in the world, as long as it's an empty tile
        while (true)
        {
            PlayerPos = new Vector2Int(
                Random.Range(0, worldSize.x), Random.Range(0, worldSize.y));
            if (world[PlayerPos.x, PlayerPos.y].TileType == TileTypeEnum.Empty)
            {
                player = Instantiate(player);
                player.transform.position = new Vector3(
                    PlayerPos.x - offset.x, PlayerPos.y - offset.y, -2);
                break;
            }
        }

        Debug.Log($"Player placed at {PlayerPos}");

        // Place goal anywhere in the world, as long as it's an empty tile and
        // noy the player position
        while (true)
        {
            GoalPos = new Vector2Int(
                Random.Range(0, worldSize.x), Random.Range(0, worldSize.y));
            if (world[GoalPos.x, GoalPos.y].TileType == TileTypeEnum.Empty
                && PlayerPos != GoalPos)
            {
                goal = Instantiate(goal);
                goal.transform.position = new Vector3(
                    GoalPos.x - offset.x, GoalPos.y - offset.y, -1);
                break;
            }
        }

        Debug.Log($"Goal placed at {GoalPos}");

    }

    // This method is used as an heuristic for A*
    // It's the euclidean distance heuristic
    private float HeuristicForAStar(int node)
    {
        Vector2 nodeVec = (Vector2)Ind2Vec(node, world.GetLength(0));
        Vector2 destVec = (Vector2)GoalPos;
        return Vector2.Distance(nodeVec, destVec);
    }

    // This co-routine performs the path finding and invokes another coroutine
    // to perform player movement animation
    private IEnumerator FindPath()
    {
        // Coroutine will be called again in moveDuration seconds
        YieldInstruction wfs = new WaitForSeconds(moveDuration);

        // The graph representation of our tile world
        TileWorldGraph graph = new TileWorldGraph(world);

        // Start player movement loop
        while (PlayerPos != GoalPos)
        {
            // Perform path finding
            path = knownPathFinders[(int)pathFindingType].FindPath(
                graph,
                Vec2Ind(PlayerPos, world.GetLength(0)),
                Vec2Ind(GoalPos, world.GetLength(0)));

            // Was a path found?
            if (path == null)
            {
                // If not, update flag
                ValidPath = false;
            }
            else
            {
                // A valid path was found, update flag
                ValidPath = true;

                // Get an enumerator for the connection enumerable (it must be
                // disposed of, as such we put it in a using block)
                using (IEnumerator<IConnection> conns = path.GetEnumerator())
                {
                    // Did the path finder return any connection at all?
                    if (conns.MoveNext())
                    {
                        // If so, move player towards the destination node in the
                        // first connection
                        StartCoroutine(MovePlayer(
                            conns.Current.ToNode, moveDuration / 2));
                    }
                }
            }
            // Return again after some fixed amount of time
            yield return wfs;
        }

        // If we got here, it means the goal was reached!
        GoalReached = true;
    }

    // Co-routine that performs the player movement animation
    private IEnumerator MovePlayer(int toNode, float duration)
    {
        // Final position of player sprite
        Vector3 finalPos;
        // Initial position of player sprite
        Vector3 initPos = player.transform.position;
        // Animation start time
        float startTime = Time.time;
        // Animation end time
        float endTime = startTime + duration;

        // Set player tile position to its final position
        PlayerPos = Ind2Vec(toNode, world.GetLength(0));

        // Determine player sprite final position
        finalPos = new Vector3(
            PlayerPos.x - offset.x,
            PlayerPos.y - offset.y,
            player.transform.position.z);

        // Perform animation
        while (Time.time < endTime)
        {
            // Get current time
            float currTime = Time.time;
            // Determine movement length
            float toMove = Mathf.InverseLerp(startTime, endTime, currTime);
            // Determine new sprite position for current frame
            Vector3 newPos = Vector3.Lerp(initPos, finalPos, toMove);
            // Move player sprite to new position
            player.transform.position = newPos;
            // Give control back to main loop, this loop will continue on the
            // next frame
            yield return null;
        }

        // Make sure player sprite ends up in the final position
        player.transform.position = finalPos;
    }

    // Draw gizmos around player, goal and along found path
    private void OnDrawGizmos()
    {
        // Player gizmo
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(player.transform.position, new Vector3(1, 1, 1));

        // Goal gizmo
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(goal.transform.position, new Vector3(1, 1, 1));

        // Path gizmo
        Gizmos.color = Color.red;
        if (path != null)
        {
            bool first = true;
            // Cycle through all connections
            foreach (IConnection conn in path)
            {
                // Don't draw first connection, it doesn't look very nice
                if (first)
                {
                    first = false;
                }
                else
                {
                    // Get start tile for current connection
                    Vector2Int start = Ind2Vec(conn.FromNode, world.GetLength(0));
                    // Get end tile for current connection
                    Vector2Int end = Ind2Vec(conn.ToNode, world.GetLength(0));
                    // Get on-screen position for start of current connection
                    Vector3 startPos = new Vector3(
                        start.x - offset.x, start.y - offset.y, -3);
                    // Get on-screen position for end of current connection
                    Vector3 endPos = new Vector3(
                        end.x - offset.x, end.y - offset.y, -3);
                    // Draw line for current connection
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
        }
    }

    // Convert index (graph node) to int vector (tile position)
    public static Vector2Int Ind2Vec(int index, int xdim) =>
        new Vector2Int(index % xdim, index / xdim);
    // Convert int vector (tile position) to index (graph node)
    public static int Vec2Ind(Vector2Int vec, int xdim) =>
        vec.y * xdim + vec.x;

}
