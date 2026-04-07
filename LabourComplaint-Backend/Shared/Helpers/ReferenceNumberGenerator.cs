namespace LabourComplaint_Backend.Shared.Helpers;

public static class ReferenceNumberGenerator
{
    /// <summary>
    /// Generates a unique complaint reference: LC-YYYY-MM-XXXXXX
    /// Example: LC-2026-04-A7B3C9
    /// </summary>
    public static string Generate()
    {
        var now = DateTime.UtcNow;
        var shortGuid = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"LC-{now:yyyy-MM}-{shortGuid}";
    }
}