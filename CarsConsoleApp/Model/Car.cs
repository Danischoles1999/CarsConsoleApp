using System.Collections.Generic;

public class Car
{
    public string Type { get; set; }
    public double AssemblyCost { get; set; }
    public List<PartRequirement> PartRequirements { get; set; }
}
