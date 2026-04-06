namespace Sherlock.Net.Core.Services;

public interface IWafDetector
{
    bool IsWafResponse(string responseBody);
}
