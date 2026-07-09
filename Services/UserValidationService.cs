using System.Net.Mail;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class UserValidationService
{
    public bool TryValidateUser(string name, string email, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Email is required.";
            return false;
        }

        if (!IsValidEmail(email))
        {
            error = "Email format is invalid.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
