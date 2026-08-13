namespace ArsaTapu.Domain.Common;

/// <summary>
/// Requirements madde 1'deki rol tanımları (Admin / Personel / Patron).
/// </summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Personel = "Personel";
    public const string Patron = "Patron";

    public static readonly string[] Tumu = { Admin, Personel, Patron };
}
