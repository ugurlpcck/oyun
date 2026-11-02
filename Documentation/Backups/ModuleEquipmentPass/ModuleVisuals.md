# Dış modül görselleri ve montaj ayrıntıları

Son ayrıntı turunda yangın göstergesine kadran/ibre, pompaya hortum bağlantı ağzı ve destek ayakları eklendi; nakliye kayışları geniş kapak yüzeyine oturtuldu. Çok malzemeli ayrıntılar submesh'leri korunarak birleştirilir. [Güncel görüntüler ve testler](VehicleDetailPass.md).

29 Ekim 2026. Beş taşınabilir servis birimi, mevcut kaynak mekanizmalar ve yeni üretilen bağlantı ayrıntılarıyla yenilendi. Modüller artık aracın arka dış yüzeyinde, arka kapıyla birlikte hareket eden taşıyıcıdadır. Kaynak ofis, özgün araç silueti ve kaynak FBX dosyaları korundu. Önceki sahne ve kod `Backups/RearWorkshopRefresh` ve `Backups/WorkshopVisualFinish` içindedir.

| Modül | Kullanılan mevcut parçalar | Unity görüntüsü |
| --- | --- | --- |
| Nakliye | Equipment_Case gövdesi, kapak ve tutamak; yeni sabitleme kayışları ve tokalar | [Görsel](ModuleVariant_cargo.png) |
| Çekici | Module_Towing tamburu; yeni 18 sarımlı çelik halat, yataklar ve mil uçları | [Görsel](ModuleVariant_towing.png) |
| Yangın | Module_Fire makara, göbek ve göstergeleri; yeni kırmızı makara kenarı, sarılı hortum ve gösterge destekleri | [Görsel](ModuleVariant_fire.png) |
| Su tahliye | Module_WaterPump motoru, kapak ve emiş borusu; yeni soğutma kanatları ve boru tutamak | [Görsel](ModuleVariant_water.png) |
| Teknik servis | Module_TechService gövdesi; üç yeni yatay çekmece ve metal çekme kolları | [Görsel](ModuleVariant_tech.png) |

Türetilmiş mekanizma ve şase meshleri `Assets/_Project/Art/VehicleMechanics/Portable*`, prefablar `Assets/_Project/Prefabs/Modules/ServiceModule_*.prefab` içindedir. Birimler pahlı kenarlar, ortak kaide, arka panel ve servis bağlantı gövdesi kullanır. Malzemelerin metal/kauçuk/boyalı yüzey farkı korundu. İsim etiketleri yalnız gerçek modül adlarını gösterir. Yeni ayrıntılar ortak şase renderer'ında birleştirilir.

## Montajın görünür davranışı

- Altıgen cıvata başı E basılıyken döner ve ekseninde ilerler. İşlem bitmeden kayıtlı cıvata durumu değişmez. Tuş bırakılırsa baş önceki konumuna döner.
- Bağlantı takılıyken kablo modülün gerçek servis portunda sonlanır. Ayrılınca fiş aşağıda park eder. Kablo taşıyıcıya bağlıdır; arka kapı ve lift hareketlerinde araçla birlikte gider.
- Kameraya giren taşınabilir modülün isim etiketi gövdesiyle birlikte gizlenir; açık alanda ikisi de geri gelir.
- Aynı modül durumu tekrar yazıldığında tavan sinyali yeniden oluşturulmaz. Durum göstergeleri malzeme kopyalamak yerine MaterialPropertyBlock kullanır. Her servis birimindeki sabit kaide/destekler birleştirilir: mekanizma, şase ve port için toplam üç MeshRenderer bulunur.

Kayıt sürümü 5 ve elle montaj sırası korunur. Arka yük alanı dış modülden bağımsızdır. Gerçek E girdisiyle kapıları açma, krikoyu yükleme, aynı krikoyu geri alma ve kapatma çalıştırıldı. [Montaj adımları](ManualGarageSystems.md).

## Doğrulama

Güncel hedefli montaj koşusu `Logs/rear_module_finish_validation.txt` içindedir. Tam oyun döngüsünün son sonucu `Logs/rear_workshop_full_validation.txt` ve [TestResults.md](TestResults.md) içindedir. Önceki 28 Ekim sonuçları `Logs/module_polish_full_release_validation.txt` dosyasında korunur.

Kontroller beş farklı gerçek mekanizma meshini, üç MeshRenderer sınırını, kablo uçlarını, cıvatanın işlem sırasında dönmesini/iptalini, yakın duvarda etiketin gizlenip geri gelmesini, fiziksel teslimat ve montajı, eski kayıt yükseltmesini, yarım montaj kaydını ve gerçek E ile arka yükleme/boşaltmayı kapsar. Montaj testinin oyuncu konumu arka raftan ve gerçek katı collider'lardan uzaktadır; modülün geniş fiziksel kabuğu cıvata etkileşim ışınını kapatmaz. Görüntüler Unity'den ayrı karelerde alınır. Saha pompa/vinç oynanışı bu görsel düzenlemenin parçası değildir.

[Araç üzerindeki görünüm](ExteriorModuleDetail.png), [cıvata üzerinde çalışma](ModuleFastenerWorking.png).
