using UnityEngine;
using UnityEngine.UI;

namespace CompanySim
{
    // Resolution-independent line icons, drawn in the same visual language as the desktop.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DesktopGlyph : MaskableGraphic
    {
        public string kind;
        public bool shortcut;
        VertexHelper mesh;
        Vector2 origin,size;
        Vector2 P(float x,float y) => origin+Vector2.Scale(size,new Vector2(x,y));
        void Line(float x,float y,float xx,float yy,float thickness=.055f)
        {
            Vector2 a=P(x,y), b=P(xx,yy), n=(b-a).normalized;
            n=new Vector2(-n.y,n.x)*Mathf.Min(size.x,size.y)*thickness*.5f;
            int i=mesh.currentVertCount;
            mesh.AddVert(a+n,color,Vector2.zero); mesh.AddVert(b+n,color,Vector2.zero);
            mesh.AddVert(b-n,color,Vector2.zero); mesh.AddVert(a-n,color,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
        void Box(float x,float y,float w,float h)
        { Line(x,y,x+w,y); Line(x+w,y,x+w,y+h); Line(x+w,y+h,x,y+h); Line(x,y+h,x,y); }
        void Circle(float x,float y,float r)
        {
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                Line(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,.05f);
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh; vh.Clear(); var r=GetPixelAdjustedRect(); origin=r.min; size=r.size;
            if(OfficeSymbols.Draw(vh,r,kind,shortcut)) return;
            switch(kind)
            {
                case "JobDesk":
                    Box(.17f,.16f,.66f,.65f); Box(.34f,.75f,.32f,.15f);
                    Line(.30f,.59f,.41f,.59f); Line(.52f,.59f,.71f,.59f); Line(.30f,.41f,.41f,.41f); Line(.52f,.41f,.71f,.41f); break;
                case "FleetManager":
                    Box(.10f,.30f,.49f,.45f); Line(.59f,.30f,.90f,.30f); Line(.9f,.30f,.9f,.55f);
                    Line(.9f,.55f,.74f,.70f); Line(.74f,.70f,.59f,.70f);
                    Circle(.29f,.25f,.11f); Circle(.75f,.25f,.11f); break;
                case "StockManager":
                    Box(.17f,.15f,.66f,.68f); Line(.17f,.65f,.83f,.65f); Line(.42f,.65f,.42f,.40f);
                    Line(.58f,.65f,.58f,.40f); Line(.42f,.40f,.58f,.40f); break;
                case "EquipmentShop":
                    Box(.20f,.16f,.60f,.56f); Line(.33f,.72f,.33f,.83f); Line(.33f,.83f,.67f,.83f);
                    Line(.67f,.83f,.67f,.72f); Line(.38f,.45f,.62f,.45f); Line(.5f,.33f,.5f,.57f); break;
                case "CompanyBank":
                    Box(.13f,.20f,.74f,.56f); Line(.13f,.61f,.87f,.61f); Line(.25f,.35f,.44f,.35f); Line(.69f,.35f,.75f,.35f); break;
                case "Dispatch":
                    Circle(.23f,.23f,.10f); Circle(.76f,.76f,.1f);
                    Line(.23f,.33f,.23f,.63f); Line(.23f,.63f,.76f,.63f); Line(.76f,.63f,.76f,.66f);
                    Line(.64f,.44f,.76f,.56f); Line(.76f,.56f,.88f,.44f); break;
                case "Reports":
                    Box(.22f,.12f,.56f,.76f); Line(.35f,.68f,.65f,.68f); Line(.35f,.51f,.65f,.51f);
                    Line(.35f,.34f,.53f,.34f); break;
                case "BuildingManager":
                    Line(.13f,.46f,.5f,.83f); Line(.5f,.83f,.87f,.46f); Line(.23f,.54f,.23f,.16f);
                    Line(.23f,.16f,.77f,.16f); Line(.77f,.16f,.77f,.54f); Box(.43f,.16f,.14f,.25f); break;
                case "CompanyStats":
                    Line(.15f,.16f,.88f,.16f); Line(.15f,.16f,.15f,.87f);
                    Line(.32f,.28f,.32f,.47f,.10f); Line(.52f,.28f,.52f,.64f,.10f); Line(.73f,.28f,.73f,.83f,.10f); break;
                case "Safety":
                    Line(.5f,.90f,.82f,.77f); Line(.82f,.77f,.79f,.40f); Line(.79f,.40f,.5f,.12f);
                    Line(.5f,.12f,.21f,.40f); Line(.21f,.40f,.18f,.77f); Line(.18f,.77f,.5f,.9f);
                    Line(.34f,.52f,.46f,.40f); Line(.46f,.40f,.68f,.64f); break;
                case "MailBox":
                    Box(.12f,.23f,.76f,.54f); Line(.12f,.77f,.5f,.46f); Line(.5f,.46f,.88f,.77f); break;
                case "Home":
                    for(int i=0;i<2;i++) for(int j=0;j<2;j++) Box(.15f+i*.42f,.15f+j*.42f,.26f,.26f); break;
                case "Save":
                    Box(.18f,.14f,.64f,.72f); Box(.34f,.14f,.32f,.29f); Line(.35f,.85f,.35f,.64f);
                    Line(.35f,.64f,.67f,.64f); Line(.67f,.64f,.67f,.85f); break;
                case "Power":
                    for(int i=0;i<22;i++)
                    {
                        float a=(i*12+138)*Mathf.Deg2Rad,b=((i+1)*12+138)*Mathf.Deg2Rad;
                        Line(.5f+Mathf.Cos(a)*.34f,.5f+Mathf.Sin(a)*.34f,.5f+Mathf.Cos(b)*.34f,.5f+Mathf.Sin(b)*.34f);
                    }
                    Line(.5f,.55f,.5f,.94f); break;
                case "Close": Line(.25f,.25f,.75f,.75f); Line(.25f,.75f,.75f,.25f); break;
                case "Minimize": Line(.2f,.32f,.8f,.32f); break;
                case "Maximize": Box(.22f,.22f,.56f,.56f); break;
                case "Crosshair": Circle(.5f,.5f,.26f); break;
                case "Check": Line(.18f,.48f,.40f,.26f,.09f); Line(.40f,.26f,.83f,.77f,.09f); break;
                default: Line(.18f,.5f,.82f,.5f); Line(.58f,.74f,.82f,.5f); Line(.82f,.5f,.58f,.26f); break;
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DesktopWallpaper : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=GetPixelAdjustedRect();
            Color a=new Color(.022f,.035f,.043f),b=new Color(.055f,.115f,.13f);
            vh.AddVert(new Vector2(r.xMin,r.yMin),a,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),a,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),b,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),a,Vector2.zero);
            vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3);
            for(int ring=0;ring<5;ring++) for(int i=0;i<100;i++)
            {
                float rad=r.height*(.49f+ring*.11f),x=r.xMin+r.width*.94f,y=r.yMin+r.height*.60f;
                float angle=i*Mathf.PI/50,next=(i+1)*Mathf.PI/50;
                var color=new Color(.3f,.6f,.57f,.035f);
                int start=vh.currentVertCount;
                vh.AddVert(new Vector2(x+Mathf.Cos(angle)*rad,y+Mathf.Sin(angle)*rad),color,Vector2.zero);
                vh.AddVert(new Vector2(x+Mathf.Cos(next)*rad,y+Mathf.Sin(next)*rad),color,Vector2.zero);
                vh.AddVert(new Vector2(x+Mathf.Cos(next)*(rad+1),y+Mathf.Sin(next)*(rad+1)),color,Vector2.zero);
                vh.AddVert(new Vector2(x+Mathf.Cos(angle)*(rad+1),y+Mathf.Sin(angle)*(rad+1)),color,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
