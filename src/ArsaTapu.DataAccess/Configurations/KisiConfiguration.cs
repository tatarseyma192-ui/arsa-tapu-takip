using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArsaTapu.DataAccess.Configurations;

public class KisiConfiguration : IEntityTypeConfiguration<Kisi>
{
    public void Configure(EntityTypeBuilder<Kisi> builder)
    {
        builder.ToTable("Kisiler");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdSoyad).IsRequired().HasMaxLength(200);
        builder.Property(x => x.KullaniciId).HasMaxLength(450);

        // Bir kullanıcı hesabı yalnızca bir Kisi'ye bağlanabilir (Patron 1:1 eşleşme).
        // Silinmiş (soft-delete) kişiler bu benzersizliğe dahil edilmez — bir Kisi silinip
        // aynı KullaniciId başka/yeni bir Kisi'ye bağlanabilmeli.
        builder.HasIndex(x => x.KullaniciId)
            .IsUnique()
            .HasFilter("\"KullaniciId\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasMany(x => x.YuklemeKayitlari)
            .WithOne(x => x.Kisi)
            .HasForeignKey(x => x.KisiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tasinmazlar)
            .WithOne(x => x.Kisi)
            .HasForeignKey(x => x.KisiId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
