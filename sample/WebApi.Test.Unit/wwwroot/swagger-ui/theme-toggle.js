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
      btn.innerHTML = "🌙";
      btn.setAttribute("title", "Switch to Dark Mode");
      btn.setAttribute("aria-label", "Switch to Dark Mode");
    } else {
      btn.innerHTML = "☀️";
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

      // Add to body
      document.body.appendChild(button);

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
   * Initialize theme
   */
  function initTheme() {
    // Get and set the theme
    const theme = getCurrentTheme();
    setTheme(theme);

    // Inject button after a brief delay to ensure DOM is ready
    setTimeout(() => {
      injectThemeToggleButton();
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
