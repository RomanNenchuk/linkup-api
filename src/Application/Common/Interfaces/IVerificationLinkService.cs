namespace Application.Common.Interfaces;

public interface IVerificationLinkService
{
    string BuildEmailConfirmationLink(string token);
    string BuildPasswordResetLink(string token);
}