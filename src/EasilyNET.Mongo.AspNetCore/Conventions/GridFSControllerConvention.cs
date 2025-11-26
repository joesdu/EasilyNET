using EasilyNET.Mongo.AspNetCore.Controllers;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace EasilyNET.Mongo.AspNetCore.Conventions;

/// <summary>
///     <para xml:lang="en">GridFS controller convention</para>
///     <para xml:lang="zh">GridFS控制器约定</para>
/// </summary>
internal sealed class GridFSControllerConvention(GridFSServerOptions options) : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerType != typeof(GridFSController))
        {
            return;
        }
        if (!options.EnableController)
        {
            controller.ApiExplorer.IsVisible = false;
            controller.Actions.Clear();
            return;
        }
        if (options.AuthorizeData.Count is not 0)
        {
            controller.Filters.Add(new AuthorizeFilter(options.AuthorizeData));
        }
        foreach (var filter in options.Filters)
        {
            controller.Filters.Add(filter);
        }
    }
}