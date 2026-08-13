namespace CafeMenu.Api.Exceptions;

public static class ApplicationErrorCodes
{
    public const string UserNotFound = "USER001";
    public const string UserEmailAlreadyExists = "USER002";
    public const string UserSetupTokenInvalid = "USER004";
    public const string UserSetupAlreadyCompleted = "USER005";
    public const string ValidationFailed = "VAL001";
    public const string CategoryNotFound = "CAT001";
    public const string CategoryReorderInvalid = "CAT002";
    public const string ProductNotFound = "PRO001";
    public const string ProductInvalidCategoryRelationship = "PRO002";
    public const string ProductReorderInvalid = "PRO003";
    public const string CafeNotFound = "CAFE001";
    public const string CafeSlugAlreadyExists = "CAFE002";
    public const string CafeInactive = "CAFE003";
    public const string CafeMembershipNotFound = "MEM001";
    public const string CafeMembershipAlreadyExists = "MEM002";
    public const string TenantAccessForbidden = "TENANT001";
    public const string ImageUnsupportedFormat = "IMG001";
    public const string ImageInvalid = "IMG002";
    public const string ImageTooLarge = "IMG003";
    public const string ImageStorageFailed = "IMG004";
}
