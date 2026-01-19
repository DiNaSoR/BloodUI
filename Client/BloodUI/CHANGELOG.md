# CHANGELOG - EclipsePlus (Client)
# ==================================
# V Rising client-side UI mod
# ==================================

## [1.4.0] - 2026-01-17
----------------------------------------

CHANGED:
  - Protocol: bumped Eclipse sync to 1.4.x (update client + server together)

FIXED:
  - Familiar stats display: HP/PP/SP no longer mis-parses when values exceed limits

## [1.3.15] - 2026-01-15
----------------------------------------

ADDED:
  - Character Menu System
    - Class Tab: view/select player classes with stat previews
    - Exoform Tab: manage exoform abilities and transformations
    - Familiars Tab: complete familiar management
      - Battles Panel: familiar combat management
      - Talents Panel: visual talent tree (Speed/Power/Vitality)
      - Overflow Panel: manage excess familiars
      - Settings Panel: per-familiar configuration
    - Prestige Tab: track and purchase prestige upgrades
    - Professions Tab: view profession levels and bonuses
    - Progression Tab: combined class/profession overview
    - Stat Bonuses Tab: detailed stat breakdown
  - HUD Components
    - Experience Bar: real-time XP progress
    - Expertise Bar: weapon mastery tracking
    - Familiar Bar: familiar health/level display
    - Legacy Bar: blood legacy progress
    - Quest Tracker: active quest objectives
  - Layout Service
    - LayoutService for UI element positions with save/load
    - In-game layout editing mode
    - Canvas-relative bounds for hit-testing
    - Enhanced drag/hover functionality

IMPROVED:
  - Renamed Professions tab to Progression
  - Modular CSS structure
  - Updated icons

FIXED:
  - Compilation issues with Professions -> Progression references
  - Improved logging framework

REMOVED:
  - Unnecessary Class UI and Tabs UI functionality
  - Deprecated services from the codebase
  - Unused UI elements (streamlined project)

## [1.3.14] - 2026-01-14
----------------------------------------

ADDED:
  - VDebug integration bridge for development tools
  - Layout editor with drag-and-drop positioning
  - Enhanced canvas service for UI management

IMPROVED:
  - Better error handling in data synchronization
  - Optimized HUD rendering performance

FIXED:
  - Layout persistence across game sessions
  - UI element overlap issues

## [1.3.13] - 2026-01-13
----------------------------------------

IMPROVED:
  - Code cleanup and refactoring
  - Removed deprecated components
  - Improved type safety throughout

FIXED:
  - Various minor UI fixes
  - Memory leak in HUD components

# ==================================
# End of Changelog
# ==================================
