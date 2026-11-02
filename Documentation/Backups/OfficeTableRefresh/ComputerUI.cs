using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CompanySim
{
    public sealed partial class ComputerUI : MonoBehaviour
    {
        public CompanyRuntime Runtime;
        public CompanyCatalog catalog;
        public PlayerInteractor player;
        public ModuleStation station;
        public AudioFeedback audioFeedback;
        string app="Desktop", selectedJob="", prompt="";
        float toastUntil;
        bool prepVisible;
        public bool IsOpen => computer && computer.activeSelf;
        public string ActiveApplication => app;
        void Awake()
        {
            font=Resources.Load<Font>("Fonts/Desktop-Regular");
            headingFont=Resources.Load<Font>("Fonts/Desktop-Medium");
            if(!font) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!headingFont) headingFont=font;
            if (!FindFirstObjectByType<EventSystem>())
            {
                var events=new GameObject("UI_EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            BuildCanvas();
        }
        void Start()
        {
            Runtime.Changed+=Refresh; Runtime.Notice+=Toast; Refresh();
        }
        void OnDestroy()
        {
            if(Runtime) { Runtime.Changed-=Refresh; Runtime.Notice-=Toast; }
            if(roundSprite) Destroy(roundSprite.texture);
            if(roundSprite) Destroy(roundSprite);
        }
        void Update()
        {
            if(IsOpen && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if(app=="Desktop") Close(); else Show("Desktop");
            }
            if(toastPanel.activeSelf && Time.unscaledTime>toastUntil) toastPanel.SetActive(false);
            if(IsOpen) clockText.text=DateTime.Now.ToString("dd.MM.yyyy    HH:mm");
        }
        void Click() { if(audioFeedback) audioFeedback.Click(); }
        public void Open(string target)
        {
            Click(); computer.SetActive(true); hud.SetActive(false);
            promptPanel.SetActive(false); prepPanel.SetActive(false);
            player.SetCursor(true); Show(target);
        }
        public void Close()
        {
            computer.SetActive(false); hud.SetActive(true); prepPanel.SetActive(prepVisible);
            prompt="";
            player.SetCursor(false);
        }
        public void SetPrompt(string value)
        {
            if(prompt==value) return;
            prompt=value; promptText.text=value;
            promptPanel.SetActive(!IsOpen && !string.IsNullOrWhiteSpace(value));
        }
        public void Toast(string value)
        {
            if(audioFeedback) audioFeedback.Notice();
            toastText.text=value; toastUntil=Time.unscaledTime+4.5f;
            toastPanel.SetActive(true); toastPanel.transform.SetAsLastSibling();
        }
        public void TogglePreparation()
        {
            prepVisible=!prepVisible; prepPanel.SetActive(prepVisible && !IsOpen);
        }
        public void Refresh()
        {
            if(!Runtime || !catalog || !mainContent) return;
            PrepRefresh(); if(IsOpen) Show(app);
        }
        string Money(int value) => "₺"+value.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        string PhaseName() => Runtime.Data.phase==JobPhase.Accepted ? "Hazırlık aşamasında" :
            Runtime.Data.phase==JobPhase.Deployed ? "Ekip sahada" :
            Runtime.Data.phase==JobPhase.AwaitingReport ? "Rapor bekliyor" : "Yeni işe hazır";
        void PrepRefresh()
        {
            var job=catalog.Job(Runtime.Data.activeJobId);
            if(!job) { prepText.text="OPERASYON DURUMU\n\nAktif görev yok.\nBilgisayardan İş Panosu'nu aç.\n\nTAB  •  Paneli gizle"; return; }
            var missing=Runtime.MissingPreparation();
            var loaded=Runtime.vehicle.LoadedItems.Select(x=>x.definitionId).ToArray();
            string equipment=string.Join("\n",job.requiredEquipment.Select(x=>
                (loaded.Contains(x)?"✓ ":"○ ")+(catalog.Item(x)?.displayName??x)));
            prepText.text="OPERASYON DURUMU\n\n"+job.title+"\n"+PhaseName()+
                "\n\nMODÜL\n"+(Runtime.Data.installedModuleId==job.requiredModule?"✓ ":"○ ")+
                (catalog.Module(job.requiredModule)?.displayName??job.requiredModule)+
                "\n\nEKİPMAN\n"+equipment+"\n\n"+(missing.Count==0?"Hazırlık tamam":"Eksik: "+missing.Count)+"\nTAB  •  Paneli gizle";
        }
        void Show(string target)
        {
            if(target!="Desktop" && !Apps.Any(x=>x.id==target)) target="Desktop";
            bool changed=app!=target; app=target;
            desktop.SetActive(app=="Desktop"); appWindow.SetActive(app!="Desktop");
            Clear(); RefreshDesktop();
            if(app=="Desktop") return;
            Canvas.ForceUpdateCanvases();
            var definition=Apps.First(x=>x.id==app);
            titleText.text=definition.title; subtitleText.text=definition.description;
            appIcon.kind=app; appIcon.color=definition.tint;
            appIcon.SetVerticesDirty();
            switch(app)
            {
                case "JobDesk": Jobs(); break;
                case "FleetManager": Fleet(); break;
                case "StockManager": Stock(); break;
                case "CompanyBank": Bank(); break;
                case "EquipmentShop": Shop(); break;
                case "CompanyStats": Stats(); break;
                case "BuildingManager": Building(); break;
                case "Safety": Safety(); break;
                case "MailBox": Mail(); break;
                case "Dispatch": Dispatch(); break;
                case "Reports": Reports(); break;
            }
            Canvas.ForceUpdateCanvases();
            if(changed) appScroll.verticalNormalizedPosition=1;
        }
        void Jobs()
        {
            StatStrip(("AÇIK İLAN",catalog.jobs.Count(x=>x.minimumReputation<=Runtime.Data.reputation).ToString()),
                ("ŞİRKET İTİBARI",Runtime.Data.reputation.ToString()),("EKİP DURUMU",PhaseName()));
            if(string.IsNullOrEmpty(selectedJob)) selectedJob=catalog.jobs.First().id;
            JobColumns(out var list,out var detail);
            var parent=mainContent; mainContent=list;
            foreach(var job in catalog.jobs) JobCard(job);
            mainContent=detail; JobDetail(); mainContent=parent;
        }
        void JobDetail()
        {
            var job=catalog.Job(selectedJob); if (!job) return;
            Row(job.category.ToUpperInvariant(),13,Mint,26);
            Row(job.title,27,Paper,45);
            Row(job.client+" • "+job.location+" • "+job.distanceKm+" km",17,Soft,30);
            Row(job.description,18,Paper,52);
            Row(Money(job.payment)+"  •  "+job.durationMinutes+" dk  •  Zorluk "+job.difficulty+"/5  •  Risk "+job.risk+"/5",16,Mint,34);
            Row("Gerekli modül: "+(catalog.Module(job.requiredModule)?.displayName ?? job.requiredModule),17,Paper,34);
            Row("Zorunlu: "+string.Join(", ",job.requiredEquipment.Select(x=>catalog.Item(x)?.displayName ?? x)),17,Paper,44);
            Row("Önerilen: "+string.Join(", ",job.recommendedEquipment.Select(x=>catalog.Item(x)?.displayName ?? x)),16,Soft,34);
            if (Runtime.Data.reputation<job.minimumReputation)
                Row("Kilitli: en az "+job.minimumReputation+" itibar gerekli. Mevcut itibar: "+
                    Runtime.Data.reputation,18,Red,55);
            if (!string.IsNullOrEmpty(job.specialTerms)) Row("Müşteri notu: "+job.specialTerms,18,Soft,65);
            if (job.contractBonus > 0)
                Row("İsteğe bağlı sözleşme: "+job.contractTitle+". Ekipman ve araç aşınması en fazla %"+
                    job.contractMaxEquipmentWear+" / %"+job.contractMaxVehicleWear+
                    "; ekipman kaybı olmamalı. Başarılıysa ek ₺"+job.contractBonus,18,Soft,75);
            if (Runtime.Data.phase==JobPhase.None && Runtime.Data.reputation>=job.minimumReputation)
            {
                var content=mainContent; mainContent=jobFooter;
                ActionButton("İşi kabul et",()=>Runtime.AcceptJob(job.id),Mint);
                if (job.contractBonus > 0)
                    ActionButton("Sözleşmeyle kabul et",()=>Runtime.AcceptJob(job.id,true));
                mainContent=content;
            }
            else
            {
                var content=mainContent; mainContent=jobFooter;
                Row(Runtime.Data.phase==JobPhase.None?"Bu iş için itibarını yükselt.":"Aktif işini tamamlayıp yeni bir iş alabilirsin.",17,Soft,92);
                mainContent=content;
            }
        }
        void Fleet()
        {
            StatStrip(("ARAÇ","Servis vanı"),("DURUM","%"+Mathf.RoundToInt(Runtime.Data.vehicleCondition)),
                ("YÜK",Runtime.vehicle.LoadedItems.Count()+" ekipman"));
            Row("Modüller",24,Paper,42);
            Row("Modül değiştirmek için önce yük bölümünü boşalt. Değişim garajdaki lift istasyonunda yapılır.",17,Soft,48);
            foreach (var mod in catalog.modules)
            {
                bool owned=Runtime.Data.ownedModules.Contains(mod.id);
                bool installed=Runtime.vehicle.InstalledModuleId==mod.id;
                InfoCard(mod.displayName,mod.payloadKg+" kg kapasite  •  "+mod.cargoSlots+" yük yuvası",
                    "FleetManager",installed?Mint:Blue,installed?"Takılı":owned?"Modülü tak":Money(mod.price)+" • Satın al",()=>
                {
                    if (!Runtime.Data.ownedModules.Contains(mod.id)) Runtime.BuyModule(mod.id);
                    else if (station && !station.Busy) Runtime.vehicle.TryChangeModule(mod.id,station);
                },installed || (station && station.Busy));
            }
        }
        void Stock()
        {
            StatStrip(("EKİPMAN",FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Length.ToString()),
                ("TESLİMATTA",Runtime.Data.orders.Count(x=>!x.delivered).ToString()),("YEDEK PARÇA",Runtime.Data.spareParts.ToString()));
            ActionButton("Yedek parça paketi al • "+Money(catalog.economy.sparePartsPackPrice),()=>Runtime.OrderParts(),Mint);
            foreach(var def in catalog.equipment)
            {
                int owned=FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Count(x=>x.definitionId==def.id);
                int pending=Runtime.Data.orders.Count(x=>x.itemId==def.id&&!x.delivered);
                InfoCard(def.displayName,owned+" adet depoda / sahnede  •  "+pending+" adet teslimatta","StockManager",Blue);
            }
        }
        void Bank()
        {
            StatStrip(("KULLANILABİLİR BAKİYE",Money(Runtime.Data.balance)),("TAMAMLANAN İŞ",Runtime.Data.jobsCompleted.ToString()));
            Row("Hesap hareketleri",24,Paper,48);
            if (Runtime.Data.transactions.Count == 0)
                EmptyState("Henüz işlem yok.","Tamamlanan işlerin ödemeleri ve giderler burada listelenir.","CompanyBank","İş Panosu'nu aç",()=>Show("JobDesk"));
            foreach (var entry in Runtime.Data.transactions.TakeLast(12).Reverse())
                InfoCard((entry.amount>=0?"+":"−")+Money(Mathf.Abs(entry.amount)),entry.reason+"  •  İşlem sonrası "+Money(entry.balanceAfter),"CompanyBank",entry.amount>=0?Mint:Peach);
        }
        void Shop()
        {
            Row("Siparişler garajdaki teslimat noktasına gelir.",14,Soft,28);
            var grid=TileGrid("EquipmentCatalog",2,120);
            foreach(var def in catalog.equipment)
                ProductCard(grid,def.displayName,(def.category=="Field equipment"?"Saha ekipmanı":def.category)+"  •  "+def.weightKg+" kg",Money(def.price),
                    ()=>Runtime.OrderEquipment(def.id),Runtime.Data.balance>=def.price);
        }
        void Stats()
        {
            StatStrip(("TAMAMLANAN İŞ",Runtime.Data.jobsCompleted.ToString()),("İTİBAR",Runtime.Data.reputation.ToString()),("BAKİYE",Money(Runtime.Data.balance)));
            InfoCard("Ekipman koleksiyonu",FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Length+" fiziksel ekipman","StockManager",Blue);
            InfoCard("Araç uzmanlıkları",Runtime.Data.ownedModules.Count+" modül sahibi","FleetManager",Mint);
            InfoCard("Şirket yatırımları",Runtime.Data.upgrades.Count+" geliştirme kuruldu","BuildingManager",Peach);
            InfoCard("Güvenlik geçmişi",Runtime.Data.inspectionsPassed+" başarılı denetim","Safety",Mint);
        }
        void Building()
        {
            foreach (var upgrade in new[]{"storage","lighting","operations"})
            {
                string name=upgrade=="storage"?"Ek depo rafı":upgrade=="lighting"?"Yeni atölye ışığı":"Operasyon panosu";
                int price=catalog.economy.UpgradePrice(upgrade);
                bool owned=Runtime.Data.upgrades.Contains(upgrade);
                string description=upgrade=="storage"?"Daha fazla ekipman için depolama alanını genişlet.":
                    upgrade=="lighting"?"Çalışma alanının aydınlatmasını güçlendir.":"Operasyonlarını iyileştir ve iş bonusu kazan.";
                InfoCard(name,description,"BuildingManager",Peach,owned?"Kurulu":"Geliştir • "+Money(price),()=>Runtime.BuyUpgrade(upgrade),owned);
            }
        }
        void Mail()
        {
            Row("Gelen kutusu",18,Paper,32);
            InfoCard("İş ilanları","Güncel müşteri talepleri.","JobDesk",Mint,"İlanları aç",()=>Show("JobDesk"));
            if (Runtime.Data.inspectionPending)
                InfoCard("Güvenlik denetimi bekliyor","Aracı ve ekipmanlarını kontrol ederek denetime hazırlan.","Safety",Peach,"Denetimi aç",()=>Show("Safety"));
            foreach(var order in Runtime.Data.orders.TakeLast(5))
                InfoCard(catalog.Item(order.itemId)?.displayName??order.itemId,order.delivered?"Teslim edildi • Garajdaki teslimat alanından alabilirsin.":"Sipariş hazırlanıyor • Teslimat yolda.","MailBox",order.delivered?Mint:Blue);
        }
        void Safety()
        {
            Row("GÜVENLİK DENETİMİ",27,Ochre,52);
            Row("Araç ve fiziksel ekipmanların durumu en az %"+
                catalog.economy.inspectionMinimumCondition+" olmalı.",19,Paper,58);
            if (!Runtime.Data.inspectionPending)
            {
                Row("Denetim beklenmiyor. Sonuç: "+
                    (string.IsNullOrEmpty(Runtime.Data.lastInspectionSummary)?"Henüz denetim yok":
                        Runtime.Data.lastInspectionSummary),18,Soft,70);
                Row("Sonraki denetim en erken "+Runtime.Data.nextInspectionAtCompletedJobs+
                    " tamamlanan işte.",18,Soft,50);
                return;
            }
            var problems=Runtime.InspectionProblems();
            Row(problems.Count==0?"Kontrol listesi temiz.":
                "Eksikler: "+string.Join(", ", problems),18,
                problems.Count==0?Ochre:Red,Mathf.Max(58,problems.Count*35));
            Row("Denetimi şimdi tamamlayabilirsiniz. Eksikler varsa küçük ceza ve itibar kaybı uygulanır.",18,Soft,65);
            if (Runtime.Data.phase==JobPhase.None)
                ActionButton("DENETİMİ TAMAMLA",()=>Runtime.ResolveInspection(),Ochre);
            else Row("Denetim için önce aktif işi bitirin.",18,Red,48);
        }
        void Dispatch()
        {
            Row("ÇIKIŞ KONTROLÜ",27,Ochre,52);
            var job=catalog.Job(Runtime.Data.activeJobId);
            if (!job) { EmptyState("Aktif iş yok.","İş kabul edildiğinde araç ve ekipman hazırlığı burada gösterilir.","Dispatch","İş seç",()=>Show("JobDesk")); return; }
            Row(job.title+" • "+PhaseName(),22,Paper,54);
            if (Runtime.Data.contractAccepted)
                Row("Sözleşme: "+job.contractTitle+" • Ek ₺"+job.contractBonus,18,Soft,48);
            var missing=Runtime.MissingPreparation();
            if (Runtime.Data.phase==JobPhase.Accepted)
            {
                Row(missing.Count==0?"Araç hazır. Fiziksel yük ve modül doğrulandı.":"Eksik: "+string.Join(", ",missing),18,missing.Count==0?Ochre:Red,75);
                ActionButton("Göreve çık",()=>Runtime.Depart(),Mint).interactable=missing.Count==0;
            }
            else if (Runtime.Data.phase==JobPhase.Deployed)
                Row("Ekip görevde. Saha sonucu geldiğinde raporun burada hazırlanacak. Saha oynanışı henüz bağlı değil.",18,Soft,80);
            else Row("Ekip döndü. Raporlar uygulamasında ödeme raporu hazırlayın.",18,Soft,70);
        }
        void Reports()
        {
            Row("GÖREV RAPORLARI",27,Ochre,52);
            if (Runtime.Data.phase==JobPhase.AwaitingReport)
            {
                var job=catalog.Job(Runtime.Data.activeJobId);
                Row(job.title+" • "+Runtime.Data.fieldSummary+" • Tahsil edilecek ₺"+
                    Runtime.Data.pendingPayment,21,Paper,60);
                Row("Müşteri: "+job.client+"  •  Kullanılan: "+Runtime.Data.reportEquipment,18,Soft,60);
                Row("Temel ₺"+Runtime.Data.reportBasePayment+"  +  Saha bonusu ₺"+
                    Runtime.Data.reportFieldBonus+"  +  Sözleşme ₺"+Runtime.Data.reportContractBonus+
                    "  −  Masraf ₺"+Runtime.Data.reportExpenses,18,Paper,60);
                Row("Ekipman aşınması %"+Runtime.Data.reportEquipmentWear+"  •  Araç aşınması %"+
                    Runtime.Data.reportVehicleWear,18,Soft,50);
                if (Runtime.Data.contractAccepted)
                    Row("Sözleşme: "+job.contractTitle+" • "+
                        (Runtime.Data.contractSatisfied?"Koşul sağlandı":"Koşul sağlanmadı"),18,
                        Runtime.Data.contractSatisfied?Ochre:Red,50);
                ActionButton("RAPORU ONAYLA VE TAHSİL ET",()=>Runtime.FileReport(),Ochre);
            }
            else EmptyState("Bekleyen rapor yok.","Tamamlanan görevin ödeme ve masraf dökümü burada gösterilir.","Reports");
        }
    }
}
