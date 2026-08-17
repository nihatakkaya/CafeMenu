using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminPlatform;

public sealed class AdminPlatformCafeResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class AdminPlatformDashboardStatsResponse
{
    public int ActiveCafeCount { get; init; }

    public int InactiveCafeCount { get; init; }

    public int PublishedCafeCount { get; init; }

    public int DraftCafeCount { get; init; }
}

public sealed class AdminPlatformCafeMemberResponse
{
    public long MembershipId { get; init; }

    public long AppUserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class AdminPlatformUserSetupResponse
{
    public long UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string SetupToken { get; init; } = string.Empty;

    public DateTimeOffset SetupTokenExpiresAt { get; init; }
}

public sealed class AdminPlatformUserSearchResponse
{
    public long AppUserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class AdminPlatformMembershipResponse
{
    public long Id { get; init; }

    public long CafeId { get; init; }

    public long AppUserId { get; init; }

    public string UserEmail { get; init; } = string.Empty;

    public string UserFullName { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class AdminPlatformCreateCafeFormModel
{
    [Required(ErrorMessage = "Cafe adi zorunludur.")]
    [StringLength(160, MinimumLength = 2, ErrorMessage = "Cafe adi 2 ile 160 karakter arasinda olmalidir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(120, MinimumLength = 2, ErrorMessage = "Menü adresi 2 ile 120 karakter arasında olmalıdır.")]
    [RegularExpression("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$", ErrorMessage = "Menü adresi yalnız harf, rakam ve tire içerebilir.")]
    public string? Slug { get; set; }
}

public sealed class AdminPlatformCreateUserSetupFormModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "E-posta geçersiz.")]
    [StringLength(320, ErrorMessage = "E-posta en fazla 320 karakter olabilir.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Ad soyad 2 ile 200 karakter arasinda olmalidir.")]
    public string FullName { get; set; } = string.Empty;
}

public sealed class AdminPlatformUserSearchFormModel
{
    [Required(ErrorMessage = "Arama metni zorunludur.")]
    [StringLength(320, MinimumLength = 2, ErrorMessage = "Arama metni 2 ile 320 karakter arasinda olmalidir.")]
    public string Query { get; set; } = string.Empty;
}

public sealed class AdminPlatformCafeActionFormModel
{
    public string? Action { get; set; }
}

public sealed class AdminPlatformMemberActionFormModel
{
    public string? Action { get; set; }
}

public sealed record AdminPlatformCreateCafeRequest(string Name, string? Slug);

public sealed record AdminPlatformCreateUserSetupRequest(string Email, string FullName);

public sealed record AdminPlatformUserSearchRequest(string Query, int PageSize = 10);

public sealed record AdminPlatformAssignCafeMemberRequest(long CafeId, long AppUserId);
