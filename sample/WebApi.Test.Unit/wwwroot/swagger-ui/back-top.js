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
    const style = document.createElement("style");
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
        transition: all 0.3s ease !important;
        cursor: pointer;
        outline: none !important;
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
    if (document.getElementById("back-to-top-btn")) {
      return;
    }

    const createButton = () => {
      const button = document.createElement("button");
      button.id = "back-to-top-btn";
      button.className = "back-to-top-btn";
      button.type = "button";
      button.innerHTML = `<svg viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5" fill="none" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19V5M5 12l7-7 7 7"/></svg>`;
      button.setAttribute("title", "Back to Top");
      button.setAttribute("aria-label", "Back to Top");
      button.style.display = "none"; // Initially hidden

      button.addEventListener("click", (e) => {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: "smooth" });
      });

      document.body.appendChild(button);

      const handleScroll = () => {
        if (window.scrollY > 200) {
          button.style.display = "block";
        } else {
          button.style.display = "none";
        }
      };

      window.addEventListener("scroll", throttle(handleScroll, 100));
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
