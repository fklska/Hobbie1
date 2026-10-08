import os
import subprocess
import sys
import tempfile
import wave

import numpy as np

SR = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
rng = np.random.default_rng(1103)


def ts(d):
    return np.arange(int(SR * d)) / SR


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def noise(d):
    return rng.uniform(-1, 1, int(SR * d))


def band(x, lo, hi, order=2):
    n = len(x)
    f = np.fft.rfftfreq(n, 1 / SR)
    gain = np.ones_like(f)
    if lo > 0:
        gain /= np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * order))
    if hi is not None:
        gain /= np.sqrt(1 + (f / hi) ** (2 * order))
    return np.fft.irfft(np.fft.rfft(x) * gain, n)


def place(buf, x, at):
    i = int(at * SR)
    if i >= len(buf):
        return
    x = x[: len(buf) - i].copy()
    k = min(len(x), int(0.004 * SR))
    x[len(x) - k :] *= np.linspace(1, 0, k)
    buf[i : i + len(x)] += x


def modes(d, freqs, amps, taus, attack=0.001):
    t = ts(d)
    out = np.zeros_like(t)
    for f, a, tau in zip(freqs, amps, taus):
        out += a * np.sin(2 * np.pi * f * t + rng.uniform(0, np.pi)) * np.exp(-t / tau)
    return out * np.minimum(1, t / attack)


def sweep_sine(d, f0, f1, tau, curve=0.08):
    t = ts(d)
    f = f1 + (f0 - f1) * np.exp(-t / curve)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / tau)


def burst(d, lo, hi, tau, attack=0.0005):
    t = ts(d)
    return band(noise(d), lo, hi) * np.exp(-t / tau) * np.minimum(1, t / attack)


def svf_sweep(x, centers, q):
    low = band_ = 0.0
    out = np.empty_like(x)
    damp = 1 / q
    for i in range(len(x)):
        f = 2 * np.sin(np.pi * min(centers[i], SR / 6) / SR)
        high = x[i] - low - damp * band_
        band_ += f * high
        low += f * band_
        out[i] = band_
    return out


def saw(freq_curve, harmonics):
    phase = 2 * np.pi * np.cumsum(freq_curve) / SR
    out = np.zeros_like(phase)
    top = freq_curve.max()
    for n in range(1, harmonics + 1):
        if n * top > SR / 2.2:
            break
        out += np.sin(n * phase) / n
    return out


def impulse_response(d=2.6, tau_low=0.42, tau_high=0.18, predelay=0.02):
    t = ts(d)
    ir = []
    for _ in range(2):
        n = noise(d)
        low = band(n, 0, 3000) * np.exp(-t / tau_low)
        high = band(n, 3000, None) * np.exp(-t / tau_high)
        ch = (low + 0.5 * high) * np.minimum(1, t / 0.01)
        ch = np.concatenate([np.zeros(int(predelay * SR)), ch])
        ir.append(ch / np.sqrt(np.sum(ch ** 2)))
    return np.stack(ir, axis=1)


def convolve(x, ir):
    n = len(x) + len(ir)
    size = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:n]


def reverb(stereo, ir, wet):
    out = np.zeros((len(stereo) + len(ir), 2))
    out[: len(stereo)] += stereo
    for c in range(2):
        out[:, c] += wet[c] * convolve(stereo[:, c], ir[:, c])[: len(out)]
    return out


def finish(x, rms_target=0.18, peak=0.95, fade=0.01):
    x = x - np.mean(x)
    win = int(0.1 * SR)
    power = np.convolve(x ** 2, np.ones(win) / win, mode="valid") if len(x) > win else [np.mean(x ** 2)]
    x = x * rms_target / max(np.sqrt(np.max(power)), 1e-6)
    p = np.max(np.abs(x))
    if p > peak:
        x = np.tanh(x / p * 1.2) / np.tanh(1.2) * peak
    k = int(fade * SR)
    x[-k:] *= np.linspace(1, 0, k)
    return x


def write_wav(path, x):
    data = np.clip(x, -1, 1)
    channels = 1 if data.ndim == 1 else data.shape[1]
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((data * 32767).astype("<i2").tobytes())


def write_ogg(path, stereo, quality=5):
    with tempfile.TemporaryDirectory() as tmp:
        src = os.path.join(tmp, "src.wav")
        write_wav(src, stereo)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-c:a", "libvorbis", "-q:a", str(quality), path], check=True)


def chop():
    d = 0.32
    out = np.zeros(int(SR * d))
    place(out, 0.7 * burst(0.02, 2000, 10000, 0.002), 0)
    place(out, modes(d, [190, 410, 735, 1160], [1, 0.6, 0.35, 0.2], [0.07, 0.05, 0.035, 0.02]), 0)
    place(out, 0.5 * sweep_sine(0.2, 140, 85, 0.04), 0)
    place(out, 0.3 * burst(0.12, 800, 3000, 0.03), 0.003)
    return finish(out, 0.2)


def mine():
    d = 0.45
    out = np.zeros(int(SR * d))
    f0 = 2100
    place(out, 0.45 * modes(d, [f0, f0 * 1.48, f0 * 2.03, f0 * 2.61], [1, 0.7, 0.45, 0.3], [0.16, 0.11, 0.07, 0.05]), 0)
    place(out, 0.6 * burst(0.05, 1000, 6000, 0.01), 0)
    place(out, 0.45 * sweep_sine(0.15, 180, 110, 0.03), 0)
    for _ in range(14):
        place(out, rng.uniform(0.05, 0.15) * burst(0.02, 1500, 5000, 0.004), rng.uniform(0.01, 0.16))
    return finish(out, 0.17)


def pickup():
    d = 0.24
    out = np.zeros(int(SR * d))
    for f, at, amp in [(midi(76), 0.0, 0.7), (midi(83), 0.06, 1.0)]:
        t = ts(0.17)
        tone = np.sin(2 * np.pi * f * t) + 0.15 * np.sin(4 * np.pi * f * t)
        place(out, amp * tone * np.exp(-t / 0.05) * np.minimum(1, t / 0.003), at)
    return finish(out, 0.14)


def swing():
    d = 0.3
    t = ts(d)
    centers = 500 + 1700 * np.sin(np.pi * np.minimum(t / 0.22, 1)) ** 2
    env = np.sin(np.pi * np.minimum(t / 0.26, 1)) ** 2 * np.where(t < 0.12, 1, np.exp(-(t - 0.12) / 0.06))
    return finish(svf_sweep(noise(d), centers, 1.6) * env, 0.16)


def hit():
    d = 0.3
    out = np.zeros(int(SR * d))
    place(out, sweep_sine(d, 170, 55, 0.07, 0.04), 0)
    place(out, 0.7 * burst(0.03, 1500, 6000, 0.006), 0)
    place(out, 0.35 * modes(0.12, [380, 620], [1, 0.5], [0.03, 0.02]), 0)
    return finish(np.tanh(out * 1.6), 0.22)


def hurt():
    d = 0.38
    t = ts(d)
    out = np.zeros(int(SR * d))
    place(out, sweep_sine(d, 140, 60, 0.08, 0.05), 0)
    place(out, 0.4 * burst(0.15, 0, 900, 0.05), 0)
    f = 330 - 150 * np.minimum(t / 0.25, 1)
    place(out, 0.3 * saw(f, 6) * np.exp(-t / 0.12) * np.minimum(1, t / 0.01), 0.01)
    return finish(np.tanh(out * 1.4), 0.2)


def knock(scale=1.0):
    d = 0.2
    out = np.zeros(int(SR * d))
    place(out, modes(d, [240 * scale, 525 * scale, 910 * scale], [1, 0.55, 0.3], [0.06, 0.04, 0.025]), 0)
    place(out, 0.6 * burst(0.02, 1500, 8000, 0.002), 0)
    place(out, 0.4 * sweep_sine(0.12, 130, 80, 0.03), 0)
    return out


def build():
    d = 0.75
    out = np.zeros(int(SR * d))
    for at, amp, sc in [(0.0, 1.0, 1.0), (0.16, 0.85, 1.04), (0.32, 1.05, 0.97)]:
        place(out, amp * knock(sc), at)
    place(out, 0.6 * sweep_sine(0.3, 90, 50, 0.08), 0.36)
    place(out, 0.15 * burst(0.35, 300, 2500, 0.1), 0.36)
    return finish(out, 0.2)


def ui_click():
    d = 0.08
    out = np.zeros(int(SR * d))
    place(out, 0.5 * burst(0.01, 3000, 12000, 0.0015), 0)
    place(out, modes(d, [1500, 760], [0.6, 0.8], [0.008, 0.014]), 0)
    return finish(out, 0.1)


def error():
    d = 0.32
    out = np.zeros(int(SR * d))
    for f, at in [(220, 0.0), (165, 0.13)]:
        t = ts(0.17)
        tone = sum(np.sin(2 * np.pi * f * n * t) / n for n in (1, 3, 5, 7))
        place(out, tone * np.minimum(1, t / 0.008) * np.minimum(1, (0.17 - t) / 0.04), at)
    return finish(band(out, 0, 2000), 0.12)


def coin():
    d = 0.55
    out = np.zeros(int(SR * d))
    for f, at, amp in [(988, 0.0, 0.7), (1319, 0.075, 1.0)]:
        place(out, amp * modes(d - at, [f, f * 2, f * 2.76, f * 5.4], [1, 0.3, 0.2, 0.08], [0.16, 0.08, 0.06, 0.03], 0.002), at)
    return finish(out, 0.14)


def forge():
    d = 1.5
    out = np.zeros(int(SR * d))
    for at, amp in [(0.0, 1.0), (0.42, 0.55)]:
        f0 = 820 * (1 + 0.004 * at)
        place(out, amp * modes(d - at, [f0 * r for r in (1, 2.32, 4.25, 6.63, 8.9)], [1, 0.6, 0.4, 0.25, 0.15], [0.9, 0.6, 0.4, 0.25, 0.15]), at)
        place(out, amp * 0.8 * burst(0.02, 2000, 9000, 0.004), at)
        place(out, amp * 0.4 * sweep_sine(0.15, 150, 90, 0.03), at)
    return finish(out, 0.16)


def giant_roar():
    d = 1.9
    t = ts(d)
    contour = 58 + 34 * np.sin(np.pi * np.minimum(t / 1.6, 1)) ** 0.7
    jitter = np.cumsum(rng.normal(0, 1, len(t)))
    jitter = band(jitter - jitter.mean(), 0, 25)
    jitter = jitter / np.max(np.abs(jitter)) * 6
    f = contour + jitter + 3 * np.sin(2 * np.pi * 6.5 * t)
    voice = saw(f, 60)
    voice = band(voice, 0, 2400) + 0.6 * band(voice, 350, 900)
    gravel = band(noise(d), 100, 1200) * (0.5 + 0.5 * np.sin(2 * np.pi * 31 * t))
    env = np.minimum(1, t / 0.25) * np.where(t > 1.25, np.exp(-(t - 1.25) / 0.2), 1)
    out = (voice + 0.5 * gravel) * env
    return finish(np.tanh(out * 1.8), 0.22)


def giant_smash():
    d = 1.1
    out = np.zeros(int(SR * d))
    place(out, sweep_sine(d, 75, 28, 0.35, 0.12), 0)
    place(out, 0.6 * burst(0.9, 0, 300, 0.35, 0.004), 0)
    place(out, 0.5 * burst(0.08, 1000, 5000, 0.02), 0)
    for _ in range(26):
        at = rng.exponential(0.15) + 0.04
        place(out, rng.uniform(0.04, 0.14) * modes(0.05, [rng.uniform(700, 2600)], [1], [rng.uniform(0.006, 0.02)]), at)
    return finish(np.tanh(out * 1.5), 0.24)


def magic_beam():
    d = 1.65
    t = ts(d)
    out = np.zeros(len(t))
    charge = t < 0.9
    f = 260 + 640 * np.clip(t / 0.9, 0, 1) ** 1.6
    f = f * (1 + 0.03 * np.sin(2 * np.pi * 7 * t))
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.4 * np.sin(4 * np.pi * np.cumsum(f * 1.003) / SR)
    out += 0.5 * tone * np.where(charge, (t / 0.9) ** 2, np.exp(-(t - 0.9) / 0.05))
    sparkle = band(noise(d), 4000, 9000) * (0.5 + 0.5 * np.sin(2 * np.pi * 17 * t)) ** 4
    out += 0.25 * sparkle * np.clip(t / 0.9, 0, 1)
    beam_t = np.clip(t - 0.9, 0, None)
    beam_env = np.where(charge, 0, np.minimum(1, beam_t / 0.02) * np.where(beam_t > 0.55, np.exp(-(beam_t - 0.55) / 0.05), 1))
    buzz = saw(np.full(len(t), 180.0) * (1 + 0.01 * np.sin(2 * np.pi * 5 * t)), 30) + 0.5 * saw(np.full(len(t), 271.0), 20)
    trem = 0.75 + 0.25 * np.sin(2 * np.pi * 24 * t)
    out += 0.45 * band(buzz, 0, 3500) * trem * beam_env
    out += 0.2 * band(noise(d), 2000, 6000) * beam_env
    return finish(np.tanh(out * 1.3), 0.2)


def collapse():
    d = 1.5
    out = np.zeros(int(SR * d))
    place(out, 0.9 * sweep_sine(0.6, 80, 40, 0.15), 0)
    place(out, burst(1.4, 0, 450, 0.45, 0.02), 0)
    for _ in range(45):
        at = rng.exponential(0.25) + 0.02
        f = rng.choice([rng.uniform(300, 650), rng.uniform(900, 3000)])
        place(out, rng.uniform(0.05, 0.2) * modes(0.08, [f, f * 1.7], [1, 0.4], [rng.uniform(0.008, 0.03)] * 2), at)
    return finish(np.tanh(out * 1.3), 0.22)


def step():
    d = 0.14
    out = np.zeros(int(SR * d))
    place(out, band(burst(0.12, 80, 900, 0.025, 0.003), 0, 1200, 4), 0)
    place(out, 0.5 * sweep_sine(0.08, 110, 70, 0.02), 0)
    place(out, 0.05 * burst(0.03, 2500, 5000, 0.008), 0.004)
    return finish(out, 0.12)


def pluck(freq, dur, vel=1.0, position=0.18):
    t = ts(dur)
    out = np.zeros_like(t)
    tau0 = 1.5 * (220 / freq) ** 0.5
    n = 1
    while n * freq < 9000 and n <= 24:
        fn = n * freq * np.sqrt(1 + 0.00008 * n * n)
        amp = abs(np.sin(np.pi * n * position)) / n ** 0.9
        out += amp * np.sin(2 * np.pi * fn * t) * np.exp(-t / (tau0 / (1 + 0.55 * (n - 1))))
        n += 1
    out *= np.minimum(1, t / 0.002)
    k = int(0.05 * SR)
    out[-k:] *= np.linspace(1, 0, k)
    k = int(0.004 * SR)
    out[:k] += 0.05 * band(noise(0.004), 0, 3500) * np.linspace(1, 0, k)
    return vel * out


def flute(freq, dur, vel=1.0):
    tail = 0.14
    t = ts(dur + tail)
    vib = 0.0045 * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.25) / 0.3, 0, 1)
    phase = 2 * np.pi * np.cumsum(freq * (1 + vib)) / SR
    tone = sum(a * np.sin(n * phase) for n, a in ((1, 1), (2, 0.32), (3, 0.12), (4, 0.05), (5, 0.025)))
    breath = band(noise(dur + tail), freq * 1.5, freq * 5)
    chiff = band(noise(dur + tail), 1500, 7000) * np.exp(-t / 0.02)
    env = np.minimum(1, t / 0.07) * (0.88 + 0.12 * np.exp(-t / 0.2))
    env *= np.where(t > dur, np.exp(-(t - dur) / 0.045), 1)
    return vel * (tone + 0.05 * breath + 0.12 * chiff) * env


def pad(freqs, dur):
    tail = 1.2
    t = ts(dur + tail)
    env = np.minimum(1, t / 0.9) * np.where(t > dur, np.exp(-(t - dur) / 0.45), 1)
    left = np.zeros_like(t)
    right = np.zeros_like(t)
    for f in freqs:
        for detune, side in ((-5, 0), (0, 2), (5, 1)):
            fd = f * 2 ** (detune / 1200)
            v = sum(np.sin(2 * np.pi * fd * n * t + rng.uniform(0, np.pi)) / n ** 1.6 for n in range(1, 7))
            if side != 1:
                left += v
            if side != 0:
                right += v
    return np.stack([left, right], axis=1) * env[:, None]


def drum(vel=1.0, low=True):
    d = 0.5
    out = np.zeros(int(SR * d))
    if low:
        place(out, sweep_sine(d, 115, 68, 0.17, 0.05), 0)
        place(out, 0.25 * burst(0.1, 0, 1200, 0.025), 0)
    else:
        place(out, 0.5 * sweep_sine(0.2, 210, 170, 0.05, 0.02), 0)
        place(out, 0.3 * burst(0.05, 600, 3000, 0.01), 0)
    return vel * out


CHORDS = {
    "Dm": [50, 57, 62, 65],
    "C": [48, 55, 60, 64],
    "G": [43, 50, 55, 59],
    "Am": [45, 52, 57, 60],
    "F": [41, 48, 53, 57],
    "A": [45, 52, 57, 61],
}

PADS = {
    "Dm": [62, 65, 69],
    "C": [60, 64, 67],
    "G": [59, 62, 67],
    "Am": [57, 60, 64],
    "F": [57, 60, 65],
    "A": [57, 61, 64],
}

D4, E4, F4, G4, A4, B4, C5, CS5, D5, E5, F5, G5, A5 = 62, 64, 65, 67, 69, 71, 72, 73, 74, 76, 77, 79, 81
R = None

PROGRESSIONS = {
    "intro": ["Dm", "C", "G", "Dm"],
    "A": ["Dm", "C", "G", "Dm", "Dm", "C", "Am", "Dm"],
    "B": ["F", "C", "G", "Dm", "F", "C", "G", "A"],
}

MELODIES = {
    "A1": [
        (D5, 1.5), (C5, 0.5), (A4, 1),
        (G4, 1), (E4, 1), (G4, 1),
        (B4, 1.5), (A4, 0.5), (G4, 1),
        (A4, 3),
        (D5, 1.5), (E5, 0.5), (F5, 1),
        (E5, 1.5), (D5, 0.5), (C5, 1),
        (A4, 1), (C5, 1), (E5, 1),
        (D5, 3),
    ],
    "A2": [
        (F5, 1.5), (E5, 0.5), (D5, 1),
        (C5, 1.5), (D5, 0.5), (E5, 1),
        (D5, 1), (B4, 1), (G4, 1),
        (A4, 2), (F4, 0.5), (E4, 0.5),
        (D4, 1), (F4, 1), (A4, 1),
        (G4, 1), (C5, 1), (E5, 1),
        (E5, 1.5), (D5, 0.5), (C5, 1),
        (D5, 2), (R, 1),
    ],
    "B": [
        (A4, 1), (C5, 1), (F5, 1),
        (E5, 2), (C5, 1),
        (D5, 1), (B4, 1), (D5, 1),
        (A4, 3),
        (C5, 1), (F5, 1), (A5, 1),
        (G5, 1.5), (F5, 0.5), (E5, 1),
        (D5, 1), (B4, 1), (G4, 1),
        (E5, 2), (CS5, 1),
    ],
    "A3": [
        (D5, 1.5), (C5, 0.5), (A4, 1),
        (G4, 1), (E4, 1), (G4, 1),
        (B4, 1.5), (A4, 0.5), (G4, 1),
        (A4, 3),
        (D5, 1.5), (E5, 0.5), (F5, 1),
        (E5, 1.5), (D5, 0.5), (C5, 1),
        (A4, 1), (G4, 1), (E4, 1),
        (D4, 3),
    ],
}

FORM = [("intro", None, False), ("A", "A1", True), ("A", "A2", True), ("B", "B", True), ("A", "A3", "fade")]


def village_theme():
    bpm = 90
    beat = 60 / bpm
    bar = beat * 3
    bars = sum(len(PROGRESSIONS[p]) for p, _, _ in FORM)
    length = bars * bar
    total = int((length + 4) * SR)
    pl = np.zeros(total)
    fl = np.zeros(total)
    dr = np.zeros(total)
    pd = np.zeros((total, 2))

    def jitter():
        return rng.normal(0, 0.006)

    start = 0.0
    for prog, melody, drums in FORM:
        chords = PROGRESSIONS[prog]
        for i, name in enumerate(chords):
            at = start + i * bar
            voicing = CHORDS[name]
            pattern = [0, 1, 2, 3, 2, 1] if prog != "B" else [0, 2, 1, 3, 2, 3]
            for k, idx in enumerate(pattern):
                vel = (1.0 if k == 0 else 0.62) * rng.uniform(0.9, 1.05)
                place(pl, pluck(midi(voicing[idx]), 1.8, vel), max(0, at + k * beat / 2 + jitter()))
            chunk = pad([midi(m) for m in PADS[name]], bar)
            j = int(at * SR)
            pd[j : j + len(chunk)] += chunk[: total - j]
            if drums is True or (drums == "fade" and i < len(chords) - 4):
                place(dr, drum(rng.uniform(0.9, 1.0)), at)
                place(dr, drum(rng.uniform(0.45, 0.55), low=False), at + 2 * beat)
        if melody:
            at = start
            for pitch, beats in MELODIES[melody]:
                if pitch is not None:
                    place(fl, flute(midi(pitch), beats * beat - 0.03, rng.uniform(0.92, 1.0)), at + jitter())
                at += beats * beat
        start += len(chords) * bar

    stereo = np.zeros((total, 2))
    for sig, gain, pan in ((pl, 0.16, -0.25), (fl, 0.2, 0.18), (dr, 0.22, 0.0)):
        stereo[:, 0] += sig * gain * np.sqrt((1 - pan) / 2)
        stereo[:, 1] += sig * gain * np.sqrt((1 + pan) / 2)
    stereo += pd * 0.012

    wet = np.zeros((total, 2))
    for sig, gain, pan, send in ((pl, 0.16, -0.25, 0.32), (fl, 0.2, 0.18, 0.4), (dr, 0.22, 0.0, 0.12)):
        wet[:, 0] += sig * gain * send * np.sqrt((1 - pan) / 2)
        wet[:, 1] += sig * gain * send * np.sqrt((1 + pan) / 2)
    wet += pd * 0.012 * 0.5
    ir = impulse_response()
    for c in range(2):
        stereo[:, c] += convolve(wet[:, c], ir[:, c])[:total]

    n = int(length * SR)
    out = stereo[:n].copy()
    out[: total - n] += stereo[n:]
    return out / np.max(np.abs(out)) * 0.8


def jingle(notes, chord_plucks, length):
    total = int(length * SR)
    pl = np.zeros(total)
    fl = np.zeros(total)
    for m, at, vel in chord_plucks:
        place(pl, pluck(midi(m), 2.2, vel), at)
    for m, at, dur in notes:
        place(fl, flute(midi(m), dur), at)
    dry = np.zeros((total, 2))
    dry[:, 0] = pl * 0.2 + fl * 0.17
    dry[:, 1] = pl * 0.15 + fl * 0.22
    wet = reverb(dry, impulse_response(), (0.45, 0.45))[:total]
    k = int(0.3 * SR)
    wet[-k:] *= np.linspace(1, 0, k)[:, None]
    return wet / np.max(np.abs(wet)) * 0.8


def victory():
    plucks = [(m, i * 0.11, 0.8) for i, m in enumerate([50, 54, 57, 62, 66])]
    plucks += [(m, 0.75, 0.9) for m in (50, 57, 62, 66, 69)]
    flutes = [(69, 0.0, 0.25), (74, 0.25, 0.25), (78, 0.5, 1.6)]
    return jingle(flutes, plucks, 3.2)


def defeat():
    plucks = [(m, 0.0 + i * 0.05, 0.7) for i, m in enumerate([50, 57, 62, 65])]
    plucks += [(m, 1.4 + i * 0.07, 0.6) for i, m in enumerate([45, 52, 57, 61])]
    plucks += [(m, 2.3, 0.7) for m in (38, 50, 57, 62)]
    flutes = [(69, 0.0, 0.6), (65, 0.65, 0.6), (64, 1.3, 0.8), (62, 2.2, 1.3)]
    return jingle(flutes, plucks, 4.2)


SFX = {
    "chop": chop,
    "mine": mine,
    "pickup": pickup,
    "swing": swing,
    "hit": hit,
    "hurt": hurt,
    "build": build,
    "ui_click": ui_click,
    "error": error,
    "coin": coin,
    "forge": forge,
    "giant_roar": giant_roar,
    "giant_smash": giant_smash,
    "magic_beam": magic_beam,
    "collapse": collapse,
    "step": step,
}

MUSIC = {"village_theme": village_theme, "victory": victory, "defeat": defeat}


def main(names):
    for name, fn in SFX.items():
        if not names or name in names:
            write_wav(os.path.join(ROOT, "sfx", name + ".wav"), fn())
            print("sfx", name)
    for name, fn in MUSIC.items():
        if not names or name in names:
            write_ogg(os.path.join(ROOT, "music", name + ".ogg"), fn())
            print("music", name)


if __name__ == "__main__":
    main(sys.argv[1:])
