using IncidentManagement.Shared.Models;
using IncidentManagement.Shared.Services;

namespace IncidentManagement.DocumentApp.Services;

public static class SeedService
{
    public static async Task SeedIfEmptyAsync(DocumentRepository repo)
    {
        if (await repo.CountAsync() > 0) return;

        var docs = new List<Document>
        {
            new()
            {
                Title = "JOB_ABEND_U4038 — Job Anormal Sonlandı",
                Category = DocumentCategory.ControlMErrors,
                AuthorName = "Platform Ops",
                AuthorTeam = "Platform Ops",
                Content = """
                    JOB_ABEND_U4038 — Job Anormal Sonlandı

                    Hata Kodu: JOB_ABEND_U4038
                    Açıklama: Job, hedef servise bağlanmaya çalışırken timeout aldı.

                    Olası Nedenler:
                    - Hedef servis down durumunda
                    - Network timeout değerleri düşük ayarlı
                    - Servis aşırı yüklenmiş

                    Çözüm Adımları:
                    1. Hedef servisin health check'ini kontrol et
                    2. Network timeout değerini artır (önerilen: 60s)
                    3. Servis loglarını incele
                    4. Gerekirse Platform Ops ekibini eskalasyon için bilgilendir

                    Sorumlu Ekip: Platform Ops
                    Öncelik: Yüksek
                    """,
                Status = DocumentStatus.Approved,
                IsIndexed = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ApprovedAt = DateTime.UtcNow.AddDays(-4),
                ApprovedBy = "Admin"
            },
            new()
            {
                Title = "2024-03 API Timeout Olayı — Connection Pool Çözümü",
                Category = DocumentCategory.IncidentHistory,
                AuthorName = "Backend Team",
                AuthorTeam = "Backend",
                Content = """
                    Incident: 2024-03 API Timeout Olayı

                    Tarih: Mart 2024
                    Etki: OrderProcessingJob sürekli timeout alarak başarısız oldu.

                    Kök Neden:
                    Connection pool limiti 10 olarak ayarlıydı. Yoğun dönemde tüm connection'lar doldu,
                    yeni istekler timeout aldı.

                    Çözüm:
                    1. Connection pool maxSize değeri 10'dan 50'ye çıkarıldı
                    2. Connection timeout 30s'den 60s'ye artırıldı
                    3. Circuit breaker pattern eklendi

                    Sonuç: Uygulama sonrası timeout sayısı %95 azaldı.

                    Alınan Ders: Yoğun dönemlerde connection pool izlenmeli.
                    """,
                Status = DocumentStatus.Approved,
                IsIndexed = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                ApprovedAt = DateTime.UtcNow.AddDays(-2),
                ApprovedBy = "Admin"
            },
            new()
            {
                Title = "OrderProcessingJob — Kod Dokümantasyonu",
                Category = DocumentCategory.CodeDocumentation,
                AuthorName = "Dev Team",
                AuthorTeam = "Backend",
                Content = """
                    OrderProcessingJob — Kod Dokümantasyonu

                    Görev: Sipariş durumunu günceller.
                    Tetikleme: Her gece 02:00'de çalışır.

                    Çağırdığı Endpoint: POST /api/orders/update
                    Bağımlılıklar:
                    - OrderService (port 8080)
                    - MongoDB orders collection
                    - Redis cache (session bilgisi)

                    Hata Noktaları:
                    - OrderService down ise JOB_ABEND_U4038 hatası alınır
                    - MongoDB bağlantı sorunu ise JOB_ABEND_U9999 alınır
                    - Redis timeout ise job warning ile tamamlanır

                    Yeniden Başlatma:
                    Job, son başarılı checkpoint'ten devam eder.
                    Manuel yeniden başlatma: Control-M konsolundan "Restart from checkpoint" seç.
                    """,
                Status = DocumentStatus.Approved,
                IsIndexed = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ApprovedAt = DateTime.UtcNow.AddHours(-2),
                ApprovedBy = "Admin"
            }
        };

        foreach (var doc in docs)
            await repo.InsertAsync(doc);
    }
}
