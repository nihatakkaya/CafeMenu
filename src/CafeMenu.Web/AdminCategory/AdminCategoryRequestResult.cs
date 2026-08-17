namespace CafeMenu.Web.AdminCategory;

public enum AdminCategoryRequestStatus
{
    Success,
    ValidationError,
    Failure
}

public sealed record AdminCategoryListResult(
    AdminCategoryRequestStatus Status,
    IReadOnlyCollection<AdminCategoryResponse> Categories)
{
    public static AdminCategoryListResult Success(IReadOnlyCollection<AdminCategoryResponse> categories)
    {
        return new AdminCategoryListResult(AdminCategoryRequestStatus.Success, categories);
    }

    public static AdminCategoryListResult Failure()
    {
        return new AdminCategoryListResult(AdminCategoryRequestStatus.Failure, []);
    }
}

public sealed record AdminCategoryMutationResult(
    AdminCategoryRequestStatus Status,
    AdminCategoryResponse? Category)
{
    public static AdminCategoryMutationResult Success(AdminCategoryResponse category)
    {
        return new AdminCategoryMutationResult(AdminCategoryRequestStatus.Success, category);
    }

    public static AdminCategoryMutationResult ValidationError()
    {
        return new AdminCategoryMutationResult(AdminCategoryRequestStatus.ValidationError, null);
    }

    public static AdminCategoryMutationResult Failure()
    {
        return new AdminCategoryMutationResult(AdminCategoryRequestStatus.Failure, null);
    }
}

public sealed record AdminCategoryDeleteResult(AdminCategoryRequestStatus Status)
{
    public static AdminCategoryDeleteResult Success()
    {
        return new AdminCategoryDeleteResult(AdminCategoryRequestStatus.Success);
    }

    public static AdminCategoryDeleteResult Failure()
    {
        return new AdminCategoryDeleteResult(AdminCategoryRequestStatus.Failure);
    }
}
