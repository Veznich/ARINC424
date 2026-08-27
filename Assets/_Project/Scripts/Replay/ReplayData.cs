using System;
using System.Collections.Generic;

namespace Arkanoid.Replay
{
    [Serializable]
    public sealed class ReplayFrameDto
    {
        public float t;
        public float moveAxis;
        public float targetWorldX;
        public bool hasPointer;
        public bool pointerPressed;
        public bool pointerInControlZone;
        public bool launchRequested;
    }

    [Serializable]
    public sealed class ReplayData
    {
        public int version = 1;
        public string id;
        public string createdUtc;
        public int levelNumber;
        public int seed;
        public string archetype;
        public float duration;
        public bool cleared;
        public List<ReplayFrameDto> frames = new List<ReplayFrameDto>(512);

        public static ReplayData CreateNew(int level, int seed, string archetype)
        {
            return new ReplayData
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 12),
                createdUtc = DateTime.UtcNow.ToString("o"),
                levelNumber = level,
                seed = seed,
                archetype = archetype ?? "",
                frames = new List<ReplayFrameDto>(512)
            };
        }
    }

    [Serializable]
    public sealed class ReplayIndex
    {
        public List<string> ids = new List<string>(10);
    }
}
