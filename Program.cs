using SpaceMission;
using System.Net;
using System.Net.Mail;
using System.Text;

// SPACE 2026 - Cosmic Navigation
// ----------------------------------
// Console app that reads a cosmic map and, for each astronaut on it,
// prints the shortest safe route to the Space Station (F).
//
// I split the project into small classes so each one has a clear job:
//   - Astronaut.cs  -> data about one astronaut
//   - Map.cs        -> the grid + printing logic
//   - PathFinder.cs -> the algorithm (Dijkstra, handles debris too)
//   - Program.cs    -> menu and user interaction
//
// The user gets a small menu so they can either type their own map,
// generate a random one (bonus), or run the example from the brief.

Console.WriteLine("=== SPACE 2026 ===");
Console.WriteLine("1) Type the map");
Console.WriteLine("2) Random map");
Console.WriteLine("3) Use the example from the brief");
Console.Write("Choose: ");
string choice = Console.ReadLine() ?? "3";

try
{
    Map map;
    if (choice == "1")
        map = ReadMapFromUser();
    else if (choice == "2")
        map = MakeRandomMap();
    else
        map = ExampleMap();

    Console.WriteLine();
    Console.WriteLine("Map:");
    map.Print();
    Console.WriteLine();

    // Run the path finder once per astronaut and collect all results
    var finder = new PathFinder();
    var results = new List<PathResult>();
    foreach (var a in map.Astronauts)
        results.Add(finder.Find(map, a));

    // The brief asks for failures on top, then successes sorted by
    // shortest distance first. I split the list in two and sort each
    // group separately - it reads more clearly than one combined sort.
    var failed = results.Where(r => !r.Found).OrderBy(r => r.Astronaut.Name).ToList();
    var ok = results.Where(r => r.Found).OrderBy(r => r.Steps).ToList();

    foreach (var r in failed)
    {
        Console.WriteLine($"Mission failed — Astronaut {r.Astronaut.Name} lost in space!");
        Console.WriteLine();
    }

    foreach (var r in ok)
    {
        Console.WriteLine($"Astronaut {r.Astronaut.Name} - Shortest path: {r.Steps} steps");
        map.Print(r.Path);
        Console.WriteLine();
    }

    // Bonus: optionally email the report. Wrapped in a y/n prompt so
    // the user is never forced to deal with SMTP if they don't want to.
    Console.Write("Send report by email? (y/n): ");
    string send = Console.ReadLine() ?? "n";
    if (send.Trim().ToLower() == "y")
        SendEmail(map, failed, ok);
}
catch (Exception ex)
{
    // I catch at the top level so the user sees a friendly message
    // instead of a raw stack trace if something goes wrong with input.
    Console.WriteLine("Error: " + ex.Message);
}


// ---------- helper functions below ----------

// Reads M, N and then M rows from the console. Validates dimensions
// and the number of cells per row. Anything weird throws and gets
// caught by the top-level try/catch.
static Map ReadMapFromUser()
{
    Console.Write("Rows: ");
    int rows = int.Parse(Console.ReadLine() ?? "0");
    Console.Write("Cols: ");
    int cols = int.Parse(Console.ReadLine() ?? "0");

    if (rows < 2 || rows > 100 || cols < 2 || cols > 100)
        throw new Exception("Rows and Cols must be between 2 and 100");

    Console.WriteLine($"Enter {rows} rows, each with {cols} symbols separated by spaces.");
    Console.WriteLine("Symbols: S1 S2 S3 F O X D  (use 0 for open space)");

    string[,] grid = new string[rows, cols];
    for (int r = 0; r < rows; r++)
    {
        string? line = Console.ReadLine();
        if (line == null) throw new Exception("Missing row " + (r + 1));

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != cols)
            throw new Exception("Row " + (r + 1) + " has wrong number of cells");

        for (int c = 0; c < cols; c++) grid[r, c] = parts[c];
    }
    return new Map(rows, cols, grid);
}

// Bonus objective: build a random map.
// I shuffle the list of all cells, then pop them off one by one to place
// F first, then the astronauts, then asteroids. This guarantees no two
// special cells end up in the same place.
static Map MakeRandomMap()
{
    Console.Write("Rows: ");
    int rows = int.Parse(Console.ReadLine() ?? "5");
    Console.Write("Cols: ");
    int cols = int.Parse(Console.ReadLine() ?? "7");
    Console.Write("How many asteroids: ");
    int asteroids = int.Parse(Console.ReadLine() ?? "5");
    Console.Write("How many astronauts (1-3): ");
    int astrCount = int.Parse(Console.ReadLine() ?? "1");

    var rnd = new Random();

    // Start with all-open
    string[,] grid = new string[rows, cols];
    for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            grid[r, c] = "0";

    // Build a shuffled list of (row, col) pairs
    var cells = new List<(int, int)>();
    for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            cells.Add((r, c));

    // Fisher-Yates shuffle
    for (int i = cells.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(i + 1);
        var tmp = cells[i];
        cells[i] = cells[j];
        cells[j] = tmp;
    }

    int idx = 0;

    // Place finish first so it's never overwritten
    var (fr, fc) = cells[idx++];
    grid[fr, fc] = "F";

    string[] names = { "S1", "S2", "S3" };
    for (int i = 0; i < astrCount; i++)
    {
        var (sr, sc) = cells[idx++];
        grid[sr, sc] = names[i];
    }

    for (int i = 0; i < asteroids && idx < cells.Count; i++)
    {
        var (ar, ac) = cells[idx++];
        grid[ar, ac] = "X";
    }

    return new Map(rows, cols, grid);
}

// The exact 5x7 sample map from the assessment PDF. Useful for
// double-checking that the program produces the same output as the brief
// (S2 in 4 steps, S1 in 10 steps).
static Map ExampleMap()
{
    string[,] g = {
        { "S1", "0",  "X",  "0", "0", "0", "S2" },
        { "X",  "0",  "0",  "0", "0", "X", "0"  },
        { "X",  "X",  "0",  "X", "0", "X", "0"  },
        { "0",  "X",  "X",  "0", "0", "X", "0"  },
        { "0",  "X",  "X",  "0", "0", "0", "F"  },
    };
    return new Map(5, 7, g);
}

// Bonus objective: email the report by SMTP.
// Credentials come from the user at runtime - I don't want them
// hard-coded anywhere in the project. Default settings are for Gmail
// since that's the most common case.
static void SendEmail(Map map, List<PathResult> failed, List<PathResult> ok)
{
    Console.Write("Your email (gmail): ");
    string from = Console.ReadLine() ?? "";
    Console.Write("Password / app password: ");
    string pwd = Console.ReadLine() ?? "";
    Console.Write("Send to: ");
    string to = Console.ReadLine() ?? "";

    // Build a short text report. Keeping it plain text on purpose -
    // works with every email client and there's no need for HTML here.
    var sb = new StringBuilder();
    sb.AppendLine("SPACE 2026 - Mission Report");
    sb.AppendLine();

    foreach (var r in failed)
        sb.AppendLine($"Mission failed — Astronaut {r.Astronaut.Name} lost in space!");

    foreach (var r in ok)
        sb.AppendLine($"Astronaut {r.Astronaut.Name} - {r.Steps} steps");

    try
    {
        var smtp = new SmtpClient("smtp.gmail.com", 587);
        smtp.EnableSsl = true;   // Gmail requires TLS on port 587
        smtp.Credentials = new NetworkCredential(from, pwd);

        var msg = new MailMessage(from, to, "SPACE 2026 Report", sb.ToString());
        smtp.Send(msg);
        Console.WriteLine("Email sent.");
    }
    catch (Exception e)
    {
        // Don't crash the whole program just because the email failed -
        // the main results were already printed above.
        Console.WriteLine("Could not send email: " + e.Message);
    }
}
