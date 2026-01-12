// Main tab switching (Equipment, Crafting, Blood Pool, Attributes, Bloodcraft)
    document.querySelectorAll(".main-tab").forEach((btn) => {
      btn.addEventListener("click", () => {
        document.querySelectorAll(".main-tab").forEach((tab) => tab.classList.remove("is-active"));
        btn.classList.add("is-active");

        const panelId = btn.dataset.main;
        document.querySelectorAll(".main-panel").forEach((panel) => {
          panel.classList.toggle("is-active", panel.id === "panel-" + panelId);
        });
      });
    });

    // Subtab switching within Bloodcraft panel
    document.querySelectorAll(".subtab").forEach((btn) => {
      btn.addEventListener("click", () => {
        document.querySelectorAll(".subtab").forEach((tab) => tab.classList.remove("is-active"));
        btn.classList.add("is-active");

        const tabId = btn.dataset.tab;
        document.querySelectorAll(".content-section").forEach((section) => {
          section.classList.toggle("is-active", section.id === "tab-" + tabId);
        });
      });
    });

    // Stat Bonuses - Switch between Weapon Expertise and Blood Legacies
    document.querySelectorAll(".stat-bonus-tab").forEach((btn) => {
      btn.addEventListener("click", () => {
        document.querySelectorAll(".stat-bonus-tab").forEach((tab) => tab.classList.remove("is-active"));
        btn.classList.add("is-active");

        const statType = btn.dataset.statType;
        document.querySelectorAll(".stat-bonus-content").forEach((content) => {
          content.classList.toggle("is-active", content.id === "stat-" + statType + "-content");
        });
      });
    });

    // Familiars - Switch between Familiars and Battle Groups (within Familiars tab)
    document.querySelectorAll("#tab-familiars .familiar-mode-tab").forEach((btn) => {
      btn.addEventListener("click", () => {
        document.querySelectorAll("#tab-familiars .familiar-mode-tab").forEach((tab) => tab.classList.remove("is-active"));
        btn.classList.add("is-active");

        const mode = btn.dataset.famMode;
        document.querySelectorAll("#tab-familiars .familiar-mode-content").forEach((content) => {
          content.classList.toggle("is-active", content.id === "familiar-" + mode + "-content");
        });
      });
    });

    // Stat Row click to toggle selection (visual only for demo)
    document.querySelectorAll(".stat-row").forEach((row) => {
      row.addEventListener("click", () => {
        row.classList.toggle("stat-row-selected");
        const check = row.querySelector(".stat-check");
        if (check) {
          check.textContent = row.classList.contains("stat-row-selected") ? "✓" : "";
        }
      });
    });
