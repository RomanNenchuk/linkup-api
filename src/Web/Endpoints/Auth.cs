using Application.Auth.Commands.LoginWithGoogle;
using Application.Auth.Commands.RefreshToken;
using Application.Auth.Commands.Register;
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
           .MapPost(RefreshToken, "refresh-token")
           .MapPost(Register, "register")
           .MapPost(ResendVerification, "resend-verification")
           .MapPost(ConfirmEmail, "confirm-email")
           .MapPost(SendTestEmail, "send-test-email");

        app.MapGet("callback/google", GoogleCallback)
           .WithName("GoogleLoginCallback");
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

    private async Task<IResult> RefreshToken(HttpContext context, ISender sender, ICookieService cookieService)
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

    private async Task<IResult> SendTestEmail(HttpContext context, ISender sender, ICookieService cookieService, IEmailService emailService)
    {
        await emailService.SendEmailAsync("rmntemporary1@gmail.com", "Hello!", "<p>Це тестовий лист</p>");
        return Results.Ok("Email sent!");
    }

    private async Task<IResult> ResendVerification(HttpContext context, ISender sender, ICookieService cookieService, IEmailService emailService)
    {
        await emailService.SendEmailAsync("rmntemporary1@gmail.com", "Hello!", "<p>Це тестовий лист</p>");
        return Results.Ok("Email sent!");
    }

    private async Task<IResult> ConfirmEmail(HttpContext context, ISender sender, ICookieService cookieService, IEmailService emailService)
    {
        await emailService.SendEmailAsync("rmntemporary1@gmail.com", "Hello!", "<p>Це тестовий лист</p>");
        return Results.Ok("Email sent!");
    }
}
