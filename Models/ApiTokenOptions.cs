namespace UserManagementAPI.Models;

public sealed class ApiTokenOptions
{
    public ApiTokenOptions(string expectedToken)
    {
        ExpectedToken = expectedToken;
    }

    public string ExpectedToken { get; }
}
