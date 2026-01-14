# 🏛️ V Auction - Complete Implementation Plan

## Overview

**V Auction** is a server-side auction house system for V Rising that allows players to buy, sell, and bid on items including the unique feature of **familiar trading**.

---

## 📋 Table of Contents

1. [Core Features](#core-features)
2. [Data Architecture](#data-architecture)
3. [Commands](#commands)
4. [Economy Design](#economy-design)
5. [Familiar Auction](#familiar-auction)
6. [UI Integration](#ui-integration)
7. [Configuration](#configuration)
8. [Implementation Phases](#implementation-phases)
9. [Technical Details](#technical-details)

---

## 🎯 Core Features

### Auction Types

| Type | Description |
|------|-------------|
| **Standard Auction** | Bidding over time, highest bid wins |
| **Buy Now** | Instant purchase option |
| **Blind Auction** | Bids hidden until end (premium feature) |
| **Dutch Auction** | Price decreases over time |

### Listable Items

| Category | Examples | Notes |
|----------|----------|-------|
| **Resources** | Blood Essence, Shards, Ore | Stackable |
| **Equipment** | Weapons, Armor, Jewelry | With stats |
| **Materials** | Crafting components | Stackable |
| **Familiars** | Any unlocked familiar | ⭐ Unique feature |
| **Recipes** | Primal Jewels | Limited |

---

## 🗄️ Data Architecture

### Auction Listing Schema

```json
{
  "auctionId": "uuid-v4",
  "sellerId": "player-steam-id",
  "sellerName": "PlayerName",
  
  "itemType": "familiar|item|resource",
  "itemData": {
    "prefabGuid": 123456,
    "quantity": 1,
    "familiarData": {
      "level": 90,
      "prestiges": 5,
      "shinyType": "Lightning",
      "stats": {}
    }
  },
  
  "startingBid": 100,
  "currentBid": 250,
  "buyNowPrice": 500,
  "currency": "GreaterStygianShard",
  
  "highestBidderId": "player-steam-id",
  "highestBidderName": "BidderName",
  "bidHistory": [
    { "bidderId": "...", "amount": 100, "timestamp": "..." },
    { "bidderId": "...", "amount": 250, "timestamp": "..." }
  ],
  
  "createdAt": "2024-01-14T00:00:00Z",
  "expiresAt": "2024-01-15T00:00:00Z",
  "status": "active|sold|expired|cancelled",
  
  "featured": false,
  "category": "familiars"
}
```

### Storage Files

```
Data/VAuction/
├── auctions.json         # Active auctions
├── history.json          # Completed auctions (30 day retention)
├── player_bids.json      # Pending bids per player
├── escrow.json           # Items awaiting claim
└── config.json           # Runtime config cache
```

---

## 💬 Commands

### Player Commands

```bash
# Listing Items
.auction sell [quantity] [startBid] [buyNow] [hours]
  # Sells equipped item or item in cursor
  # Example: .auction sell 1 100 500 24
  
.auction sellfam [familiarIndex] [startBid] [buyNow] [hours]
  # Sell a familiar from current box
  # Example: .auction sellfam 3 500 1000 48

# Browsing
.auction browse [category] [page]
  # Categories: all, familiars, weapons, armor, resources
  # Example: .auction browse familiars 1
  
.auction search [keyword]
  # Search by name
  # Example: .auction search "Alpha Wolf"

.auction view [auctionId]
  # View auction details
  
# Bidding
.auction bid [auctionId] [amount]
  # Place a bid
  # Example: .auction bid abc123 300

.auction buy [auctionId]
  # Buy Now (instant purchase)

# Management
.auction my
  # View your active listings

.auction bids
  # View your active bids

.auction claim
  # Claim won auctions from escrow

.auction cancel [auctionId]
  # Cancel listing (only if no bids)

.auction history [page]
  # View your auction history
```

### Admin Commands 🔒

```bash
.auction admin remove [auctionId]
  # Force remove an auction

.auction admin clear
  # Clear all expired auctions

.auction admin stats
  # View auction statistics

.auction admin pause
  # Pause all auctions (maintenance)

.auction admin resume
  # Resume auctions
```

---

## 💰 Economy Design

### Currency Options

| Currency | PrefabGUID | Tier | Use Case |
|----------|------------|------|----------|
| Blood Essence | 862477668 | Common | Small trades |
| Greater Blood Essence | ... | Uncommon | Medium trades |
| Greater Stygian Shard | ... | Rare | Premium items |
| Primal Stygian Shard | ... | Epic | Familiars, rare items |
| **V Coins** (new) | Custom | Universal | All trades |

### V Coins (Optional New Currency)

If implemented:
- Earned from: Quests, Prestige, Events
- Spent on: Auctions, NPC shops
- Exchange rate with other items
- Prevents resource inflation

### Fees & Taxes

| Action | Fee | Purpose |
|--------|-----|---------|
| Listing | 5% of starting bid | Prevents spam |
| Sale | 10% of final price | Gold sink |
| Cancel | Refund listing fee - 50% | Discourages frivolous listings |
| Expired | No fee | Return to seller |

### Anti-Manipulation

- **Minimum bid increment**: 5% of current bid
- **Sniping protection**: Bids in last 5 min extend by 5 min
- **Self-bidding blocked**: Can't bid on own auctions
- **Price floor**: Minimum 10 of currency
- **Price ceiling**: Maximum 100,000 of currency

---

## 🐺 Familiar Auction

### Unique Selling Point

This is the **killer feature** - no other mod allows familiar trading!

### Familiar Listing Data

```json
{
  "familiarName": "Alpha Wolf",
  "prefabGuid": 123456,
  "level": 90,
  "maxLevel": 90,
  "prestiges": 5,
  "shinyType": "Lightning",
  "shinyChance": 0.075,
  "stats": {
    "maxHealth": 5000,
    "physicalPower": 150,
    "spellPower": 100
  },
  "originalOwner": "PlayerName",
  "captureDate": "2024-01-01"
}
```

### Familiar Auction Rules

1. **Cannot sell bound familiar** - Must unbind first
2. **Stats preserved** - Level, prestiges, shiny all transfer
3. **History tracked** - Shows original captor
4. **VBlood familiars premium** - Higher minimum price
5. **Shiny markup** - Suggested pricing based on shiny

### Familiar Categories

```
.auction browse familiars
├── All Familiars
├── Shiny Only 🌟
├── VBlood Only 👑
├── Max Level (90)
├── High Prestige (5+)
└── Recently Listed
```

---

## 🖥️ UI Integration (EclipsePlus)

### Auction Tab in Character Menu

```
┌─────────────────────────────────────────────────────┐
│ 🏛️ V AUCTION                           [🔍 Search] │
├─────────────────────────────────────────────────────┤
│ [All] [Familiars] [Weapons] [Armor] [Resources]     │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ 🐺 Alpha Wolf 🌟                    [VIEW]      │ │
│ │ Lv.90 | P5 | Shiny: Lightning                   │ │
│ │ Current: 500 | Buy Now: 1000 | ⏰ 2h 34m        │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ ⚔️ Sanguine Greatsword                [VIEW]    │ │
│ │ GS 83 | +15% Crit                               │ │
│ │ Current: 200 | Buy Now: 400 | ⏰ 12h 0m         │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│                  [< Prev] Page 1/5 [Next >]         │
└─────────────────────────────────────────────────────┘
```

### Auction Detail View

```
┌─────────────────────────────────────────────────────┐
│ 🐺 Alpha Wolf 🌟                                    │
├─────────────────────────────────────────────────────┤
│                                                     │
│ Level: 90/90                                        │
│ Prestiges: 5                                        │
│ Shiny: ⚡ Lightning                                 │
│                                                     │
│ Stats:                                              │
│   ❤️ Health: 5000                                  │
│   ⚔️ Physical: 150                                 │
│   ✨ Spell: 100                                    │
│                                                     │
│ Seller: VampireLord420                              │
│ Original Owner: xX_BloodHunter_Xx                   │
│                                                     │
├─────────────────────────────────────────────────────┤
│ Current Bid: 500 Greater Shards                     │
│ Buy Now: 1000 Greater Shards                        │
│ Time Left: 2h 34m                                   │
├─────────────────────────────────────────────────────┤
│ Your Bid: [____550____]                             │
│                                                     │
│       [PLACE BID]        [BUY NOW]                  │
└─────────────────────────────────────────────────────┘
```

---

## ⚙️ Configuration

```ini
[VAuction.General]
# Enable/disable the auction system
Enabled = true

# Maximum active listings per player
MaxListingsPerPlayer = 10

# Minimum auction duration (hours)
MinAuctionDuration = 1

# Maximum auction duration (hours)
MaxAuctionDuration = 168

# Auction history retention (days)
HistoryRetentionDays = 30

[VAuction.Economy]
# Primary currency PrefabGUID
PrimaryCurrency = 576389135

# Listing fee percentage
ListingFeePercent = 5

# Sale tax percentage
SaleTaxPercent = 10

# Minimum listing price
MinimumPrice = 10

# Maximum listing price
MaximumPrice = 100000

[VAuction.Bidding]
# Minimum bid increment percentage
MinBidIncrementPercent = 5

# Anti-snipe window (minutes)
AntiSnipeWindowMinutes = 5

# Anti-snipe extension (minutes)
AntiSnipeExtensionMinutes = 5

[VAuction.Familiars]
# Allow familiar auctions
AllowFamiliarAuctions = true

# Minimum price for VBlood familiars
VBloodMinimumPrice = 500

# Minimum price for Shiny familiars
ShinyMinimumPrice = 200

[VAuction.Notifications]
# Notify on outbid
NotifyOnOutbid = true

# Notify on auction won
NotifyOnWin = true

# Notify on auction sold
NotifyOnSold = true

# Notify on auction expired
NotifyOnExpired = true
```

---

## 📅 Implementation Phases

### Phase 1: Core Foundation (Week 1-2)
- [ ] Data models and storage
- [ ] Basic listing/browsing commands
- [ ] Currency handling
- [ ] Escrow system

### Phase 2: Bidding System (Week 3)
- [ ] Bid placement and validation
- [ ] Anti-snipe logic
- [ ] Automatic auction resolution
- [ ] Winner notification

### Phase 3: Familiar Trading (Week 4)
- [ ] Familiar serialization
- [ ] Transfer mechanics
- [ ] Familiar listing commands
- [ ] Familiar-specific UI data

### Phase 4: UI Integration (Week 5-6)
- [ ] Auction tab in EclipsePlus
- [ ] Browse/search UI
- [ ] Detail view UI
- [ ] Bid/Buy UI interactions

### Phase 5: Polish & Testing (Week 7)
- [ ] Admin commands
- [ ] Edge case handling
- [ ] Performance optimization
- [ ] Bug fixes

### Phase 6: Launch (Week 8)
- [ ] Documentation
- [ ] Config defaults
- [ ] Thunderstore release
- [ ] Community feedback

---

## 🔧 Technical Details

### File Structure

```
Docs/VAuction/
├── plan.md                    # This file
├── VAuction.csproj           # Project file
├── Plugin.cs                  # Entry point
├── Core.cs                    # Core initialization
│
├── Commands/
│   ├── AuctionCommands.cs     # Player commands
│   └── AdminCommands.cs       # Admin commands
│
├── Data/
│   ├── AuctionListing.cs      # Listing model
│   ├── Bid.cs                 # Bid model
│   └── AuctionRepository.cs   # Data access
│
├── Services/
│   ├── AuctionService.cs      # Core auction logic
│   ├── BiddingService.cs      # Bid handling
│   ├── EscrowService.cs       # Item escrow
│   ├── FamiliarService.cs     # Familiar trading
│   └── NotificationService.cs # Player notifications
│
├── Systems/
│   └── AuctionTimerSystem.cs  # Auction expiration
│
├── Config/
│   └── VAuctionConfig.cs      # Configuration
│
└── Resources/
    └── LocalizedStrings.json  # Localization
```

### Dependencies

- BloodCraftPlus (for familiar data access)
- VampireCommandFramework (for commands)
- BepInEx 6.x

### Data Sync with EclipsePlus

```csharp
// Server sends auction data via existing sync system
public class AuctionSyncData
{
    public List<AuctionListingDto> Listings { get; set; }
    public List<AuctionListingDto> MyListings { get; set; }
    public List<BidDto> MyBids { get; set; }
    public int EscrowCount { get; set; }
}
```

### Familiar Transfer Flow

```
1. Seller: .auction sellfam 3 500 1000 24
   └─> Familiar removed from seller's box
   └─> Familiar data stored in auction listing
   └─> Auction created

2. Buyer: .auction buy abc123
   └─> Currency deducted from buyer
   └─> Currency (minus tax) sent to seller
   └─> Familiar added to buyer's current box
   └─> Auction marked as sold

3. If Expired:
   └─> Familiar returned to seller's box
   └─> Auction marked as expired
```

---

## 🎉 Success Metrics

| Metric | Target |
|--------|--------|
| Daily Active Listings | 50+ per server |
| Familiar Trades/Day | 10+ |
| Average Auction Length | 12-24 hours |
| Player Engagement | 30% of players use weekly |
| Revenue per Trade | Healthy gold sink |

---

## 🚀 Future Enhancements

1. **Auction House NPC** - Physical location to access auctions
2. **Price History Graphs** - Track item value over time
3. **Watchlist** - Get notified when items you want are listed
4. **Bulk Listing** - List multiple items at once
5. **Auction Templates** - Save common listing settings
6. **Cross-Server Trading** - Trade between servers (advanced)

---

## 📝 Notes

- This system integrates with BloodCraftPlus familiars
- Requires EclipsePlus for UI (commands work without)
- All data persists across server restarts
- Designed for both PvE and PvP servers

---

*Created for BloodCraftPlus by DiNaSoR*
