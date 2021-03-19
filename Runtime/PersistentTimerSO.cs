using Elysium.Core;
using Elysium.Utils.Attributes;
using Elysium.Utils.Timers;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Elysium.Timers
{
    [CreateAssetMenu(fileName = "TimerSO_", menuName = "Scriptable Objects/Persistent Timer")]
    public class PersistentTimerSO : ScriptableObject, ISavable
    {
        [SerializeField] private bool repeat = default;
        [SerializeField] private bool startByDefault = default;
        [SerializeField] private float defaultInitial = default;

        [Separator("Debug", true)]
        [SerializeField, ReadOnly] private float initial = default;
        [SerializeField, ReadOnly] private float current = default;        
        [SerializeField, ReadOnly] private int cycles = default;

        [NonSerialized] private long last = default;
        [NonSerialized] private TimerInstance runtimeTimer = null;

        public event UnityAction OnValueChanged;

        private long CurrentUnixTime => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private float Elapsed => CurrentUnixTime - last;
        public ushort Size => sizeof(float) * 2 + sizeof(long) + sizeof(int);        

        private TimerInstance RuntimeTimer
        {
            get
            {
                if (runtimeTimer == null)
                {
                    Debug.Log($"Creating timer for {name}");
                    runtimeTimer = Timer.CreateEmptyTimer(() => !this);
                    runtimeTimer.OnTick += UpdateCurrent;
                    runtimeTimer.OnTimerEnd += TimerEnded;
                }

                return runtimeTimer;
            }
        }

        public void StartNewTimer(float _time)
        {
            if (_time == 0) { throw new System.Exception("trying to start a 0 timer!"); }

            initial = _time;
            OnValueChanged?.Invoke();
            RuntimeTimer.SetTime(initial);
        }

        public int GetAndResetCycles()
        {
            int count = cycles;
            cycles = 0;

            OnValueChanged?.Invoke();
            return count;
        }

        private void UpdateCurrent()
        {
            last = CurrentUnixTime;
            current = RuntimeTimer.Time;
            OnValueChanged?.Invoke();
        }

        private void TimerEnded()
        {
            Debug.Log($"Timer {name} has ended");
            cycles++;
            OnValueChanged?.Invoke();

            if (repeat) { RuntimeTimer.SetTime(initial); }            
        }

        private void HandleAFKIterations()
        {
            float elapsed = Elapsed;
            while(elapsed > 0)
            {
                float min = Mathf.Min(elapsed, current);
                elapsed -= min;
                current -= min;
                if (current <= 0)
                {
                    current += initial;
                    cycles++;
                }
            }

            OnValueChanged?.Invoke();
        }        

        public void Load(BinaryReader _reader) 
        {
            initial = _reader.ReadSingle();
            current = _reader.ReadSingle();
            last = _reader.ReadInt64();
            cycles = _reader.ReadInt32();

            Debug.Log($"Elapsed Seconds: {Elapsed}");
            HandleAFKIterations();            

            if (initial != 0)
            {
                RuntimeTimer.SetTime(current);
            }
        }

        public void LoadDefault()
        {
            initial = defaultInitial;
            current = 0f;
            last = DateTimeOffset.MinValue.ToUnixTimeSeconds();
            cycles = 0;

            if (startByDefault) { StartNewTimer(defaultInitial); }
        }

        public void Save(BinaryWriter _writer) 
        {
            _writer.Write(initial);
            _writer.Write(current);
            _writer.Write(last);
            _writer.Write(cycles);
        }
    }
}
