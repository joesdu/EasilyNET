/**
 * Swagger UI Helper Script
 * Adds Back to Top functionality
 */

(function () {
  "use strict";

  /**
   * Throttle function to limit the rate at which a function can be called.
   */
  function throttle(func, limit) {
    let inThrottle;
    return function () {
      const args = arguments;
      const context = this;
      if (!inThrottle) {
        func.apply(context, args);
        inThrottle = true;
        setTimeout(() => (inThrottle = false), limit);
      }
    };
  }

  /**
   * Inject CSS styles
   */
  function injectStyles() {
    const existingStyle = document.getElementById("back-to-top-style");
    if (existingStyle) {
      existingStyle.remove();
    }
    const style = document.createElement("style");
    style.id = "back-to-top-style";
    style.textContent = `
      .back-to-top-btn {
        position: fixed;
        bottom: 30px;
        right: 30px;
        z-index: 10000;
        background-color: #212121;
        color: #fff !important;
        border: none !important;
        border-radius: 50% !important;
        width: 48px;
        height: 48px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
        transition: all 0.3s ease, transform 0.3s ease !important;
        cursor: pointer;
        outline: none !important;
        opacity: 0;
        visibility: hidden;
        transform: translateY(20px);
      }

      .back-to-top-btn.show {
        opacity: 1;
        visibility: visible;
        transform: translateY(0);
      }

      .back-to-top-btn svg {
        width: 24px;
        height: 24px;
      }

      .back-to-top-btn:hover {
        background-color: #000;
        transform: translateY(-2px);
        box-shadow: 0 6px 16px rgba(0, 0, 0, 0.5);
      }

      .back-to-top-btn:active {
        transform: translateY(0);
      }

      .swagger-ui-dark .back-to-top-btn {
        background-color: rgba(255, 255, 255, 0.9);
        color: #333 !important;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
      }

      .swagger-ui-dark .back-to-top-btn:hover {
        background-color: #fff;
        transform: translateY(-2px);
      }

      @media (max-width: 768px) {
        .back-to-top-btn {
          bottom: 20px;
          right: 20px;
          width: 40px;
          height: 40px;
        }

        .back-to-top-btn svg {
          width: 20px;
          height: 20px;
        }
      }
    `;
    document.head.appendChild(style);
  }

  /**
   * Create and inject the back-to-top button
   */
  function injectBackToTopButton() {
    const existingButton = document.getElementById("back-to-top-btn");
    if (existingButton) {
      existingButton.remove();
    }

    if (window.swaggerBackToTopScrollHandler) {
      window.removeEventListener("scroll", window.swaggerBackToTopScrollHandler);
    }

    const createButton = () => {
      const button = document.createElement("button");
      button.id = "back-to-top-btn";
      button.className = "back-to-top-btn";
      button.type = "button";
      button.innerHTML = `<svg viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5" fill="none" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19V5M5 12l7-7 7 7"/></svg>`;
      button.setAttribute("title", "Back to Top");
      button.setAttribute("aria-label", "Back to Top");

      button.addEventListener("click", (e) => {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: "smooth" });
      });

      document.body.appendChild(button);

      const handleScroll = () => {
        if (window.scrollY > 200) {
          button.classList.add("show");
        } else {
          button.classList.remove("show");
        }
      };

      const throttledScroll = throttle(handleScroll, 100);
      window.swaggerBackToTopScrollHandler = throttledScroll;
      window.addEventListener("scroll", throttledScroll);
      // Ensure correct initial visibility even if the page loads scrolled down
      handleScroll();
    };

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", createButton);
    } else {
      createButton();
    }
  }

  // Initialize
  injectStyles();
  injectBackToTopButton();
})();
