namespace UserManagementApp.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken);
    }
}
