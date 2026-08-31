# RolandConverter

A Windows Forms utility that converts between **Roland S-MRC** (Roland proprietary sequencer format) and standard **MIDI** (`.mid`) files. Bidirectional: S-MRC to MIDI and MIDI to S-MRC.

**Source last updated:** 2025-05-24

**Initiated:** 2025-05-24 · **Framework:** .NET 8 Windows Forms · **Solution:** `RolandConverter.sln`

---

## What Is S-MRC?

S-MRC is a Roland-proprietary binary sequencer format used on Roland hardware (arranger keyboards, sequencers).

| Property | Value |
|----------|-------|
| Header size | 168 bytes |
| Title field | 32 bytes, ASCII + NUL padding |
| Max tracks | 8 linear phrase tracks |
| Default PPQN | 96 clocks per quarter note |
| Tempo range | 10-250 BPM |

---

## Features

- **Bidirectional conversion** - S-MRC to MIDI or MIDI to S-MRC
- **Format validation** - structural correctness checked before and after conversion
- **Timestamped log panel** - conversion steps, warnings, and errors with colour coding
- **Sample files included** - *The Imperial March* in both `.mid` and `.seq` formats

---

## Getting Started

```bash
dotnet build RolandConverter.sln
dotnet run --project RolandConverter/RolandConverter.csproj
```

1. Select input file (`.seq` or `.mid`)
2. Select output file
3. Choose direction: **To MIDI** or **To S-MRC**
4. Click **Convert**