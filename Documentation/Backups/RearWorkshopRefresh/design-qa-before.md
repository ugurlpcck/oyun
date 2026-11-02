# Arayüz tasarımı doğrulaması

final result: passed

- Görsel hedef: `Documentation/UI-Selected-WorkOrder.png` (kullanıcının seçtiği ikinci görsel, 1672×941).
- Son Unity çıktısı: `Documentation/UI-QA/JobsFinal.png`, 1600×900, İlanlar / Çamura Saplanan Araç.
- Yoğunluk: UI referansı 1600×900, Canvas ölçeği 1. Hedef 1600×900'e normalize edildi; tarayıcı/CSS katmanı yok.
- Son birlikte karşılaştırma: `Documentation/UI-QA/ComparisonFinal.png` (solda hedef, sağda Unity). İşlem alanının yakın karşılaştırması: `Documentation/UI-QA/FooterComparisonFinal.png`.

## İlk karşılaştırma

- [P1] İş bedelinin sayısal değeri görünmüyor. Fontun satır yüksekliği 35 px alana sığmıyor. Fiyat alanını 44 px'e çıkar.
- [P2] Fişin son müşteri notu kesiliyor. Ayrıntı listesinin boşluklarını ve alt dolgusunu daralt; tam notu görünür tut.
- [P2] Başlık hedefe göre küçük ve bölüm başlığı ince. Başlığı büyüt, bölüm başlıklarında Semibold kullan.
- [P1] Sipariş akışı gerçek Play Mode kontrolünde ikinci ürüne geçtikten sonra görünür olmayan düğmeye ulaşıyor. Satın alma alanını kaydırılan ürün bilgisinden ayırıp sabitle; seçimi ve siparişi ardışık kullanıcı kareleriyle yeniden doğrula.

## Düzeltme ve yeniden doğrulama

İlk karşılaştırma `JobsIterationA.png` ve `ComparisonA.png` içinde korunur. Fiyat yüksekliği 44 px'e çıkarıldı; ayrıntı boşlukları daraltılıp müşteri notunun tamamı görünür tutuldu. Ana başlık 38 px, bölüm başlıkları Semibold oldu. Mağazanın satın alma alanı kaydırılan içerikten ayrılarak pencere altına sabitlendi. Ürün seçimi ve satın alma, ardışık kullanıcı karelerinde gerçek pointer olaylarıyla tekrar geçti.

Son hedef ve Unity görüntüsü aynı karşılaştırma görselinde incelendi. Fiyat, sözleşme açıklaması ve kabul düğmesi görünür. 1280×720, 1600×900 ve 1920×1080 çıktıları alındı; yedi uygulamanın açılması, pencere sürükleme/büyütme/küçültme, sipariş, sözleşme, rapor tahsilatı ve kayıt akışı bütünleşik Play Mode denetiminde geçti. Sonuçlar `Logs/office_garage_validation.txt` içindedir.

## Beş zorunlu yüzey

- Yazı: Source Sans 3 Regular/Semibold, Türkçe ve ₺ glif kontrolü geçti. Başlık, fiyat ve bölüm hiyerarşisi düzeltildi. Hedefin üretilmiş yazısına göre ana başlık biraz daha hafif; kalan fark P3 görsel ayrıntıdır.
- Yerleşim: hedefteki iki sütun masaüstü, sol bölüm menüsü, ilan listesi ve sağ fiş korunuyor. Fiş altı kırpması ve mağaza işlem alanı düzeltildi. Pencere sürükleme ve görev çubuğu gerçek Unity EventSystem ile çalışıyor.
- Renk: planın sıcak kâğıt, mürekkep ve servis mavisi uygulanmış. Üretim ekranında hedefteki yapay gradyan kaldırıldı.
- Varlıklar: Phosphor Regular resmî kaynakları kullanılıyor; ürün önizlemeleri oyunun gerçek prefablarından render edildi. Araç/ofis yeniden tasarlanmadı.
- Metin: gerçek oyun kataloğu, farklı müşteriler ve altı gerçek ilan kullanılıyor; hedefteki örnek beş ilanla sınırlanmadı. Sözleşme gerçek aşınma/kayıp koşullarını açıklıyor.

## Bilinçli uyarlamalar ve sınır

Gerçek katalogdaki altı iş ve farklı müşteriler kullanılır; örnek görselin beş satırı kopyalanmaz. Tarih gerçek oyun tarihidir. Sözleşme gerçek aşınma/kayıp koşulunu açıklar. Simgeler resmî Phosphor Regular ailesinden gelir; ürün görselleri gerçek oyun modelleridir. Üretilmiş görseldeki gradyanlar ve kabartmalı pencere düğmeleri düz Unity yüzeylerine uyarlandı. Kullanıcının istediği FPS sayacı eklendi; başsız test görüntüsünde sunulmayan kareler FPS olarak raporlanmadığı için `FPS —` görünür.

Bu değerlendirme Unity Canvas/camera görüntüleri ve gerçek Input System/EventSystem olaylarıyla yapılan otomatik Play Mode kontrollerine dayanır. Tarayıcı prototipi, insanla serbest oynanış veya bağımsız derlemede FPS sertifikasyonu yapılmadı. Açık P1/P2 görsel engel kalmadı.
