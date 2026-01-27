import { useEffect, useRef } from 'react'
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

gsap.registerPlugin(ScrollTrigger)

/**
 * Custom hook for parallax scrolling effects
 * @param {Object} options - Parallax options
 * @param {number} options.speed - Parallax speed multiplier (negative for reverse)
 * @param {string} options.direction - 'y' or 'x' for vertical/horizontal parallax
 * @returns {React.RefObject} - Reference to attach to element
 */
export function useParallax({ speed = 0.5, direction = 'y' } = {}) {
  const elementRef = useRef(null)

  useEffect(() => {
    const element = elementRef.current
    if (!element) return

    const movement = speed * 100

    const tween = gsap.to(element, {
      [direction]: movement,
      ease: 'none',
      scrollTrigger: {
        trigger: element,
        start: 'top bottom',
        end: 'bottom top',
        scrub: true,
      },
    })

    return () => {
      tween.scrollTrigger?.kill()
      tween.kill()
    }
  }, [speed, direction])

  return elementRef
}

/**
 * Hook for layered parallax depth effect
 * @param {number} depth - Depth layer (0 = no movement, 1 = max movement)
 * @returns {React.RefObject} - Reference to attach to element
 */
export function useParallaxDepth(depth = 0.5) {
  const elementRef = useRef(null)

  useEffect(() => {
    const element = elementRef.current
    if (!element) return

    const handleScroll = () => {
      const rect = element.getBoundingClientRect()
      const scrolled = window.scrollY
      const rate = scrolled * depth * 0.1
      element.style.transform = `translateY(${rate}px)`
    }

    window.addEventListener('scroll', handleScroll, { passive: true })

    return () => {
      window.removeEventListener('scroll', handleScroll)
    }
  }, [depth])

  return elementRef
}

/**
 * Hook for mouse-responsive parallax effect
 * @param {Object} options - Mouse parallax options
 * @param {number} options.intensity - Movement intensity
 * @param {boolean} options.reverse - Reverse movement direction
 * @returns {React.RefObject} - Reference to attach to element
 */
export function useMouseParallax({ intensity = 20, reverse = false } = {}) {
  const elementRef = useRef(null)

  useEffect(() => {
    const element = elementRef.current
    if (!element) return

    const handleMouseMove = (e) => {
      const rect = element.getBoundingClientRect()
      const centerX = rect.left + rect.width / 2
      const centerY = rect.top + rect.height / 2

      const moveX = ((e.clientX - centerX) / rect.width) * intensity * (reverse ? -1 : 1)
      const moveY = ((e.clientY - centerY) / rect.height) * intensity * (reverse ? -1 : 1)

      gsap.to(element, {
        x: moveX,
        y: moveY,
        duration: 0.5,
        ease: 'power2.out',
      })
    }

    const handleMouseLeave = () => {
      gsap.to(element, {
        x: 0,
        y: 0,
        duration: 0.5,
        ease: 'power2.out',
      })
    }

    const parent = element.parentElement
    parent?.addEventListener('mousemove', handleMouseMove)
    parent?.addEventListener('mouseleave', handleMouseLeave)

    return () => {
      parent?.removeEventListener('mousemove', handleMouseMove)
      parent?.removeEventListener('mouseleave', handleMouseLeave)
    }
  }, [intensity, reverse])

  return elementRef
}

/**
 * Hook for hero section layered parallax
 * Creates a depth effect with multiple layers moving at different speeds
 * @returns {Object} - Object containing refs for different layers
 */
export function useHeroParallax() {
  const bgRef = useRef(null)
  const midRef = useRef(null)
  const fgRef = useRef(null)

  useEffect(() => {
    const layers = [
      { ref: bgRef, speed: 0.2 },
      { ref: midRef, speed: 0.5 },
      { ref: fgRef, speed: 0.8 },
    ]

    const tweens = layers
      .filter((layer) => layer.ref.current)
      .map((layer) =>
        gsap.to(layer.ref.current, {
          y: layer.speed * 200,
          ease: 'none',
          scrollTrigger: {
            trigger: layer.ref.current.parentElement,
            start: 'top top',
            end: 'bottom top',
            scrub: true,
          },
        })
      )

    return () => {
      tweens.forEach((tween) => {
        tween.scrollTrigger?.kill()
        tween.kill()
      })
    }
  }, [])

  return { bgRef, midRef, fgRef }
}

export default useParallax
