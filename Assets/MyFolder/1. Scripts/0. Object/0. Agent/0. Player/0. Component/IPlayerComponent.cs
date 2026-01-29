using FishNet.Example.Scened;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public interface IPlayerComponent
    {
        public void Start(PlayerContext context);
        public void Stop();
        public void SetKeyEvent(PlayerInputControll inputControll);
        public void KeyEnter();
        public void KeyPress();
        public void KeyExit();
    }
}