// Smooth scrolling for navigation links
document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
  anchor.addEventListener("click", function (e) {
    e.preventDefault();
    const target = document.querySelector(this.getAttribute("href"));
    if (target) {
      target.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }
  });
});

// Search functionality
const searchButton = document.querySelector("button:has(.fa-search)");
if (searchButton) {
  searchButton.addEventListener("click", function () {
    const type = document.querySelector("select").value;
    const title = document.querySelector('input[placeholder="Title"]').value;
    const address = document.querySelector(
      'input[placeholder="Address"]'
    ).value;
    const min = document.querySelector("select:nth-of-type(2)").value;
    const max = document.querySelector("select:nth-of-type(3)").value;

    console.log("Search parameters:", { type, title, address, min, max });
    // Here you can add actual search functionality
    alert("Search functionality will be implemented here!");
  });
}

// Property card hover effects
document
  .querySelectorAll(".bg-white.rounded-lg.overflow-hidden.shadow-md")
  .forEach((card) => {
    card.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-5px)";
      this.style.transition = "transform 0.3s ease";
    });

    card.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)";
    });
  });

// Location card click handlers
document
  .querySelectorAll(".relative.rounded-lg.overflow-hidden.group")
  .forEach((card) => {
    card.addEventListener("click", function () {
      const locationName = this.querySelector("h3").textContent;
      console.log("Location clicked:", locationName);
      // Here you can add navigation to location details
    });
  });

// View all properties button
const viewAllButton = Array.from(document.querySelectorAll("button")).find(
  (btn) => btn.textContent.includes("View all properties")
);
if (viewAllButton) {
  viewAllButton.addEventListener("click", function () {
    console.log("View all properties clicked");
    // Navigate to properties page
  });
}

// CTA buttons
document.querySelectorAll("button").forEach((button) => {
  if (
    button.textContent.includes("Browse properties") ||
    button.textContent.includes("Contact US")
  ) {
    button.addEventListener("click", function () {
      if (this.textContent.includes("Browse properties")) {
        console.log("Browse properties clicked");
        // Navigate to properties page
      } else if (this.textContent.includes("Contact US")) {
        console.log("Contact US clicked");
        // Navigate to contact page or open contact form
      }
    });
  }
});

// Header scroll effect
let lastScroll = 0;
const header = document.querySelector("header");

window.addEventListener("scroll", () => {
  const currentScroll = window.pageYOffset;

  if (currentScroll > 100) {
    header.style.boxShadow = "0 2px 10px rgba(0,0,0,0.1)";
  } else {
    header.style.boxShadow = "0 1px 3px rgba(0,0,0,0.05)";
  }

  lastScroll = currentScroll;
});

// Image lazy loading (if needed)
if ("IntersectionObserver" in window) {
  const imageObserver = new IntersectionObserver((entries, observer) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        const img = entry.target;
        img.src = img.dataset.src || img.src;
        img.classList.remove("lazy");
        observer.unobserve(img);
      }
    });
  });

  document.querySelectorAll("img").forEach((img) => {
    imageObserver.observe(img);
  });
}

// Form validation for search (if needed)
const searchForm = document.querySelector(".space-y-4");
if (searchForm) {
  const inputs = searchForm.querySelectorAll("input, select");
  inputs.forEach((input) => {
    input.addEventListener("blur", function () {
      if (this.value.trim() === "" && this.hasAttribute("required")) {
        this.style.borderColor = "#DC2626";
      } else {
        this.style.borderColor = "#D1D5DB";
      }
    });
  });
}

// Animate statistics on scroll
const animateCounter = (element, target, duration = 2000) => {
  let start = 0;
  const increment = target / (duration / 16);
  const timer = setInterval(() => {
    start += increment;
    if (start >= target) {
      element.textContent =
        target + (element.textContent.includes("%") ? "%" : "");
      clearInterval(timer);
    } else {
      element.textContent =
        Math.floor(start) + (element.textContent.includes("%") ? "%" : "");
    }
  }, 16);
};

const observerOptions = {
  threshold: 0.5,
  rootMargin: "0px",
};

const statsObserver = new IntersectionObserver((entries) => {
  entries.forEach((entry) => {
    if (entry.isIntersecting) {
      const statElements = entry.target.querySelectorAll("h3");
      statElements.forEach((stat) => {
        const text = stat.textContent;
        const number = parseFloat(text.replace(/[^0-9.]/g, ""));
        if (number && !stat.classList.contains("animated")) {
          stat.classList.add("animated");
          animateCounter(stat, number);
        }
      });
      statsObserver.unobserve(entry.target);
    }
  });
}, observerOptions);

const statsSection = document.querySelector(".gradient-pink");
if (statsSection) {
  statsObserver.observe(statsSection);
}

console.log("DUBiRent website loaded successfully!");
