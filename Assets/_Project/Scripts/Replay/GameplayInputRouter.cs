using Arkanoid.Input;
using UnityEngine;

namespace Arkanoid.Replay
{
    /// <summary>Переключатель live-ввод ↔ playback для IGameplayInput.</summary>
    public sealed class GameplayInputRouter : MonoBehaviour, IGameplayInput
    {
        private GameplayInputReader _live;
        private ReplayService _replay;
        private bool _playback;
        private GameplayInputFrame _playbackFrame;

        public bool IsPlayback => _playback;
        public GameplayInputFrame Current =>
            _playback
                ? _playbackFrame
                : _live != null
                    ? _live.Current
                    : default;

        public void BindLive(GameplayInputReader live)
        {
            _live = live;
        }

        public void BindReplay(ReplayService replay)
        {
            _replay = replay;
        }

        public void SetCamera(Camera camera)
        {
            _live?.SetCamera(camera);
        }

        public void BeginPlayback()
        {
            _playback = true;
            _playbackFrame = default;
        }

        public void SetPlaybackFrame(GameplayInputFrame frame)
        {
            _playbackFrame = frame;
        }

        public void EndPlayback()
        {
            _playback = false;
            _playbackFrame = default;
        }

        private void LateUpdate()
        {
            _replay?.OnLateUpdate();
        }
    }
}
