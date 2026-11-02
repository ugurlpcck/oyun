using System;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySim
{
    public sealed partial class ComputerUI
    {
        public const string VolumePreference="Company.Player.MasterVolume";
        public const string SensitivityPreference="Company.Player.LookSensitivity";
        public const string SwayPreference="Company.Player.EquipmentSway";
        GameObject pausePanel;
        Slider volumeSlider,sensitivitySlider;
        Toggle swayToggle;
        Text volumeValue,sensitivityValue;
        float previousTimeScale=1;
        bool previousAudioPause,previousControlLock,pauseActive;
        public bool IsPaused => pauseActive;

        void BuildPauseMenu(Transform root)
        {
            pausePanel=Block("PauseMenu",root,new Color(.018f,.022f,.024f,.72f)); Fill(pausePanel);
            var sheet=Block("PauseSheet",pausePanel.transform,new Color(.085f,.093f,.095f,.98f));
            var rect=Rect(sheet,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            rect.sizeDelta=new Vector2(430,586); HudEdge(sheet);
            var title=Label("PauseTitle",sheet.transform,"Duraklatıldı",28,HudPaper); title.font=headingFont;
            Place(title.gameObject,30,26,340,46); KeyCap(sheet.transform,"ESC",349,34,51);
            PauseButton(sheet.transform,"ResumeGame","Devam et",90,()=>SetPaused(false));
            PauseButton(sheet.transform,"PauseSave","Oyunu kaydet",141,()=>{if(Runtime.Save()) Toast("Oyun kaydedildi.");});
            var divider=Block("SettingsDivider",sheet.transform,new Color(1,1,1,.10f)); Place(divider,30,210,370,1);
            Place(Label("OptionsHeading",sheet.transform,"AYARLAR",12,HudMuted).gameObject,30,224,370,25);
            volumeSlider=PauseSlider(sheet.transform,"MasterVolume","Ses",265,0,1,out volumeValue);
            volumeSlider.onValueChanged.AddListener(value=>
            {
                AudioListener.volume=value; volumeValue.text=Mathf.RoundToInt(value*100)+"%";
                PlayerPrefs.SetFloat(VolumePreference,value);
            });
            sensitivitySlider=PauseSlider(sheet.transform,"LookSensitivity","Fare hassasiyeti",343,.04f,.25f,out sensitivityValue);
            sensitivitySlider.onValueChanged.AddListener(value=>
            {
                player.lookSensitivity=value; sensitivityValue.text=Mathf.RoundToInt(value/.11f*100)+"%";
                PlayerPrefs.SetFloat(SensitivityPreference,value);
            });
            var toggle=Block("EquipmentSway",sheet.transform,new Color(1,1,1,.04f)); Place(toggle,30,430,370,42);
            var box=Block("ToggleBox",toggle.transform,new Color(1,1,1,.13f)); Place(box,13,11,20,20);
            var check=Icon(box.transform,"Check",HudReady); Fill(check.gameObject,2);
            swayToggle=toggle.AddComponent<Toggle>(); swayToggle.targetGraphic=toggle.GetComponent<Image>(); swayToggle.graphic=check;
            var nav=swayToggle.navigation; nav.mode=Navigation.Mode.None; swayToggle.navigation=nav;
            swayToggle.onValueChanged.AddListener(value=>{player.carrySway=value; PlayerPrefs.SetInt(SwayPreference,value?1:0); Click();});
            Place(Label("SwayLabel",toggle.transform,"Elde eşya salınımı",15,HudPaper).gameObject,46,0,308,42);
            var controls=Label("QuickControls",sheet.transform,"WASD  Yürü     E  Kullan     F  Bırak\nR  Döndür     TAB  Hazırlık",13,HudMuted);
            Place(controls.gameObject,30,494,370,59);
            pausePanel.SetActive(false);
        }
        void PauseButton(Transform parent,string name,string text,float y,Action action)
        {
            var go=Block(name,parent,new Color(.17f,.18f,.18f)); Place(go,30,y,370,43);
            Bind(go,action,false);
            var label=Label("Caption",go.transform,text,16,HudPaper); Place(label.gameObject,15,0,340,43);
        }
        Slider PauseSlider(Transform parent,string name,string text,float y,float min,float max,out Text value)
        {
            var go=Obj(name,parent); Place(go,30,y,370,60);
            Place(Label("Caption",go.transform,text,15,HudPaper).gameObject,0,0,290,24);
            value=Label("Value",go.transform,"",14,HudMuted,TextAnchor.MiddleRight); Place(value.gameObject,294,0,76,24);
            var hit=Block("SliderHit",go.transform,Color.clear); Place(hit,0,29,370,30);
            var track=Block("Track",hit.transform,new Color(1,1,1,.13f));
            Rect(track,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,-2),new Vector2(0,2));
            var fill=Block("Fill",track.transform,new Color(.60f,.66f,.60f)); Fill(fill);
            var handleArea=Obj("HandleArea",hit.transform); Fill(handleArea,5);
            var handle=Block("Handle",handleArea.transform,HudPaper);
            var handleRect=Rect(handle,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); handleRect.sizeDelta=new Vector2(10,0);
            var slider=hit.AddComponent<Slider>(); slider.minValue=min; slider.maxValue=max;
            slider.fillRect=fill.GetComponent<RectTransform>(); slider.handleRect=handleRect; slider.targetGraphic=handle.GetComponent<Image>();
            var nav=slider.navigation; nav.mode=Navigation.Mode.None; slider.navigation=nav;
            return slider;
        }
        void LoadPlayerPreferences()
        {
            float volume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePreference,AudioListener.volume));
            float sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityPreference,player.lookSensitivity),.04f,.25f);
            bool sway=PlayerPrefs.GetInt(SwayPreference,player.carrySway?1:0)!=0;
            AudioListener.volume=volume; player.lookSensitivity=sensitivity; player.carrySway=sway;
            volumeSlider.SetValueWithoutNotify(volume); sensitivitySlider.SetValueWithoutNotify(sensitivity); swayToggle.SetIsOnWithoutNotify(sway);
            volumeValue.text=Mathf.RoundToInt(volume*100)+"%"; sensitivityValue.text=Mathf.RoundToInt(sensitivity/.11f*100)+"%";
        }
        public void SetPaused(bool paused)
        {
            if(paused==pauseActive || (paused && (!pausePanel || IsOpen))) return;
            pauseActive=paused;
            if(paused)
            {
                previousTimeScale=Time.timeScale; previousAudioPause=AudioListener.pause; previousControlLock=player.InputLocked;
                pausePanel.SetActive(true); hud.SetActive(false); pausePanel.transform.SetAsLastSibling();
                Time.timeScale=0; AudioListener.pause=true; player.SetCursor(true);
            }
            else
            {
                Time.timeScale=previousTimeScale; AudioListener.pause=previousAudioPause;
                if(pausePanel) pausePanel.SetActive(false); if(hud) hud.SetActive(!IsOpen); if(player) player.SetCursor(previousControlLock);
                PlayerPrefs.Save();
            }
        }
        void OnDisable() { if(IsPaused) SetPaused(false); }
    }
}
