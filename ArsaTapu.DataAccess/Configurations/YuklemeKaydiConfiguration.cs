using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArsaTapu.DataAccess.Configurations;

public class YuklemeKaydiConfiguration : IEntityTypeConfiguration<YuklemeKaydi>
{
    public void Configure(EntityTypeBuilder<YuklemeKaydi> builder)
    {
        builder.ToTable("YuklemeKayitlari");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KaynakDosyaAdi).IsRequired().HasMaxLength(300);
        builder.Property(x => x.YukleyenKullaniciId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.KaynakTuru).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.KisiId, x.YuklemeTarihi });
    }
}
