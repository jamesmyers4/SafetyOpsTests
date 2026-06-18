namespace SafetyOpsTests.Config;

public class TestSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SafetyOpsMainUrl { get; set; } = string.Empty;
    public string FirUrl { get; set; } = string.Empty;
    public string LoginUsername { get; set; } = string.Empty;
    public string LoginPassword { get; set; } = string.Empty;
    public bool Headless { get; set; } = true;
}