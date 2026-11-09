# Pathfinding Lab

**Group members:** (Connor Hewitt)

## Overview

This Unity project modifies the BFS `Pathfinding` script so the grid can be changed while the game is running. The grid is drawn with Gizmos, so turn on **Gizmos** in the Game view to see it.

## What was added

- `GenerateRandomGrid(int width, int height, float obstacleProbability)`: fills a new grid with obstacles based on a probability. The start and goal cells are always kept walkable.
- `AddObstacle(Vector2Int position)`: turns any cell into an obstacle at runtime and recalculates the path.
- `SetStart(Vector2Int position)` and `SetGoal(Vector2Int position)`: change the start and goal cells at runtime and recalculate the path.
- `path.Clear()` at the start of `FindPath`, so old paths don't pile up between recalculations.

## How to run

1. Open the project in Unity (version: add yours here).
2. Open the scene and select the GameObject with the `Pathfinding` script.
3. Press Play with **Gizmos** turned on in the Game view.

| Input | Action |
|---|---|
| Left click | Set the start cell (green) |
| Right click | Set the goal cell (red) |
| Shift + left click | Add an obstacle (black) |
| R | Generate a new random grid |

The blue cells show the shortest path found.

If you get an error about `Input`, go to Edit > Project Settings > Player > Active Input Handling and choose **Both**.

## Demo video

(paste your video link here)

## Reflection

### 1. How does each pathfinding algorithm calculate and prioritize paths?

**BFS** keeps its frontier in a first-in, first-out queue. It explores every cell one step from the start, then every cell two steps away, and so on, so it spreads out evenly in all directions. Because it treats every move as equal, the first time it reaches the goal it has found the path with the fewest steps. It doesn't use costs, so it can't tell a cheap path from an expensive one.

**Dijkstra** uses a priority queue ordered by the cost so far, g(n), which is the total cost of getting from the start to a cell. It always expands the cheapest unexplored cell next, and it updates a cell's route if it finds a cheaper way to reach it. This guarantees the lowest-cost path even when moves have different costs, but it still spreads out in every direction without knowing where the goal is.

**A\*** works like Dijkstra but orders its priority queue by f(n) = g(n) + h(n), where h(n) is a heuristic estimate of the remaining distance to the goal. In our code h is the Manhattan distance, which fits a grid where you can only move up, down, left and right. This pulls the search toward the goal, so A* usually explores far fewer cells than BFS or Dijkstra. It still finds the best path as long as the heuristic never overestimates the true distance, which Manhattan distance doesn't on our grid.

### 2. What challenges arise when dynamically updating obstacles in real time?

- **Stale paths:** an obstacle can appear on the path that was already calculated, so the old path is no longer valid and has to be recalculated.
- **Recalculation cost:** rerunning the search on every change is cheap on a small grid, but on a large grid or with many changes per second it could cause lag. In our project we only recompute when something changes, not every frame.
- **Blocked endpoints:** an obstacle could be placed on the start or goal cell. We prevent this in `AddObstacle` by ignoring those cells.
- **Unreachable goal:** a new obstacle can completely cut off the goal, so the search has to handle the "no path" case without crashing. Our code logs "Path not found" and clears the old path.
- **Moving agents:** if something is walking along the path when the map changes, it needs to re-plan from where it currently is, not from the original start.
- **Alternatives:** algorithms like D* Lite reuse earlier search work and only repair the part of the path affected by the change, which is more efficient than restarting.

### 3. Which algorithm should you choose, and how should you adapt it for larger grids or open-world settings?

We would choose **A\***. It finds the shortest path like Dijkstra, but the heuristic keeps it from wasting time exploring away from the goal, so it scales much better than BFS or Dijkstra as the grid grows.

To adapt it for larger grids or open worlds:

- **Faster priority queue:** the provided `PriorityQueue` scans the whole list on every `Dequeue`, which gets slow as the frontier grows. A binary heap would be much faster.
- **Hierarchical pathfinding:** split the world into chunks or regions, plan a rough route between regions first, then plan detailed paths inside each region.
- **Navigation meshes:** in an open 3D world, a NavMesh uses far fewer nodes than a fine grid.
- **Path caching:** reuse paths that have already been calculated instead of searching again.
- **Spread the work out:** run the search over several frames, or on a separate thread, so the game doesn't freeze.

### 4. What would your approach be if you added weighted cells (for example, "difficult terrain")?

BFS can't handle weights because it only counts steps, so it would treat swamp the same as open ground. We would switch to **Dijkstra or A\***, which already use a priority queue and track the cost so far.

The changes would be:

- Store a movement cost for each cell, for example 1 for normal ground and 5 for difficult terrain.
- When expanding a neighbor, replace `costSoFar[current] + 1` with `costSoFar[current] + cost of the next cell`.
- Keep the A* heuristic from overestimating. If the cheapest cell costs 1, Manhattan distance is still safe. If the minimum cost were lower, we would multiply the heuristic by that minimum cost.
- Draw the weighted cells in a different color in `OnDrawGizmos` so the weights are visible.

With this, the algorithm would prefer a longer route over open ground rather than a shorter route through difficult terrain, when the extra cost makes it cheaper overall.
