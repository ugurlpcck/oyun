# Ofis ve garaj doğrulaması

28 Ekim 2026, Unity 6000.5.5f1, `C:/Users/uur/oyunnn` projesi. Beş ayrı modül görseli, cıvata hareketi ve servis kablosunu içeren son bütünleşik Play Mode koşusu on kontrol grubunu geçti ve normal kapandı. `Logs/office_garage_validation.txt` on **PASS** satırı, **Validation complete** ve son destek/etiket düzeltmesinin hedefli doğrulama notunu içerir.

Tam koşunun değişmeden korunan sonuçları `Logs/module_polish_full_release_validation.txt`, Unity günlüğü `Logs/module_polish_final.log` içindedir. Ardından mekanizmaların destekleri birleştirildi ve yakın duvarda modül etiketinin gövdesiyle birlikte gizlenmesi düzeltildi; iki modül grubu tekrar geçti. Bu koşu `Logs/module_polish_finish_validation.txt` ve `Logs/module_polish_finish.log` içindedir. İki son koşuda da C# derleme hatası, FAIL veya exception bulunmadı. İlk ara koşuda eski genel Renderer sınır testi kablonun genişletilmiş culling hacmini kaporta saydı; fiziksel sınır testi MeshRenderer geometrisine ayrıldı ve kablonun gerçek köşeleri ayrıca sınandı. Önceki rapor `Documentation/Backups/ModulePolish/TestResults.md` içinde korunur.

## Geçen kontroller

1. **Özgün van:** İki yan, tavan ve arka kapılarda doğrudan görsel mesh üzerinde 227 ışın örneği; dış yüzler, opak gövde/cam, farlar ve tutamaklar. Kaba araç collider'ı sorguları maskelemez.
2. **Sunum ve hareket:** Sabit oda yansımaları, FXAA, taşıma merkezi, duvara güvenli bırakma, kameraya giren ekipmanın gizlenip geri gelmesi, oyuncu çarpışma filtresi, gerçek W basma/bırakma ve seslerin kaldırılması.
3. **Ofis kapıları:** İki kapıyı iki yönden gerçek E/W girdisiyle açıp geçme; bağlı kollar, oyuncudan uzak açılma, oyuncu/eşyada güvenli durma, alan açılınca devam ve hızlı komutlardan toparlanma.
4. **Çekmeceler:** On altı tepsinin açılıp kapanması, oyuncu/ekipman engelinde durup devam etmesi, kolun etkileşim ışını ve seslerin kaldırılması.
5. **HUD ve duraklatma:** Gerçek yük/modül/bakım ve eşya durumu; Escape ile zaman/hareket/ses durması, pointer ayarları, kayıt geri okuması ve bilgisayar açıkken menü çakışmaması.
6. **Araç ve lift:** Dört özgün kapıyı gerçek E ile açıp kapama; cam/kol/kaporta bağlantıları; lift düğmeleriyle aracın ve özgün kolların yükselip inmesi; açık kapı/eksik montaj engeli, gövde ve dış modül altındaki oyuncuda güvenli bekleme ve devam; FPS/ms sayacı ve statik gruplama. Arka panel/kol geometrisinin 3.336 köşesi kendi kapı yarısında kaldı. Backface ışınları kapalıyken içeriden 120 örnek gerçek opak mesh yüzeyine çarptı. Ayrıntı: `Logs/vehicle_exterior_fix_checks.txt`.
7. **Bilgisayar:** Yedi uygulama, iş fişi ve sözleşme, pencere sürükleme/büyütme/küçültme/görev çubuğu, kapatma/kayıt/çıkış; siyah monitör ve insan ölçeği.
8. **Elle dış modül montajı:** Satın alma sonrası fiziksel teslimat; arka kapılar kapalıyken yan dış yuvada gerçek E/F ile alma/bırakma/yerleştirme; dört cıvatada basılı E ve erken bırakınca iptal; bağlantı sırası, sökme/yeniden takma, yarım montaj kayıt/yükleme. Takılı modülün bütün mesh köşeleri aracın yan dış yüzeyinin dışında kaldı. Yeteneğin yalnız tam montajda açılması ve gerçek v4 kaydının v5'e yükselmesi de doğrulandı.
9. **Modül ayrıntıları:** Beş ayrı kaynak mekanizma; kaide/desteklerin birleştirilmesiyle modül başına üç MeshRenderer; kablonun gerçek portta sonlanması ve ayrılınca park etmesi; cıvatanın işlem sırasında dönmesi, erken bırakılınca konum/durumun geri alınması; yakın duvarda modül ve etiketin birlikte gizlenip görünmesi; aynı modülde tavan sinyalinin korunması.
10. **Şirket döngüsü:** Sahne referansları, itibar kilidi, yük ve hazırlık, saha sonucu callback'i, sözleşme başarı/başarısızlığı, tek seferlik rapor ödemesi, denetim ödül/cezası, bakım, sipariş/teslimat, geliştirme ve kayıt/yükleme.

Kontroller gerçek Unity Input System ve EventSystem olaylarıyla Play Mode içinde çalışır. Kullanıcının şirket kaydı ve testte değiştirilen tercihleri korunup geri yüklenir.

## Görsel kontrol

Seçilen ikinci arayüz tasarımı, aynı iş durumundaki Unity görüntüsüyle `UI-QA/ComparisonFinal.png` içinde yan yana karşılaştırıldı; fiyat, müşteri notu ve sabit satın alma alanının önceki sorunları giderildi. Sonuç ve bilinçli uyarlamalar [design-qa.md](../design-qa.md) içindedir. 1280×720, 1600×900 ve 1920×1080 bilgisayar çıktıları alındı.

`CabOpenInteriorFixed.png`, `CargoOpenInteriorFixed.png`, `RearDoor_Right_HardwareFixed.png`, `RearDoor_Left_HardwareFixed.png` ve `ExteriorModuleMounted.png` gerçek Unity çıktılarıdır. Kabin ve boş yük bölmesinin iç yüzeyleri, tek tek açılan arka kapıların kolları ve yan dış modül görsel olarak incelendi. `ManualModuleBay.png` / `ManualModuleInstalled.png` artık dış montaj alanını gösterir. [Düzeltme notları](VehicleExteriorFix.md), [kontroller ve montaj sırası](ManualGarageSystems.md).

`ExteriorModuleDetail.png`, `ModuleFastenerWorking.png` ve `ModuleVariant_*.png` yeni servis birimlerini, desteklerini, kabloyu ve cıvata ilerlemesini gösterir. [Kaynak parçalar ve modül görüntüleri](ModuleVisuals.md). Dış modül takılıyken gerçek E ile arka kapı açma, krikoyu yükleyip geri alma ve kapatma da geçti.

## Performans ölçümünün kapsamı

`Logs/module_polish_performance.json` tam koşunun aynı kamera açısından 1600×900 statik gruplama A/B ölçümüdür; 3.420 sabit renderer gruplandı. Süreler CPU render gönderim maliyetidir; bağımsız derlemenin GPU süresi veya oyun FPS artışı değildir. Farklı koşuların süreleri birbirine karşı bir FPS karşılaştırması olarak kullanılmaz. Çizim çağrısı sayacı bu ortamda kullanılamadığı için -1 raporlanır. Render ölçeği 1,0 kalır. `garage_performance_comparison.json` her garaj doğrulamasında yenilenir. Son hedefli koşu, modül başına üç gerçek MeshRenderer ve sabit tavan sinyali kimliğini ayrıca doğruladı.

Oyundaki sayaç FPS ve ortalama kare süresini 0,5 saniyede bir, duraklatmada da gerçek zamanla günceller. Başsız test döngüsü görüntü sunmadığı için ekran görüntülerinde `FPS —` gösterilir.

## Kaynak ofisin korunması

- `Art/audit_presentation_source.py`: **2.887 kaynak dönüşümünde değişiklik yok**. Ortam FBX SHA-256: `2DA10567574E9A75FE2C0C9AF79F9AD0FC57F7D262898874BF8E10733980BD4C`. Sonuç: `Logs/presentation_source_parity.json`.
- `Art/audit_garage_refinement.py`: **989 korunan ofis renderer'ının mesh/malzeme bağlantıları aynı**. Sonuç: `Logs/garage_office_preservation.json`.
- Kaynak ofis yerleşimi, ölçeği ve modelleri değiştirilmedi. Monitörün siyah sahne malzemesi önceki kullanıcı isteğidir. Vanın kayıtlı park konumu korunur; lift yalnız çalışma sırasında hareket ettirir.

## Sınırlar

İnsanla serbest uçtan uca oynanış ve bağımsız derlemede FPS ölçümü yapılmadı. Saha sonucu callback'i sınandı; gerçek saha sahnesi, sürüş, NPC ve çok oyunculu ağ eşitlemesi uygulanmış özellik olarak değerlendirilmez.
