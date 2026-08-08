namespace CafeMenu.Api.Entities;

public sealed class RoleEntity
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<AppUserEntity> Users { get; } = new List<AppUserEntity>();
}
