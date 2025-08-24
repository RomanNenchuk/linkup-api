using Application.Auth.Commands.LoginWithGoogle;
using Application.Auth.Commands.RefreshToken;
using Application.Common;
using Application.Common.Interfaces;
using Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
           .MapGet(Login, "login/google")
           .MapPost(RefreshToken, "refresh-token");

        app.MapGet("callback/google", GoogleCallback)
           .WithName("GoogleLoginCallback");
    }

    private IResult Login([FromQuery] string returnUrl, LinkGenerator linkGenerator,
        SignInManager<ApplicationUser> signInManager, HttpContext context)
    {
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google",
            linkGenerator.GetPathByName(context, "GoogleLoginCallback")
            + $"?returnUrl={returnUrl}");

        return Results.Challenge(properties, ["Google"]);
    }

    private async Task<IResult> GoogleCallback([FromQuery] string returnUrl, HttpContext context,
        ISender sender, ICookieService cookieService)
    {
        var authResult = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authResult.Succeeded) return Results.Unauthorized();

        var result = await sender.Send(new LoginWithGoogleCommand(authResult.Principal));

        if (!result.IsSuccess || result.Value == null)
            return Results.Redirect(returnUrl);

        cookieService.SetCookie("refreshToken", result.Value);
        return Results.Redirect(returnUrl);
    }

    private async Task<IResult> RefreshToken(HttpContext context, ISender sender, ICookieService cookieService)
    {
        var refreshToken = cookieService.GetCookie("refreshToken");
        if (string.IsNullOrEmpty(refreshToken))
            return Results.BadRequest("Refresh token is missing");

        var result = await sender.Send(new RefreshTokenCommand(refreshToken));
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }
}
