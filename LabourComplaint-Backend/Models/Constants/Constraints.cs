// Models/Constants/Constraints.cs
namespace LabourComplaint_Backend.Models.Constants;

public static class Constraints
{
    public const int NameMaxLength = 100;
    public const int UsernameMin = 3, UsernameMax = 50;
    public const int PasswordHashLength = 255;
    public const int EmailMax = 255, PhoneMax = 20;
    public const int DescriptionMax = 2000, ContentMax = 4000;
    public const int TitleMax = 200, BodyMax = 2000;
    public const int UrlMax = 500, PathMax = 300;
    public const int JsonMax = 4000;
    public const int RefCodeMax = 50, ErrorCodeMax = 50;
}