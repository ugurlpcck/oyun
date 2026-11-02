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
        Text carriedText;
        DesktopGlyph focusReticle;
        CarryableItem carriedItem;
        GameObject carriedPanel;
        DesktopGlyph appIcon;
        bool maximized;
        string lastWindowApp;
        float lastWindowScroll=1;
        Button taskWindowButton;
        Text taskWindowText;
        DesktopGlyph taskWindowIcon;
        Vector2 normalAnchorMin,normalAnchorMax,normalOffsetMin,normalOffsetMax;
        RectTransform jobFooter;
        static readonly Color Navy=new Color(.12f,.12f,.12f), Slate=new Color(.82f,.81f,.78f);
        static readonly Color Paper=new Color(.10f,.10f,.10f), Soft=new Color(.36f,.36f,.35f);
        static readonly Color Mint=new Color(.12f,.22f,.42f), Blue=new Color(.12f,.25f,.48f);
        static readonly Color Peach=new Color(.43f,.26f,.10f), Ochre=Mint, Red=new Color(.65f,.10f,.08f);
        sealed class AppDefinition
        {
            public string id,title,description; public Color tint;
            public AppDefinition(string id,string title,string description,Color tint)
            { this.id=id; this.title=title; this.description=description; this.tint=tint; }
        }
        static readonly AppDefinition[] Apps={
            new AppDefinition("JobDesk","İş Panosu","Güncel iş ilanları",Mint),
            new AppDefinition("FleetManager","Filo","Araç ve modüller",Blue),
            new AppDefinition("StockManager","Depo","Stok ve teslimatlar",Peach),
            new AppDefinition("EquipmentShop","Mağaza","Ekipman siparişleri",Blue),
            new AppDefinition("CompanyBank","Finans","Bakiye ve hesap hareketleri",Mint),
            new AppDefinition("Dispatch","Sevkiyat","Görev hazırlığı ve çıkış kontrolü",Peach),
            new AppDefinition("Reports","Raporlar","Görev sonuçları ve ödemeler",Blue),
            new AppDefinition("BuildingManager","Geliştirmeler","Bina ve atölye geliştirmeleri",Peach),
            new AppDefinition("CompanyStats","Şirket","Şirket istatistikleri",Blue),
            new AppDefinition("Safety","Güvenlik","Bakım ve denetimler",Mint),
            new AppDefinition("MailBox","Posta","Bildirimler",Blue)
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
        GameObject Block(string name,Transform parent,Color color,bool rounded=false)
        {
            var go=Obj(name,parent); var img=go.AddComponent<Image>(); img.color=color;
            if(rounded) { img.sprite=roundSprite; img.type=Image.Type.Sliced; }
            return go;
        }
        void Border(GameObject go)
        {
            Bevel(go,true);
        }
        void Bevel(GameObject go,bool inset=false)
        {
            var light=inset?new Color(.4f,.4f,.39f):Color.white;
            var dark=inset?Color.white:new Color(.35f,.35f,.34f);
            foreach(var edge in new[]{"Top","Left","Bottom","Right"})
            {
                var line=Block("Edge_"+edge,go.transform,edge=="Top"||edge=="Left"?light:dark,false);
                line.GetComponent<Image>().raycastTarget=false;
                if(edge=="Top") Rect(line,new Vector2(0,1),Vector2.one,new Vector2(0,-1),Vector2.zero);
                if(edge=="Bottom") Rect(line,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,1));
                if(edge=="Left") Rect(line,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(1,0));
                if(edge=="Right") Rect(line,new Vector2(1,0),Vector2.one,new Vector2(-1,0),Vector2.zero);
            }
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
        Button Bind(GameObject go,Action action,bool frame=true)
        {
            if(frame) Bevel(go);
            var button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>();
            var colors=button.colors; colors.normalColor=Color.white;
            colors.highlightedColor=new Color(.97f,.97f,.97f); colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.82f,.82f,.82f); colors.disabledColor=new Color(.75f,.75f,.75f,.6f);
            colors.fadeDuration=0; button.colors=colors;
            button.onClick.AddListener(()=>{Click(); action();});
            var nav=button.navigation; nav.mode=Navigation.Mode.None; button.navigation=nav;
            return button;
        }
        Button SmallButton(string name,Transform parent,string kind,string caption,Action action,Color? tint=null)
        {
            var go=Block(name,parent,tint??Slate); Bind(go,action);
            var icon=Icon(go.transform,kind,Paper);
            if(string.IsNullOrEmpty(caption)) Fill(icon.gameObject,5);
            else Rect(icon.gameObject,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(9,-8),new Vector2(25,8));
            if(!string.IsNullOrEmpty(caption))
            {
                var label=Label("Caption",go.transform,caption,14,Paper);
                Rect(label.gameObject,Vector2.zero,Vector2.one,new Vector2(34,0),new Vector2(-7,0));
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
            var t=Label("Row",mainContent,value,Mathf.Min(size,18),color??Paper); Height(t.gameObject,height);
            if(size>=23) t.font=headingFont;
        }
        Button ActionButton(string caption,Action click,Color? color=null)
        {
            var go=Block("Action_"+caption,mainContent,Slate); Height(go,34);
            var b=Bind(go,click);
            var t=Label("Caption",go.transform,caption,14,Paper,TextAnchor.MiddleCenter);
            Fill(t.gameObject,4);
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
            Rect(promptPanel,new Vector2(.5f,.055f),new Vector2(.5f,.055f),new Vector2(-245,0),new Vector2(245,38));
            promptText=Label("InteractionPrompt",promptPanel.transform,"",16,Color.white,TextAnchor.MiddleCenter); Fill(promptText.gameObject,6);
            promptPanel.SetActive(false);
            var cross=Icon(hud.transform,"Crosshair",new Color(.9f,.96f,.94f,.65f));
            focusReticle=cross;
            Rect(cross.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-3,-3),new Vector2(3,3));
            var hint=Label("StatusHint",hud.transform,"TAB  /  OPERASYON",15,new Color(.7f,.75f,.75f),TextAnchor.MiddleRight);
            Rect(hint.gameObject,new Vector2(.75f,.935f),new Vector2(.97f,.98f),Vector2.zero,Vector2.zero);
            prepPanel=Block("PreparationBoard",hud.transform,new Color(.04f,.07f,.08f,.97f));
            Rect(prepPanel,new Vector2(.70f,.39f),new Vector2(.98f,.92f),Vector2.zero,Vector2.zero); Border(prepPanel);
            prepText=Label("PreparationText",prepPanel.transform,"",20,Color.white); Fill(prepText.gameObject,22); prepPanel.SetActive(false);
            carriedPanel=Block("CarriedEquipment",hud.transform,new Color(.03f,.05f,.06f,.88f));
            Rect(carriedPanel,new Vector2(1,0),new Vector2(1,0),new Vector2(-310,30),new Vector2(-26,94));
            carriedText=Label("CarriedEquipmentName",carriedPanel.transform,"",15,Color.white);
            Fill(carriedText.gameObject,10); carriedPanel.SetActive(false);

            computer=Block("OfficeComputerWindow",root.transform,Color.black,false); Fill(computer);
            var workspace=Obj("DesktopWorkArea",computer.transform);
            Fill(workspace); var af=workspace.AddComponent<AspectRatioFitter>(); af.aspectMode=AspectRatioFitter.AspectMode.FitInParent; af.aspectRatio=16f/9;
            workspace.AddComponent<RectMask2D>();
            BuildDesktop(workspace.transform);
            BuildAppWindow(workspace.transform);
            BuildTaskbar(workspace.transform);
            computer.SetActive(false);

            toastPanel=Block("Notification",root.transform,new Color(.12f,.20f,.21f,.99f));
            Rect(toastPanel,new Vector2(.5f,.91f),new Vector2(.5f,.91f),new Vector2(-310,0),new Vector2(310,62)); Border(toastPanel);
            toastText=Label("NotificationText",toastPanel.transform,"",16,Color.white,TextAnchor.MiddleCenter); Fill(toastText.gameObject,15); toastPanel.SetActive(false);
        }
        void BuildDesktop(Transform parent)
        {
            desktop=Obj("Desktop",parent); Fill(desktop);
            var grid=Obj("DesktopApplications",desktop.transform);
            Place(grid,18,20,232,594);
            var layout=grid.AddComponent<GridLayoutGroup>(); layout.cellSize=new Vector2(104,82); layout.spacing=new Vector2(8,12);
            layout.startAxis=GridLayoutGroup.Axis.Vertical;
            layout.constraint=GridLayoutGroup.Constraint.FixedRowCount; layout.constraintCount=6;
            foreach(var definition in Apps)
            {
                var def=definition; var tile=Block("App_"+def.id,grid.transform,Color.clear);
                var button=Bind(tile,()=>Show(def.id),false);
                var glyph=Icon(tile.transform,def.id,Color.white); glyph.shortcut=true; Place(glyph.gameObject,34,3,36,36);
                var title=Label("AppName",tile.transform,def.title,14,Color.white,TextAnchor.UpperCenter); Place(title.gameObject,0,46,104,34);
                var highlight=Block("Selection",tile.transform,Color.clear,false); Place(highlight,3,44,98,31);
                highlight.transform.SetSiblingIndex(0); button.targetGraphic=highlight.GetComponent<Image>();
                highlight.GetComponent<Image>().color=Color.white;
                var colors=button.colors; colors.normalColor=Color.clear;
                colors.highlightedColor=new Color(.10f,.20f,.40f,.8f); colors.pressedColor=new Color(.14f,.25f,.5f,.9f);
                colors.selectedColor=colors.highlightedColor; button.colors=colors;
                if(def.id=="MailBox")
                {
                    mailBadge=Label("MailBadge",tile.transform,"",18,Color.white,TextAnchor.UpperRight);
                    Place(mailBadge.gameObject,67,2,15,20);
                }
            }
        }
        void BuildAppWindow(Transform parent)
        {
            appWindow=Block("ApplicationWindow",parent,Slate);
            windowRect=Rect(appWindow,new Vector2(.065f,.16f),new Vector2(.935f,.89f),Vector2.zero,Vector2.zero); Border(appWindow);
            var header=Block("ApplicationHeader",appWindow.transform,new Color(.12f,.18f,.32f));
            Rect(header,new Vector2(0,1),Vector2.one,new Vector2(3,-32),new Vector2(-3,-3));
            var drag=header.AddComponent<DesktopWindowDrag>(); drag.window=windowRect;
            drag.canDrag=()=>!maximized; drag.doubleClick=ToggleMaximize;
            appIcon=Icon(header.transform,"JobDesk",Color.white); Place(appIcon.gameObject,6,5,20,20);
            titleText=Label("WindowTitle",header.transform,"",15,Color.white); titleText.font=headingFont;
            Rect(titleText.gameObject,Vector2.zero,Vector2.one,new Vector2(34,0),new Vector2(-100,0));
            var minimize=SmallButton("MinimizeApplication",header.transform,"Minimize","",()=>Show("Desktop"));
            Rect(minimize.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-82,-25),new Vector2(-58,-4));
            var maximize=SmallButton("MaximizeApplication",header.transform,"Maximize","",ToggleMaximize);
            Rect(maximize.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-56,-25),new Vector2(-32,-4));
            var close=SmallButton("CloseApplication",header.transform,"Close","",CloseApplicationWindow);
            Rect(close.gameObject,new Vector2(1,1),Vector2.one,new Vector2(-28,-25),new Vector2(-4,-4));
            subtitleText=Label("WindowDescription",appWindow.transform,"",14,Paper);
            Rect(subtitleText.gameObject,new Vector2(0,1),Vector2.one,new Vector2(12,-61),new Vector2(-12,-35));
            var scroll=Obj("AppScroll",appWindow.transform);
            Rect(scroll,Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-25,-66));
            scroll.AddComponent<RectMask2D>(); var hit=scroll.AddComponent<Image>(); hit.color=new Color(.95f,.95f,.93f);
            appScroll=scroll.AddComponent<ScrollRect>(); appScroll.horizontal=false; appScroll.movementType=ScrollRect.MovementType.Clamped; appScroll.scrollSensitivity=38;
            var content=Obj("AppContent",scroll.transform);
            mainContent=Rect(content,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); mainContent.pivot=new Vector2(.5f,1);
            Layout(mainContent); appScroll.content=mainContent; appScroll.viewport=scroll.GetComponent<RectTransform>();
            var bar=Block("ScrollTrack",appWindow.transform,new Color(.73f,.73f,.71f));
            Rect(bar,new Vector2(1,0),Vector2.one,new Vector2(-22,9),new Vector2(-7,-67));
            var handle=Block("ScrollHandle",bar.transform,Slate); Fill(handle); Bevel(handle);
            var sb=bar.AddComponent<Scrollbar>(); sb.direction=Scrollbar.Direction.BottomToTop; sb.handleRect=handle.GetComponent<RectTransform>(); sb.targetGraphic=handle.GetComponent<Image>();
            appScroll.verticalScrollbar=sb; appScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            appWindow.SetActive(false);
        }
        void ToggleMaximize()
        {
            if(!maximized)
            {
                normalAnchorMin=windowRect.anchorMin; normalAnchorMax=windowRect.anchorMax;
                normalOffsetMin=windowRect.offsetMin; normalOffsetMax=windowRect.offsetMax;
                Rect(appWindow,new Vector2(.025f,.12f),new Vector2(.975f,.91f),Vector2.zero,Vector2.zero);
            }
            else
            {
                windowRect.anchorMin=normalAnchorMin; windowRect.anchorMax=normalAnchorMax;
                windowRect.offsetMin=normalOffsetMin; windowRect.offsetMax=normalOffsetMax;
            }
            maximized=!maximized;
            Show(app);
        }
        void CloseApplicationWindow() { lastWindowApp=null; Show("Desktop"); }
        void ToggleTaskWindow()
        {
            if(string.IsNullOrEmpty(lastWindowApp)) return;
            if(app==lastWindowApp) Show("Desktop");
            else { float position=lastWindowScroll; Show(lastWindowApp); appScroll.verticalNormalizedPosition=position; }
        }
        void UpdateTaskWindow()
        {
            if(!taskWindowButton) return;
            var definition=Apps.FirstOrDefault(x=>x.id==lastWindowApp);
            taskWindowButton.gameObject.SetActive(definition!=null);
            if(definition==null) return;
            taskWindowText.text=definition.title; taskWindowIcon.kind=definition.id; taskWindowIcon.SetVerticesDirty();
            var colors=taskWindowButton.colors;
            colors.normalColor=app==lastWindowApp?new Color(.87f,.87f,.86f):Color.white;
            taskWindowButton.colors=colors;
        }
        void BuildTaskbar(Transform parent)
        {
            var dock=Block("Taskbar",parent,Slate,false); Bevel(dock);
            Rect(dock,new Vector2(0,0),new Vector2(1,0),Vector2.zero,new Vector2(0,38));
            var home=SmallButton("DesktopHome",dock.transform,"Home","Masaüstü",()=>Show("Desktop")); Place(home.gameObject,5,5,112,28);
            clockText=Label("Clock",dock.transform,"",14,Soft,TextAnchor.MiddleRight);
            Rect(clockText.gameObject,new Vector2(1,0),Vector2.one,new Vector2(-220,2),new Vector2(-12,-2));
            var save=SmallButton("SaveCompany",dock.transform,"Save","Kaydet",()=>{Runtime.Save(); Toast("Şirket ilerlemesi kaydedildi.");});
            Place(save.gameObject,126,5,98,28);
            var exit=SmallButton("ExitComputer",dock.transform,"Power","Çıkış",Close);
            Place(exit.gameObject,232,5,86,28);
            taskWindowButton=SmallButton("TaskApplication",dock.transform,"JobDesk","Uygulama",ToggleTaskWindow);
            Place(taskWindowButton.gameObject,330,5,210,28);
            taskWindowText=taskWindowButton.GetComponentInChildren<Text>(); taskWindowIcon=taskWindowButton.GetComponentInChildren<DesktopGlyph>();
            taskWindowButton.gameObject.SetActive(false);
        }
        void RefreshDesktop()
        {
            mailBadge.text=Runtime.Data.inspectionPending?"•":"";
        }
        void StatStrip(params (string label,string value)[] stats)
        {
            var row=Block("StatusLine",mainContent,Slate); Height(row,30);
            var l=row.AddComponent<HorizontalLayoutGroup>(); l.spacing=18;
            l.padding=new RectOffset(10,10,0,0); l.childForceExpandWidth=true;
            foreach(var stat in stats)
            {
                var text=Label("Status",row.transform,stat.label+": "+stat.value,13,Paper);
                var element=text.gameObject.AddComponent<LayoutElement>(); element.flexibleWidth=1;
                element.minWidth=0; element.preferredWidth=0;
            }
        }
        void JobColumns(out RectTransform list,out RectTransform detail)
        {
            var row=Obj("JobWorkspace",mainContent);
            Height(row,Mathf.Max(320,appScroll.viewport.rect.height-84));
            var horizontal=row.AddComponent<HorizontalLayoutGroup>(); horizontal.spacing=24;
            horizontal.childControlWidth=true; horizontal.childControlHeight=true;
            horizontal.childForceExpandWidth=false; horizontal.childForceExpandHeight=true;
            var left=Obj("JobList",row.transform); var right=Block("JobDetail",row.transform,new Color(.97f,.97f,.95f));
            var leftSize=left.AddComponent<LayoutElement>(); leftSize.flexibleWidth=.38f; leftSize.preferredWidth=0; leftSize.minWidth=0;
            var rightSize=right.AddComponent<LayoutElement>(); rightSize.flexibleWidth=.62f; rightSize.preferredWidth=0; rightSize.minWidth=0;
            list=ScrollColumn("JobListScroll",left.transform,0,0);
            detail=ScrollColumn("JobDetailScroll",right.transform,102,20);
            var footer=Obj("JobActions",right.transform);
            Rect(footer,Vector2.zero,new Vector2(1,0),new Vector2(20,12),new Vector2(-20,88));
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
            var track=Block("ScrollTrack",viewport.transform,new Color(.73f,.73f,.71f));
            Rect(track,new Vector2(1,0),Vector2.one,new Vector2(-6,10),new Vector2(-2,-10));
            var handle=Block("ScrollHandle",track.transform,Slate); Fill(handle);
            var bar=track.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop;
            bar.handleRect=handle.GetComponent<RectTransform>(); bar.targetGraphic=handle.GetComponent<Image>();
            scroll.verticalScrollbar=bar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            return rect;
        }
        void JobCard(JobDefinition job)
        {
            bool locked=Runtime.Data.reputation<job.minimumReputation;
            bool selected=selectedJob==job.id;
            var card=Block("Job_"+job.id,mainContent,selected?new Color(.15f,.26f,.44f):new Color(.94f,.94f,.92f)); Height(card,52);
            Bind(card,()=>{selectedJob=job.id; Show("JobDesk");},false);
            var heading=Label("JobTitle",card.transform,job.title,15,selected?Color.white:Paper); heading.font=headingFont;
            Rect(heading.gameObject,new Vector2(0,1),Vector2.one,new Vector2(12,-28),new Vector2(-12,-3));
            var meta=Label("JobMeta",card.transform,locked?job.minimumReputation+" itibar gerekli":Money(job.payment)+"  •  "+job.durationMinutes+" dk",13,selected?Color.white:locked?Peach:Soft);
            Rect(meta.gameObject,Vector2.zero,new Vector2(1,0),new Vector2(12,3),new Vector2(-12,23));
        }
    }
}
