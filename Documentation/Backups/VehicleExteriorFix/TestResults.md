# Ofis ve garaj doğrulaması

28 Ekim 2026, Unity 6000.5.5f1, `C:/Users/uur/oyunnn` projesi. Son bütünleşik Play Mode koşusu dokuz kontrol grubunu geçti ve normal kapandı. `Logs/office_garage_validation.txt` dokuz **PASS** satırı, **Validation complete** ve son görsel düzeltmenin hedefli doğrulama notunu içerir.

Tam koşunun değişmeden korunan sonuçları `Logs/office_garage_full_release_validation.txt`, Unity günlüğü `Logs/manual_garage_release_validation.log` içindedir. Ardından yalnız eldeki alet çantasının cıvatalara bakarken daha aşağıda tutulması değiştirildi; modül akışı tekrar geçti. Bu koşu `Logs/manual_module_view_validation.txt` ve `Logs/manual_module_view_validation.log` içindedir. İki koşuda da C# derleme hatası, FAIL veya exception bulunmadı. Önceki rapor `Documentation/Backups/GarageSystems/TestResults.md` içinde korunur.

## Geçen kontroller

1. **Özgün van:** İki yan, tavan ve arka kapılarda doğrudan görsel mesh üzerinde 227 ışın örneği; dış yüzler, opak gövde/cam, farlar ve tutamaklar. Kaba araç collider'ı sorguları maskelemez.
2. **Sunum ve hareket:** Sabit oda yansımaları, FXAA, taşıma merkezi, duvara güvenli bırakma, kameraya giren ekipmanın gizlenip geri gelmesi, oyuncu çarpışma filtresi, gerçek W basma/bırakma ve seslerin kaldırılması.
3. **Ofis kapıları:** İki kapıyı iki yönden gerçek E/W girdisiyle açıp geçme; bağlı kollar, oyuncudan uzak açılma, oyuncu/eşyada güvenli durma, alan açılınca devam ve hızlı komutlardan toparlanma.
4. **Çekmeceler:** On altı tepsinin açılıp kapanması, oyuncu/ekipman engelinde durup devam etmesi, kolun etkileşim ışını ve seslerin kaldırılması.
5. **HUD ve duraklatma:** Gerçek yük/modül/bakım ve eşya durumu; Escape ile zaman/hareket/ses durması, pointer ayarları, kayıt geri okuması ve bilgisayar açıkken menü çakışmaması.
6. **Araç ve lift:** Dört özgün kapıyı gerçek E ile açıp kapama; cam/kol/kaporta bağlantıları; lift düğmeleriyle aracın ve özgün kolların yükselip inmesi; açık kapı/eksik montaj engeli, araç altındaki oyuncuda güvenli bekleme ve devam; FPS/ms sayacı ve statik gruplama.
7. **Bilgisayar:** Yedi uygulama, iş fişi ve sözleşme, pencere sürükleme/büyütme/küçültme/görev çubuğu, kapatma/kayıt/çıkış; siyah monitör ve insan ölçeği.
8. **Elle modül montajı:** Satın alma sonrası fiziksel teslimat; gerçek E/F ile alma/bırakma/yerleştirme; dört cıvatada basılı E ve erken bırakınca iptal; bağlantı sırası, sökme/yeniden takma, yarım montaj kayıt/yükleme. Yeteneğin yalnız tam montajda açılması ve gerçek v4 kaydının v5'e yükselmesi de doğrulandı.
9. **Şirket döngüsü:** Sahne referansları, itibar kilidi, yük ve hazırlık, saha sonucu callback'i, sözleşme başarı/başarısızlığı, tek seferlik rapor ödemesi, denetim ödül/cezası, bakım, sipariş/teslimat, geliştirme ve kayıt/yükleme.

Kontroller gerçek Unity Input System ve EventSystem olaylarıyla Play Mode içinde çalışır. Kullanıcının şirket kaydı ve testte değiştirilen tercihleri korunup geri yüklenir.

## Görsel kontrol

Seçilen ikinci arayüz tasarımı, aynı iş durumundaki Unity görüntüsüyle `UI-QA/ComparisonFinal.png` içinde yan yana karşılaştırıldı; fiyat, müşteri notu ve sabit satın alma alanının önceki sorunları giderildi. Sonuç ve bilinçli uyarlamalar [design-qa.md](../design-qa.md) içindedir. 1280×720, 1600×900 ve 1920×1080 bilgisayar çıktıları alındı.

`ManualModuleBay.png`, `ManualModuleInstalled.png`, `VanFrontDoorOpen.png`, `VanRearDoorOpen.png` ve `LiftRaised.png` gerçek Unity çıktılarıdır. Elde tutulan çanta cıvatalara bakarken aşağı çekilir; çalışma alanı görünür kalır. [Kontroller ve montaj sırası](ManualGarageSystems.md).

## Performans ölçümünün kapsamı

`Logs/garage_performance_comparison.json`: Aynı kamera açısından 1600×900 açık render isteğinde, 3.420 sabit renderer gruplandığında ortanca CPU render gönderim süresi **24,47 ms → 12,72 ms** oldu. Bu ölçüm bağımsız derlemenin GPU süresi veya oyun FPS artışı değildir. Bu ortamda çizim çağrısı sayacı kullanılamadığı için -1 raporlanır. Render ölçeği 1,0 kalır.

Oyundaki sayaç FPS ve ortalama kare süresini 0,5 saniyede bir, duraklatmada da gerçek zamanla günceller. Başsız test döngüsü görüntü sunmadığı için ekran görüntülerinde `FPS —` gösterilir.

## Kaynak ofisin korunması

- `Art/audit_presentation_source.py`: **2.887 kaynak dönüşümünde değişiklik yok**. Ortam FBX SHA-256: `2DA10567574E9A75FE2C0C9AF79F9AD0FC57F7D262898874BF8E10733980BD4C`. Sonuç: `Logs/presentation_source_parity.json`.
- `Art/audit_garage_refinement.py`: **989 korunan ofis renderer'ının mesh/malzeme bağlantıları aynı**. Sonuç: `Logs/garage_office_preservation.json`.
- Kaynak ofis yerleşimi, ölçeği ve modelleri değiştirilmedi. Monitörün siyah sahne malzemesi önceki kullanıcı isteğidir. Vanın kayıtlı park konumu korunur; lift yalnız çalışma sırasında hareket ettirir.

## Sınırlar

İnsanla serbest uçtan uca oynanış ve bağımsız derlemede FPS ölçümü yapılmadı. Saha sonucu callback'i sınandı; gerçek saha sahnesi, sürüş, NPC ve çok oyunculu ağ eşitlemesi uygulanmış özellik olarak değerlendirilmez.
