using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArsaTapu.DataAccess.Configurations;

public class TasinmazConfiguration : IEntityTypeConfiguration<Tasinmaz>
{
    public void Configure(EntityTypeBuilder<Tasinmaz> builder)
    {
        builder.ToTable("Tasinmazlar");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TasinmazNo).HasMaxLength(50); // Nullable — bkz. Tasinmaz.cs üzerindeki açıklama
        builder.Property(x => x.Nitelik).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Il).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Ilce).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Mahalle).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ZeminHisseId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Yuzolcum).HasColumnType("numeric(14,2)");
        builder.Property(x => x.Durum).HasConversion<string>().HasMaxLength(20);

        // Mülkiyet tekilleştirme anahtarı: BagimsizBolumNo + ZeminHisseId (KisiId bazında).
        // TasinmazNo BİLEREK anahtarın DIŞINDA tutulur — 2026-08-04'te sağlanan gerçek bir
        // Excel örneğinde bu sütun hiç yoktu; aynı gerçek taşınmaz hem TasinmazNo'lu (PDF)
        // hem TasinmazNo'suz (Excel) kaynaktan gelebildiğinden, anahtara dahil edilirse aynı
        // taşınmaz iki kez (yanlışlıkla "Yeni Alım" olarak) kaydedilirdi. KML anahtarıyla
        // KARIŞTIRILMAZ (o, Il+Ilce+Mahalle+Ada+Parsel kullanır).
        // FİLTRELİ (partial) unique index — ParselKml'deki aynı gerekçeyle: bir taşınmaz
        // soft-delete edildikten sonra aynı anahtarla yeniden eklenebilmesi (ör. bir sonraki
        // yüklemede "Yeni Alım" olarak tekrar görülmesi) için yalnızca silinmemiş kayıtlar
        // arasında benzersizlik zorunlu kılınır.
        builder.HasIndex(x => new { x.KisiId, x.BagimsizBolumNo, x.ZeminHisseId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Tasinmaz_MulkiyetAnahtari");

        // Ortaklık/komşuluk hesaplaması ve KML tetikleme için parsel bazlı arama indeksi.
        builder.HasIndex(x => new { x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel })
            .HasDatabaseName("IX_Tasinmaz_ParselAnahtari");

        builder.HasOne(x => x.IlkGorulduguYukleme)
            .WithMany()
            .HasForeignKey(x => x.IlkGorulduguYuklemeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SonGorulduguYukleme)
            .WithMany()
            .HasForeignKey(x => x.SonGorulduguYuklemeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
