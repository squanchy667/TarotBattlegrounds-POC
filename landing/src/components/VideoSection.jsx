import { useState, useRef } from 'react'
import { motion } from 'framer-motion'
import './VideoSection.css'

export default function VideoSection({ isVisible }) {
  const [isPlaying, setIsPlaying] = useState(false)
  const videoRef = useRef(null)

  const handlePlayClick = () => {
    if (videoRef.current) {
      if (isPlaying) {
        videoRef.current.pause()
      } else {
        videoRef.current.play()
      }
      setIsPlaying(!isPlaying)
    }
  }

  const handleVideoEnd = () => {
    setIsPlaying(false)
  }

  return (
    <section id="video" className={`section video-section ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// PREVIEW</span>
        <h2>See the Arcana in motion</h2>
        <p className="section-desc">
          Watch how fate unfolds on the battlefield.
        </p>
      </motion.div>

      <motion.div
        className="video-container"
        initial={{ opacity: 0, scale: 0.95 }}
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6, delay: 0.2 }}
      >
        <div className="video-wrapper">
          <video
            ref={videoRef}
            className="teaser-video"
            poster="/images/hero-battle.webp"
            onEnded={handleVideoEnd}
            onClick={handlePlayClick}
            playsInline
            preload="none"
            width="2488"
            height="1527"
          >
            <source src="/tarot-battlegrounds-teaser.mp4" type="video/mp4" />
            Your browser does not support the video tag.
          </video>

          {!isPlaying && (
            <motion.button
              className="video-play-btn"
              onClick={handlePlayClick}
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              whileHover={{ scale: 1.1 }}
              whileTap={{ scale: 0.95 }}
            >
              <span className="play-icon">▶</span>
            </motion.button>
          )}

          <div className="video-frame-corner video-frame-tl" />
          <div className="video-frame-corner video-frame-tr" />
          <div className="video-frame-corner video-frame-bl" />
          <div className="video-frame-corner video-frame-br" />
        </div>

        <div className="video-glow" aria-hidden="true" />
      </motion.div>

      <motion.p
        className="video-caption"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        viewport={{ once: true }}
        transition={{ delay: 0.4, duration: 0.6 }}
      >
        Early development footage — final game may vary
      </motion.p>
    </section>
  )
}
