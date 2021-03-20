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
        [SerializeField] private bool repeat = true;
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
        public bool IsEnded => current <= 0 && !repeat;
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
            if (_time <= 0) { throw new System.Exception("trying to start a 0 or less timer!"); }

            initial = _time;
            RuntimeTimer.SetTime(initial);
        }

        public int GetAndResetCycles()
        {
            if (cycles <= 0) { return 0; }

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
            var elapsed = Elapsed + (initial - current);
            cycles += (int)(elapsed / initial);
            current = initial - (elapsed % initial);
        }

        public void Load(BinaryReader _reader) 
        {
            initial = _reader.ReadSingle();
            current = _reader.ReadSingle();
            last = _reader.ReadInt64();
            cycles = _reader.ReadInt32();

            Debug.Log($"Elapsed Seconds: {Elapsed}");
            HandleAFKIterations();            

            if (initial != 0 && !IsEnded)
            {
                RuntimeTimer.SetTime(current);
            }
        }

        public void LoadDefault()
        {
            initial = defaultInitial;
            current = 0f;
            last = CurrentUnixTime;
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
