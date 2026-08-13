namespace ArsaTapu.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityAdi, object anahtar)
        : base($"{entityAdi} bulunamadı. (Id: {anahtar})") { }
}
