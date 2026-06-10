using UnityEngine;

namespace ShadowSeller.Core
{
    // NPC AI 상태 머신 (FSM).
    //   상태 전이: Idle ↔ Patrol → Suspicious → Alert → Chase → Search → Patrol/Idle
    //   - 플레이어가 그림자 안에 있으면 CanSeePlayer() = false → 추격 중단
    //   - Alert/Chase 상태 진입 시 PlayerExposureTracker에 위협 등록, 이탈 시 해제
    //   - Chase 중 arrestTime 동안 근접 시 즉시 게임오버 (TriggerArrest)
    public class NPCController : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.NpcAI;

        [SerializeField] private NpcKindData   data;
        [SerializeField] private Transform[]   patrolPoints;
        [SerializeField] private LayerMask     obstacleLayer;

        public NpcState    CurrentState    { get; private set; } = NpcState.Idle;
        public bool        IsSeeingPlayer  { get; private set; }
        public Vector2     FacingDir       => _facingDir;
        public NpcKindData KindData        => data;

        private float   _suspicion;
        private float   _sightLoseTimer;
        private float   _arrestTimer;
        private float   _searchTimer;
        private int     _patrolIndex;
        private Vector2 _facingDir = Vector2.right;
        private Vector2 _lastKnown;

        private Transform             _player;
        private PlayerExposureTracker _tracker;
        private Rigidbody2D           _rb;
        private SpeechBubble          _speechBubble;

        private void Awake()
        {
            _rb           = GetComponent<Rigidbody2D>();
            _speechBubble = GetComponent<SpeechBubble>();
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _player  = playerGo.transform;
                _tracker = playerGo.GetComponent<PlayerExposureTracker>();
            }
            GameLoopController.Instance.Register(this);

            // 시야 시각화 자식 생성
            var coneGo = new UnityEngine.GameObject("VisionCone");
            coneGo.transform.SetParent(transform);
            coneGo.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            coneGo.AddComponent<VisionCone>();
        }

        private void OnDestroy()
        {
            _tracker?.UnregisterNpcThreat(this);
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            if (data == null || _player == null) return;
            IsSeeingPlayer = CanSeePlayer();

            switch (CurrentState)
            {
                case NpcState.Idle:       TickIdle();       break;
                case NpcState.Patrol:     TickPatrol();     break;
                case NpcState.Suspicious: TickSuspicious(); break;
                case NpcState.Alert:      TickAlert();      break;
                case NpcState.Chase:      TickChase();      break;
                case NpcState.Search:     TickSearch();     break;
            }
        }

        // ── State ticks ──────────────────────────────────────────────────────

        private void TickIdle()
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            { TransitionTo(NpcState.Patrol); return; }

            if (IsSeeingPlayer) GainSuspicion(allowImmediateChase: false);
            else                DecaySuspicion();
        }

        private void TickPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            { TransitionTo(NpcState.Idle); return; }

            var dest = (Vector2)patrolPoints[_patrolIndex].position;
            MoveToward(dest, data.patrolSpeed);
            if (Vector2.Distance(transform.position, dest) < 0.15f)
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;

            if (IsSeeingPlayer) GainSuspicion(allowImmediateChase: true);
            else                DecaySuspicion();
        }

        private void TickSuspicious()
        {
            if (IsSeeingPlayer)
            {
                UpdateFacing();
                _suspicion += data.suspicionGainRate * Time.deltaTime;
                if (_suspicion >= data.alertThreshold) TransitionTo(NpcState.Alert);
            }
            else
            {
                DecaySuspicion();
                if (_suspicion <= 0f)
                    TransitionTo(patrolPoints?.Length > 0 ? NpcState.Patrol : NpcState.Idle);
            }
        }

        private void TickAlert()
        {
            bool sees = IsSeeingPlayer;
            MoveToward(sees ? (Vector2)_player.position : _lastKnown, data.patrolSpeed * 1.4f);

            if (sees)
            {
                UpdateFacing();
                _suspicion += data.suspicionGainRate * 2f * Time.deltaTime;
                if (_suspicion >= data.chaseThreshold) TransitionTo(NpcState.Chase);
            }
            else
            {
                DecaySuspicion();
                if (_suspicion < data.alertThreshold * 0.4f) TransitionTo(NpcState.Search);
            }
        }

        private void TickChase()
        {
            bool sees = IsSeeingPlayer;
            if (sees)
            {
                UpdateFacing();
                _lastKnown = (Vector2)_player.position;
                _sightLoseTimer = 0f;
                _arrestTimer   += Time.deltaTime;
                MoveToward(_lastKnown, data.chaseSpeed);

                if (_arrestTimer >= data.arrestTime)
                    SuspicionManager.Instance?.TriggerArrest();
            }
            else
            {
                _sightLoseTimer += Time.deltaTime;
                MoveToward(_lastKnown, data.chaseSpeed);
                if (_sightLoseTimer >= data.sightLoseDelay) TransitionTo(NpcState.Search);
            }
        }

        private void TickSearch()
        {
            MoveToward(_lastKnown, data.patrolSpeed);
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= data.searchDuration)
                TransitionTo(patrolPoints?.Length > 0 ? NpcState.Patrol : NpcState.Idle);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void GainSuspicion(bool allowImmediateChase)
        {
            UpdateFacing();

            if (allowImmediateChase && IsCloseExposed())
            { TransitionTo(NpcState.Chase); return; }

            _suspicion += data.suspicionGainRate * Time.deltaTime;

            if (_suspicion >= data.chaseThreshold && allowImmediateChase)
                TransitionTo(NpcState.Chase);
            else if (_suspicion >= data.alertThreshold)
                TransitionTo(NpcState.Alert);
            else
                TransitionTo(NpcState.Suspicious);
        }

        private void DecaySuspicion()
        {
            _suspicion = Mathf.Max(0f, _suspicion - data.suspicionDecayRate * Time.deltaTime);
        }

        private bool CanSeePlayer()
        {
            if (_tracker != null && _tracker.IsInShadow) return false;

            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            float dist   = toPlayer.magnitude;

            if (dist > data.viewRange) return false;
            if (Vector2.Angle(_facingDir, toPlayer) > data.viewAngle * 0.5f) return false;

            if ((int)obstacleLayer != 0)
            {
                var hit = Physics2D.Raycast(transform.position, toPlayer.normalized, dist, obstacleLayer);
                if (hit.collider != null) return false;
            }

            _lastKnown = (Vector2)_player.position;
            return true;
        }

        private bool IsCloseExposed() =>
            Vector2.Distance(transform.position, _player.position) <= data.closeRange;

        private void MoveToward(Vector2 target, float speed)
        {
            var desired = target - (Vector2)transform.position;
            if (desired.sqrMagnitude < 0.01f) return;

            Vector2 dir = AvoidWalls(desired.normalized);

            var next = (Vector2)transform.position + dir * speed * Time.deltaTime;
            if (_rb != null) _rb.MovePosition(next);
            else              transform.position = next;

            if (dir.sqrMagnitude > 0.01f) _facingDir = dir.normalized;
        }

        private Vector2 AvoidWalls(Vector2 desired)
        {
            if ((int)obstacleLayer == 0) return desired;

            const float checkDist = 0.5f;
            const float radius    = 0.2f;

            if (!Physics2D.CircleCast(transform.position, radius, desired, checkDist, obstacleLayer))
                return desired;

            // 22.5° 단위로 좌우 교대 탐색 (최대 ±180°)
            for (int i = 1; i <= 8; i++)
            {
                float a = i * 22.5f;

                var left = RotateVec(desired, a);
                if (!Physics2D.CircleCast(transform.position, radius, left, checkDist, obstacleLayer))
                    return left;

                var right = RotateVec(desired, -a);
                if (!Physics2D.CircleCast(transform.position, radius, right, checkDist, obstacleLayer))
                    return right;
            }

            return desired;
        }

        private void UpdateFacing()
        {
            var dir = (Vector2)_player.position - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.01f) _facingDir = dir.normalized;
        }

        private void TransitionTo(NpcState next)
        {
            if (CurrentState == next) return;

            bool wasSuspicious = CurrentState == NpcState.Suspicious;
            bool willSuspicious = next        == NpcState.Suspicious;
            bool wasThreating  = CurrentState == NpcState.Alert || CurrentState == NpcState.Chase;
            bool willThreaten  = next         == NpcState.Alert || next         == NpcState.Chase;

            if (wasSuspicious && !willSuspicious) _tracker?.UnregisterSoftThreat(this);
            if (!wasSuspicious && willSuspicious)  _tracker?.RegisterSoftThreat(this);

            if (wasThreating && !willThreaten) _tracker?.UnregisterNpcThreat(this);
            if (!wasThreating && willThreaten)  _tracker?.RegisterNpcThreat(this);

            CurrentState = next;

            if (next == NpcState.Chase)                            { _sightLoseTimer = 0f; _arrestTimer = 0f; }
            if (next == NpcState.Search)                           { _searchTimer    = 0f; }
            if (next == NpcState.Patrol || next == NpcState.Idle)  { _suspicion      = 0f; }

            if (_speechBubble != null)
            {
                switch (next)
                {
                    case NpcState.Suspicious: _speechBubble.Show("음...?");         break;
                    case NpcState.Alert:      _speechBubble.Show("거기 누구야?!");  break;
                    case NpcState.Chase:      _speechBubble.Show("잡아라!");        break;
                    case NpcState.Search:     _speechBubble.Show("어디 갔지...");   break;
                    case NpcState.Patrol:
                    case NpcState.Idle:
                        if (wasThreating) _speechBubble.Show("...착각이었나");      break;
                }
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (data == null) return;

            Gizmos.color = CurrentState switch
            {
                NpcState.Chase      => Color.red,
                NpcState.Alert      => new Color(1f, 0.5f, 0f),
                NpcState.Suspicious => Color.yellow,
                _                   => new Color(1f, 1f, 1f, 0.4f),
            };

            float half  = data.viewAngle * 0.5f;
            var   left  = (Vector3)(RotateVec(_facingDir,  half) * data.viewRange);
            var   right = (Vector3)(RotateVec(_facingDir, -half) * data.viewRange);
            var   fwd   = (Vector3)(_facingDir * data.viewRange);

            Gizmos.DrawRay(transform.position, left);
            Gizmos.DrawRay(transform.position, right);
            Gizmos.DrawRay(transform.position, fwd);
        }

        private static Vector2 RotateVec(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
