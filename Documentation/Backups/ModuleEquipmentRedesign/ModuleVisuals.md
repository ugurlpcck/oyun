# Dış modül görselleri ve montaj ayrıntıları

31 Ekim turunda beş modülün mekanizma modelleri baştan yenilendi. Araç modeli değiştirilmedi. [Yeni model önizlemeleri ve kapsam](ModuleEquipmentPass.md).

Beş taşınabilir servis birimi ekipman için ayrı üretilen geometri kullanır. Modüller aracın arka dış yüzeyinde, arka kapıyla birlikte hareket eden taşıyıcıdadır. Kaynak ofis, araç geometrisi/malzemeleri ve park konumu korundu. Önceki modül prefabları ve kod `Backups/ModuleEquipmentPass` içindedir.

| Modül | Yeni model | Unity görüntüsü |
| --- | --- | --- |
| Nakliye | Kapaklı koruma kasası, conta, mandal, taşıma kolu ve çevreleyen kayışlar | [Görsel](ModuleEquipmentAfter_cargo.png) |
| Çekici | Motor, dişli kutusu, tambur, 30 sarım, dört makaralı kılavuz ve kanca | [Görsel](ModuleEquipmentAfter_towing.png) |
| Yangın | Açık kollu makara, hortum, vana, gösterge ve nozul | [Görsel](ModuleEquipmentAfter_fire.png) |
| Su tahliye | Pompa gövdesi, içi açık bağlantılar, motor, depo ve boru çerçeve | [Görsel](ModuleEquipmentAfter_water.png) |
| Teknik servis | İki çekmeceli servis birimi, üst bağlantı paneli ve ölçüm kablosu | [Görsel](ModuleEquipmentAfter_tech.png) |

Güncel mekanizma ve kaide meshleri `Assets/_Project/Art/ModuleEquipment`, prefablar `Assets/_Project/Prefabs/Modules/ServiceModule_*.prefab` içindedir. Birimler alçak bir kaide ve servis bağlantı yuvası kullanır. Metal, kauçuk, boya ve dokuma ayrı malzemelerdir. Küçük isim etiketleri yalnız gerçek modül adlarını gösterir. Eski `VehicleMechanics/Portable*` varlıkları yeni modüllerin kaynağı değildir.

## Montajın görünür davranışı

- Altıgen cıvata başı E basılıyken döner ve ekseninde ilerler. İşlem bitmeden kayıtlı cıvata durumu değişmez. Tuş bırakılırsa baş önceki konumuna döner.
- Bağlantı takılıyken kablo modülün gerçek servis portunda sonlanır. Ayrılınca fiş aşağıda park eder. Kablo taşıyıcıya bağlıdır; arka kapı ve lift hareketlerinde araçla birlikte gider.
- Kameraya giren taşınabilir modülün isim etiketi gövdesiyle birlikte gizlenir; açık alanda ikisi de geri gelir.
- Aynı modül durumu tekrar yazıldığında tavan sinyali yeniden oluşturulmaz. Durum göstergeleri malzeme kopyalamak yerine MaterialPropertyBlock kullanır. Her servis birimindeki sabit kaide/destekler birleştirilir: mekanizma, şase ve port için toplam üç MeshRenderer bulunur.

Kayıt sürümü 5 ve elle montaj sırası korunur. Arka yük alanı dış modülden bağımsızdır. Gerçek E girdisiyle kapıları açma, krikoyu yükleme, aynı krikoyu geri alma ve kapatma çalıştırıldı. [Montaj adımları](ManualGarageSystems.md).

## Doğrulama

Yeni model ailesinin tam koşusu `Logs/module_equipment_full_validation.txt`, son destek/bağlantı ayrıntılarından sonraki hedefli montaj koşusu `Logs/module_equipment_finish_validation.txt` içindedir. [Doğrulama sonuçları](TestResults.md). Önceki koşular kendi raporlarında korunur.

Kontroller beş farklı gerçek mekanizma meshini, üç MeshRenderer sınırını, kablo uçlarını, cıvatanın işlem sırasında dönmesini/iptalini, yakın duvarda etiketin gizlenip geri gelmesini, fiziksel teslimat ve montajı, eski kayıt yükseltmesini, yarım montaj kaydını ve gerçek E ile arka yükleme/boşaltmayı kapsar. Montaj testinin oyuncu konumu arka raftan ve gerçek katı collider'lardan uzaktadır; modülün geniş fiziksel kabuğu cıvata etkileşim ışınını kapatmaz. Görüntüler Unity'den ayrı karelerde alınır. Saha pompa/vinç oynanışı bu görsel düzenlemenin parçası değildir.

[Araç üzerindeki görünüm](ExteriorModuleDetail.png), [cıvata üzerinde çalışma](ModuleFastenerWorking.png).
