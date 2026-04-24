namespace SpaceMission;

// Result returned for one astronaut after we try to find their path.
// Found = false means the astronaut is "lost in space" (no route to F).
public class PathResult
{
    public Astronaut Astronaut;
    public bool Found;
    public int Steps;
    public List<(int r, int c)> Path = new List<(int r, int c)>();

    public PathResult(Astronaut a)
    {
        Astronaut = a;
        Found = false;
        Steps = 0;
    }
}

// Finds the shortest path from an astronaut to F.
//
// I started with plain BFS because every move on the basic map costs 1.
// But then the bonus says debris ('D') costs 2 instead of 1, and BFS only
// works when all edge weights are equal. So I switched to Dijkstra
// (priority queue ordered by distance). Dijkstra still returns the right
// answer when there is no debris, so it's safe as the only algorithm.
public class PathFinder
{
    // 4 directions: up, down, left, right
    private int[] dr = { -1, 1, 0, 0 };
    private int[] dc = { 0, 0, -1, 1 };

    public PathResult Find(Map map, Astronaut astro)
    {
        var result = new PathResult(astro);

        // Distance from start to each cell. -1 means "not visited yet".
        // Parent[r,c] tells us where we came from, so we can rebuild the
        // path by walking backwards from F to the start at the end.
        int[,] dist = new int[map.Rows, map.Cols];
        (int, int)[,] parent = new (int, int)[map.Rows, map.Cols];
        for (int r = 0; r < map.Rows; r++)
        {
            for (int c = 0; c < map.Cols; c++)
            {
                dist[r, c] = -1;
                parent[r, c] = (-1, -1);
            }
        }

        var pq = new PriorityQueue<(int r, int c), int>();
        pq.Enqueue((astro.Row, astro.Col), 0);
        dist[astro.Row, astro.Col] = 0;

        while (pq.Count > 0)
        {
            var (r, c) = pq.Dequeue();

            // Early exit: once we pop F we already know its shortest distance
            // (Dijkstra guarantees that). No need to keep searching.
            if (r == map.FinishRow && c == map.FinishCol) break;

            for (int i = 0; i < 4; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];

                // Skip cells outside the grid
                if (nr < 0 || nr >= map.Rows || nc < 0 || nc >= map.Cols) continue;

                string cell = map.Grid[nr, nc];

                // Asteroids are walls
                if (cell == "X") continue;

                // Don't walk through *another* astronaut's launch pod.
                // (We allow our own start cell because that's where we came from.)
                if ((cell == "S1" || cell == "S2" || cell == "S3") && cell != astro.Name) continue;

                // Cost of *entering* the next cell. Debris costs 2, anything
                // else costs 1. This is what makes Dijkstra needed instead of BFS.
                int cost = (cell == "D") ? 2 : 1;
                int newDist = dist[r, c] + cost;

                // Only update if we found a better (or first) way to reach this cell
                if (dist[nr, nc] == -1 || newDist < dist[nr, nc])
                {
                    dist[nr, nc] = newDist;
                    parent[nr, nc] = (r, c);
                    pq.Enqueue((nr, nc), newDist);
                }
            }
        }

        // F was never reached -> astronaut is lost in space
        if (dist[map.FinishRow, map.FinishCol] == -1)
            return result;

        // Found a path. Walk back from F to the start using the parent table.
        result.Found = true;
        result.Steps = dist[map.FinishRow, map.FinishCol];

        var path = new List<(int r, int c)>();
        int curR = map.FinishRow;
        int curC = map.FinishCol;
        while (curR != -1 && curC != -1)
        {
            path.Add((curR, curC));
            var (pr, pc) = parent[curR, curC];
            curR = pr;
            curC = pc;
        }
        path.Reverse(); // we built it backwards, flip to start->finish order
        result.Path = path;

        return result;
    }
}
