using System.Collections.Generic;

public class Module
{
    public string Name { get; private set; }

    public Module(Dictionary<string, object> Data)
    {
        Name = (string)Data["Name"];
    }
}
