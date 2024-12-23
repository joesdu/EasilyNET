using EasilyNET.Mongo.AspNetCore.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Factories;

internal sealed class GridFSBucketFactory(IOptionsMonitor<GridFSBucketOptions> optionsMonitor) : IGridFSBucketFactory
{
    public IGridFSBucket CreateBucket(IMongoDatabase db)
    {
        var options = optionsMonitor.Get(Constant.ConfigName);
        return new GridFSBucket(db, options);
    }
}