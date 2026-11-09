using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [Header("Random grid settings")]
    public int width = 10;
    public int height = 10;
    [Range(0f, 0.6f)] public float obstacleProbability = 0.25f;

    private List<Vector2Int> path = new List<Vector2Int>();
    private Vector2Int start = new Vector2Int(0, 0);
    private Vector2Int goal = new Vector2Int(9, 9);

    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    // 0 = walkable, 1 = wall
    private int[,] grid = new int[,]
    {
        { 0, 1, 0, 0, 0 },
        { 0, 1, 0, 1, 0 },
        { 0, 0, 0, 1, 0 },
        { 0, 1, 1, 1, 0 },
        { 0, 0, 0, 0, 0 }
    };

    private void Start()
    {
        GenerateRandomGrid(width, height, obstacleProbability);
    }

    private void Update()
    {
        // Left click = set start | Right click = set goal
        // Shift + left click = add obstacle | R = new random grid
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int cell;
            if (TryGetMouseCell(out cell))
            {
                if (Input.GetKey(KeyCode.LeftShift)) AddObstacle(cell);
                else SetStart(cell);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector2Int cell;
            if (TryGetMouseCell(out cell)) SetGoal(cell);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateRandomGrid(width, height, obstacleProbability);
        }
    }

    // ---------- Enhancement: random grid ----------
    public void GenerateRandomGrid(int gridWidth, int gridHeight, float obstacleProbability)
    {
        width = gridWidth;
        height = gridHeight;
        grid = new int[gridHeight, gridWidth];   // [y, x] like the original

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                grid[y, x] = Random.value < obstacleProbability ? 1 : 0;
            }
        }

        // Keep start and goal inside the grid and walkable
        start = new Vector2Int(Mathf.Clamp(start.x, 0, gridWidth - 1), Mathf.Clamp(start.y, 0, gridHeight - 1));
        goal = new Vector2Int(Mathf.Clamp(goal.x, 0, gridWidth - 1), Mathf.Clamp(goal.y, 0, gridHeight - 1));
        grid[start.y, start.x] = 0;
        grid[goal.y, goal.x] = 0;

        FindPath(start, goal);
    }

    // ---------- Enhancement: runtime obstacle ----------
    public void AddObstacle(Vector2Int position)
    {
        if (!IsInBounds(position)) return;
        if (position == start || position == goal) return;   // don't block the endpoints

        grid[position.y, position.x] = 1;
        FindPath(start, goal);   // recalculate with the new obstacle
    }

    // ---------- Enhancement: dynamic start / goal ----------
    public void SetStart(Vector2Int position)
    {
        if (!IsInBounds(position) || grid[position.y, position.x] == 1) return;
        start = position;
        FindPath(start, goal);
    }

    public void SetGoal(Vector2Int position)
    {
        if (!IsInBounds(position) || grid[position.y, position.x] == 1) return;
        goal = position;
        FindPath(start, goal);
    }

    // Converts the mouse position to a grid cell (grid lies on the XZ plane at y = 0)
    private bool TryGetMouseCell(out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        float distance;
        if (!groundPlane.Raycast(ray, out distance)) return false;

        Vector3 hit = ray.GetPoint(distance);
        cell = new Vector2Int(Mathf.RoundToInt(hit.x), Mathf.RoundToInt(hit.z));
        return true;
    }

    private void OnDrawGizmos()
    {
        float cellSize = 1f;

        // Draw grid cells
        for (int y = 0; y < grid.GetLength(0); y++)
        {
            for (int x = 0; x < grid.GetLength(1); x++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, 0, y * cellSize);
                Gizmos.color = grid[y, x] == 1 ? Color.black : Color.white;
                Gizmos.DrawCube(cellPosition, new Vector3(cellSize, 0.1f, cellSize));
            }
        }

        // Draw path
        foreach (var step in path)
        {
            Vector3 cellPosition = new Vector3(step.x * cellSize, 0, step.y * cellSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(cellPosition, new Vector3(cellSize, 0.1f, cellSize));
        }

        // Draw start and goal
        Gizmos.color = Color.green;
        Gizmos.DrawCube(new Vector3(start.x * cellSize, 0, start.y * cellSize), new Vector3(cellSize, 0.1f, cellSize));

        Gizmos.color = Color.red;
        Gizmos.DrawCube(new Vector3(goal.x * cellSize, 0, goal.y * cellSize), new Vector3(cellSize, 0.1f, cellSize));
    }

    private bool IsInBounds(Vector2Int point)
    {
        return point.x >= 0 && point.x < grid.GetLength(1) && point.y >= 0 && point.y < grid.GetLength(0);
    }

    private void FindPath(Vector2Int start, Vector2Int goal)
    {
        path.Clear();   // IMPORTANT: the original never cleared this, so old paths would pile up

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            if (current == goal)
            {
                break;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (IsInBounds(next) && grid[next.y, next.x] == 0 && !cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            Debug.Log("Path not found.");
            return;
        }

        // Trace path from goal to start
        Vector2Int step = goal;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Add(start);
        path.Reverse();
    }
}