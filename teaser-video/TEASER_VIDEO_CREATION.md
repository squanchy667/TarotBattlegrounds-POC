# Tarot Battlegrounds - 10 Second Teaser Video

## Overview

This document describes the creation of a 10-second teaser video for Tarot Battlegrounds using [Remotion](https://remotion.dev) - a framework for creating videos programmatically with React.

**Output:** `tarot-battlegrounds-teaser.mp4` (6.7 MB, 1920x1080, 30fps)

---

## Source Assets Used

All images were sourced from the root folder of the repository:

| Asset | Source File | Usage |
|-------|-------------|-------|
| Main Logo | `Logo no background.png` | Intro and finale scenes |
| Hero Battle | `Hero .png` | Battle scene background |
| Swords Tribe | `Swords logo.png` | Tribes reveal (red/aggression) |
| Wands Tribe | `Wands logo.png` | Tribes reveal (purple/buffs) |
| Cups Tribe | `cups logo 2.png` | Tribes reveal (blue/healing) |
| Pentacles Tribe | `pentacles logo 3.png` | Tribes reveal (gold/economy) |
| Background | `Gemini_Generated_Image_8figp98figp98fig.png` | Constellation/gear blueprint pattern |
| Crystal | `Gemini_Generated_Image_aqctj3aqctj3aqct.png` | Additional visual element |

---

## Video Structure

The video is divided into 4 scenes across 300 frames (10 seconds at 30fps):

### Scene 1: Logo Intro (0-2.5s, frames 0-75)
- Main logo rotates in from 180 degrees with spring physics
- Blue radial glow fades in behind the logo
- Tagline "The Universe is a Machine" appears at the bottom

### Scene 2: Tribes Reveal (2.5-6s, frames 75-180)
- Constellation background with 30% opacity
- Title "Choose Your Tribe" fades in at top
- Four tribe emblems appear sequentially with 15-frame delays:
  1. **Swords** (red) - Aggression & Burst damage
  2. **Wands** (purple) - Buffs & Scaling
  3. **Cups** (blue) - Healing & Restoration
  4. **Pentacles** (gold) - Wealth & Economy
- Each emblem has spring scale animation and pulsing glow effect

### Scene 3: Battle Scene (6-8.5s, frames 180-255)
- Hero battle image with slow zoom out (1.2x to 1x scale)
- Dark gradient overlay for text readability
- Text "Let Fate Collide" with blue glow shadow

### Scene 4: Final CTA (8.5-10s, frames 255-300)
- Logo with pulsing blue glow effect
- Title "TAROT BATTLEGROUNDS" in gold
- "COMING SOON" button with blue border

---

## Brand Guidelines Applied

The video follows the "Celestial Clockwork" visual identity:

### Color Palette
| Role | Color | Hex |
|------|-------|-----|
| Background | Cosmos Navy | `#121620` |
| Primary Accent | Aged Brass | `#C0A060` |
| Energy/Magic | Sapphire Star | `#4090FF` |
| Swords | Ruby | `#A03030` |
| Wands | Purple | `#9040A0` |

### Typography
- Headlines: Georgia serif (fallback for Cinzel)
- All text: uppercase with letter-spacing for engraved feel

### Visual Effects
- Radial glow effects (not sparkles)
- Spring physics for organic motion
- Smooth interpolations for cinematic feel

---

## Project Structure

```
teaser-video/
├── package.json          # Dependencies and scripts
├── tsconfig.json         # TypeScript configuration
├── remotion.config.ts    # Remotion settings
├── src/
│   ├── index.ts          # Entry point
│   ├── Root.tsx          # Composition registration
│   └── TarotTeaser.tsx   # Main video component (all scenes)
├── public/
│   └── images/           # Asset files
│       ├── logo.png
│       ├── hero.png
│       ├── swords.png
│       ├── wands.png
│       ├── cups.png
│       ├── pentacles.png
│       ├── background.png
│       └── crystal.png
└── out/
    └── tarot-battlegrounds-teaser.mp4
```

---

## How to Modify

### Prerequisites
- Node.js 18+
- npm

### Development
```bash
cd teaser-video
npm install
npm start
```
This opens Remotion Studio where you can preview and adjust the video in real-time.

### Re-render
```bash
npm run render
```
Output: `out/tarot-battlegrounds-teaser.mp4`

### Key Files to Edit

**`src/TarotTeaser.tsx`** - Main component containing all scenes:
- `LogoIntro` - First scene animations
- `TribesReveal` - Tribe emblems sequence
- `BattleScene` - Hero image scene
- `FinalCTA` - Closing logo and CTA

### Adjusting Timing
Scene timing is controlled in the main `TarotTeaser` component via `<Sequence>` components:
```tsx
<Sequence from={0} durationInFrames={75}>     {/* 0-2.5s */}
<Sequence from={75} durationInFrames={105}>   {/* 2.5-6s */}
<Sequence from={180} durationInFrames={75}>   {/* 6-8.5s */}
<Sequence from={255} durationInFrames={45}>   {/* 8.5-10s */}
```

### Changing Text
Edit the text strings directly in each scene component:
- `"The Universe is a Machine"` - Logo intro tagline
- `"Choose Your Tribe"` - Tribes scene header
- `"Let Fate Collide"` - Battle scene text
- `"Tarot Battlegrounds"` - Final title
- `"Coming Soon"` - CTA button

---

## Remotion Techniques Used

### Animations
- **`spring()`** - Natural bounce/settle animations for logos and emblems
- **`interpolate()`** - Linear/clamped transitions for opacity, scale, rotation
- **`Math.sin()`** - Pulsing glow effects

### Components
- **`<AbsoluteFill>`** - Full-frame container
- **`<Sequence>`** - Timed scene transitions
- **`<Img>`** - Static image rendering with `staticFile()`

### Configuration
- Resolution: 1920x1080 (Full HD)
- Frame rate: 30fps
- Duration: 300 frames (10 seconds)
- Codec: H.264 (MP4)

---

## Dependencies

```json
{
  "@remotion/cli": "^4.0.0",
  "@remotion/player": "^4.0.0",
  "react": "^18.2.0",
  "react-dom": "^18.2.0",
  "remotion": "^4.0.0"
}
```

---

## Future Enhancements

Potential improvements for future versions:
- Add background music/sound effects
- Include card flip animations
- Add particle effects for magic/energy
- Create longer 30-second version with gameplay footage
- Add voiceover narration
- Create vertical (9:16) version for social media

---

## Created With

- **Remotion** - React-based video creation framework
- **Remotion Skill** - Claude Code skill for Remotion best practices
- **Claude Code** - AI-assisted development

---

*Generated: January 2026*
