namespace SpaceMission;

// Simple data class for an astronaut.
// I kept this as its own class (instead of just a tuple) because the spec
// said the solution should follow OOP, and an astronaut is a clear "thing"
// in the problem domain.
public class Astronaut
{
    public string Name { get; set; }   // S1, S2 or S3
    public int Row { get; set; }
    public int Col { get; set; }

    public Astronaut(string name, int row, int col)
    {
        Name = name;
        Row = row;
        Col = col;
    }
}
