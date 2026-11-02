# Ofis ve garaj doğrulaması

29 Ekim 2026, Unity 6000.5.5f1. Arka modül taşıyıcısı, 180 derece kapılar, gerçek yürüyüşle yük bölmesine erişim, yeni model/yüzeyler ve okunurluk düzenlemesinin son bütünleşik Play Mode koşusu **10 PASS** grubu ve **Validation complete** ile tamamlandı. Bu son raporda FAIL yoktur.

Değişmeden korunan sonuç: `Logs/rear_workshop_full_validation.txt`. Güncel çalışma raporu: `Logs/office_garage_validation.txt`. Hedefli montaj: `Logs/rear_module_finish_validation.txt`. `Logs/Editor.log` geliştirme sürecindeki önceki başarısız ara koşuları da içerir; bütünü hatasız bir son koşu günlüğü olarak sunulmaz. Önceki sürüm raporu `Backups/RearWorkshopRefresh/TestResults.md` içinde korunur.

## Geçen kontroller

1. **Özgün van:** İki yan, tavan ve arka kapılarda doğrudan görsel mesh üzerinde 227 dış yüzey ışın örneği; opak gövde/cam, özgün farlar ve tutamaklar. Park konumu korunur.
2. **Hareket ve taşıma:** Gerçek W basma/bırakma, duvara güvenli bırakma, merkezlenmiş taşıma, kameraya giren modelin gizlenmesi, oyuncu çarpışma filtresi, FXAA ve sabit oda yansımaları.
3. **Ofis kapıları:** Gerçek E/W ile iki yönden açıp geçme; kolların bağlı kalması, oyuncudan uzağa açılma, engelde durma/devam ve hızlı komutlardan toparlanma.
4. **Çekmeceler:** On altı tepsiyi açıp kapama; oyuncu/eşyada güvenli bekleme, engel kalkınca devam ve kol etkileşim ışını.
5. **HUD ve duraklatma:** Gerçek modül/yük/bakım ve eşya durumu; Escape ile zaman/hareket durması, ayarlar, kayıt geri okuması ve bilgisayar açıkken menü çakışmaması.
6. **Arka erişim ve lift:** Gerçek E ile dört araç kapısı; arka kanatların 180 dereceye ulaşması ve modülün kapıyla gitmesi. Gerçek W ile sağ yandan arkaya, basamaklardan yük tabanına, dışarıya ve sol yana yürüyüş. Karakterin ayak yüksekliği içeride 0,66 m tabana ulaştı. Lift gövde, arka taşıyıcı ve basamak altında oyuncuda bekleyip güvenle devam etti. Seslerin kaldırılması, FPS/ms sayacı ve statik gruplama doğrulandı.
7. **Bilgisayar:** Yedi uygulama, üst bölüm sekmeleri, gerçek iş/sözleşme seçimi, pencere sürükleme/büyütme/küçültme/görev çubuğu, kapatma/kayıt/çıkış; siyah monitör ve 1,72 m karakter ölçeği.
8. **Modül ayrıntıları:** Beş farklı mekanizma meshi; modül başına üç MeshRenderer; cıvatanın basılı E sırasında dönmesi ve erken bırakınca görsel/durum iptali; gerçek porta giden/ayrılınca park eden kablo; duvar yakınında model/etiketin birlikte gizlenmesi ve sabit tavan sinyali kimliği.
9. **Elle arka modül montajı:** Fiziksel satın alma/teslimat; kapı kapalıyken arka dış yuvada gerçek E/F ile alma/bırakma/yerleştirme; dört cıvata ve bağlantı sırası, sökme/takma ve yarım montaj kaydı. Takılı modülün bütün köşeleri arka gövdenin dışında kaldı. Gerçek v4 → v5 yükseltmesi, eski basamak alanındaki ekipmanın kimlik/durum kaybı olmadan taşınması ve E ile arka yükleme/boşaltma geçti. Test, katı collider içine yerleştirilmiş oyuncuya dayanmaz; eşya alma gerçek görünür hedefe açık bir yönden yapılır.
10. **Şirket döngüsü:** Hazırlık, saha sonucu callback'i, sözleşme başarı/başarısızlığı, raporu gerçek pointer ile onaylama ve tek seferlik ödeme, denetim ödül/cezası, araç/ekipman bakımı, sipariş/teslimat, geliştirme ve kayıt/yükleme. Rapor toplamı/tahsilat düğmesi sabit alana alındıktan sonra ödeme tekrar geçti.

Testler Unity Input System ve EventSystem olaylarını kullanır. Şirket kaydı test başında yedeklenir ve sonunda geri yüklenir. Test tercihlerinin yanında arka planda çalışma seçeneği de geri yüklenir; açık editör kapatılmaz.

## Görsel ve geometri kontrolü

`ComputerJobs720Preview.png`, `ComputerJobsPreview.png`, `ComputerJobs1080Preview.png` ve `ComputerShopPreview.png` üst sekmeleri, genişleyen iş alanını ve büyük yazıları gösterir. İş fiyatı/sözleşme/kabul düğmesi 720p ve 900p görüntülerde incelendi. `ComputerReport720Preview.png` sabit toplam/tahsilat alanını gösterir. [Görsel değerlendirme](../design-qa.md).

`RearDoorWalkwayClear.png`, `ExteriorModuleMounted.png`, `ExteriorModuleDetail.png` ve beş `ModuleVariant_*.png` Unity'den alınıp incelendi. [Arka erişim ve yüzey notları](RearWorkshopRefresh.md), [modül kaynakları](ModuleVisuals.md).

`Logs/vehicle_exterior_fix_checks.txt`: arka panel/kol geometrisinin **3.336 köşesi** kendi kapı yarısında; backface ışınları kapalıyken **120 içeriden görünürlük örneği** gerçek opak mesh yüzeyine çarptı.

`Logs/workshop_visual_geometry.txt`: **64 van yüzey meshi**, dört lastik normal grubu, kaynak köşelerinde en büyük yer değiştirme **0**. Yeni zemin izi bir collider'sız renderer'dır; yeni ışık eklenmedi. Modül başına üç MeshRenderer, üç çizim çağrısı garantisi değildir.

## Kaynak ofisin korunması

- `Art/audit_presentation_source.py`: **2.887 kaynak dönüşümünde değişiklik yok**. Ortam FBX SHA-256: `2DA10567574E9A75FE2C0C9AF79F9AD0FC57F7D262898874BF8E10733980BD4C`. Rapor: `Logs/presentation_source_parity.json`.
- `Art/audit_garage_refinement.py`: **989 korunan ofis renderer'ının mesh/malzeme bağlantıları aynı**. Rapor: `Logs/garage_office_preservation.json`.
- Kaynak ofisin yerleşimi, ölçeği ve modelleri değiştirilmedi. Monitörün siyah sahne malzemesi önceki kullanıcı isteğidir.

## Performans ve sınırlar

`Logs/rear_workshop_performance.json`, aynı açıdan 1600×900 açık render isteğiyle statik gruplama A/B ölçümüdür; **3.420** sabit renderer gruplandı. Son koşuda CPU render gönderim medyanı 6,77 ms → 5,98 ms ölçüldü. Bunlar GPU sunum süresi veya bağımsız oyuncu FPS artışı değildir. Çizim çağrısı sayacı kullanılamadığından -1 raporlanır. Render ölçeği 1,0 kalır. FPS/ms sayacı duraklatmada da gerçek zamanla güncellenir; ekran görüntüsü yakalama yükü performans kıyası sayılmaz.

İnsanla serbest uçtan uca oynanış ve bağımsız derlemede FPS ölçümü yapılmadı. Saha sonucu callback'i sınandı; gerçek saha sahnesi, sürüş, NPC ve çok oyunculu ağ eşitlemesi bu çalışmanın tamamlanmış özellikleri değildir.
