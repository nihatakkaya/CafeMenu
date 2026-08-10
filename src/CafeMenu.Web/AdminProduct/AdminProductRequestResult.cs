namespace CafeMenu.Web.AdminProduct;

public enum AdminProductRequestStatus
{
    Success,
    ValidationError,
    Failure
}

public sealed record AdminProductListResult(
    AdminProductRequestStatus Status,
    IReadOnlyCollection<AdminProductResponse> Products)
{
    public static AdminProductListResult Success(IReadOnlyCollection<AdminProductResponse> products)
    {
        return new AdminProductListResult(AdminProductRequestStatus.Success, products);
    }

    public static AdminProductListResult Failure()
    {
        return new AdminProductListResult(AdminProductRequestStatus.Failure, []);
    }
}

public sealed record AdminProductMutationResult(
    AdminProductRequestStatus Status,
    AdminProductResponse? Product)
{
    public static AdminProductMutationResult Success(AdminProductResponse product)
    {
        return new AdminProductMutationResult(AdminProductRequestStatus.Success, product);
    }

    public static AdminProductMutationResult ValidationError()
    {
        return new AdminProductMutationResult(AdminProductRequestStatus.ValidationError, null);
    }

    public static AdminProductMutationResult Failure()
    {
        return new AdminProductMutationResult(AdminProductRequestStatus.Failure, null);
    }
}

public sealed record AdminProductDeleteResult(AdminProductRequestStatus Status)
{
    public static AdminProductDeleteResult Success()
    {
        return new AdminProductDeleteResult(AdminProductRequestStatus.Success);
    }

    public static AdminProductDeleteResult Failure()
    {
        return new AdminProductDeleteResult(AdminProductRequestStatus.Failure);
    }
}
