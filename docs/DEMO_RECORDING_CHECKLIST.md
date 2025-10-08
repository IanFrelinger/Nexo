# Demo Recording Checklist

Complete checklist for recording a professional demo video of the Forge framework-agnostic UI system.

---

## PRE-RECORDING SETUP (1 hour before)

### Computer Preparation
- [ ] Close all unnecessary applications
- [ ] Disable notifications (Do Not Disturb mode)
- [ ] Clear browser tabs (if showing browser)
- [ ] Close messaging apps (Slack, Discord, email)
- [ ] Disable system sounds
- [ ] Set desktop wallpaper to clean, professional background
- [ ] Hide desktop icons if visible
- [ ] Charge laptop fully (if not plugged in)
- [ ] Ensure good Wi-Fi connection (if needed)

### Display Settings
- [ ] Set display resolution to 1920x1080
- [ ] Ensure scaling is 100% (no high-DPI scaling)
- [ ] Test that text is readable at recording resolution
- [ ] Adjust brightness to medium-high (avoid too dark)
- [ ] Use "Night Light" or "Night Shift" if needed for color accuracy

### Recording Software Setup
- [ ] Install OBS Studio (or preferred screen recorder)
- [ ] Configure recording settings:
  - Resolution: 1920x1080
  - Frame rate: 60fps
  - Encoder: x264 (CPU) or NVENC (GPU)
  - Quality: High (bitrate 10-20 Mbps)
  - Format: MP4
- [ ] Test recording 30 seconds and verify quality
- [ ] Set up hotkeys for start/stop recording
- [ ] Create recording directory with ample space (10GB+)

### Audio Setup
- [ ] Connect external microphone (if available)
- [ ] Test microphone levels in recording software
- [ ] Reduce background noise (close windows, turn off fans)
- [ ] Test audio recording and playback
- [ ] Have water nearby (stay hydrated for voiceover)
- [ ] Do vocal warm-ups (read script aloud once)

### Application Setup

**Avalonia Demo:**
- [ ] Build and run Avalonia demo app
- [ ] Position window in center of screen
- [ ] Test all interactive elements work
- [ ] Verify design tokens are applied correctly
- [ ] Check that all buttons/inputs respond to interactions
- [ ] Close any error/warning dialogs

**Unity Demo:**
- [ ] Open Unity Editor
- [ ] Open Forge Design System demo window
- [ ] Position window for good visibility
- [ ] Test all interactive elements
- [ ] Clear console logs
- [ ] Set Unity to fullscreen or maximize window

**Code Editor (for walkthrough):**
- [ ] Open VS Code, Visual Studio, or preferred editor
- [ ] Open three files side-by-side:
  - src/Nexo.Core.UI/Primitives/Button.cs
  - src/Nexo.Core.UI.Avalonia/Renderers/ButtonRenderer.cs
  - src/Nexo.Core.UI.Unity/Renderers/ButtonRenderer.cs
- [ ] Use readable font size (14-16pt)
- [ ] Use high-contrast theme (dark mode or light mode)
- [ ] Collapse unnecessary code sections
- [ ] Highlight key lines (if possible)

---

## WINDOW ARRANGEMENT

### Option 1: Full-Screen Switch Method
- Record Avalonia demo (full screen)
- Stop recording
- Switch to Unity (full screen)
- Start recording
- Edit together in post-production

### Option 2: Side-by-Side Method
- [ ] Avalonia demo on left (960px width)
- [ ] Unity demo on right (960px width)
- [ ] Record both simultaneously

### Option 3: Sequence Method (Recommended)
1. Intro screen (logo/title)
2. Problem statement (slides or screen)
3. Architecture diagram
4. Avalonia demo (full screen, 1920x1080)
5. Unity demo (full screen, 1920x1080)
6. Code walkthrough (editor, 1920x1080)
7. Impact/metrics (slides or screen)
8. Closing (logo)

---

## RECORDING SEQUENCE

### Take 1: Intro & Problem (0:00 - 0:45)
- [ ] Record intro sequence
- [ ] Record problem statement
- [ ] Show architecture diagram
- [ ] Review and retake if needed

### Take 2: Avalonia Demo (1:15 - 2:15)
- [ ] Start recording
- [ ] Open Avalonia demo
- [ ] Follow script precisely
- [ ] Show button variants with hover interactions
- [ ] Show input states
- [ ] Show card layouts
- [ ] Speak clearly and at moderate pace
- [ ] Stop recording
- [ ] Review footage

### Take 3: Unity Demo (2:15 - 3:15)
- [ ] Start recording
- [ ] Open Unity demo window
- [ ] Follow script precisely
- [ ] Show same components in Unity
- [ ] Emphasize framework differences
- [ ] Show interactions
- [ ] Stop recording
- [ ] Review footage

### Take 4: Code Walkthrough (3:15 - 4:00)
- [ ] Start recording
- [ ] Open code editor with three-panel view
- [ ] Slowly pan through Core.UI code
- [ ] Show Avalonia renderer
- [ ] Show Unity renderer
- [ ] Highlight design tokens
- [ ] Mention automation future
- [ ] Stop recording
- [ ] Review footage

### Take 5: Impact & Closing (4:00 - 4:20)
- [ ] Start recording
- [ ] Show metrics (20 hours → <1 hour)
- [ ] Show ROI ($450K savings)
- [ ] Show Forge logo
- [ ] Say "Forge. Development Evolved."
- [ ] Stop recording
- [ ] Review footage

---

## RECORDING TIPS

### Camera/Mouse Movements
- [ ] Move mouse slowly and deliberately
- [ ] Pause on important elements (1-2 seconds)
- [ ] Use smooth scrolling (not jarky)
- [ ] Avoid rapid clicking or movements
- [ ] Show cursor clearly (large cursor if needed)

### Voiceover
- [ ] Speak clearly and at moderate pace
- [ ] Pause between sections (easier to edit)
- [ ] Emphasize key metrics and terms
- [ ] Vary tone to maintain interest
- [ ] Re-record sections that feel rushed or unclear

### Timing
- [ ] Don't rush - better to be slightly slow than too fast
- [ ] Allow 1-2 second pauses between major transitions
- [ ] Give visuals time to register before moving on
- [ ] Total should be 3:30 - 4:30 (aim for 4:00)

---

## POST-RECORDING REVIEW

### Quality Check
- [ ] Watch entire recording without audio
- [ ] Check for visual glitches or errors
- [ ] Verify text is readable
- [ ] Check that interactions are visible
- [ ] Verify color accuracy

### Audio Check
- [ ] Listen to audio with headphones
- [ ] Check for background noise
- [ ] Verify volume levels are consistent
- [ ] Check for audio pops or clicks
- [ ] Verify script was followed accurately

### Retakes Needed?
- [ ] List sections that need retakes
- [ ] Note timestamp and reason
- [ ] Re-record only those sections
- [ ] Verify retakes are better

---

## EDITING CHECKLIST

### Import & Organization
- [ ] Import all footage into editing software (Premiere, Final Cut, DaVinci)
- [ ] Label clips clearly (intro, avalonia-demo, unity-demo, code, closing)
- [ ] Create backup of raw footage

### Basic Editing
- [ ] Trim beginning/end of each clip
- [ ] Remove mistakes, long pauses, "ums"
- [ ] Add 1-2 second transitions between major sections (fade, dissolve)
- [ ] Ensure timing matches script (3:30 - 4:30 total)

### Visual Enhancements
- [ ] Add zoom-ins on important code sections
- [ ] Add highlights/boxes around mentioned elements
- [ ] Add text overlays for key metrics:
  - "20 hours → <1 hour"
  - "94% cost reduction"
  - "$450K annual savings"
- [ ] Add arrows or pointers where helpful
- [ ] Color grade for consistency (if needed)

### Audio Enhancements
- [ ] Normalize audio levels
- [ ] Remove background noise (use noise reduction)
- [ ] Add subtle background music (royalty-free)
  - Volume: Very low (10-20% of voice)
  - Genre: Ambient, tech, corporate (non-distracting)
- [ ] Add sound effects (optional, subtle):
  - "Whoosh" for transitions
  - "Click" for button interactions
  - Keep it minimal and professional

### Text & Graphics
- [ ] Add title cards:
  - Opening: "Forge: Framework-Agnostic Design System"
  - Section intros if needed
  - Closing: "Forge. Development Evolved."
- [ ] Add subtitles/captions (for accessibility)
  - Use clean sans-serif font
  - White text with black background or shadow
  - Bottom center position
- [ ] Add logo watermark (bottom corner, subtle)

### Final Polish
- [ ] Watch entire video 2-3 times
- [ ] Check that pacing feels right
- [ ] Verify all text overlays are readable
- [ ] Ensure transitions are smooth
- [ ] Check audio-visual sync
- [ ] Verify no jarring cuts or jumps

---

## EXPORT SETTINGS

### Primary Export (YouTube, Vimeo)
- [ ] Resolution: 1920x1080
- [ ] Frame rate: 60fps
- [ ] Codec: H.264
- [ ] Bitrate: 10-15 Mbps (high quality)
- [ ] Audio: AAC, 192kbps, stereo
- [ ] Format: MP4

### Social Media Exports
- [ ] LinkedIn: 1920x1080, 60fps, MP4
- [ ] Twitter: 1280x720, 30fps, MP4(smaller file)
- [ ] Instagram: 1080x1080 (square crop), 30fps

### Short Clips
- [ ] Create 60-second "teaser" version
- [ ] Create 15-second "highlight" version
- [ ] Export key moments as GIFs for social media

---

## POST-PRODUCTION DELIVERABLES

- [ ] Full 4-minute demo video (primary)
- [ ] 60-second teaser (for social media)
- [ ] 15-second highlight (for ads/quick shares)
- [ ] 3-5 GIFs of key moments (button variants, architecture, metrics)
- [ ] Thumbnail image (1920x1080, eye-catching)
- [ ] Closed captions/subtitles file (.srt)

---

## DISTRIBUTION CHECKLIST

### Upload Locations
- [ ] YouTube (unlisted initially, then public)
- [ ] Vimeo (as backup/portfolio)
- [ ] GitHub README (embed YouTube link)
- [ ] Website/landing page (when created)
- [ ] LinkedIn post
- [ ] Twitter/X post
- [ ] Send to investors/potential customers

### Video Description (YouTube/Vimeo)
```
Forge: Framework-Agnostic Design System Demo

See how framework-agnostic UI patterns work across Avalonia and Unity with zero code duplication.

This demo shows a manually-built system that proves cross-framework pattern extraction and generation works. The same primitives (Button, Input, Card) render identically across different frameworks using shared design tokens and framework-specific renderers.

Key Metrics:
• 80% code reuse across frameworks
• 20 hours manual build time for 2 frameworks
• Projected <1 hour with AI automation (95% time reduction)
• $450K annual savings for 10-person team

Learn more: https://github.com/IanFrelinger/Nexo

#DeveloperTools #SoftwareDevelopment #UIComponents #FrameworkAgnostic #AI #Productivity
```

### Thumbnail Ideas
- Architecture diagram with "80% Code Reuse"
- Side-by-side Avalonia + Unity screenshots
- "20 hours → <1 hour" with arrow
- Forge logo with tagline

---

## FINAL REVIEW QUESTIONS

Before publishing, answer these:
- [ ] Does the video clearly explain the problem?
- [ ] Is the value proposition obvious?
- [ ] Are the demos smooth and professional?
- [ ] Is the audio clear and understandable?
- [ ] Are the metrics mentioned clearly?
- [ ] Does it end with a clear call to action?
- [ ] Would a non-technical person understand the value?
- [ ] Would a technical person be impressed by the execution?
- [ ] Is it the right length (not too long, not too short)?
- [ ] Would you show this to an investor confidently?

If all answers are YES → Publish!

---

## EMERGENCY TROUBLESHOOTING

**If recording fails:**
- Save immediately
- Check disk space
- Check if OBS/recorder crashed
- Restart computer if needed
- Restore from backup if file corrupted

**If audio has issues:**
- Use noise reduction in Audacity
- Re-record voiceover separately
- Sync with video in editing
- Use text overlays if audio unfixable

**If video quality is poor:**
- Check recording settings (should be 1080p, 60fps)
- Re-record affected sections
- Use upscaling tools (Topaz Video Enhance) as last resort

---

## TIME ESTIMATES

- Setup: 30-60 minutes
- Recording all takes: 60-90 minutes
- Reviewing footage: 15-30 minutes
- Basic editing: 2-3 hours
- Advanced editing (graphics, music): 2-3 hours
- Export and upload: 30-60 minutes

**Total: 6-9 hours** for professional-quality demo video

---

## SUCCESS CRITERIA

✅ Professional-quality video suitable for investor pitches  
✅ Clear demonstration of framework-agnostic patterns  
✅ Compelling narrative arc (problem → solution → impact)  
✅ Quantified metrics prominently featured  
✅ Smooth, polished, no obvious errors  
✅ Duration: 3:30 - 4:30 (sweet spot for engagement)  
✅ Ready to share publicly  

---

**READY TO RECORD? Go through this checklist step by step. Quality over speed.**
