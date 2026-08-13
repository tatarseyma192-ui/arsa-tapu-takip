using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArsaTapu.DataAccess.Configurations;

public class ParselKmlConfiguration : IEntityTypeConfiguration<ParselKml>
{
    public void Configure(EntityTypeBuilder<ParselKml> builder)
    {
        builder.ToTable("ParselKmlleri");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Il).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Ilce).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Mahalle).IsRequired().HasMaxLength(150);
        builder.Property(x => x.DosyaYolu).HasMaxLength(500);
        builder.Property(x => x.Durum).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Kaynak).HasConversion<string>().HasMaxLength(20);

        // KML tekilleştirme anahtarı (Requirements madde 4.2) — mülkiyet anahtarından farklı.
        // FİLTRELİ (partial) unique index: yalnızca silinmemiş kayıtlar arasında benzersizlik
        // zorunlu kılınır. Aksi halde madde 4.3 çalışmaz: bir kayıt soft-delete edildikten sonra
        // aynı Ada/Parsel tekrar sorgulandığında yeni satır eklenmek istendiğinde, eski (silinmiş)
        // satır hâlâ fiziksel olarak tabloda durduğu için UNIQUE ihlali oluşurdu.
        builder.HasIndex(x => new { x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_ParselKml_Anahtar");
    }
}
