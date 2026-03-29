"""
Generate audio sound effects for a Suika-style sports ball merge game.
All files: mono, 44100 Hz, 16-bit PCM WAV.
"""

import os
import numpy as np
from scipy.io import wavfile

SAMPLE_RATE = 44100
OUTPUT_DIR = "/home/user/Sports-Ball-Unity/Audio"
os.makedirs(OUTPUT_DIR, exist_ok=True)


def save(filename: str, samples: np.ndarray) -> None:
    """Normalise to int16 and write WAV file."""
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples / peak
    int16_samples = (samples * 32767).astype(np.int16)
    path = os.path.join(OUTPUT_DIR, filename)
    wavfile.write(path, SAMPLE_RATE, int16_samples)
    size = os.path.getsize(path)
    print(f"  Wrote {path}  ({size} bytes)")


def t(duration: float) -> np.ndarray:
    """Time array for given duration in seconds."""
    return np.linspace(0, duration, int(SAMPLE_RATE * duration), endpoint=False)


# ---------------------------------------------------------------------------
# 1. drop.wav — downward pitch swoosh 800 Hz → 200 Hz over 0.18 s + noise
# ---------------------------------------------------------------------------
print("Generating drop.wav ...")
dur = 0.18
ts = t(dur)
# Instantaneous frequency sweeps from 800 to 200 Hz (linear in frequency)
freq_sweep = np.linspace(800, 200, len(ts))
# Integrate to get instantaneous phase
phase = 2 * np.pi * np.cumsum(freq_sweep) / SAMPLE_RATE
sweep = np.sin(phase)
# Slight noise layer (10 % amplitude)
noise = 0.10 * np.random.uniform(-1, 1, len(ts))
envelope = np.exp(-5 * ts / dur)           # moderate decay
wave = (sweep + noise) * envelope
save("drop.wav", wave)

# ---------------------------------------------------------------------------
# 2. merge_small.wav — high ping ~880 Hz, 0.15 s, fast decay
# ---------------------------------------------------------------------------
print("Generating merge_small.wav ...")
dur = 0.15
ts = t(dur)
freq = 880.0
wave = np.sin(2 * np.pi * freq * ts)
# Short attack (first 5 ms) then fast exponential decay
attack_samples = int(0.005 * SAMPLE_RATE)
envelope = np.exp(-18 * ts / dur)
envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
wave = wave * envelope
save("merge_small.wav", wave)

# ---------------------------------------------------------------------------
# 3. merge_medium.wav — mid thwack ~440 Hz with 2nd harmonic, 0.2 s
# ---------------------------------------------------------------------------
print("Generating merge_medium.wav ...")
dur = 0.2
ts = t(dur)
wave = (np.sin(2 * np.pi * 440 * ts)
        + 0.5 * np.sin(2 * np.pi * 880 * ts)
        + 0.25 * np.sin(2 * np.pi * 1320 * ts))
# Punchy: fast attack, medium decay
attack_samples = int(0.004 * SAMPLE_RATE)
envelope = np.exp(-10 * ts / dur)
envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
wave = wave * envelope
save("merge_medium.wav", wave)

# ---------------------------------------------------------------------------
# 4. merge_large.wav — deep thud ~180 Hz with low rumble, 0.3 s
# ---------------------------------------------------------------------------
print("Generating merge_large.wav ...")
dur = 0.3
ts = t(dur)
fundamental = np.sin(2 * np.pi * 180 * ts)
subharmonic = 0.6 * np.sin(2 * np.pi * 90 * ts)
# Low-frequency rumble: filtered noise band around 60–120 Hz
noise_raw = np.random.uniform(-1, 1, len(ts))
# Simple IIR-style low-pass: very crude but effective for rumble character
rumble = np.zeros(len(ts))
alpha = 0.15
for i in range(1, len(ts)):
    rumble[i] = alpha * noise_raw[i] + (1 - alpha) * rumble[i - 1]
rumble *= 0.4

wave = fundamental + subharmonic + rumble
attack_samples = int(0.003 * SAMPLE_RATE)
envelope = np.exp(-7 * ts / dur)
envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
wave = wave * envelope
save("merge_large.wav", wave)

# ---------------------------------------------------------------------------
# 5. score_tick.wav — short blip ~1200 Hz, 0.08 s
# ---------------------------------------------------------------------------
print("Generating score_tick.wav ...")
dur = 0.08
ts = t(dur)
wave = np.sin(2 * np.pi * 1200 * ts)
attack_samples = int(0.003 * SAMPLE_RATE)
envelope = np.exp(-25 * ts / dur)
envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
wave = wave * envelope
save("score_tick.wav", wave)

# ---------------------------------------------------------------------------
# 6. danger.wav — 3 short beeps at ~600 Hz, each 0.1 s with 0.05 s gap
# ---------------------------------------------------------------------------
print("Generating danger.wav ...")
beep_dur = 0.10
gap_dur  = 0.05
beep_samples = int(SAMPLE_RATE * beep_dur)
gap_samples  = int(SAMPLE_RATE * gap_dur)

def make_beep(freq: float, dur: float) -> np.ndarray:
    ts_b = t(dur)
    w = np.sin(2 * np.pi * freq * ts_b)
    env = np.exp(-8 * ts_b / dur)
    attack = int(0.005 * SAMPLE_RATE)
    env[:attack] = np.linspace(0, 1, attack)
    return w * env

gap = np.zeros(gap_samples)
beep = make_beep(600, beep_dur)
wave = np.concatenate([beep, gap, beep, gap, beep])
save("danger.wav", wave)

# ---------------------------------------------------------------------------
# 7. game_over.wav — descending 3-note C5→A4→F4, 0.25 s each, smooth decay
# ---------------------------------------------------------------------------
print("Generating game_over.wav ...")
note_dur = 0.25
# C5 = 523.25 Hz, A4 = 440 Hz, F4 = 349.23 Hz
notes = [523.25, 440.0, 349.23]

def make_note(freq: float, dur: float) -> np.ndarray:
    ts_n = t(dur)
    w = (np.sin(2 * np.pi * freq * ts_n)
         + 0.3 * np.sin(2 * np.pi * freq * 2 * ts_n))
    # Smooth ADSR-like envelope: short attack, long decay to zero
    attack = int(0.010 * SAMPLE_RATE)
    env = np.exp(-4 * ts_n / dur)
    env[:attack] = np.linspace(0, 1, attack)
    return w * env

wave = np.concatenate([make_note(f, note_dur) for f in notes])
save("game_over.wav", wave)

# ---------------------------------------------------------------------------
# 8. basketball_merge.wav — triumphant ascending arpeggio C5-E5-G5, 0.15 s each
# ---------------------------------------------------------------------------
print("Generating basketball_merge.wav ...")
note_dur = 0.15
# C5 = 523.25 Hz, E5 = 659.25 Hz, G5 = 783.99 Hz
arp_notes = [523.25, 659.25, 783.99]

def make_bright_note(freq: float, dur: float) -> np.ndarray:
    ts_n = t(dur)
    # Brighter timbre: fundamental + 2nd + 3rd harmonics
    w = (np.sin(2 * np.pi * freq * ts_n)
         + 0.5 * np.sin(2 * np.pi * freq * 2 * ts_n)
         + 0.25 * np.sin(2 * np.pi * freq * 3 * ts_n))
    attack = int(0.005 * SAMPLE_RATE)
    env = np.exp(-6 * ts_n / dur)
    env[:attack] = np.linspace(0, 1, attack)
    return w * env

wave = np.concatenate([make_bright_note(f, note_dur) for f in arp_notes])
save("basketball_merge.wav", wave)

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
print("\nAll sound effects generated successfully.")
print(f"Output directory: {OUTPUT_DIR}")
files = sorted(os.listdir(OUTPUT_DIR))
for f in files:
    path = os.path.join(OUTPUT_DIR, f)
    size = os.path.getsize(path)
    print(f"  {f:30s}  {size:>8} bytes")
