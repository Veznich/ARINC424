using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>Доп. мяч для Multi Ball.</summary>
    public sealed class ExtraBall : MonoBehaviour
    {
        private BlockField _blocks;
        private PlayfieldBounds _bounds;
        private PaddleController _paddle;
        private float _halfPaddleH;
        private Vector3 _velocity;
        private float _speed;
        private float _radius = 0.22f;
        private bool _alive;
        private bool _fireball;
        private int _pierceLeft;
        private bool _magnetDocked;
        private float _dockOffsetX;
        private const float DockOffsetY = 0.55f;

        public bool IsAlive => _alive;
        public bool IsMagnetDocked => _alive && _magnetDocked;

        public void Launch(
            Vector3 position,
            Vector3 direction,
            float speed,
            BlockField blocks,
            PlayfieldBounds bounds,
            PaddleController paddle,
            float paddleHalfHeight)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            _speed = speed;
            _velocity = direction.sqrMagnitude > 0.001f
                ? direction.normalized * speed
                : Vector3.up * speed;
            _blocks = blocks;
            _bounds = bounds;
            _paddle = paddle;
            _halfPaddleH = paddleHalfHeight;
            _alive = true;
            _fireball = false;
            _pierceLeft = 0;
            _magnetDocked = false;
            _dockOffsetX = 0f;
            gameObject.SetActive(true);
        }

        public void SetFireball(bool on, int pierce)
        {
            _fireball = on;
            _pierceLeft = pierce;
        }

        public void Kill()
        {
            _alive = false;
            _magnetDocked = false;
            gameObject.SetActive(false);
        }

        public void LaunchFromMagnetDock(Vector3 direction)
        {
            if (!_alive || !_magnetDocked)
            {
                return;
            }

            _magnetDocked = false;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.up;
            }

            _velocity = direction.normalized * _speed;
        }

        public void Tick(float dt, bool magnetActive = false)
        {
            if (!_alive)
            {
                return;
            }

            if (_magnetDocked)
            {
                FollowPaddle();
                return;
            }

            var pos = transform.position;
            pos += _velocity * dt;
            pos.z = 0f;

            if (_bounds != null)
            {
                if (pos.x - _radius < _bounds.MinX)
                {
                    pos.x = _bounds.MinX + _radius;
                    _velocity.x = Mathf.Abs(_velocity.x);
                }
                else if (pos.x + _radius > _bounds.MaxX)
                {
                    pos.x = _bounds.MaxX - _radius;
                    _velocity.x = -Mathf.Abs(_velocity.x);
                }

                if (pos.y + _radius > _bounds.MaxY)
                {
                    pos.y = _bounds.MaxY - _radius;
                    _velocity.y = -Mathf.Abs(_velocity.y);
                }

                if (pos.y - _radius < _bounds.MinY)
                {
                    Kill();
                    return;
                }
            }

            if (_blocks != null)
            {
                var vel = _velocity;
                var pierce = _pierceLeft;
                if (_blocks.ResolveBall(ref pos, ref vel, _radius, out _, _fireball, pierce))
                {
                    if (_fireball && pierce > 0)
                    {
                        _pierceLeft = Mathf.Max(0, pierce - 1);
                    }
                    else
                    {
                        _velocity = vel.normalized * _speed;
                    }
                }
            }

            if (_paddle != null && _velocity.y < 0f)
            {
                var pp = _paddle.Position;
                var top = pp.y + _halfPaddleH;
                var halfW = _paddle.HalfWidth;
                if (Mathf.Abs(pos.x - pp.x) <= halfW + _radius &&
                    pos.y - _radius <= top &&
                    pos.y + _radius >= pp.y - _halfPaddleH)
                {
                    if (magnetActive)
                    {
                        _dockOffsetX = Mathf.Clamp(pos.x - pp.x, -halfW, halfW);
                        _magnetDocked = true;
                        _velocity = Vector3.zero;
                        FollowPaddle();
                        return;
                    }

                    pos.y = top + _radius + 0.01f;
                    var factor = (pos.x - pp.x) / Mathf.Max(0.01f, halfW);
                    _velocity = Quaternion.Euler(0f, 0f, -factor * 50f) * Vector3.up * _speed;
                }
            }

            transform.position = pos;
        }

        private void FollowPaddle()
        {
            if (_paddle == null)
            {
                return;
            }

            var p = _paddle.Position;
            transform.position = new Vector3(p.x + _dockOffsetX, p.y + DockOffsetY, 0f);
        }
    }
}
