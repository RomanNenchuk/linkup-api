using Application.Common;
using Application.Common.DTOs;
using Application.Posts.Queries.GetPosts;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
namespace Tests.Posts;

public class GetPostsTests
{
    [Fact]
    public async Task GetPosts_ReturnsOk_WhenHandlerReturnsSuccess()
    {
        // arrange
        var sender = new Mock<ISender>();

        sender
            .Setup(s => s.Send(It.IsAny<GetPostsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<PostResponseDto>>.Success(new PagedResult<PostResponseDto>()));

        var postsEndpoint = new Web.Endpoints.Posts();

        // act
        var result = await postsEndpoint.GetPosts(
            sender.Object,
            latitude: null,
            longitude: null,
            cursor: null,
            authorId: null,
            sort: "recent",
            radius: 10,
            pageSize: 10
        );

        // assert
        Assert.IsType<Ok<PagedResult<PostResponseDto>>>(result);
    }
}
