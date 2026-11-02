using UnityEngine;

namespace CompanySim
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioFeedback : MonoBehaviour
    {
        public AudioClip uiClick, notification, garageMotor, itemDrop, footstep, doorOpen, doorClose, drawerSlide;
        AudioSource source;
        void Awake() { source=GetComponent<AudioSource>(); source.spatialBlend=0; source.volume=.35f; source.ignoreListenerPause=true; }
        public void Click() { if (uiClick) source.PlayOneShot(uiClick); }
        public void Notice() { if (notification) source.PlayOneShot(notification); }
        public void Motor() { if (garageMotor) source.PlayOneShot(garageMotor); }
        public void Drop() { if (itemDrop) source.PlayOneShot(itemDrop); }
        public void Step() { if(footstep) source.PlayOneShot(footstep,.38f); }
        public void Drawer(Vector3 position) { if(drawerSlide) AudioSource.PlayClipAtPoint(drawerSlide,position,.12f); }
        public void Door(bool opening,Vector3 position)
        {
            var clip=opening?doorOpen:doorClose;
            if(clip) AudioSource.PlayClipAtPoint(clip,position,.2f);
        }
        public void Impact(Vector3 position,float strength)
        {
            if(itemDrop) AudioSource.PlayClipAtPoint(itemDrop,position,Mathf.Lerp(.06f,.25f,strength));
        }
    }
}
