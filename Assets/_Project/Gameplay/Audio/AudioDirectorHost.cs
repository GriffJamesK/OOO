using System.Collections.Generic;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Audio
{
    /// <summary>
    /// The runtime half of AudioDirector, on the hidden "[OOO Audio]" object: a small pool of 2D
    /// sources for one-shots, two looping sources that crossfade the area ambience, and one for
    /// the ending chord. It reacts to gameplay events and polls the player for footsteps and
    /// landings (no event exists for those). It never adds an AudioListener: the main camera owns
    /// the only one, and while none exists nothing plays, so Unity never warns about it.
    /// </summary>
    public sealed class AudioDirectorHost : MonoBehaviour
    {
        const float ListenerCheckInterval = 0.5f;

        AudioSource[] voices;
        int nextVoice;
        readonly AudioSource[] amb = new AudioSource[2];
        readonly float[] ambGain = new float[2];
        readonly float[] ambTarget = new float[2];
        int ambActive;
        string ambKey;
        AudioSource ending;

        PlayerController player;
        bool wasGrounded;
        float fallSpeed;
        float stepDistance;
        int stepVariant;

        bool paused;
        bool completed;
        float duck = 1f;

        AudioListener listener;
        bool listenerOk;
        float listenerCheckTimer;
        readonly Queue<string> prewarm = new Queue<string>();

        static AudioConfig Cfg => ConfigLookup.Get<AudioConfig>();

        /// <summary>Current ambience key ("" = the fallback room tone), or null before any ambience started.</summary>
        public string AmbienceKey => ambKey;
        public bool ListenerPresent => listenerOk;
        public bool Paused => paused;
        public float Duck => duck;
        public float AmbienceGain(int i) => ambGain[i];
        public bool HasPlayer => player != null;

        void Awake()
        {
            var cfg = Cfg;
            voices = new AudioSource[cfg.SfxVoices];
            for (int i = 0; i < voices.Length; i++) voices[i] = NewSource(loop: false);
            for (int i = 0; i < amb.Length; i++)
            {
                amb[i] = NewSource(loop: true);
                amb[i].ignoreListenerPause = true; // the ambience keeps breathing, ducked, behind the pause menu
            }
            ending = NewSource(loop: false);

            // One-shots are small: build them all now so the first jump never hitches.
            foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
                if (id != SoundId.Ending) AudioDirector.Clip(id);

            // Ambiences are larger: build the current one now, the rest one per frame.
            foreach (var key in ProcSynth.AmbienceIds) prewarm.Enqueue(key);
            listenerOk = FindListener();
            SetAmbience(AreaZone.Current);

            if (player == null) Track(FindAnyObjectByType<PlayerController>());
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<PlayerJumped>(OnJumped);
            EventBus.Subscribe<PlayerSlid>(OnSlid);
            EventBus.Subscribe<PlayerWallRan>(OnWallRan);
            EventBus.Subscribe<PlayerCrossedThreshold>(OnCrossed);
            EventBus.Subscribe<TilePulled>(OnTilePulled);
            EventBus.Subscribe<TilePlaced>(OnTilePlaced);
            EventBus.Subscribe<PlayerRespawned>(OnRespawned);
            EventBus.Subscribe<PlayerEnteredArea>(OnEnteredArea);
            EventBus.Subscribe<GameCompleted>(OnCompleted);
            EventBus.Subscribe<GamePaused>(OnPaused);
            EventBus.Subscribe<GameStarted>(OnStarted);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Unsubscribe<PlayerJumped>(OnJumped);
            EventBus.Unsubscribe<PlayerSlid>(OnSlid);
            EventBus.Unsubscribe<PlayerWallRan>(OnWallRan);
            EventBus.Unsubscribe<PlayerCrossedThreshold>(OnCrossed);
            EventBus.Unsubscribe<TilePulled>(OnTilePulled);
            EventBus.Unsubscribe<TilePlaced>(OnTilePlaced);
            EventBus.Unsubscribe<PlayerRespawned>(OnRespawned);
            EventBus.Unsubscribe<PlayerEnteredArea>(OnEnteredArea);
            EventBus.Unsubscribe<GameCompleted>(OnCompleted);
            EventBus.Unsubscribe<GamePaused>(OnPaused);
            EventBus.Unsubscribe<GameStarted>(OnStarted);
        }

        void OnDestroy()
        {
            AudioListener.pause = false;
            AudioDirector.ReleaseClips();
            AudioDirector.Forget(this);
        }

        /// <summary>A plain 2D source: no spatialisation, so placeholder sounds read the same wherever they happen.</summary>
        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            s.dopplerLevel = 0f;
            return s;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (prewarm.Count > 0) PrewarmOne();
            UpdateListener(dt);
            UpdateAmbience(dt);
            if (!paused && player != null) UpdateFootsteps();
        }

        // ---------------------------------------------------------------- listener

        /// <summary>
        /// Finds the camera's AudioListener (cheap while cached). Without one, Unity warns every
        /// frame that audio plays, so playback waits until a listener exists.
        /// </summary>
        bool FindListener()
        {
            if (listener != null && listener.isActiveAndEnabled) return true;
            listener = FindAnyObjectByType<AudioListener>();
            return listener != null && listener.isActiveAndEnabled;
        }

        /// <summary>Re-checks for a listener (immediately while cached, every half second while missing) and pauses or resumes the ambience to match.</summary>
        void UpdateListener(float dt)
        {
            bool ok;
            if (listener != null && listener.isActiveAndEnabled) ok = true;
            else
            {
                listenerCheckTimer -= dt;
                if (listenerCheckTimer > 0f && !listenerOk) return;
                listenerCheckTimer = ListenerCheckInterval;
                ok = FindListener();
            }
            if (ok == listenerOk) return;
            listenerOk = ok;
            for (int i = 0; i < amb.Length; i++)
            {
                if (amb[i].clip == null) continue;
                if (!ok) amb[i].Pause();
                else if (ambTarget[i] > 0f || ambGain[i] > 0f)
                {
                    amb[i].UnPause();
                    if (!amb[i].isPlaying) amb[i].Play(); // never started: there was no listener when it was chosen
                }
            }
            if (!ok) { foreach (var v in voices) v.Stop(); ending.Stop(); }
        }

        // ---------------------------------------------------------------- ambience

        /// <summary>Builds one queued ambience per frame, so no single frame pays for all of them.</summary>
        void PrewarmOne()
        {
            while (prewarm.Count > 0)
            {
                string key = prewarm.Dequeue();
                if (AudioDirector.HasAmbience(key)) continue;
                AudioDirector.Ambience(key);
                return;
            }
        }

        /// <summary>Crossfades to the ambience for an area id over AudioConfig.AmbienceCrossfade.</summary>
        void SetAmbience(string areaId)
        {
            string key = ProcSynth.AmbienceKey(areaId);
            if (key == ambKey) return;
            ambKey = key;
            var clip = AudioDirector.Ambience(key);

            int next = 1 - ambActive;
            var src = amb[next];
            bool resume = src.clip == clip && ambGain[next] > 0f;
            if (!resume)
            {
                src.Stop();
                src.clip = clip;
                ambGain[next] = 0f;
                // Start somewhere in the loop so revisiting an area doesn't restart it audibly; it fades in from silence.
                src.timeSamples = Random.Range(0, clip.samples);
                if (listenerOk) src.Play();
            }
            ambTarget[next] = 1f;
            ambTarget[ambActive] = 0f;
            ambActive = next;

            if (Cfg.AmbienceCrossfade <= 0f)
                for (int i = 0; i < amb.Length; i++) ambGain[i] = ambTarget[i];
        }

        /// <summary>Moves crossfade gains and the duck toward their targets in unscaled time (pause sets timeScale to 0) and applies the volumes.</summary>
        void UpdateAmbience(float dt)
        {
            var cfg = Cfg;
            float duckTarget = (completed ? cfg.EndingAmbienceDuck : 1f) * (paused ? cfg.PauseAmbienceDuck : 1f);
            duck = Mathf.MoveTowards(duck, duckTarget, dt / cfg.DuckTime);

            float step = cfg.AmbienceCrossfade > 0f ? dt / cfg.AmbienceCrossfade : 1f;
            float level = cfg.MasterVolume * cfg.AmbienceVolume * duck;
            for (int i = 0; i < amb.Length; i++)
            {
                ambGain[i] = Mathf.MoveTowards(ambGain[i], ambTarget[i], step);
                // Equal-power curve: two uncorrelated loops at half gain each would dip in the middle otherwise.
                amb[i].volume = level * Mathf.Sin(ambGain[i] * Mathf.PI * 0.5f);
                if (ambGain[i] <= 0f && ambTarget[i] <= 0f && amb[i].clip != null)
                {
                    amb[i].Stop();
                    amb[i].clip = null;
                }
            }
        }

        // ---------------------------------------------------------------- one-shots

        /// <summary>Plays a one-shot on a free pooled voice (or the oldest one) at SFX level x volume.</summary>
        void Play(SoundId id, float volume, float pitch)
        {
            if (!listenerOk || paused) return;
            var clip = AudioDirector.Clip(id);
            AudioSource voice = null;
            for (int k = 0; k < voices.Length; k++)
            {
                var v = voices[(nextVoice + k) % voices.Length];
                if (!v.isPlaying) { voice = v; nextVoice = (nextVoice + k + 1) % voices.Length; break; }
            }
            if (voice == null) { voice = voices[nextVoice]; nextVoice = (nextVoice + 1) % voices.Length; }

            var cfg = Cfg;
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(cfg.MasterVolume * cfg.SfxVolume * volume);
            voice.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
            voice.Play();
            AudioDirector.Remember(id.ToString());
        }

        static float Jitter(float spread) => 1f + Random.Range(-spread, spread);

        // ---------------------------------------------------------------- player polling

        /// <summary>Starts polling a player. Clears landing state so a respawn or spawn mid-air doesn't thump.</summary>
        void Track(PlayerController p)
        {
            player = p;
            wasGrounded = p != null && p.IsGrounded;
            fallSpeed = 0f;
            stepDistance = Cfg.FootstepStride * 0.5f;
        }

        /// <summary>
        /// Footsteps by distance walked on the ground (not by time), so pace follows speed; and a
        /// landing thump on the grounded false-to-true edge, scaled by the last airborne fall speed
        /// (by the landing frame the controller has already zeroed its vertical speed).
        /// </summary>
        void UpdateFootsteps()
        {
            var cfg = Cfg;
            bool grounded = player.IsGrounded;
            if (grounded && !wasGrounded) { Land(fallSpeed); stepDistance = 0f; }
            if (!grounded) fallSpeed = Mathf.Max(0f, -player.VerticalSpeed);

            if (grounded && !player.IsSliding)
            {
                var v = player.Velocity;
                float speed = new Vector2(v.x, v.z).magnitude;
                if (speed < 0.2f) stepDistance = cfg.FootstepStride * 0.5f; // the first step lands half a stride in
                else
                {
                    stepDistance += speed * Time.deltaTime;
                    if (stepDistance >= cfg.FootstepStride)
                    {
                        stepDistance %= cfg.FootstepStride;
                        Step(speed);
                    }
                }
            }
            wasGrounded = grounded;
        }

        /// <summary>One footstep: carpet in the carpeted areas (A1, A3, A6), concrete elsewhere; never the same variation twice running.</summary>
        void Step(float speed)
        {
            var cfg = Cfg;
            string key = ProcSynth.AmbienceKey(AreaZone.Current);
            bool carpet = key == "A1" || key == "A3" || key == "A6";
            stepVariant = (stepVariant + 1 + Random.Range(0, 2)) % 3;
            var id = (carpet ? SoundId.StepCarpet0 : SoundId.StepConcrete0) + stepVariant;
            float loud = Mathf.Lerp(0.7f, 1f, Mathf.InverseLerp(2f, 10f, speed));
            Play(id, cfg.FootstepVolume * loud, Jitter(cfg.FootstepPitchJitter));
        }

        /// <summary>Landing thump: louder and lower the harder the fall; soft landings stay silent.</summary>
        void Land(float impact)
        {
            var cfg = Cfg;
            if (impact < cfg.LandMinSpeed) return;
            float t = Mathf.InverseLerp(cfg.LandMinSpeed, cfg.LandMaxSpeed, impact);
            Play(SoundId.Land, cfg.MovementVolume * Mathf.Lerp(0.35f, 1f, t),
                 Mathf.Lerp(cfg.LandPitchRange.x, cfg.LandPitchRange.y, t) * Jitter(0.03f));
        }

        // ---------------------------------------------------------------- events

        void OnPlayerSpawned(PlayerSpawned e) => Track(e.Root != null ? e.Root.GetComponent<PlayerController>() : null);

        void OnJumped(PlayerJumped e)
        {
            var cfg = Cfg;
            Play(SoundId.Jump, cfg.MovementVolume * (e.Chained ? 1f : 0.85f), (e.Chained ? 1.08f : 1f) * Jitter(0.04f));
        }

        void OnSlid(PlayerSlid e)
        {
            float t = Mathf.InverseLerp(4f, 20f, e.Speed);
            Play(SoundId.Slide, Cfg.MovementVolume * Mathf.Lerp(0.6f, 1f, t), Mathf.Lerp(0.95f, 1.1f, t));
        }

        void OnWallRan(PlayerWallRan e)
        {
            float t = Mathf.InverseLerp(7f, 20f, e.Speed);
            Play(SoundId.WallRun, Cfg.MovementVolume * Mathf.Lerp(0.7f, 1f, t), Mathf.Lerp(0.95f, 1.1f, t) * Jitter(0.03f));
        }

        /// <summary>The signature crossing sound, pitched up slightly with speed so fast dives feel quicker.</summary>
        void OnCrossed(PlayerCrossedThreshold e)
        {
            var cfg = Cfg;
            float t = Mathf.Clamp01(e.Speed / cfg.ThresholdPitchSpeed);
            Play(SoundId.Threshold, cfg.ThresholdVolume, Mathf.Lerp(cfg.ThresholdPitchRange.x, cfg.ThresholdPitchRange.y, t));
        }

        void OnTilePulled(TilePulled e) => Play(SoundId.TilePull, Cfg.TileVolume, Jitter(0.04f));
        void OnTilePlaced(TilePlaced e) => Play(SoundId.TilePlace, Cfg.TileVolume, Jitter(0.04f));

        void OnRespawned(PlayerRespawned e)
        {
            Track(player);
            Play(SoundId.Respawn, Cfg.RespawnVolume, 1f);
        }

        void OnEnteredArea(PlayerEnteredArea e) => SetAmbience(e.AreaId);

        /// <summary>The ending: the chord (built now; it's only needed once) and the ambience ducks under it.</summary>
        void OnCompleted(GameCompleted e)
        {
            completed = true;
            if (!listenerOk) return;
            var cfg = Cfg;
            ending.Stop();
            ending.clip = AudioDirector.Clip(SoundId.Ending);
            ending.volume = Mathf.Clamp01(cfg.MasterVolume * cfg.EndingVolume);
            ending.pitch = 1f;
            ending.Play();
            AudioDirector.Remember(SoundId.Ending.ToString());
        }

        /// <summary>Pause freezes one-shots and the ending (AudioListener.pause); the ambience, which ignores it, ducks instead.</summary>
        void OnPaused(GamePaused e)
        {
            paused = e.Paused;
            AudioListener.pause = e.Paused;
        }

        /// <summary>A fresh run (first Play or Restart): clear pause and the ending duck.</summary>
        void OnStarted(GameStarted e)
        {
            paused = false;
            AudioListener.pause = false;
            completed = false;
            ending.Stop();
        }
    }
}
