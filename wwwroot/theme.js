window.theme = {
  apply: function (theme) {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("theme", theme);
    return theme;
  },
  init: function () {
    var saved = localStorage.getItem("theme");
    if (saved === "light" || saved === "dark") return window.theme.apply(saved);
    return window.theme.apply(window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
  },
  toggle: function () {
    var next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    return window.theme.apply(next);
  }
};
