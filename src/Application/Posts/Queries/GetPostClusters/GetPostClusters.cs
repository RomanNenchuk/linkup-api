using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Posts.Queries.GetPostClusters;


public class GetPostClustersQuery : IRequest<Result<List<ClusterDto>>>
{
}

public class GetPostClustersQueryHandler(IGeoService geoService)
    : IRequestHandler<GetPostClustersQuery, Result<List<ClusterDto>>>
{
    public async Task<Result<List<ClusterDto>>> Handle(GetPostClustersQuery request, CancellationToken ct)
    {
        return await geoService.GetPostClustersAsync(ct);
    }
}
