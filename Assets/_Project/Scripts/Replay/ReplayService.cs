using System;
using Arkanoid.Core;
using Arkanoid.Input;
using Arkanoid.Utils;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.Replay
{
    /// <summary>Запись / воспроизведение / export replay (seed + лента ввода).</summary>
    public sealed class ReplayService : IStartable, ITickable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IGameStateMachine _stateMachine;
        private readonly ReplayStore _store = new ReplayStore();

        private GameplayInputRouter _router;
        private IDisposable _startedSub;
        private IDisposable _completedSub;
        private IDisposable _gameOverSub;

        private ReplayData _recording;
        private float _recordStartTime;
        private bool _isRecording;

        private ReplayData _playing;
        private float _playElapsed;
        private int _playIndex;
        private bool _pendingPlayStart;
        private int _pendingPlayLevel;

        public bool IsPlaying => _router != null && _router.IsPlayback;
        public string StorePath => _store.DirectoryPath;

        public ReplayService(IEventBus eventBus, IGameStateMachine stateMachine)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
        }

        public void Bind(GameplayInputRouter router)
        {
            _router = router;
        }

        public void Start()
        {
            if (_eventBus == null)
            {
                return;
            }

            _startedSub = _eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            _completedSub = _eventBus.Subscribe<LevelCompletedEvent>(e => FinalizeRecording(cleared: true));
            _gameOverSub = _eventBus.Subscribe<RequestGameOverEvent>(_ => FinalizeRecording(cleared: false));
        }

        public void Dispose()
        {
            _startedSub?.Dispose();
            _completedSub?.Dispose();
            _gameOverSub?.Dispose();
            StopPlaybackInternal(clearSeed: true);
        }

        public void Tick()
        {
            if (_pendingPlayStart)
            {
                _pendingPlayStart = false;
                _eventBus?.Publish(new RequestGameplayEvent(_pendingPlayLevel));
            }
        }

        /// <summary>Вызывать из LateUpdate роутера — после сэмпла live-ввода.</summary>
        public void OnLateUpdate()
        {
            if (_router == null || _stateMachine == null ||
                _stateMachine.CurrentState != GameState.Gameplay)
            {
                return;
            }

            if (IsPlaying)
            {
                AdvancePlayback(Time.deltaTime);
                return;
            }

            if (_isRecording && _recording != null)
            {
                AppendFrame();
            }
        }

        public bool TryPlayLatest()
        {
            var data = _store.LoadLatest();
            if (data == null || data.frames == null || data.frames.Count == 0)
            {
                Debug.LogWarning("[Replay] Нет сохранённых replay.");
                return false;
            }

            return StartPlayback(data);
        }

        public string ExportLatest()
        {
            return _store.ExportLatest();
        }

        private bool StartPlayback(ReplayData data)
        {
            if (_router == null || data == null)
            {
                return false;
            }

            FinalizeRecording(cleared: false); // сбросить незавершённую запись
            _isRecording = false;
            _recording = null;

            _playing = data;
            _playElapsed = 0f;
            _playIndex = 0;
            SeedGenerator.SetManualOverride(data.seed);
            _router.BeginPlayback();
            ApplyFrame(data.frames[0]);

            _pendingPlayLevel = Mathf.Max(1, data.levelNumber);
            _pendingPlayStart = true;
            _eventBus?.Publish(new ReplayPlaybackStartedEvent(data.id, data.levelNumber));
            Debug.Log($"[Replay] Playback {data.id} · L{data.levelNumber} · seed {data.seed}");
            return true;
        }

        private void AdvancePlayback(float dt)
        {
            if (_playing == null || _playing.frames == null || _playing.frames.Count == 0)
            {
                StopPlaybackInternal(clearSeed: true);
                return;
            }

            _playElapsed += dt;
            var frames = _playing.frames;
            while (_playIndex + 1 < frames.Count && frames[_playIndex + 1].t <= _playElapsed)
            {
                _playIndex++;
            }

            ApplyFrame(frames[_playIndex]);

            if (_playIndex >= frames.Count - 1 && _playElapsed > frames[frames.Count - 1].t + 0.5f)
            {
                StopPlaybackInternal(clearSeed: true);
            }
        }

        private void ApplyFrame(ReplayFrameDto dto)
        {
            if (_router == null || dto == null)
            {
                return;
            }

            _router.SetPlaybackFrame(new GameplayInputFrame(
                dto.moveAxis,
                dto.targetWorldX,
                dto.hasPointer,
                dto.pointerPressed,
                dto.pointerInControlZone,
                dto.launchRequested,
                Vector2.zero,
                Vector2.zero));
        }

        private void StopPlaybackInternal(bool clearSeed)
        {
            if (_router != null && _router.IsPlayback)
            {
                _router.EndPlayback();
                _eventBus?.Publish(new ReplayPlaybackEndedEvent());
            }

            _playing = null;
            _playIndex = 0;
            _playElapsed = 0f;
            if (clearSeed)
            {
                SeedGenerator.SetManualOverride(null);
            }
        }

        private void OnLevelStarted(LevelStartedEvent e)
        {
            if (IsPlaying)
            {
                _playElapsed = 0f;
                _playIndex = 0;
                if (_playing != null && _playing.frames.Count > 0)
                {
                    ApplyFrame(_playing.frames[0]);
                }

                return;
            }

            _recording = ReplayData.CreateNew(e.LevelNumber, e.Seed, e.Archetype.ToString());
            _recordStartTime = Time.time;
            _isRecording = true;
        }

        private void AppendFrame()
        {
            if (_router == null || _recording == null)
            {
                return;
            }

            var f = _router.Current;
            _recording.frames.Add(new ReplayFrameDto
            {
                t = Time.time - _recordStartTime,
                moveAxis = f.MoveAxis,
                targetWorldX = f.TargetWorldX,
                hasPointer = f.HasPointer,
                pointerPressed = f.PointerPressed,
                pointerInControlZone = f.PointerInControlZone,
                launchRequested = f.LaunchRequested
            });
        }

        private void FinalizeRecording(bool cleared)
        {
            if (!_isRecording || _recording == null || _recording.frames.Count == 0)
            {
                _isRecording = false;
                _recording = null;
                if (IsPlaying && cleared)
                {
                    StopPlaybackInternal(clearSeed: true);
                }

                return;
            }

            if (IsPlaying)
            {
                _isRecording = false;
                _recording = null;
                return;
            }

            _recording.cleared = cleared;
            _recording.duration = _recording.frames[_recording.frames.Count - 1].t;
            _store.Save(_recording);
            _eventBus?.Publish(new ReplaySavedEvent(_recording.id, _recording.levelNumber, _recording.frames.Count));
            _isRecording = false;
            _recording = null;
        }
    }
}
