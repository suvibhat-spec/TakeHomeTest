namespace ECommerce.Shared.ExceptionHandling.Exceptions;
public class EmailTakenException : Exception
{
    public EmailTakenException(string email) 
        : base($"Email {email} is already taken") { }
}   