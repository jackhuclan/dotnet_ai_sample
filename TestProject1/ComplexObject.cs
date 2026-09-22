namespace dotnet_ai_agent_sample;

internal class ComplexObject
{
    public string AString { get; set; } = string.Empty;
    public int AnInt { get; set; }
    public long ALong { get; set; }
    public double ADouble { get; set; }
    public bool ABool { get; set; }
    public SubObject ASubObject { get; set; } = new SubObject();
}
