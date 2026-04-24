namespace SpaceMission;

// Holds the cosmic map (the grid) plus useful info we need later:
// the list of astronauts and the position of the Space Station (F).
//
// I scan the grid once in the constructor instead of searching each
// time we need it - the map can be up to 100x100, no point repeating
// the same loops over and over.
public class Map
{
    public int Rows;
    public int Cols;
    public string[,] Grid;

    public List<Astronaut> Astronauts = new List<Astronaut>();
    public int FinishRow;
    public int FinishCol;

    public Map(int rows, int cols, string[,] grid)
    {
        Rows = rows;
        Cols = cols;
        Grid = grid;

        // Walk through every cell once and pick out the special ones
        bool finishFound = false;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                string cell = grid[r, c];

                if (cell == "S1" || cell == "S2" || cell == "S3")
                {
                    Astronauts.Add(new Astronaut(cell, r, c));
                }
                else if (cell == "F")
                {
                    FinishRow = r;
                    FinishCol = c;
                    finishFound = true;
                }
            }
        }

        // Basic validation - the spec says map always has at least S1
        // and we obviously need a destination
        if (!finishFound)
            throw new Exception("No Space Station (F) found on the map.");
        if (Astronauts.Count == 0)
            throw new Exception("No astronauts found on the map.");
    }

    // Print the map. If a path is given, draw '*' on the cells that
    // belong to the path - but keep S1/S2/S3 and F visible because
    // the example output in the brief shows them too.
    public void Print(List<(int r, int c)>? path = null)
    {
        // Work on a copy so we don't permanently overwrite the grid
        // (we need it again for the next astronaut's printout).
        string[,] copy = new string[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                copy[r, c] = Grid[r, c];

        if (path != null)
        {
            foreach (var (r, c) in path)
            {
                string s = copy[r, c];
                if (s != "S1" && s != "S2" && s != "S3" && s != "F")
                    copy[r, c] = "*";
            }
        }

        // PadLeft(2) so single-char cells like '0' or 'X' line up nicely
        // next to two-char cells like 'S1' / 'S2'. Without this the columns
        // get jagged when astronauts are present.
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                if (c > 0) Console.Write(" ");
                Console.Write(copy[r, c].PadLeft(2));
            }
            Console.WriteLine();
        }
    }
}
