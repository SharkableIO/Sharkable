using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Sharkable.Extensions;

namespace Sharkable;

public abstract class SharkEndpoint : ISharkEndpoint
{
    //internal string corsName;
    internal string? grouName;
    internal string? apiPrefix;
    internal string? baseApiPath;
    public abstract void AddRoutes(IEndpointRouteBuilder app);

    protected SharkEndpoint()
    {
        var name = GetType().Name;

        grouName = name.FormatAsGroupName()!;

        // using var scope = Shark.ServiceScopeFactory.CreateScope();
        // var opt = scope.ServiceProvider.GetService<IOptions<SharkOption>>();

        // if(opt != null)
        // {
        //     apiPrefix = opt.Value.ApiPrefix;
        // }
        // else
        // {
        //     apiPrefix = "api";
        // }
        // baseApiPath = apiPrefix + "/" + grouName;
    }

    protected SharkEndpoint(string? grouName, string apiPrefix = "api")
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(grouName);
        this.grouName = grouName;
        this.apiPrefix = apiPrefix;
        // baseApiPath = apiPrefix + "/" + grouName;
    }
}
