namespace CompanySim
{
    public sealed class LiftButton : InteractableBehaviour
    {
        public GarageLift lift;
        public int direction=1;
        public override string Prompt(PlayerInteractor player) =>
            lift.IsBlocked?"E — Lifti durdur · Alanı boşaltın":
            lift.IsMoving?"E — Lifti durdur":direction>0 && lift.Height>=lift.maximumHeight-.005f?"Lift üst konumda":direction<0 && lift.IsLowered?"Lift zeminde":direction>0?"E — Lifti yükselt":"E — Lifti indir";
        public override void Interact(PlayerInteractor player)
        { if(lift.IsMoving) lift.Stop(); else lift.RequestMotion(direction,player); }
    }
}
