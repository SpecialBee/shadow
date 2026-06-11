using UnityEngine;

namespace ShadowSeller.Core
{
    // NPC AI 상태 머신 (FSM).
    //   Guard   : Idle→Patrol→Suspicious→Alert→Chase→Search
    //   Civilian: 전역 의심도 4구간에 따라 반응 (무시/말풍선/추격/빠른추격)
    //
    //   [고도화 시스템]
    //   A. 정보 전파: Guard Chase 시 주변 Guard에 lastKnown 공유, Civilian Chase 시 주변 Civilian 티어 상승
    //   B. 복합 승수: 동시에 보는 Civilian 수에 비례해 의심도 상승률 증가
    //   D. 전역 경계 레벨: AlertManager.Level에 따라 속도/시야/반응 임계 변화
    public class NPCController : MonoBehaviour, ITickable
    {
        public TickPhase Phase => TickPhase.NpcAI;

        [Header("기본 설정")]
        [SerializeField] private NpcKindData data;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private LayerMask   obstacleLayer;

        [Header("NPC 종류")]
        [SerializeField] private NpcType npcType = NpcType.Guard;

        [Header("일반 NPC — 의심도 구간 경계")]
        [SerializeField] private float civilianTier2At  = 25f;
        [SerializeField] private float civilianTier3At  = 50f;
        [SerializeField] private float civilianTier4At  = 75f;
        [SerializeField] private float civilianFastMult = 1.6f;
        [SerializeField] private float civilianSightRate = 3f;

        [Header("정보 전파")]
        [SerializeField] private float alertBroadcastRadius = 5f;

        public NpcState    CurrentState   { get; private set; } = NpcState.Idle;
        public bool        IsSeeingPlayer { get; private set; }
        public Vector2     FacingDir      => _facingDir;
        public NpcType     NpcKind        => npcType;
        public NpcKindData KindData       => data;

        private float   _suspicion;
        private float   _sightLoseTimer;
        private float   _searchTimer;
        private int     _patrolIndex;
        private int     _civilianTier;
        private bool    _wasSeeingPlayer;
        private float   _alertPointCooldown;  // Civilian이 AlertManager에 중복 기여 방지
        private Vector2 _facingDir = Vector2.right;
        private Vector2 _lastKnown;

        private Transform             _player;
        private PlayerExposureTracker _tracker;
        private Rigidbody2D           _rb;
        private SpeechBubble          _speechBubble;
        private GameObject            _watchIndicatorGo;
        private TMPro.TextMeshPro     _watchIndicatorTmp;

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

            var coneGo = new GameObject("VisionCone");
            coneGo.transform.SetParent(transform);
            coneGo.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            coneGo.AddComponent<VisionCone>();

            if (npcType == NpcType.Civilian)
                BuildWatchIndicator();
        }

        private void Start()
        {
            // Guard 초기 시선 방향: 첫 순찰 포인트 방향으로 설정
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                var dir = (Vector2)patrolPoints[0].position - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.01f) _facingDir = dir.normalized;
            }
        }

        private void OnDestroy()
        {
            if (npcType == NpcType.Guard)
            {
                _tracker?.UnregisterNpcThreat(this);
                _tracker?.UnregisterSoftThreat(this);
            }
            if (npcType == NpcType.Civilian && _wasSeeingPlayer)
                _tracker?.UnregisterCivilianWatch();
            GameLoopController.Instance?.Unregister(this);
        }

        public void Tick()
        {
            if (data == null || _player == null) return;
            IsSeeingPlayer = CanSeePlayer();
            if (_alertPointCooldown > 0f) _alertPointCooldown -= Time.deltaTime;

            if (npcType == NpcType.Civilian)
            {
                // 시야 상태 변화 시 Civilian watcher 등록/해제 (복합 승수 B용)
                if (IsSeeingPlayer != _wasSeeingPlayer)
                {
                    if (IsSeeingPlayer) _tracker?.RegisterCivilianWatch();
                    else                _tracker?.UnregisterCivilianWatch();
                    _wasSeeingPlayer = IsSeeingPlayer;
                }
                TickCivilian();
                return;
            }

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

        // ── Civilian 감시 아이콘 (tier 1 무반응 구간에서만 표시) ──────────────

        private void BuildWatchIndicator()
        {
            _watchIndicatorGo = new GameObject("_WatchIndicator");
            _watchIndicatorGo.transform.SetParent(transform);
            _watchIndicatorGo.transform.localPosition = new Vector3(0.35f, 1.1f, -0.1f);

            _watchIndicatorTmp = _watchIndicatorGo.AddComponent<TMPro.TextMeshPro>();
            _watchIndicatorTmp.text      = "◉";
            _watchIndicatorTmp.fontSize  = 1.8f;
            _watchIndicatorTmp.color     = new Color(0.85f, 0.85f, 0.85f, 0.75f);
            _watchIndicatorTmp.alignment = TMPro.TextAlignmentOptions.Center;
            _watchIndicatorTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            _watchIndicatorTmp.sortingOrder     = 10;

            _watchIndicatorGo.SetActive(false);
        }

        // ── Civilian ──────────────────────────────────────────────────────────

        private void TickCivilian()
        {
            if (CurrentState != NpcState.Patrol && CurrentState != NpcState.Idle && CurrentState != NpcState.Chase)
                TransitionTo(patrolPoints?.Length > 0 ? NpcState.Patrol : NpcState.Idle);

            if (CurrentState == NpcState.Idle && patrolPoints?.Length > 0)
                TransitionTo(NpcState.Patrol);

            if (CurrentState == NpcState.Chase)
            {
                float speed = _civilianTier >= 4
                    ? data.chaseSpeed * civilianFastMult * GetAlertSpeedMult()
                    : data.chaseSpeed * GetAlertSpeedMult();

                if (IsSeeingPlayer)
                {
                    _lastKnown      = _player.position;
                    _sightLoseTimer = 0f;
                    SuspicionManager.Instance?.AddSuspicion(civilianSightRate * GetCivilianSightRateMult() * Time.deltaTime);
                    int chaseNewTier = GetCivilianTier(SuspicionManager.Instance?.CurrentSuspicion ?? 0f);
                    if (chaseNewTier != _civilianTier) SetCivilianTier(chaseNewTier);
                }
                else
                {
                    _sightLoseTimer += Time.deltaTime;
                    if (_sightLoseTimer >= data.sightLoseDelay)
                    {
                        SetCivilianTier(0);
                        return;
                    }
                }
                MoveToward(_lastKnown, speed);
                return;
            }

            // 순찰 이동
            if (CurrentState == NpcState.Patrol && patrolPoints != null && patrolPoints.Length > 0)
            {
                var dest = (Vector2)patrolPoints[_patrolIndex].position;
                MoveToward(dest, data.patrolSpeed * GetAlertSpeedMult());
                if (Vector2.Distance(transform.position, dest) < 0.15f)
                    _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            }

            if (!IsSeeingPlayer)
            {
                if (_civilianTier != 0) SetCivilianTier(0);
                return;
            }

            _lastKnown = _player.position;
            UpdateFacing();

            SuspicionManager.Instance?.AddSuspicion(civilianSightRate * GetCivilianSightRateMult() * Time.deltaTime);

            int newTier = GetCivilianTier(SuspicionManager.Instance?.CurrentSuspicion ?? 0f);
            if (newTier != _civilianTier) SetCivilianTier(newTier);
        }

        private int GetCivilianTier(float suspicion)
        {
            float offset = GetCivilianTierOffset();
            if (suspicion < Mathf.Max(0f, civilianTier2At + offset)) return 1;
            if (suspicion < Mathf.Max(0f, civilianTier3At + offset)) return 2;
            if (suspicion < Mathf.Max(0f, civilianTier4At + offset)) return 3;
            return 4;
        }

        private void SetCivilianTier(int tier)
        {
            _civilianTier = tier;
            switch (tier)
            {
                case 0:
                    _speechBubble?.Hide();
                    _watchIndicatorGo?.SetActive(false);
                    if (CurrentState == NpcState.Chase)
                    {
                        _sightLoseTimer = 0f;
                        TransitionTo(patrolPoints?.Length > 0 ? NpcState.Patrol : NpcState.Idle);
                    }
                    break;
                case 1:
                    _speechBubble?.Hide();
                    _watchIndicatorGo?.SetActive(true);  // 말풍선 없이 조용히 감시 중
                    break;
                case 2:
                    _watchIndicatorGo?.SetActive(false);
                    _speechBubble?.Show("?");
                    break;
                case 3:
                    _watchIndicatorGo?.SetActive(false);
                    if (CurrentState != NpcState.Chase) { _sightLoseTimer = 0f; TransitionTo(NpcState.Chase); }
                    _speechBubble?.Show("뭐지?");
                    BroadcastAlert();
                    if (_alertPointCooldown <= 0f)
                    {
                        AlertManager.Instance?.RegisterCivilianAlert();
                        _alertPointCooldown = 30f;
                    }
                    break;
                case 4:
                    _watchIndicatorGo?.SetActive(false);
                    if (CurrentState != NpcState.Chase) { _sightLoseTimer = 0f; TransitionTo(NpcState.Chase); }
                    _speechBubble?.Show("그림자가 없다!");
                    BroadcastAlert();
                    if (_alertPointCooldown <= 0f)
                    {
                        AlertManager.Instance?.RegisterCivilianAlert();
                        _alertPointCooldown = 30f;
                    }
                    break;
            }
        }

        // ── Alert 전파 ────────────────────────────────────────────────────────

        private void BroadcastAlert()
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, alertBroadcastRadius);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                if (!col.TryGetComponent<NPCController>(out var npc)) continue;

                if (npcType == NpcType.Guard && npc.npcType == NpcType.Guard)
                    npc.ReceiveGuardAlert(_lastKnown);
                else if (npcType == NpcType.Civilian && npc.npcType == NpcType.Civilian)
                    npc.ReceiveCivilianAlert();
            }
        }

        public void ReceiveGuardAlert(Vector2 lastKnown)
        {
            if (CurrentState == NpcState.Chase || CurrentState == NpcState.Alert) return;
            _lastKnown = lastKnown;
            TransitionTo(NpcState.Suspicious);
            _speechBubble?.Show("어디야?!"); // 전파로 받은 경우 "음...?" 대신
        }

        public void ReceiveCivilianAlert()
        {
            if (!IsSeeingPlayer) return;
            int bumped = Mathf.Min(4, _civilianTier + 1);
            if (bumped != _civilianTier) SetCivilianTier(bumped);
        }

        // ── Guard state ticks ─────────────────────────────────────────────────

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
            MoveToward(dest, data.patrolSpeed * GetAlertSpeedMult());
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
            MoveToward(sees ? (Vector2)_player.position : _lastKnown, data.patrolSpeed * 1.4f * GetAlertSpeedMult());

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
                _lastKnown      = (Vector2)_player.position;
                _sightLoseTimer = 0f;
                MoveToward(_lastKnown, data.chaseSpeed * GetAlertSpeedMult());
            }
            else
            {
                _sightLoseTimer += Time.deltaTime;
                MoveToward(_lastKnown, data.chaseSpeed * GetAlertSpeedMult());
                if (_sightLoseTimer >= GetEffectiveSightLoseDelay()) TransitionTo(NpcState.Search);
            }
        }

        private void TickSearch()
        {
            MoveToward(_lastKnown, data.patrolSpeed * GetAlertSpeedMult());
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= GetEffectiveSearchDuration())
                TransitionTo(patrolPoints?.Length > 0 ? NpcState.Patrol : NpcState.Idle);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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

            float viewRange = data.viewRange * GetAlertViewRangeMult();
            if (dist > viewRange) return false;
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

            NpcState prev = CurrentState;

            if (npcType == NpcType.Guard)
            {
                bool wasSuspicious  = prev == NpcState.Suspicious;
                bool willSuspicious = next == NpcState.Suspicious;
                bool wasThreating   = prev == NpcState.Alert || prev == NpcState.Chase;
                bool willThreaten   = next == NpcState.Alert || next == NpcState.Chase;

                if (wasSuspicious && !willSuspicious)  _tracker?.UnregisterSoftThreat(this);
                if (!wasSuspicious && willSuspicious)   _tracker?.RegisterSoftThreat(this);
                if (wasThreating  && !willThreaten)     _tracker?.UnregisterNpcThreat(this);
                if (!wasThreating  && willThreaten)
                {
                    _tracker?.RegisterNpcThreat(this);
                    AlertManager.Instance?.RegisterGuardChase();
                    BroadcastAlert();
                }
            }

            CurrentState = next;

            if (next == NpcState.Chase)                           { _sightLoseTimer = 0f; }
            if (next == NpcState.Search)                          { _searchTimer    = 0f; }
            // 완전 초기화 대신 alertThreshold 30%를 잔류 — Guard가 이전 조우를 약하게 기억
            if (next == NpcState.Patrol || next == NpcState.Idle) { _suspicion = data != null ? data.alertThreshold * 0.3f : 0f; }

            if (npcType == NpcType.Guard && _speechBubble != null)
            {
                switch (next)
                {
                    case NpcState.Suspicious: _speechBubble.Show("음...?");        break;
                    case NpcState.Alert:      _speechBubble.Show("거기 누구야?!"); break;
                    case NpcState.Chase:      _speechBubble.Show("잡아라!");       break;
                    case NpcState.Search:     _speechBubble.Show("어디 갔지..."); break;
                    case NpcState.Patrol:
                    case NpcState.Idle:
                        if (prev == NpcState.Alert || prev == NpcState.Chase)
                            _speechBubble.Show("...착각이었나");
                        break;
                }
            }
        }

        // ── 충돌 기반 체포 ────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (CurrentState != NpcState.Chase) return;
            if (!col.gameObject.CompareTag("Player")) return;
            SuspicionManager.Instance?.TriggerArrest();
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (CurrentState != NpcState.Chase) return;
            if (!col.CompareTag("Player")) return;
            SuspicionManager.Instance?.TriggerArrest();
        }

        // ── 전역 경계 레벨 보조 프로퍼티 (D) ─────────────────────────────────

        private float GetAlertSpeedMult()
        {
            return (AlertManager.Instance?.Level ?? 1) switch
            {
                2 => 1.3f,
                3 => 1.3f,
                4 => 1.6f,
                _ => 1f,
            };
        }

        // Civilian만 시야 범위 확대
        private float GetAlertViewRangeMult()
        {
            if (npcType != NpcType.Civilian) return 1f;
            return (AlertManager.Instance?.Level ?? 1) >= 2 ? 1.2f : 1f;
        }

        // Guard만 포기 지연 감소 (레벨 3: 절반)
        private float GetEffectiveSightLoseDelay()
        {
            if (npcType != NpcType.Guard) return data.sightLoseDelay;
            return (AlertManager.Instance?.Level ?? 1) >= 3
                ? data.sightLoseDelay * 0.5f
                : data.sightLoseDelay;
        }

        // Guard만 수색 시간 증가 (레벨 3: 두 배)
        private float GetEffectiveSearchDuration()
        {
            if (npcType != NpcType.Guard) return data.searchDuration;
            return (AlertManager.Instance?.Level ?? 1) >= 3
                ? data.searchDuration * 2f
                : data.searchDuration;
        }

        // Civilian 티어 경계 낮춤 (레벨 3: -15)
        private float GetCivilianTierOffset()
        {
            return (AlertManager.Instance?.Level ?? 1) >= 3 ? -15f : 0f;
        }

        // Civilian 의심도 상승률 배율 (레벨 4: ×2, 복합 승수 스택)
        private float GetCivilianSightRateMult()
        {
            float alertMult = (AlertManager.Instance?.Level ?? 1) >= 4 ? 2f : 1f;
            float crowdMult = _tracker?.GetCrowdMultiplier() ?? 1f;
            return alertMult * crowdMult;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

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
