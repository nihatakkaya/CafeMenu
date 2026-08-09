namespace CafeMenu.Api.Exceptions;

public static class ApplicationErrorCodes
{
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
}
