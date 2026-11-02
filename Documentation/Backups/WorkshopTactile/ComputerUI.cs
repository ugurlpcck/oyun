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
            if(carriedItem) carriedText.text=carriedItem.DisplayName+"\nDurum %"+Mathf.RoundToInt(carriedItem.durability);
        }
        public void SetFocus(bool focused)
        {
            if(!focusReticle) return;
            focusReticle.color=focused?new Color(1,.86f,.55f,.95f):new Color(.9f,.96f,.94f,.65f);
            focusReticle.rectTransform.sizeDelta=Vector2.one*(focused?10:6);
        }
        public void SetCarriedItem(CarryableItem item)
        {
            carriedItem=item;
            if(carriedPanel) carriedPanel.SetActive(item);
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
            Row("Modül değişimi için araç boş olmalıdır.",14,Soft,26);
            var table=Table("Modules",new[]{"Modül","Kapasite","Yuva","Durum","Fiyat","İşlem"},
                new[]{.32f,.12f,.08f,.14f,.14f,.20f},new[]{1,2,4});
            foreach (var mod in catalog.modules)
            {
                bool owned=Runtime.Data.ownedModules.Contains(mod.id);
                bool installed=Runtime.vehicle.InstalledModuleId==mod.id;
                table.Add("Module_"+mod.id,new[]{mod.displayName,mod.payloadKg+" kg",mod.cargoSlots.ToString(),
                    installed?"Takılı":owned?"Satın alındı":"Satışta",owned?"-":Money(mod.price),""},
                    installed?"Takılı":owned?"Modülü tak":"Satın al",()=>
                {
                    if (!Runtime.Data.ownedModules.Contains(mod.id)) Runtime.BuyModule(mod.id);
                    else if (station && !station.Busy) Runtime.vehicle.TryChangeModule(mod.id,station);
                },!installed && (!station || !station.Busy) && (owned || Runtime.Data.balance>=mod.price));
            }
        }
        void Stock()
        {
            var items=FindObjectsByType<CarryableItem>(FindObjectsSortMode.None);
            StatStrip(("EKİPMAN",items.Length.ToString()),
                ("TESLİMATTA",Runtime.Data.orders.Count(x=>!x.delivered).ToString()),("YEDEK PARÇA",Runtime.Data.spareParts.ToString()));
            ToolbarButton("OrderParts","Yedek parça al  ("+Money(catalog.economy.sparePartsPackPrice)+")",()=>Runtime.OrderParts(),
                Runtime.Data.balance>=catalog.economy.sparePartsPackPrice);
            var table=Table("Stock",new[]{"Ekipman","Toplam","Araçta","Teslimatta","En düşük durum"},
                new[]{.40f,.12f,.12f,.14f,.22f},new[]{1,2,3,4});
            foreach(var def in catalog.equipment)
            {
                var stock=items.Where(x=>x.definitionId==def.id).ToArray();
                int pending=Runtime.Data.orders.Count(x=>x.itemId==def.id&&!x.delivered);
                table.Add("Stock_"+def.id,new[]{def.displayName,stock.Length.ToString(),stock.Count(x=>x.Loaded).ToString(),
                    pending.ToString(),stock.Length==0?"-":"%"+Mathf.RoundToInt(stock.Min(x=>x.durability))});
            }
        }
        void Bank()
        {
            StatStrip(("BAKİYE",Money(Runtime.Data.balance)),("KAYITLI İŞLEM",Runtime.Data.transactions.Count.ToString()));
            var table=Table("Transactions",new[]{"Tarih","Açıklama","Tutar","Bakiye"},
                new[]{.18f,.50f,.16f,.16f},new[]{2,3});
            foreach (var entry in Runtime.Data.transactions.AsEnumerable().Reverse())
                table.Add("Transaction_"+entry.id,new[]{DateTimeOffset.FromUnixTimeSeconds(entry.createdUnix).ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    entry.reason,(entry.amount>=0?"+":"−")+Money(Mathf.Abs(entry.amount)),Money(entry.balanceAfter)});
            if(Runtime.Data.transactions.Count==0) Row("Henüz hesap hareketi yok.",14,Soft,30);
        }
        void Shop()
        {
            var items=FindObjectsByType<CarryableItem>(FindObjectsSortMode.None);
            int capacity=Runtime.Data.upgrades.Contains("storage")?24:12;
            int occupied=items.Length+Runtime.Data.orders.Count(x=>!x.delivered);
            StatStrip(("BAKİYE",Money(Runtime.Data.balance)),("DEPO KULLANIMI",occupied+" / "+capacity));
            Row("Siparişler garajdaki teslimat noktasına gelir.",14,Soft,28);
            var table=Table("EquipmentCatalog",new[]{"Ekipman","Ağırlık","Stok","Yolda","Birim fiyat","Sipariş"},
                new[]{.35f,.11f,.08f,.08f,.16f,.22f},new[]{1,2,3,4});
            foreach(var def in catalog.equipment)
            {
                int owned=items.Count(x=>x.definitionId==def.id);
                int pending=Runtime.Data.orders.Count(x=>x.itemId==def.id&&!x.delivered);
                var button=table.Add("Product_"+def.id,new[]{def.displayName,def.weightKg+" kg",owned.ToString(),pending.ToString(),Money(def.price),""},
                    "Sipariş ver",()=>Runtime.OrderEquipment(def.id),Runtime.Data.balance>=def.price && occupied<capacity);
                button.gameObject.name="Buy_"+def.displayName;
            }
            if(occupied>=capacity) Row("Depo kapasitesi dolu. Yeni sipariş için depoyu genişletin.",14,Red,30);
        }
        void Stats()
        {
            Property("Bakiye",Money(Runtime.Data.balance));
            Property("Tamamlanan iş",Runtime.Data.jobsCompleted.ToString());
            Property("İtibar",Runtime.Data.reputation.ToString());
            Property("Ekipman",FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Length.ToString());
            Property("Satın alınan modül",Runtime.Data.ownedModules.Count.ToString());
            Property("Bina geliştirmesi",Runtime.Data.upgrades.Count.ToString());
            Property("Başarılı denetim",Runtime.Data.inspectionsPassed.ToString());
        }
        void Building()
        {
            var table=Table("Upgrades",new[]{"Geliştirme","Açıklama","Fiyat","İşlem"},
                new[]{.23f,.43f,.14f,.20f},new[]{2},46);
            foreach (var upgrade in new[]{"storage","lighting","operations"})
            {
                string name=upgrade=="storage"?"Ek depo rafı":upgrade=="lighting"?"Yeni atölye ışığı":"Operasyon panosu";
                int price=catalog.economy.UpgradePrice(upgrade);
                bool owned=Runtime.Data.upgrades.Contains(upgrade);
                string description=upgrade=="storage"?"Ekipman kapasitesi: 12 → 24":
                    upgrade=="lighting"?"Ek atölye aydınlatması":"Teslimat süresi ve operasyon bonusu";
                table.Add("Upgrade_"+upgrade,new[]{name,description,owned?"-":Money(price),""},
                    owned?"Kurulu":"Satın al",()=>Runtime.BuyUpgrade(upgrade),!owned && Runtime.Data.balance>=price);
            }
        }
        void Mail()
        {
            var table=Table("Inbox",new[]{"Konu","Durum","İşlem"},new[]{.58f,.22f,.20f});
            table.Add("Mail_jobs",new[]{"Güncel iş ilanları","",""},"Aç",()=>Show("JobDesk"));
            if (Runtime.Data.inspectionPending)
                table.Add("Mail_inspection",new[]{"Güvenlik denetimi","Bekliyor",""},"Aç",()=>Show("Safety"));
            foreach(var order in Runtime.Data.orders.AsEnumerable().Reverse())
                table.Add("Mail_order_"+order.uid,new[]{catalog.Item(order.itemId)?.displayName??order.itemId,
                    order.delivered?"Teslim edildi":"Yolda",""},"Depoyu aç",()=>Show("StockManager"));
        }
        void Safety()
        {
            float required=catalog.economy.inspectionMinimumCondition;
            StatStrip(("GEREKEN DURUM","%"+required),("DENETİM",Runtime.Data.inspectionPending?"Bekliyor":"Beklenmiyor"));
            var table=Table("Inspection",new[]{"Araç / ekipman","Durum","Kontrol"},new[]{.55f,.20f,.25f},new[]{1});
            table.Add("Inspection_vehicle",new[]{"Servis vanı","%"+Mathf.RoundToInt(Runtime.Data.vehicleCondition),
                Runtime.Data.vehicleCondition>=required?"Uygun":"Bakım gerekli"});
            foreach(var item in FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).OrderBy(x=>x.DisplayName))
                table.Add("Inspection_"+item.uid,new[]{item.DisplayName,"%"+Mathf.RoundToInt(item.durability),
                    item.durability>=required?"Uygun":"Bakım gerekli"});
            if (!Runtime.Data.inspectionPending)
            {
                if(!string.IsNullOrEmpty(Runtime.Data.lastInspectionSummary)) Row(Runtime.Data.lastInspectionSummary,14,Soft,32);
                Row("Sonraki denetim: en erken "+Runtime.Data.nextInspectionAtCompletedJobs+" tamamlanan işte.",14,Soft,30);
                return;
            }
            var problems=Runtime.InspectionProblems();
            Row(problems.Count==0?"Araç ve ekipman denetime hazır.":"Eksikler tamamlanmadan onay verilirse ceza ve itibar kaybı uygulanır.",14,
                problems.Count==0?Soft:Red,32);
            ToolbarButton("CompleteInspection","Denetimi tamamla",()=>Runtime.ResolveInspection(),Runtime.Data.phase==JobPhase.None);
            if(Runtime.Data.phase!=JobPhase.None) Row("Denetim için aktif işin bitmesi gerekiyor.",14,Soft,28);
        }
        void Dispatch()
        {
            var job=catalog.Job(Runtime.Data.activeJobId);
            if (!job)
            {
                Row("Aktif iş yok.",14,Soft,30);
                ToolbarButton("DispatchJobs","İş Panosu'nu aç",()=>Show("JobDesk")); return;
            }
            Property("Görev",job.title);
            Property("Durum",PhaseName());
            if (Runtime.Data.contractAccepted)
                Property("Sözleşme",job.contractTitle+" (ek "+Money(job.contractBonus)+")");
            var missing=Runtime.MissingPreparation();
            if (Runtime.Data.phase==JobPhase.Accepted)
            {
                var moduleTable=Table("RequiredModule",new[]{"Gerekli modül","Takılı modül","Kontrol"},new[]{.40f,.40f,.20f});
                moduleTable.Add("ModuleCheck",new[]{catalog.Module(job.requiredModule)?.displayName??job.requiredModule,
                    catalog.Module(Runtime.Data.installedModuleId)?.displayName??Runtime.Data.installedModuleId,
                    Runtime.Data.installedModuleId==job.requiredModule?"Tamam":"Değiştirilmeli"});
                var equipment=Table("RequiredEquipment",new[]{"Ekipman","Gerekli","Yüklü","Kontrol"},
                    new[]{.45f,.15f,.15f,.25f},new[]{1,2});
                foreach(var group in job.requiredEquipment.GroupBy(x=>x))
                {
                    var loaded=Runtime.vehicle.LoadedItems.Where(x=>x.definitionId==group.Key).ToArray();
                    equipment.Add("Required_"+group.Key,new[]{catalog.Item(group.Key)?.displayName??group.Key,
                        group.Count().ToString(),loaded.Length.ToString(),loaded.Length<group.Count()?"Eksik":
                        loaded.Any(x=>x.durability<20)?"Bakım gerekli":"Tamam"});
                }
                Property("Araç durumu","%"+Mathf.RoundToInt(Runtime.Data.vehicleCondition));
                Row(missing.Count==0?"Çıkış için hazırlık tamam.":"Eksik: "+string.Join(", ",missing),14,
                    missing.Count==0?Soft:Red,42);
                ToolbarButton("DepartJob","Göreve çık",()=>Runtime.Depart(),missing.Count==0);
            }
            else if (Runtime.Data.phase==JobPhase.Deployed)
                Row("Saha sonucu bekleniyor. Saha oynanışı henüz bağlı değil.",14,Soft,36);
            else ToolbarButton("DispatchReports","Raporu aç",()=>Show("Reports"));
        }
        void Reports()
        {
            if (Runtime.Data.phase==JobPhase.AwaitingReport)
            {
                var job=catalog.Job(Runtime.Data.activeJobId);
                Property("Görev",job.title);
                Property("Müşteri",job.client);
                Property("Sonuç",Runtime.Data.fieldSummary);
                var table=Table("ReportAmounts",new[]{"Kalem","Tutar"},new[]{.75f,.25f},new[]{1},30);
                table.Add("BasePayment",new[]{"İş bedeli",Money(Runtime.Data.reportBasePayment)});
                table.Add("FieldBonus",new[]{"Saha bonusu",Money(Runtime.Data.reportFieldBonus)});
                table.Add("ContractBonus",new[]{"Sözleşme bonusu",Money(Runtime.Data.reportContractBonus)});
                table.Add("Expenses",new[]{"Masraflar","−"+Money(Runtime.Data.reportExpenses)});
                table.Add("TotalPayment",new[]{"Tahsil edilecek toplam",Money(Runtime.Data.pendingPayment)});
                Property("Kullanılan ekipman",Runtime.Data.reportEquipment);
                Property("Ekipman aşınması","%"+Runtime.Data.reportEquipmentWear);
                Property("Araç aşınması","%"+Runtime.Data.reportVehicleWear);
                if (Runtime.Data.contractAccepted)
                    Property("Sözleşme",job.contractTitle+" / "+(Runtime.Data.contractSatisfied?"Koşul sağlandı":"Koşul sağlanmadı"));
                ToolbarButton("ApproveReport","Raporu onayla ve tahsil et",()=>Runtime.FileReport());
            }
            else Row("Bekleyen rapor yok.",14,Soft,30);
        }
    }
}
