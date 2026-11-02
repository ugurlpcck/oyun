using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySim
{
    public sealed partial class ComputerUI
    {
        Canvas canvas;
        Font font,headingFont;
        Sprite roundSprite;
        GameObject computer, desktop, appWindow, hud, prepPanel, toastPanel, promptPanel;
        RectTransform mainContent, windowRect;
        ScrollRect appScroll;
        Text promptText, toastText, prepText, titleText, subtitleText, clockText;
        Text mailBadge;
        DesktopGlyph appIcon;
        bool maximized;
        RectTransform jobFooter;
        static readonly Color Navy=new Color(.035f,.049f,.062f), Slate=new Color(.10f,.14f,.16f);
        static readonly Color Paper=new Color(.93f,.95f,.94f), Soft=new Color(.56f,.65f,.67f);
        static readonly Color Mint=new Color(.55f,.86f,.75f), Blue=new Color(.56f,.72f,.91f);
        static readonly Color Peach=new Color(.94f,.67f,.48f), Ochre=Mint, Red=new Color(.91f,.44f,.43f);
        sealed class AppDefinition
        {
            public string id,title,description; public Color tint;
            public AppDefinition(string id,string title,string description,Color tint)
            { this.id=id; this.title=title; this.description=description; this.tint=tint; }
        }
        static readonly AppDefinition[] Apps={
            new AppDefinition("JobDesk","İş Panosu","Yeni fırsatları keşfet, sıradaki işini seç.",Mint),
            new AppDefinition("FleetManager","Filo","Servis vanını ve uzmanlık modüllerini yönet.",Blue),
            new AppDefinition("StockManager","Depo","Ekipmanların, yedek parçaların ve teslimatların.",Peach),
            new AppDefinition("EquipmentShop","Mağaza","İşine uygun ekipmanı bul, atölyeni güçlendir.",new Color(.77f,.69f,.91f)),
            new AppDefinition("CompanyBank","Finans","Şirketinin bakiyesini ve hesap hareketlerini izle.",Mint),
            new AppDefinition("Dispatch","Sevkiyat","Modülü ve ekipmanları kontrol et, göreve hazırlan.",Peach),
            new AppDefinition("Reports","Raporlar","İş sonuçlarını incele ve ödemeni tahsil et.",Blue),
            new AppDefinition("BuildingManager","Geliştirmeler","Çalışma alanına yatırım yap, şirketini büyüt.",Peach),
            new AppDefinition("CompanyStats","Şirket","Küçük ekibinin büyümesini ve başarılarını takip et.",new Color(.77f,.69f,.91f)),
            new AppDefinition("Safety","Güvenlik","Ekipman sağlığı, bakım koşulları ve denetimler.",Mint),
            new AppDefinition("MailBox","Posta","Şirketinden haberler ve teslimat bildirimleri.",Blue)
        };
        GameObject Obj(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return go;
        }
        RectTransform Rect(GameObject go,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {
            var rt=go.GetComponent<RectTransform>(); rt.anchorMin=min; rt.anchorMax=max;
            rt.offsetMin=low; rt.offsetMax=high; return rt;
        }
        void Fill(GameObject go,float pad=0) => Rect(go,Vector2.zero,Vector2.one,Vector2.one*pad,Vector2.one*-pad);
        void Place(GameObject go,float x,float y,float w,float h)
        {
            var rt=Rect(go,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero);
            rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(x,-y); rt.sizeDelta=new Vector2(w,h);
        }
        GameObject Block(string name,Transform parent,Color color,bool rounded=true)
        {
            var go=Obj(name,parent); var img=go.AddComponent<Image>(); img.color=color;
            if(rounded) { img.sprite=roundSprite; img.type=Image.Type.Sliced; }
            return go;
        }
        void Border(GameObject go)
        {
            var outline=go.AddComponent<Outline>(); outline.effectColor=new Color(.63f,.78f,.77f,.09f);
            outline.effectDistance=new Vector2(1,-1); outline.useGraphicAlpha=true;
        }
        Text Label(string name,Transform parent,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var go=Obj(name,parent); var t=go.AddComponent<Text>();
            t.font=font; t.text=value; t.fontSize=size; t.color=color; t.alignment=align;
            t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap;
            t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        DesktopGlyph Icon(Transform parent,string kind,Color color)
        {
            var go=Obj("Icon_"+kind,parent); var icon=go.AddComponent<DesktopGlyph>();
            icon.kind=kind; icon.color=color; icon.raycastTarget=false; return icon;
        }
        Button Bind(GameObject go,Action action)
        {
            var button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>();
            var colors=button.colors; colors.normalColor=Color.white;
            colors.highlightedColor=new Color(1.2f,1.2f,1.2f); colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.75f,.85f,.83f); colors.disabledColor=new Color(.65f,.65f,.65f,.45f);
            colors.fadeDuration=.12f; button.colors=colors;
            button.onClick.AddListener(()=>{Click(); action();});
            var nav=button.navigation; nav.mode=Navigation.Mode.None; button.navigation=nav;
            return button;
        }
        Button SmallButton(string name,Transform parent,string kind,string caption,Action action,Color? tint=null)
        {
            var go=Block(name,parent,tint??Slate); Bind(go,action);
            var icon=Icon(go.transform,kind,Paper); Place(icon.gameObject,12,12,24,24);
            if(!string.IsNullOrEmpty(caption))
            {
                var label=Label("Caption",go.transform,caption,16,Paper);
                Rect(label.gameObject,Vector2.zero,Vector2.one,new Vector2(46,0),new Vector2(-10,0));
            }
            return go.GetComponent<Button>();
        }
        void Layout(Transform t,float spacing=10)
        {
            var l=t.gameObject.AddComponent<VerticalLayoutGroup>(); l.spacing=spacing;
            l.padding=new RectOffset(26,26,20,24); l.childControlHeight=true; l.childControlWidth=true;
            l.childForceExpandHeight=false; l.childForceExpandWidth=true;
            t.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        }
        void Height(GameObject go,float height) { go.AddComponent<LayoutElement>().preferredHeight=height; }
        void Row(string value,int size=20,Color? color=null,float height=48)
        {
            var t=Label("Row",mainContent,value,size,color??Paper); Height(t.gameObject,height);
            if(size>=23) t.font=headingFont;
        }
        Button ActionButton(string caption,Action click,Color? color=null)
        {
            var tint=color??Slate; var go=Block("Action_"+caption,mainContent,tint); Height(go,48);
            var b=Bind(go,click); var foreground=color.HasValue?Navy:Paper;
            var t=Label("Caption",go.transform,caption,17,foreground); t.font=headingFont;
            Rect(t.gameObject,Vector2.zero,Vector2.one,new Vector2(20,0),new Vector2(-44,0));
            var arrow=Icon(go.transform,"Arrow",foreground);
            Rect(arrow.gameObject,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-34,-10),new Vector2(-14,10));
            return b;
        }
        void Clear()
        {
            for(int i=mainContent.childCount-1;i>=0;i--)
            {
                var child=mainContent.GetChild(i).gameObject; child.SetActive(false); Destroy(child);
            }
        }
        Sprite RoundedSprite()
        {
            const int size=64; var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
            tex.name="Desktop_RoundedCorners"; tex.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var point=new Vector2(Mathf.Max(Mathf.Abs(x-31.5f)-15.5f,0),Mathf.Max(Mathf.Abs(y-31.5f)-15.5f,0));
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(16-point.magnitude));
            }
            tex.SetPixels(pixels); tex.Apply();
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
        }
        void BuildCanvas()
        {
            roundSprite=RoundedSprite();
            var root=Obj("Company_UI",transform); canvas=root.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<GraphicRaycaster>();
            hud=Obj("HUD",root.transform); Fill(hud);
            promptPanel=Block("HUD_Interaction",hud.transform,new Color(.03f,.05f,.06f,.94f));
            Rect(promptPanel,new Vector2(.5f,.055f),new Vector2(.5f,.055f),new Vector2(-260,0),new Vector2(260,52));
            promptText=Label("InteractionPrompt",promptPanel.transform,"",19,Paper,TextAnchor.MiddleCenter); Fill(promptText.gameObject,10);
            promptPanel.SetActive(false);
            var cross=Icon(hud.transform,"Crosshair",new Color(.9f,.96f,.94f,.65f));
            Rect(cross.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-3,-3),new Vector2(3,3));
            var hint=Label("StatusHint",hud.transform,"TAB  /  OPERASYON",15,Soft,TextAnchor.MiddleRight);
            Rect(hint.gameObject,new Vector2(.75f,.935f),new Vector2(.97f,.98f),Vector2.zero,Vector2.zero);
            prepPanel=Block("PreparationBoard",hud.transform,new Color(.04f,.07f,.08f,.97f));
            Rect(prepPanel,new Vector2(.70f,.39f),new Vector2(.98f,.92f),Vector2.zero,Vector2.zero); Border(prepPanel);
            prepText=Label("PreparationText",prepPanel.transform,"",20,Paper); Fill(prepText.gameObject,22); prepPanel.SetActive(false);

            computer=Block("OfficeComputerWindow",root.transform,new Color(.012f,.017f,.021f),false); Fill(computer);
            var workspace=Obj("DesktopWorkArea",computer.transform);
            Fill(workspace); var af=workspace.AddComponent<AspectRatioFitter>(); af.aspectMode=AspectRatioFitter.AspectMode.FitInParent; af.aspectRatio=16f/9;
            workspace.AddComponent<RectMask2D>();
            BuildDesktop(workspace.transform);
            BuildAppWindow(workspace.transform);
            BuildTaskbar(workspace.transform);
            computer.SetActive(false);

            toastPanel=Block("Notification",root.transform,new Color(.12f,.20f,.21f,.99f));
            Rect(toastPanel,new Vector2(.5f,.91f),new Vector2(.5f,.91f),new Vector2(-310,0),new Vector2(310,62)); Border(toastPanel);
            toastText=Label("NotificationText",toastPanel.transform,"",18,Paper,TextAnchor.MiddleCenter); Fill(toastText.gameObject,15); toastPanel.SetActive(false);
        }
        void BuildDesktop(Transform parent)
        {
            desktop=Obj("Desktop",parent); Fill(desktop);
            var grid=Obj("DesktopApplications",desktop.transform);
            Place(grid,48,48,600,420);
            var layout=grid.AddComponent<GridLayoutGroup>(); layout.cellSize=new Vector2(132,120); layout.spacing=new Vector2(20,20);
            layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount=4;
            foreach(var definition in Apps)
            {
                var def=definition; var tile=Block("App_"+def.id,grid.transform,Color.clear);
                var button=Bind(tile,()=>Show(def.id));
                var iconBg=Block("IconBackground",tile.transform,new Color(def.tint.r*.17f,def.tint.g*.17f,def.tint.b*.17f,1));
                Place(iconBg,34,8,64,64); button.targetGraphic=iconBg.GetComponent<Image>();
                var glyph=Icon(iconBg.transform,def.id,def.tint); Fill(glyph.gameObject,15);
                var title=Label("AppName",tile.transform,def.title,17,Paper,TextAnchor.MiddleCenter); Place(title.gameObject,0,80,132,28);
                if(def.id=="MailBox")
                {
                    mailBadge=Label("MailBadge",iconBg.transform,"",23,Peach,TextAnchor.UpperRight);
                    Fill(mailBadge.gameObject,1);
                }
            }
        }
        void BuildAppWindow(Transform parent)
        {
            appWindow=Block("ApplicationWindow",parent,new Color(.055f,.079f,.093f));
            windowRect=Rect(appWindow,new Vector2(.065f,.16f),new Vector2(.935f,.89f),Vector2.zero,Vector2.zero); Border(appWindow);
            var shadow=appWindow.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.55f); shadow.effectDistance=new Vector2(0,-8);
            var header=Block("ApplicationHeader",appWindow.transform,new Color(.09f,.13f,.15f));
            Rect(header,new Vector2(0,1),Vector2.one,new Vector2(0,-66),Vector2.zero);
            appIcon=Icon(header.transform,"JobDesk",Mint); Place(appIcon.gameObject,24,20,28,28);
            titleText=Label("WindowTitle",header.transform,"",22,Paper); titleText.font=headingFont;
            Rect(titleText.gameObject,Vector2.zero,Vector2.one,new Vector2(67,0),new Vector2(-190,0));
            var minimize=SmallButton("MinimizeApplication",header.transform,"Minimize","",()=>Show("Desktop"));
            Rect(minimize.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-165,-56),new Vector2(-117,-8));
            var maximize=SmallButton("MaximizeApplication",header.transform,"Maximize","",ToggleMaximize);
            Rect(maximize.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-111,-56),new Vector2(-63,-8));
            var close=SmallButton("CloseApplication",header.transform,"Close","",()=>Show("Desktop"),new Color(.27f,.16f,.17f));
            Rect(close.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-57,-56),new Vector2(-9,-8));
            subtitleText=Label("WindowDescription",appWindow.transform,"",16,Soft);
            Rect(subtitleText.gameObject,new Vector2(0,1),Vector2.one,new Vector2(28,-108),new Vector2(-28,-72));
            var scroll=Obj("AppScroll",appWindow.transform);
            Rect(scroll,Vector2.zero,Vector2.one,new Vector2(3,8),new Vector2(-14,-112));
            scroll.AddComponent<RectMask2D>(); var hit=scroll.AddComponent<Image>(); hit.color=Color.clear;
            appScroll=scroll.AddComponent<ScrollRect>(); appScroll.horizontal=false; appScroll.movementType=ScrollRect.MovementType.Clamped; appScroll.scrollSensitivity=38;
            var content=Obj("AppContent",scroll.transform);
            mainContent=Rect(content,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); mainContent.pivot=new Vector2(.5f,1);
            Layout(mainContent); appScroll.content=mainContent; appScroll.viewport=scroll.GetComponent<RectTransform>();
            var bar=Block("ScrollTrack",appWindow.transform,new Color(1,1,1,.035f));
            Rect(bar,new Vector2(1,0),Vector2.one,new Vector2(-10,18),new Vector2(-6,-126));
            var handle=Block("ScrollHandle",bar.transform,new Color(.5f,.7f,.7f,.36f)); Fill(handle);
            var sb=bar.AddComponent<Scrollbar>(); sb.direction=Scrollbar.Direction.BottomToTop; sb.handleRect=handle.GetComponent<RectTransform>(); sb.targetGraphic=handle.GetComponent<Image>();
            appScroll.verticalScrollbar=sb; appScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            appWindow.SetActive(false);
        }
        void ToggleMaximize()
        {
            maximized=!maximized;
            Rect(appWindow,maximized?new Vector2(.025f,.12f):new Vector2(.065f,.16f),maximized?new Vector2(.975f,.91f):new Vector2(.935f,.89f),Vector2.zero,Vector2.zero);
            Show(app);
        }
        void BuildTaskbar(Transform parent)
        {
            var dock=Block("Taskbar",parent,new Color(.045f,.057f,.067f),false);
            Rect(dock,new Vector2(0,0),new Vector2(1,0),Vector2.zero,new Vector2(0,68));
            var home=SmallButton("DesktopHome",dock.transform,"Home","Masaüstü",()=>Show("Desktop")); Place(home.gameObject,14,10,148,48);
            clockText=Label("Clock",dock.transform,"",14,Soft,TextAnchor.MiddleRight);
            Rect(clockText.gameObject,new Vector2(1,0),Vector2.one,new Vector2(-490,0),new Vector2(-280,0));
            var save=SmallButton("SaveCompany",dock.transform,"Save","Kaydet",()=>{Runtime.Save(); Toast("Şirket ilerlemesi kaydedildi.");});
            Rect(save.gameObject,new Vector2(1,0),new Vector2(1,0),new Vector2(-260,10),new Vector2(-140,58));
            var exit=SmallButton("ExitComputer",dock.transform,"Power","Çıkış",Close,new Color(.15f,.10f,.11f));
            Rect(exit.gameObject,new Vector2(1,0),new Vector2(1,0),new Vector2(-130,10),new Vector2(-14,58));
        }
        void RefreshDesktop()
        {
            mailBadge.text=Runtime.Data.inspectionPending?"•":"";
        }
        void StatStrip(params (string label,string value)[] stats)
        {
            var row=Obj("Metrics",mainContent); Height(row,92);
            var l=row.AddComponent<HorizontalLayoutGroup>(); l.spacing=12; l.childForceExpandWidth=true;
            foreach(var stat in stats)
            {
                var card=Block("Metric",row.transform,new Color(.085f,.12f,.14f));
                card.AddComponent<LayoutElement>().flexibleWidth=1;
                var label=Label("Label",card.transform,stat.label,12,Soft); Place(label.gameObject,18,12,350,23);
                var value=Label("Value",card.transform,stat.value,24,Paper); value.font=headingFont;
                Rect(value.gameObject,Vector2.zero,Vector2.one,new Vector2(18,10),new Vector2(-12,-37));
                Rect(label.gameObject,new Vector2(0,1),Vector2.one,new Vector2(18,-35),new Vector2(-12,-10));
            }
        }
        void InfoCard(string title,string description,string kind,Color tint,string action=null,Action click=null,bool disabled=false)
        {
            var card=Block("Card_"+title,mainContent,new Color(.085f,.12f,.14f)); Height(card,action==null?92:108);
            var icon=Icon(card.transform,kind,tint); Place(icon.gameObject,20,29,32,32);
            var label=Label("Title",card.transform,title,20,Paper); label.font=headingFont;
            Rect(label.gameObject,new Vector2(0,.5f),Vector2.one,new Vector2(72,1),new Vector2(action==null?-20:-270,-12));
            var desc=Label("Description",card.transform,description,16,Soft);
            Rect(desc.gameObject,Vector2.zero,new Vector2(1,.5f),new Vector2(72,10),new Vector2(action==null?-20:-270,-1));
            if(action!=null)
            {
                var button=SmallButton("CardAction",card.transform,"Arrow",action,click??(()=>{})); button.interactable=!disabled;
                Rect(button.gameObject,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-252,-24),new Vector2(-16,24));
            }
        }
        void EmptyState(string title,string description,string kind,string action=null,Action click=null)
        {
            var box=Obj("EmptyState",mainContent); Height(box,260);
            var icon=Icon(box.transform,kind,Mint);
            Rect(icon.gameObject,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-26,-78),new Vector2(26,-26));
            var heading=Label("Title",box.transform,title,28,Paper,TextAnchor.MiddleCenter); heading.font=headingFont;
            Rect(heading.gameObject,new Vector2(0,.40f),new Vector2(1,.68f),Vector2.zero,Vector2.zero);
            var detail=Label("Detail",box.transform,description,18,Soft,TextAnchor.UpperCenter);
            Rect(detail.gameObject,new Vector2(.12f,.05f),new Vector2(.88f,.39f),Vector2.zero,Vector2.zero);
            if(action!=null) ActionButton(action,click,Mint);
        }
        void JobColumns(out RectTransform list,out RectTransform detail)
        {
            var row=Obj("JobWorkspace",mainContent);
            Height(row,Mathf.Max(320,appScroll.viewport.rect.height-146));
            var horizontal=row.AddComponent<HorizontalLayoutGroup>(); horizontal.spacing=24;
            horizontal.childControlWidth=true; horizontal.childControlHeight=true;
            horizontal.childForceExpandWidth=false; horizontal.childForceExpandHeight=true;
            var left=Obj("JobList",row.transform); var right=Block("JobDetail",row.transform,new Color(.075f,.105f,.12f));
            var leftSize=left.AddComponent<LayoutElement>(); leftSize.flexibleWidth=.38f; leftSize.preferredWidth=0; leftSize.minWidth=0;
            var rightSize=right.AddComponent<LayoutElement>(); rightSize.flexibleWidth=.62f; rightSize.preferredWidth=0; rightSize.minWidth=0;
            list=ScrollColumn("JobListScroll",left.transform,0,0);
            detail=ScrollColumn("JobDetailScroll",right.transform,128,20);
            var footer=Obj("JobActions",right.transform);
            Rect(footer,Vector2.zero,new Vector2(1,0),new Vector2(20,12),new Vector2(-20,120));
            var layout=footer.AddComponent<VerticalLayoutGroup>(); layout.spacing=8;
            layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandHeight=false;
            jobFooter=footer.GetComponent<RectTransform>();
        }
        RectTransform ScrollColumn(string name,Transform parent,float bottom,int padding)
        {
            var viewport=Obj(name,parent); Rect(viewport,Vector2.zero,Vector2.one,new Vector2(0,bottom),Vector2.zero);
            viewport.AddComponent<RectMask2D>(); viewport.AddComponent<Image>().color=Color.clear;
            var scroll=viewport.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=32;
            var content=Obj("Content",viewport.transform);
            var rect=Rect(content,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); rect.pivot=new Vector2(.5f,1);
            Layout(rect,8); rect.GetComponent<VerticalLayoutGroup>().padding=new RectOffset(padding,padding,padding,padding);
            scroll.content=rect; scroll.viewport=viewport.GetComponent<RectTransform>();
            var track=Block("ScrollTrack",viewport.transform,new Color(1,1,1,.035f));
            Rect(track,new Vector2(1,0),Vector2.one,new Vector2(-6,10),new Vector2(-2,-10));
            var handle=Block("ScrollHandle",track.transform,new Color(.5f,.7f,.7f,.36f)); Fill(handle);
            var bar=track.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop;
            bar.handleRect=handle.GetComponent<RectTransform>(); bar.targetGraphic=handle.GetComponent<Image>();
            scroll.verticalScrollbar=bar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            return rect;
        }
        void JobCard(JobDefinition job)
        {
            bool locked=Runtime.Data.reputation<job.minimumReputation;
            bool selected=selectedJob==job.id;
            var card=Block("Job_"+job.id,mainContent,selected?new Color(.13f,.23f,.23f):Slate); Height(card,85);
            Bind(card,()=>{selectedJob=job.id; Show("JobDesk");});
            var heading=Label("JobTitle",card.transform,job.title,18,selected?Mint:Paper); heading.font=headingFont;
            Rect(heading.gameObject,new Vector2(0,.45f),Vector2.one,new Vector2(17,0),new Vector2(-15,-10));
            var meta=Label("JobMeta",card.transform,locked?job.minimumReputation+" itibar gerekli":Money(job.payment)+"  •  "+job.durationMinutes+" dk",15,locked?Peach:Soft);
            Rect(meta.gameObject,Vector2.zero,new Vector2(1,.45f),new Vector2(17,9),new Vector2(-15,0));
        }
        Transform TileGrid(string name,int columns,float height)
        {
            var go=Obj(name,mainContent); var layout=go.AddComponent<GridLayoutGroup>();
            float width=(windowRect.rect.width-66-16*(columns-1))/columns;
            layout.cellSize=new Vector2(width,height); layout.spacing=new Vector2(16,16);
            layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount=columns;
            return go.transform;
        }
        void ProductCard(Transform parent,string title,string description,string price,Action buy,bool affordable)
        {
            var card=Block("Product_"+title,parent,new Color(.085f,.12f,.14f));
            var icon=Icon(card.transform,"EquipmentShop",Peach); Place(icon.gameObject,20,20,32,32);
            var heading=Label("ProductName",card.transform,title,20,Paper); heading.font=headingFont;
            Rect(heading.gameObject,new Vector2(0,1),Vector2.one,new Vector2(68,-53),new Vector2(-16,-17));
            var meta=Label("Description",card.transform,description,15,Soft); Place(meta.gameObject,20,64,450,30);
            var button=SmallButton("Buy_"+title,card.transform,"Arrow",price+"  •  Sipariş ver",buy);
            Rect(button.gameObject,Vector2.zero,new Vector2(1,0),new Vector2(16,14),new Vector2(-16,62));
            button.interactable=affordable;
        }
    }
}
