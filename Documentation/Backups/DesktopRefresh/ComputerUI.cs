using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CompanySim
{
    public sealed class ComputerUI : MonoBehaviour
    {
        public CompanyRuntime Runtime;
        public CompanyCatalog catalog;
        public PlayerInteractor player;
        public ModuleStation station;
        public AudioFeedback audioFeedback;
        Canvas canvas;
        GameObject computer, prepPanel, toastPanel, promptPanel;
        RectTransform mainContent;
        Text promptText, toastText, prepText, titleText;
        Font font;
        string app = "Desktop", selectedJob = "", prompt = "";
        float toastUntil;
        bool prepVisible = false;
        static readonly Color Navy = new Color(.13f,.19f,.22f), Slate = new Color(.21f,.29f,.32f);
        static readonly Color Paper = new Color(.92f,.90f,.83f), Ochre = new Color(.82f,.59f,.24f);
        static readonly Color Soft = new Color(.75f,.81f,.79f), Red = new Color(.72f,.29f,.24f);

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!FindFirstObjectByType<EventSystem>())
            {
                var events = new GameObject("UI_EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            BuildCanvas();
        }
        void Start()
        {
            Runtime.Changed += Refresh;
            Runtime.Notice += Toast;
            Refresh();
        }
        void OnDestroy()
        {
            if (Runtime) { Runtime.Changed -= Refresh; Runtime.Notice -= Toast; }
        }
        void Update()
        {
            if (computer.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            if (toastPanel && toastPanel.activeSelf && Time.unscaledTime > toastUntil)
                toastPanel.SetActive(false);
        }

        GameObject Obj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
        RectTransform Rect(GameObject go, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }
        GameObject Block(string name, Transform parent, Color color)
        {
            var go = Obj(name, parent); go.AddComponent<Image>().color = color; return go;
        }
        Text Label(string name, Transform parent, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = Obj(name, parent);
            var t = go.AddComponent<Text>(); t.font=font; t.text=value; t.fontSize=size; t.color=color;
            t.alignment=align; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow;
            return t;
        }
        void Layout(Transform t, float spacing=10)
        {
            var l=t.gameObject.AddComponent<VerticalLayoutGroup>();
            l.padding=new RectOffset(18,18,14,14); l.spacing=spacing;
            l.childControlHeight=true; l.childControlWidth=true;
            l.childForceExpandHeight=false; l.childForceExpandWidth=true;
            var f=t.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        }
        void Height(GameObject go, float h)
        {
            var e=go.AddComponent<LayoutElement>(); e.preferredHeight=h;
        }
        void Row(string value, int size=20, Color? color=null, float height=48)
        {
            var t=Label("Row",mainContent,value,size,color ?? Paper);
            Height(t.gameObject,height);
        }
        void ActionButton(string caption, Action click, Color? color=null)
        {
            var go=Block("Action_"+caption, mainContent, color ?? Slate);
            Height(go,48);
            var button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>();
            var colors=button.colors; colors.highlightedColor=Ochre; colors.pressedColor=Soft;
            button.colors=colors;
            button.onClick.AddListener(()=>{ if(audioFeedback) audioFeedback.Click(); click(); });
            var t=Label("Caption",go.transform,caption,19,Paper,TextAnchor.MiddleCenter);
            Rect(t.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        }
        void BuildCanvas()
        {
            var root=Obj("Company_UI",transform);
            canvas=root.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1600,900);
            root.AddComponent<GraphicRaycaster>();
            Rect(root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

            promptPanel=Block("HUD_Interaction",root.transform,new Color(.08f,.12f,.14f,.92f));
            Rect(promptPanel,new Vector2(.02f,.025f),new Vector2(.52f,.095f),Vector2.zero,Vector2.zero);
            promptText=Label("InteractionPrompt",promptPanel.transform,"",22,Paper);
            Rect(promptText.gameObject,Vector2.zero,Vector2.one,new Vector2(16,0),new Vector2(-10,0));
            promptPanel.SetActive(false);
            var toast=Block("Notification",root.transform,Navy);
            Rect(toast,new Vector2(.27f,.89f),new Vector2(.73f,.965f),Vector2.zero,Vector2.zero);
            toastText=Label("NotificationText",toast.transform,"",19,Paper,TextAnchor.MiddleCenter);
            Rect(toastText.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            toastPanel=toast; toastPanel.SetActive(false);

            var statusHint=Block("StatusHint",root.transform,new Color(.08f,.12f,.14f,.90f));
            Rect(statusHint,new Vector2(.82f,.925f),new Vector2(.985f,.975f),Vector2.zero,Vector2.zero);
            var hintText=Label("StatusHintText",statusHint.transform,"TAB  •  DURUM",17,Paper,TextAnchor.MiddleCenter);
            Rect(hintText.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            prepPanel=Block("PreparationBoard",root.transform,new Color(.13f,.19f,.22f,.97f));
            Rect(prepPanel,new Vector2(.69f,.44f),new Vector2(.985f,.915f),Vector2.zero,Vector2.zero);
            prepText=Label("PreparationText",prepPanel.transform,"",21,Paper);
            Rect(prepText.gameObject,Vector2.zero,Vector2.one,new Vector2(19,14),new Vector2(-14,-14));
            prepPanel.SetActive(prepVisible);

            computer=Block("OfficeComputerWindow",root.transform,Navy);
            Rect(computer,new Vector2(.12f,.10f),new Vector2(.88f,.91f),Vector2.zero,Vector2.zero);
            var header=Block("WindowHeader",computer.transform,Slate);
            Rect(header,new Vector2(0,.92f),new Vector2(1,1),Vector2.zero,Vector2.zero);
            titleText=Label("WindowTitle",header.transform,"FİRMA OS",25,Paper);
            Rect(titleText.gameObject,Vector2.zero,new Vector2(.76f,1),new Vector2(22,0),Vector2.zero);
            var close=Block("Close",header.transform,Red);
            Rect(close,new Vector2(.92f,.12f),new Vector2(.985f,.88f),Vector2.zero,Vector2.zero);
            close.AddComponent<Button>().onClick.AddListener(Close);
            var closeText=Label("X",close.transform,"KAPAT",15,Paper,TextAnchor.MiddleCenter);
            Rect(closeText.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

            var nav=Block("Sidebar",computer.transform,new Color(.17f,.24f,.27f));
            Rect(nav,new Vector2(0,0),new Vector2(.22f,.92f),Vector2.zero,Vector2.zero);
            var navContent=Obj("SidebarContent",nav.transform);
            Rect(navContent,Vector2.zero,Vector2.one,new Vector2(5,5),new Vector2(-5,-5));
            var navLayout=navContent.AddComponent<VerticalLayoutGroup>(); navLayout.spacing=7;
            navLayout.padding=new RectOffset(8,8,10,10); navLayout.childForceExpandHeight=false;
            foreach (var name in new[]{"Desktop","JobDesk","FleetManager","StockManager","CompanyBank","EquipmentShop","CompanyStats","BuildingManager","Safety","MailBox","Dispatch","Reports"})
            {
                var buttonGo=Block(name,navContent.transform,Slate); Height(buttonGo,43);
                buttonGo.AddComponent<Button>().onClick.AddListener(()=>Show(name));
                var t=Label("Label",buttonGo.transform,name,17,Paper,TextAnchor.MiddleCenter);
                Rect(t.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            }
            var scroll=Obj("AppScroll",computer.transform);
            Rect(scroll,new Vector2(.22f,0),new Vector2(1,.92f),new Vector2(8,8),new Vector2(-8,-8));
            var scrollImage=scroll.AddComponent<Image>(); scrollImage.color=new Color(.10f,.15f,.17f);
            var mask=scroll.AddComponent<Mask>(); mask.showMaskGraphic=true;
            var sc=scroll.AddComponent<ScrollRect>(); sc.horizontal=false;
            var content=Obj("AppContent",scroll.transform);
            mainContent=Rect(content,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);
            mainContent.pivot=new Vector2(.5f,1);
            Layout(mainContent); sc.content=mainContent; sc.viewport=scroll.GetComponent<RectTransform>();
            computer.SetActive(false);
        }
        void Clear()
        {
            for (int i=mainContent.childCount-1;i>=0;i--) Destroy(mainContent.GetChild(i).gameObject);
        }
        public void Open(string target)
        {
            if (audioFeedback) audioFeedback.Click();
            app=target; computer.SetActive(true); player.SetCursor(true); Show(target);
        }
        public void Close()
        {
            computer.SetActive(false); player.SetCursor(false);
        }
        public void SetPrompt(string value)
        {
            if (prompt==value) return;
            prompt=value;
            if (promptText) promptText.text=value;
            if (promptPanel) promptPanel.SetActive(!string.IsNullOrWhiteSpace(value));
        }
        public void Toast(string value)
        {
            if (!toastText) return;
            if (audioFeedback) audioFeedback.Notice();
            toastText.text=value; toastUntil=Time.unscaledTime+4.5f;
            toastPanel.SetActive(true);
        }
        public void TogglePreparation()
        {
            prepVisible=!prepVisible; prepPanel.SetActive(prepVisible);
        }
        public void Refresh()
        {
            if (!Runtime || !catalog || !mainContent) return;
            PrepRefresh();
            if (computer.activeSelf) Show(app);
        }
        void PrepRefresh()
        {
            var job=catalog.Job(Runtime.Data.activeJobId);
            if (!job) { prepText.text="OPERASYON DURUMU\n\nAktif görev yok.\nBilgisayardan JobDesk'i açın.\n\nTAB: paneli gizle"; return; }
            var missing=Runtime.MissingPreparation();
            var loaded=Runtime.vehicle.LoadedItems.Select(x=>x.definitionId).ToArray();
            string equipment=string.Join("\n", job.requiredEquipment.Select(x=>
                (loaded.Contains(x)?"✓ ":"○ ")+(catalog.Item(x)?.displayName ?? x)));
            var phase=Runtime.Data.phase == JobPhase.Accepted ? "Kabul edildi" :
                Runtime.Data.phase == JobPhase.Deployed ? "Sahada" : Runtime.Data.phase.ToString();
            prepText.text="OPERASYON DURUMU\n\n"+job.title+"\nDurum: "+phase+
                "\n\nMODÜL\n"+(Runtime.Data.installedModuleId==job.requiredModule?"✓ ":"○ ")+
                (catalog.Module(job.requiredModule)?.displayName ?? job.requiredModule)+
                "\n\nEKİPMAN\n"+equipment+"\n\n"+
                (missing.Count==0?"Hazırlık tamam":"Eksik: "+missing.Count)+"\nTAB: paneli gizle";
        }
        void Show(string target)
        {
            app=target; Clear(); titleText.text="FİRMA OS   /   "+target;
            switch(target)
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
                default: Desktop(); break;
            }
        }
        void Desktop()
        {
            Row("KÜÇÜK EKİP • BÜYÜK İŞLER",28,Ochre,58);
            Row("Bakiye ₺"+Runtime.Data.balance+"    |    İtibar "+Runtime.Data.reputation+
                "    |    Tamamlanan iş "+Runtime.Data.jobsCompleted,22,Paper,58);
            Row("Bir işi seçin, garajda modül ve ekipmanı hazırlayın. Çıkış kontrolü gerçek araç yükünü inceler.",19,Soft,82);
            ActionButton("İş ilanlarını aç",()=>Show("JobDesk"));
            ActionButton("Hazırlık durumunu aç",()=>Show("Dispatch"));
            ActionButton("Kaydet",()=>{Runtime.Save(); Runtime.Notify("Şirket ilerlemesi kaydedildi.");});
        }
        void Jobs()
        {
            Row("İŞ İLANLARI",27,Ochre,52);
            if (Runtime.Data.phase!=JobPhase.None)
                Row("Aktif iş: "+catalog.Job(Runtime.Data.activeJobId)?.title+" ("+Runtime.Data.phase+")",20,Paper,50);
            foreach (var job in catalog.jobs)
                ActionButton(job.title+"    •    "+job.category+"    •    "+
                    (Runtime.Data.reputation<job.minimumReputation?
                        "İtibar "+job.minimumReputation+" gerekli":"₺"+job.payment),
                    ()=>{selectedJob=job.id; Show("JobDesk");});
            if (!string.IsNullOrEmpty(selectedJob)) JobDetail();
        }
        void JobDetail()
        {
            var job=catalog.Job(selectedJob); if (!job) return;
            Row("SEÇİLİ İŞ: "+job.title,23,Ochre,53);
            Row(job.client+" • "+job.location+" • "+job.distanceKm+" km",19,Soft,43);
            Row(job.description,19,Paper,83);
            Row("Ödeme ₺"+job.payment+"   Zorluk "+job.difficulty+"/5   Risk "+job.risk+"/5   Süre "+job.durationMinutes+" dk",19,Paper,55);
            Row("Gerekli modül: "+(catalog.Module(job.requiredModule)?.displayName ?? job.requiredModule),19,Paper,45);
            Row("Zorunlu: "+string.Join(", ",job.requiredEquipment.Select(x=>catalog.Item(x)?.displayName ?? x)),19,Paper,60);
            Row("Önerilen: "+string.Join(", ",job.recommendedEquipment.Select(x=>catalog.Item(x)?.displayName ?? x)),18,Soft,56);
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
                ActionButton("İŞİ KABUL ET",()=>Runtime.AcceptJob(job.id),Ochre);
                if (job.contractBonus > 0)
                    ActionButton("SÖZLEŞMEYLE KABUL ET",()=>Runtime.AcceptJob(job.id,true));
            }
        }
        void Fleet()
        {
            Row("FİLO / MODÜL İSTASYONU",27,Ochre,52);
            Row("Servis vanı • Takılı: "+catalog.Module(Runtime.vehicle.InstalledModuleId)?.displayName+
                " • Durum %"+Mathf.RoundToInt(Runtime.Data.vehicleCondition),20,Paper,55);
            Row("Yük: "+Runtime.vehicle.LoadedItems.Count()+" ekipman. Modül değişimi için yük bölümü boş olmalı.",18,Soft,56);
            foreach (var mod in catalog.modules)
            {
                bool owned=Runtime.Data.ownedModules.Contains(mod.id);
                string label=mod.displayName+" • "+mod.payloadKg+" kg • "+mod.cargoSlots+" slot • "+(owned?"Sahip":"₺"+mod.price);
                ActionButton(label,()=>
                {
                    if (!Runtime.Data.ownedModules.Contains(mod.id)) Runtime.BuyModule(mod.id);
                    else if (station && !station.Busy) Runtime.vehicle.TryChangeModule(mod.id,station);
                });
            }
        }
        void Stock()
        {
            Row("STOK VE TESLİMAT",27,Ochre,52);
            Row("Tamir parçaları: "+Runtime.Data.spareParts,20,Paper);
            ActionButton("Yedek parça paketi al • ₺"+catalog.economy.sparePartsPackPrice,()=>Runtime.OrderParts());
            foreach(var def in catalog.equipment)
            {
                int owned=FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Count(x=>x.definitionId==def.id);
                int pending=Runtime.Data.orders.Count(x=>x.itemId==def.id&&!x.delivered);
                Row(def.displayName+"    Depoda/sahnede: "+owned+"    Yolda: "+pending,18,Paper,42);
            }
        }
        void Bank()
        {
            Row("ŞİRKET BANKASI",27,Ochre,52);
            Row("Kullanılabilir bakiye: ₺"+Runtime.Data.balance,26,Paper,65);
            Row("Tamamlanan iş: "+Runtime.Data.jobsCompleted,19,Paper);
            Row("SON İŞLEMLER",21,Ochre,48);
            if (Runtime.Data.transactions.Count == 0) Row("Henüz işlem yok.",18,Soft);
            foreach (var entry in Runtime.Data.transactions.TakeLast(12).Reverse())
                Row((entry.amount >= 0 ? "+₺" : "−₺") + Mathf.Abs(entry.amount) + "  •  " + entry.reason +
                    "  •  Bakiye ₺" + entry.balanceAfter,18,Paper,48);
        }
        void Shop()
        {
            Row("EKİPMAN MAĞAZASI",27,Ochre,52);
            Row("Siparişler kısa süre sonra fiziksel teslimat noktasına gelir.",18,Soft,55);
            foreach(var def in catalog.equipment)
                ActionButton(def.displayName+" • "+def.weightKg+" kg • ₺"+def.price,()=>Runtime.OrderEquipment(def.id));
        }
        void Stats()
        {
            Row("ŞİRKET İSTATİSTİKLERİ",27,Ochre,52);
            Row("İtibar: "+Runtime.Data.reputation+" / Tamamlanan iş: "+Runtime.Data.jobsCompleted,22,Paper,58);
            Row("Sahip olunan modüller: "+Runtime.Data.ownedModules.Count,20,Paper);
            Row("Ekipmanlar: "+FindObjectsByType<CarryableItem>(FindObjectsSortMode.None).Length,20,Paper);
            Row("Geliştirmeler: "+Runtime.Data.upgrades.Count,20,Paper);
            Row("Başarılı denetimler: "+Runtime.Data.inspectionsPassed,20,Paper);
        }
        void Building()
        {
            Row("BİNA GELİŞTİRMELERİ",27,Ochre,52);
            foreach (var upgrade in new[]{"storage","lighting","operations"})
            {
                string name=upgrade=="storage"?"Ek depo rafı":upgrade=="lighting"?"Yeni atölye ışığı":"Operasyon panosu";
                int price=catalog.economy.UpgradePrice(upgrade);
                bool owned=Runtime.Data.upgrades.Contains(upgrade);
                ActionButton(name+(owned?" • KURULU":" • ₺"+price),()=>Runtime.BuyUpgrade(upgrade));
            }
        }
        void Mail()
        {
            Row("GELEN KUTUSU",27,Ochre,52);
            Row("Yeni müşteriler JobDesk üzerinden iş tekliflerini iletti.",19,Paper,56);
            if (Runtime.Data.inspectionPending)
                Row("Güvenlik denetimi bekliyor. Safety uygulamasından ekipmanı ve aracı kontrol edin.",19,Red,65);
            foreach(var order in Runtime.Data.orders.TakeLast(5))
                Row((catalog.Item(order.itemId)?.displayName??order.itemId)+" • "+(order.delivered?"Teslim edildi":"Yolda"),18,Soft,43);
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
            if (!job) { Row("Aktif iş yok. JobDesk'ten iş kabul edin.",20,Paper); return; }
            Row(job.title+" • "+Runtime.Data.phase,22,Paper,54);
            if (Runtime.Data.contractAccepted)
                Row("Sözleşme: "+job.contractTitle+" • Ek ₺"+job.contractBonus,18,Soft,48);
            var missing=Runtime.MissingPreparation();
            if (Runtime.Data.phase==JobPhase.Accepted)
            {
                Row(missing.Count==0?"Araç hazır. Fiziksel yük ve modül doğrulandı.":"Eksik: "+string.Join(", ",missing),18,missing.Count==0?Ochre:Red,75);
                ActionButton("GÖREVE ÇIK",()=>Runtime.Depart(),Ochre);
            }
            else if (Runtime.Data.phase==JobPhase.Deployed)
                Row("Saha görevi daha sonra bağlanacak. Editör geliştirme testinde F9 ile test sonucu oluşturulabilir.",18,Soft,80);
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
            else Row("Rapor bekleyen saha dönüşü yok.",19,Soft,55);
        }
    }
}
