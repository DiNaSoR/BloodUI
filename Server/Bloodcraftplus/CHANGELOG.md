# CHANGELOG - Bloodcraftplus (Server)
# ====================================
# V Rising server-side RPG mod
# ====================================

## [1.12.21] - 2026-01-17
----------------------------------------

CHANGED:
  - Equipment Tab Gear Level shows Exp level only (no weapon expertise, no +1)

FIXED:
  - Primal rifts: RiftFrequency <= 0 now disables scheduling (no 0-interval spam)
  - Familiar stats sync: HP/PP/SP sent as hp|pp|sp to avoid overflow
  - Documentation: clarified config options cached at startup

## [1.12.20] - 2026-01-15
----------------------------------------

ADDED:
  - Familiar Talent System
    - Three talent paths: Speed, Power, Vitality
    - Talent allocation with prerequisites and keystones
    - Stat bonuses: PhysPower, SpellPower, AtkSpeed, MoveSpeed, MaxHP, DmgReduction
    - Visual buff effects for keystones
    - Server-side persistence per SteamID + familiarId
    - Commands: .fam ta <id>, .fam tr, .fam tl
  - Gear Level shows Exp level (armor hidden, expertise shown separately)

IMPROVED:
  - Standardized naming to Bloodcraftplus
  - GitHub Actions workflows updated
  - BuildToServer target with SkipServerCopy variable

FIXED:
  - Familiar catch-up speed: 2x boost when 15+ units away
  - GitHub Actions bracket notation for secrets
  - Build output paths for release artifacts

## [1.12.19] - 2026-01-14
----------------------------------------

IMPROVED:
  - Enhanced familiar AI behavior
  - Better quest tracking accuracy

FIXED:
  - Edge case in prestige calculations
  - Rare crash during familiar summoning

## [1.12.18] - 2026-01-13
----------------------------------------

IMPROVED:
  - Database query optimizations
  - Reduced server tick overhead

FIXED:
  - Rare desync in familiar stats
  - Experience calculation edge cases

# ====================================
# End of Changelog
# ====================================
