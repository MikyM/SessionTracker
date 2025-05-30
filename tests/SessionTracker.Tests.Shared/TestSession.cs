using System.Text.Json;

namespace SessionTracker.Tests.Shared;

public class TestSession : Session
{
    public string Description { get; set; } = Guid.NewGuid().ToString();
    
    public TestSession(string key) : base(key)
    {
    }

    public override string ToString()
        => JsonSerializer.Serialize(this);
}