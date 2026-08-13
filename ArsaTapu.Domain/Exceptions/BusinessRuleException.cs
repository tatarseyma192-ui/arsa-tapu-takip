namespace ArsaTapu.Domain.Exceptions;

/// <summary>
/// İş kuralı ihlali (ör. tekilleştirme anahtarı çakışması). Message alanı doğrudan
/// kullanıcıya gösterilebilecek şekilde yazılmalıdır (Handbook madde 9).
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
