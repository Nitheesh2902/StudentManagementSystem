namespace ServiceDesk.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsActive { get; private set; } = true;

    public void Activate() => IsActive = true;
    public virtual void Deactivate() => IsActive = false;
}
