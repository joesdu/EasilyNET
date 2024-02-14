using MongoDB.Bson;

namespace EasilyNET.MongoDistributedLock.Attributes;

/// <summary>
/// IAcquire
/// </summary>
public interface IAcquire
{
    /// <summary>
    /// true if lock successfully acquired; otherwise, false
    /// </summary>
    bool Acquired { get; }

    /// <summary>
    /// ID
    /// </summary>
    ObjectId AcquireId { get; }
}
