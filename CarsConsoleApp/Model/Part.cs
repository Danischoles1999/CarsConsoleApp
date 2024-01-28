using System.Collections.Generic;

public class Part
{
    public string Name { get; set; }
    public double? AssemblyPercentage { get; set; }
    public double? Price { get; set; }
    public List<SubPart> SubParts { get; set; }
}
