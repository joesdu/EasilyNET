/**
 * Swagger UI Theme Toggle Script
 * Supports Dark/Light Mode with localStorage persistence
 */

(function () {
  "use strict";

  const THEME_KEY = "swagger-ui-theme";
  const DARK_THEME = "dark";
  const LIGHT_THEME = "light";

  /**
   * Get the current theme from localStorage or system preference
   */
  function getCurrentTheme() {
    // Check localStorage first
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) {
      return saved;
    }

    // Fall back to system preference
    if (
      window.matchMedia &&
      window.matchMedia("(prefers-color-scheme: dark)").matches
    ) {
      return DARK_THEME;
    }

    return LIGHT_THEME;
  }

  /**
   * Set the theme on the document
   */
  function setTheme(theme) {
    const html = document.documentElement;

    // Set or remove data-theme attribute
    if (theme === DARK_THEME) {
      html.setAttribute("data-theme", DARK_THEME);
    } else {
      html.removeAttribute("data-theme");
    }

    // Save to localStorage
    localStorage.setItem(THEME_KEY, theme);

    // Update button appearance
    updateThemeButtonText(theme);
  }

  /**
   * Toggle between themes
   */
  function toggleTheme() {
    const html = document.documentElement;
    const currentTheme = html.getAttribute("data-theme") || LIGHT_THEME;
    const newTheme = currentTheme === DARK_THEME ? LIGHT_THEME : DARK_THEME;
    setTheme(newTheme);
  }

  /**
   * Update the theme button text and icon
   */
  function updateThemeButtonText(theme) {
    const btn = document.getElementById("theme-toggle-btn");
    if (!btn) return;

    if (theme === LIGHT_THEME) {
      // Moon icon for switching to dark mode
      btn.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>`;
      btn.setAttribute("title", "Switch to Dark Mode");
      btn.setAttribute("aria-label", "Switch to Dark Mode");
    } else {
      // Sun icon for switching to light mode
      btn.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line></svg>`;
      btn.setAttribute("title", "Switch to Light Mode");
      btn.setAttribute("aria-label", "Switch to Light Mode");
    }
  }

  /**
   * Create and inject the theme toggle button
   */
  function injectThemeToggleButton() {
    // 避免重复创建按钮
    if (document.getElementById("theme-toggle-btn")) {
      return;
    }

    const createButton = () => {
      const topbar = document.querySelector(".topbar");
      if (!topbar) {
        // 如果 topbar 不存在,则稍后重试
        setTimeout(createButton, 100);
        return;
      }
      const button = document.createElement("button");
      button.id = "theme-toggle-btn";
      button.className = "theme-toggle-btn";
      button.type = "button";

      // Add click event
      button.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        toggleTheme();
      });

      // Add to topbar
      topbar.appendChild(button);

      // Update button text
      const currentTheme =
        document.documentElement.getAttribute("data-theme") || LIGHT_THEME;
      updateThemeButtonText(currentTheme);
    };

    // Try immediate creation
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", createButton);
    } else {
      createButton();
    }
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
      // Use SVG icon instead of emoji
      button.innerHTML = `<svg viewBox="0 0 24 24" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19V5M5 12l7-7 7 7"/></svg>`;
      button.setAttribute("title", "Back to Top");
      button.setAttribute("aria-label", "Back to Top");
      button.style.display = "none"; // Initially hidden

      button.addEventListener("click", (e) => {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: "smooth" });
      });

      document.body.appendChild(button);

      window.addEventListener("scroll", () => {
        if (window.scrollY > 200) {
          button.style.display = "block";
        } else {
          button.style.display = "none";
        }
      });
    };

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", createButton);
    } else {
      createButton();
    }
  }

  /**
   * Initialize theme
   */
  function initTheme() {
    // Get and set the theme
    const theme = getCurrentTheme();
    setTheme(theme);

    // Inject button after a brief delay to ensure DOM is ready
    setTimeout(() => {
      injectThemeToggleButton();
      injectBackToTopButton();
    }, 100);

    // Listen for system theme changes
    if (window.matchMedia) {
      const darkModeQuery = window.matchMedia("(prefers-color-scheme: dark)");

      const handleThemeChange = (e) => {
        // Only auto-switch if user hasn't set a preference
        if (!localStorage.getItem(THEME_KEY)) {
          setTheme(e.matches ? DARK_THEME : LIGHT_THEME);
        }
      };

      // Use modern addEventListener API only
      darkModeQuery.addEventListener("change", handleThemeChange);
    }
  }

  // Wait for DOM to be ready
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initTheme);
  } else {
    initTheme();
  }

  // Expose functions globally for external use
  window.toggleSwaggerTheme = toggleTheme;
  window.setSwaggerTheme = setTheme;
  window.getSwaggerTheme = () =>
    document.documentElement.getAttribute("data-theme") || LIGHT_THEME;
})();
