namespace ArsaTapu.Domain.Common;

/// <summary>
/// Tüm domain tablolarının ortak audit alanları (Handbook madde 5).
/// Soft delete tercih edilir; fiziksel silme yapılmaz.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
