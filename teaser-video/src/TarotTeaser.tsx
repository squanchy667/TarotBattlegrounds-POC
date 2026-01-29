import {
  AbsoluteFill,
  Img,
  interpolate,
  Sequence,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";

// Scene 1: Logo Intro (frames 0-75)
const LogoIntro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const logoScale = spring({
    frame,
    fps,
    config: { damping: 12, stiffness: 80 },
  });

  const logoRotation = interpolate(frame, [0, 75], [180, 0], {
    extrapolateRight: "clamp",
  });

  const glowOpacity = interpolate(frame, [30, 60], [0, 0.8], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const textOpacity = interpolate(frame, [45, 65], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <AbsoluteFill
      style={{
        justifyContent: "center",
        alignItems: "center",
        backgroundColor: "#121620",
      }}
    >
      {/* Glow effect */}
      <div
        style={{
          position: "absolute",
          width: 500,
          height: 500,
          borderRadius: "50%",
          background: "radial-gradient(circle, rgba(64,144,255,0.4) 0%, transparent 70%)",
          opacity: glowOpacity,
          filter: "blur(40px)",
        }}
      />

      {/* Logo */}
      <Img
        src={staticFile("images/logo.png")}
        style={{
          width: 400,
          transform: `scale(${logoScale}) rotate(${logoRotation}deg)`,
        }}
      />

      {/* Tagline */}
      <div
        style={{
          position: "absolute",
          bottom: 200,
          opacity: textOpacity,
          fontFamily: "Georgia, serif",
          fontSize: 36,
          color: "#C0A060",
          letterSpacing: 8,
          textTransform: "uppercase",
        }}
      >
        The Universe is a Machine
      </div>
    </AbsoluteFill>
  );
};

// Scene 2: Tribes Reveal (frames 75-180)
const TribesReveal: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const tribes = [
    { name: "Swords", image: "swords.png", color: "#A03030", delay: 0 },
    { name: "Wands", image: "wands.png", color: "#9040A0", delay: 15 },
    { name: "Cups", image: "cups.png", color: "#4090FF", delay: 30 },
    { name: "Pentacles", image: "pentacles.png", color: "#C0A060", delay: 45 },
  ];

  return (
    <AbsoluteFill
      style={{
        backgroundColor: "#121620",
        justifyContent: "center",
        alignItems: "center",
      }}
    >
      {/* Background pattern */}
      <Img
        src={staticFile("images/background.png")}
        style={{
          position: "absolute",
          width: "100%",
          height: "100%",
          objectFit: "cover",
          opacity: 0.3,
        }}
      />

      {/* Title */}
      <div
        style={{
          position: "absolute",
          top: 80,
          fontFamily: "Georgia, serif",
          fontSize: 48,
          color: "#C0A060",
          letterSpacing: 6,
          textTransform: "uppercase",
          opacity: interpolate(frame, [0, 20], [0, 1], { extrapolateRight: "clamp" }),
        }}
      >
        Choose Your Tribe
      </div>

      {/* Tribes container */}
      <div
        style={{
          display: "flex",
          gap: 80,
          marginTop: 40,
        }}
      >
        {tribes.map((tribe, index) => {
          const tribeFrame = frame - tribe.delay;
          const scale = spring({
            frame: Math.max(0, tribeFrame),
            fps,
            config: { damping: 10, stiffness: 100 },
          });

          const opacity = interpolate(tribeFrame, [0, 15], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          });

          const glowIntensity = interpolate(
            tribeFrame,
            [20, 40, 60, 80],
            [0, 0.6, 0.3, 0.5],
            { extrapolateLeft: "clamp", extrapolateRight: "clamp" }
          );

          return (
            <div
              key={tribe.name}
              style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                opacity,
              }}
            >
              <div
                style={{
                  position: "relative",
                }}
              >
                {/* Glow */}
                <div
                  style={{
                    position: "absolute",
                    inset: -30,
                    borderRadius: "50%",
                    background: `radial-gradient(circle, ${tribe.color}80 0%, transparent 70%)`,
                    opacity: glowIntensity,
                    filter: "blur(20px)",
                  }}
                />
                <Img
                  src={staticFile(`images/${tribe.image}`)}
                  style={{
                    width: 200,
                    height: 200,
                    objectFit: "contain",
                    transform: `scale(${scale})`,
                  }}
                />
              </div>
              <div
                style={{
                  marginTop: 20,
                  fontFamily: "Georgia, serif",
                  fontSize: 28,
                  color: tribe.color,
                  letterSpacing: 4,
                  textTransform: "uppercase",
                  opacity: interpolate(tribeFrame, [15, 30], [0, 1], {
                    extrapolateLeft: "clamp",
                    extrapolateRight: "clamp",
                  }),
                }}
              >
                {tribe.name}
              </div>
            </div>
          );
        })}
      </div>
    </AbsoluteFill>
  );
};

// Scene 3: Battle Scene (frames 180-255)
const BattleScene: React.FC = () => {
  const frame = useCurrentFrame();

  const imageScale = interpolate(frame, [0, 75], [1.2, 1], {
    extrapolateRight: "clamp",
  });

  const imageOpacity = interpolate(frame, [0, 20], [0, 1], {
    extrapolateRight: "clamp",
  });

  const overlayOpacity = interpolate(frame, [0, 30], [0.8, 0.3], {
    extrapolateRight: "clamp",
  });

  const textOpacity = interpolate(frame, [30, 50], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <AbsoluteFill style={{ backgroundColor: "#121620" }}>
      {/* Hero image with zoom effect */}
      <Img
        src={staticFile("images/hero.png")}
        style={{
          position: "absolute",
          width: "100%",
          height: "100%",
          objectFit: "cover",
          transform: `scale(${imageScale})`,
          opacity: imageOpacity,
        }}
      />

      {/* Dark overlay */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          background: "linear-gradient(to bottom, rgba(18,22,32,0.9) 0%, transparent 30%, transparent 70%, rgba(18,22,32,0.9) 100%)",
          opacity: overlayOpacity,
        }}
      />

      {/* Battle text */}
      <div
        style={{
          position: "absolute",
          bottom: 120,
          left: 0,
          right: 0,
          textAlign: "center",
          opacity: textOpacity,
        }}
      >
        <div
          style={{
            fontFamily: "Georgia, serif",
            fontSize: 64,
            color: "#C0A060",
            letterSpacing: 8,
            textTransform: "uppercase",
            textShadow: "0 0 40px rgba(64,144,255,0.6)",
          }}
        >
          Let Fate Collide
        </div>
      </div>
    </AbsoluteFill>
  );
};

// Scene 4: Final CTA (frames 255-300)
const FinalCTA: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const logoScale = spring({
    frame,
    fps,
    config: { damping: 15, stiffness: 100 },
  });

  const titleOpacity = interpolate(frame, [10, 25], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const ctaOpacity = interpolate(frame, [25, 40], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const pulseGlow = interpolate(
    Math.sin(frame * 0.15),
    [-1, 1],
    [0.4, 0.8]
  );

  return (
    <AbsoluteFill
      style={{
        backgroundColor: "#121620",
        justifyContent: "center",
        alignItems: "center",
      }}
    >
      {/* Background pattern */}
      <Img
        src={staticFile("images/background.png")}
        style={{
          position: "absolute",
          width: "100%",
          height: "100%",
          objectFit: "cover",
          opacity: 0.2,
        }}
      />

      {/* Logo glow */}
      <div
        style={{
          position: "absolute",
          width: 600,
          height: 600,
          borderRadius: "50%",
          background: "radial-gradient(circle, rgba(64,144,255,0.5) 0%, transparent 60%)",
          opacity: pulseGlow,
          filter: "blur(60px)",
        }}
      />

      {/* Logo */}
      <Img
        src={staticFile("images/logo.png")}
        style={{
          width: 350,
          transform: `scale(${logoScale})`,
          marginBottom: 40,
        }}
      />

      {/* Title */}
      <div
        style={{
          marginTop: 30,
          opacity: titleOpacity,
          fontFamily: "Georgia, serif",
          fontSize: 72,
          fontWeight: "bold",
          color: "#C0A060",
          letterSpacing: 12,
          textTransform: "uppercase",
          textShadow: "0 0 30px rgba(192,160,96,0.4)",
        }}
      >
        Tarot Battlegrounds
      </div>

      {/* CTA */}
      <div
        style={{
          marginTop: 50,
          opacity: ctaOpacity,
          padding: "20px 60px",
          border: "2px solid #4090FF",
          backgroundColor: "rgba(64,144,255,0.1)",
          fontFamily: "monospace",
          fontSize: 28,
          color: "#4090FF",
          letterSpacing: 6,
          textTransform: "uppercase",
        }}
      >
        Coming Soon
      </div>
    </AbsoluteFill>
  );
};

// Main Composition
export const TarotTeaser: React.FC = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: "#121620" }}>
      {/* Scene 1: Logo Intro - 0 to 2.5s (frames 0-75) */}
      <Sequence from={0} durationInFrames={75}>
        <LogoIntro />
      </Sequence>

      {/* Scene 2: Tribes Reveal - 2.5s to 6s (frames 75-180) */}
      <Sequence from={75} durationInFrames={105}>
        <TribesReveal />
      </Sequence>

      {/* Scene 3: Battle Scene - 6s to 8.5s (frames 180-255) */}
      <Sequence from={180} durationInFrames={75}>
        <BattleScene />
      </Sequence>

      {/* Scene 4: Final CTA - 8.5s to 10s (frames 255-300) */}
      <Sequence from={255} durationInFrames={45}>
        <FinalCTA />
      </Sequence>
    </AbsoluteFill>
  );
};
