import { useEffect, useRef, useState } from 'react'
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

gsap.registerPlugin(ScrollTrigger)

/**
 * Custom hook for scroll-triggered animations using GSAP ScrollTrigger
 * @param {Object} options - Animation options
 * @param {string} options.animation - Animation type: 'fadeUp', 'fadeIn', 'slideLeft', 'slideRight', 'scale', 'stagger'
 * @param {number} options.delay - Delay before animation starts
 * @param {number} options.duration - Animation duration
 * @param {string} options.start - ScrollTrigger start position
 * @param {boolean} options.once - Whether animation should only play once
 * @returns {[React.RefObject, boolean]} - Reference to attach to element and visibility state
 */
export function useScrollAnimation({
  animation = 'fadeUp',
  delay = 0,
  duration = 0.8,
  start = 'top 85%',
  once = true,
} = {}) {
  const elementRef = useRef(null)
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    const element = elementRef.current
    if (!element) return

    const getAnimationProps = () => {
      switch (animation) {
        case 'fadeUp':
          return {
            from: { opacity: 0, y: 50 },
            to: { opacity: 1, y: 0 },
          }
        case 'fadeIn':
          return {
            from: { opacity: 0 },
            to: { opacity: 1 },
          }
        case 'slideLeft':
          return {
            from: { opacity: 0, x: 100 },
            to: { opacity: 1, x: 0 },
          }
        case 'slideRight':
          return {
            from: { opacity: 0, x: -100 },
            to: { opacity: 1, x: 0 },
          }
        case 'scale':
          return {
            from: { opacity: 0, scale: 0.8 },
            to: { opacity: 1, scale: 1 },
          }
        default:
          return {
            from: { opacity: 0, y: 30 },
            to: { opacity: 1, y: 0 },
          }
      }
    }

    const { from, to } = getAnimationProps()

    gsap.set(element, from)

    const trigger = ScrollTrigger.create({
      trigger: element,
      start,
      onEnter: () => {
        setIsVisible(true)
        gsap.to(element, {
          ...to,
          duration,
          delay,
          ease: 'power3.out',
        })
      },
      onLeaveBack: once
        ? undefined
        : () => {
            setIsVisible(false)
            gsap.to(element, {
              ...from,
              duration: duration * 0.5,
              ease: 'power2.in',
            })
          },
      once,
    })

    return () => {
      trigger.kill()
    }
  }, [animation, delay, duration, start, once])

  return [elementRef, isVisible]
}

/**
 * Hook for staggered children animations
 * @param {Object} options - Animation options
 * @param {number} options.stagger - Stagger delay between children
 * @param {string} options.childSelector - CSS selector for children
 * @returns {React.RefObject} - Reference to attach to parent element
 */
export function useStaggerAnimation({
  stagger = 0.1,
  childSelector = ':scope > *',
  delay = 0,
  duration = 0.6,
  start = 'top 85%',
} = {}) {
  const containerRef = useRef(null)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const children = container.querySelectorAll(childSelector)
    if (!children.length) return

    gsap.set(children, { opacity: 0, y: 40 })

    const trigger = ScrollTrigger.create({
      trigger: container,
      start,
      onEnter: () => {
        gsap.to(children, {
          opacity: 1,
          y: 0,
          duration,
          delay,
          stagger,
          ease: 'power3.out',
        })
      },
      once: true,
    })

    return () => {
      trigger.kill()
    }
  }, [stagger, childSelector, delay, duration, start])

  return containerRef
}

/**
 * Hook for text reveal animation (word by word)
 * @param {Object} options - Animation options
 * @returns {React.RefObject} - Reference to attach to text element
 */
export function useTextReveal({ delay = 0, stagger = 0.05, start = 'top 85%' } = {}) {
  const textRef = useRef(null)

  useEffect(() => {
    const element = textRef.current
    if (!element) return

    const text = element.textContent
    const words = text.split(' ')

    element.innerHTML = words
      .map((word) => `<span class="word-wrap"><span class="word">${word}</span></span>`)
      .join(' ')

    const wordElements = element.querySelectorAll('.word')

    gsap.set(wordElements, { y: '100%', opacity: 0 })

    const trigger = ScrollTrigger.create({
      trigger: element,
      start,
      onEnter: () => {
        gsap.to(wordElements, {
          y: '0%',
          opacity: 1,
          duration: 0.5,
          delay,
          stagger,
          ease: 'power3.out',
        })
      },
      once: true,
    })

    return () => {
      trigger.kill()
      element.textContent = text
    }
  }, [delay, stagger, start])

  return textRef
}

export default useScrollAnimation
