using Application.Auth.Commands.ComfirmEmail;
using Application.Auth.Commands.Login;
using Application.Auth.Commands.LoginWithGoogle;
using Application.Auth.Commands.Logout;
using Application.Auth.Commands.RefreshToken;
using Application.Auth.Commands.Register;
using Application.Auth.Commands.ResendEmailVerification;
using Application.Auth.Queries.GetCurrentUserInfo;
using Application.Auth.Queries.GetEmailVerificationCooldown;
using Application.Common.Interfaces;
using Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(LoginWithGoogle, "login/google")
            .MapPost(Login, "login")
            .MapPost(Register, "register")
            .MapPost(RefreshToken, "refresh-token");

        app.MapGroup(this)
            .MapGet("callback/google", GoogleCallback)
            .WithName("GoogleLoginCallback");

        app.MapGroup(this)
           .RequireAuthorization()
           .MapPost(ResendEmailVerification, "resend-verification")
           .MapPost(ConfirmEmail, "confirm-email")
           .MapGet(GetCooldownRemainingSeconds, "verification-cooldown")
           .MapGet(GetCurrentUserInfo, "me")
           .MapGet(Logout, "logout");
    }

    private IResult LoginWithGoogle([FromQuery] string returnUrl, LinkGenerator linkGenerator,
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

    private async Task<IResult> RefreshToken(ISender sender, ICookieService cookieService)
    {
        var refreshToken = cookieService.GetCookie("refreshToken");
        if (string.IsNullOrEmpty(refreshToken))
            return Results.BadRequest("Refresh token is missing");

        var result = await sender.Send(new RefreshTokenCommand(refreshToken));
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> Register(RegisterCommand command, ISender sender, ICookieService cookieService)
    {
        var result = await sender.Send(command);
        if (!result.IsSuccess || result.Value?.RefreshToken == null || result.Value?.AccessToken == null)
            return Results.BadRequest(result.Error);

        cookieService.SetCookie("refreshToken", result.Value.RefreshToken);
        return Results.Ok(result.Value.AccessToken);
    }

    private async Task<IResult> Login(LoginCommand command, ISender sender, ICookieService cookieService)
    {
        var result = await sender.Send(command);
        if (!result.IsSuccess || result.Value?.RefreshToken == null || result.Value?.AccessToken == null)
            return Results.BadRequest(result.Error);

        cookieService.SetCookie("refreshToken", result.Value.RefreshToken);
        return Results.Ok(result.Value.AccessToken);
    }

    private async Task<IResult> ResendEmailVerification(ISender sender)
    {
        var result = await sender.Send(new ResendEmailVerificationCommand());
        return result.IsSuccess ? Results.Ok("Email resend successfully!") : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetCooldownRemainingSeconds(ISender sender)
    {
        var result = await sender.Send(new GetEmailVerificationCooldownQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> ConfirmEmail(ConfirmEmailCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.Ok("Email confirmed successfully") : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetCurrentUserInfo(ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserInfoQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> Logout(ISender sender, ICookieService cookieService)
    {
        var refreshToken = cookieService.GetCookie("refreshToken");
        if (string.IsNullOrEmpty(refreshToken))
            return Results.BadRequest("Refresh token is missing");

        var result = await sender.Send(new LogoutCommand(refreshToken));
        if (!result.IsSuccess) return Results.BadRequest(result.Error);

        cookieService.DeleteCookie("refreshToken");

        return Results.Ok();
    }
}
