# Ofis ve garaj doğrulaması

28 Ekim 2026, Unity 6000.5.5f1, `C:/Users/uur/oyunnn` projesi. Araç iç yüzeyleri, arka kapı kolları ve dış modül yuvasının son bütünleşik Play Mode koşusu dokuz kontrol grubunu geçti ve normal kapandı. `Logs/office_garage_validation.txt` dokuz **PASS** satırı, **Validation complete** ve son görsel düzeltmenin hedefli doğrulama notunu içerir.

Tam koşunun değişmeden korunan sonuçları `Logs/vehicle_exterior_full_release_validation.txt`, Unity günlüğü `Logs/vehicle_exterior_fix_final.log` içindedir. Ardından ön cam direğinin küçük iç açıklığı kapatıldı; iç görünürlük, dört kapı ve lift güvenliği tekrar geçti. Bu koşu `Logs/vehicle_interior_final_validation.txt` ve `Logs/vehicle_interior_final_validation.log` içindedir. İki son koşuda da C# derleme hatası, FAIL veya exception bulunmadı. İlk ara koşuda dış modül için fazla geniş tutulan lift güvenlik hacmi kumandadaki oyuncuyu engel saydı; gövde ve dış taşıyıcı ayrı denetlendi ve yeniden geçti. Önceki rapor `Documentation/Backups/VehicleExteriorFix/TestResults.md` içinde korunur.

## Geçen kontroller

1. **Özgün van:** İki yan, tavan ve arka kapılarda doğrudan görsel mesh üzerinde 227 ışın örneği; dış yüzler, opak gövde/cam, farlar ve tutamaklar. Kaba araç collider'ı sorguları maskelemez.
2. **Sunum ve hareket:** Sabit oda yansımaları, FXAA, taşıma merkezi, duvara güvenli bırakma, kameraya giren ekipmanın gizlenip geri gelmesi, oyuncu çarpışma filtresi, gerçek W basma/bırakma ve seslerin kaldırılması.
3. **Ofis kapıları:** İki kapıyı iki yönden gerçek E/W girdisiyle açıp geçme; bağlı kollar, oyuncudan uzak açılma, oyuncu/eşyada güvenli durma, alan açılınca devam ve hızlı komutlardan toparlanma.
4. **Çekmeceler:** On altı tepsinin açılıp kapanması, oyuncu/ekipman engelinde durup devam etmesi, kolun etkileşim ışını ve seslerin kaldırılması.
5. **HUD ve duraklatma:** Gerçek yük/modül/bakım ve eşya durumu; Escape ile zaman/hareket/ses durması, pointer ayarları, kayıt geri okuması ve bilgisayar açıkken menü çakışmaması.
6. **Araç ve lift:** Dört özgün kapıyı gerçek E ile açıp kapama; cam/kol/kaporta bağlantıları; lift düğmeleriyle aracın ve özgün kolların yükselip inmesi; açık kapı/eksik montaj engeli, gövde ve dış modül altındaki oyuncuda güvenli bekleme ve devam; FPS/ms sayacı ve statik gruplama. Arka panel/kol geometrisinin 3.336 köşesi kendi kapı yarısında kaldı. Backface ışınları kapalıyken içeriden 120 örnek gerçek opak mesh yüzeyine çarptı. Ayrıntı: `Logs/vehicle_exterior_fix_checks.txt`.
7. **Bilgisayar:** Yedi uygulama, iş fişi ve sözleşme, pencere sürükleme/büyütme/küçültme/görev çubuğu, kapatma/kayıt/çıkış; siyah monitör ve insan ölçeği.
8. **Elle dış modül montajı:** Satın alma sonrası fiziksel teslimat; arka kapılar kapalıyken yan dış yuvada gerçek E/F ile alma/bırakma/yerleştirme; dört cıvatada basılı E ve erken bırakınca iptal; bağlantı sırası, sökme/yeniden takma, yarım montaj kayıt/yükleme. Takılı modülün bütün mesh köşeleri aracın yan dış yüzeyinin dışında kaldı. Yeteneğin yalnız tam montajda açılması ve gerçek v4 kaydının v5'e yükselmesi de doğrulandı.
9. **Şirket döngüsü:** Sahne referansları, itibar kilidi, yük ve hazırlık, saha sonucu callback'i, sözleşme başarı/başarısızlığı, tek seferlik rapor ödemesi, denetim ödül/cezası, bakım, sipariş/teslimat, geliştirme ve kayıt/yükleme.

Kontroller gerçek Unity Input System ve EventSystem olaylarıyla Play Mode içinde çalışır. Kullanıcının şirket kaydı ve testte değiştirilen tercihleri korunup geri yüklenir.

## Görsel kontrol

Seçilen ikinci arayüz tasarımı, aynı iş durumundaki Unity görüntüsüyle `UI-QA/ComparisonFinal.png` içinde yan yana karşılaştırıldı; fiyat, müşteri notu ve sabit satın alma alanının önceki sorunları giderildi. Sonuç ve bilinçli uyarlamalar [design-qa.md](../design-qa.md) içindedir. 1280×720, 1600×900 ve 1920×1080 bilgisayar çıktıları alındı.

`CabOpenInteriorFixed.png`, `CargoOpenInteriorFixed.png`, `RearDoor_Right_HardwareFixed.png`, `RearDoor_Left_HardwareFixed.png` ve `ExteriorModuleMounted.png` gerçek Unity çıktılarıdır. Kabin ve boş yük bölmesinin iç yüzeyleri, tek tek açılan arka kapıların kolları ve yan dış modül görsel olarak incelendi. `ManualModuleBay.png` / `ManualModuleInstalled.png` artık dış montaj alanını gösterir. [Düzeltme notları](VehicleExteriorFix.md), [kontroller ve montaj sırası](ManualGarageSystems.md).

## Performans ölçümünün kapsamı

`Logs/vehicle_exterior_performance_full.json`: Son tam koşuda aynı kamera açısından 1600×900 açık render isteğinde, 3.420 sabit renderer gruplandığında ortanca CPU render gönderim süresi **25,42 ms → 12,54 ms** oldu. Bu ölçüm bağımsız derlemenin GPU süresi veya oyun FPS artışı değildir. Bu ortamda çizim çağrısı sayacı kullanılamadığı için -1 raporlanır. Render ölçeği 1,0 kalır. `garage_performance_comparison.json` her garaj doğrulamasında yenilenir.

Oyundaki sayaç FPS ve ortalama kare süresini 0,5 saniyede bir, duraklatmada da gerçek zamanla günceller. Başsız test döngüsü görüntü sunmadığı için ekran görüntülerinde `FPS —` gösterilir.

## Kaynak ofisin korunması

- `Art/audit_presentation_source.py`: **2.887 kaynak dönüşümünde değişiklik yok**. Ortam FBX SHA-256: `2DA10567574E9A75FE2C0C9AF79F9AD0FC57F7D262898874BF8E10733980BD4C`. Sonuç: `Logs/presentation_source_parity.json`.
- `Art/audit_garage_refinement.py`: **989 korunan ofis renderer'ının mesh/malzeme bağlantıları aynı**. Sonuç: `Logs/garage_office_preservation.json`.
- Kaynak ofis yerleşimi, ölçeği ve modelleri değiştirilmedi. Monitörün siyah sahne malzemesi önceki kullanıcı isteğidir. Vanın kayıtlı park konumu korunur; lift yalnız çalışma sırasında hareket ettirir.

## Sınırlar

İnsanla serbest uçtan uca oynanış ve bağımsız derlemede FPS ölçümü yapılmadı. Saha sonucu callback'i sınandı; gerçek saha sahnesi, sürüş, NPC ve çok oyunculu ağ eşitlemesi uygulanmış özellik olarak değerlendirilmez.
