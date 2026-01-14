## VAuction ↔ EclipsePlus Protocol (v1)

This document defines the *server → client* payload formats used to drive the EclipsePlus **VAuction** UI.

### Transport

- **Channel**: V Rising system chat message (`ServerChatUtils.SendSystemMessageToClient`)
- **Integrity**: `;mac<base64(HMAC-SHA256(message))>` where `message` is the full payload before `;mac`.
- **Client verification**: EclipsePlus verifies HMAC using the shared key loaded from `Resources/secrets.json`.
- **Size limit**: `FixedString512Bytes` on the server; payloads **must** fit within 512 bytes including the `;mac...` suffix.

### Envelope

All server→client payloads use the following envelope:

`[{eventId}]:{body};mac{base64Hmac}`

Where:
- `eventId` is an integer matching the client `ClientChatSystemPatch.NetworkEventSubType` enum value.
- `body` is the event-specific body defined below.

### Event IDs (must match client)

- `8`  `AuctionPageToClient`
- `9`  `AuctionDetailToClient`
- `10` `AuctionMyListingsToClient`
- `11` `AuctionMyBidsToClient`
- `12` `AuctionEscrowToClient`
- `13` `AuctionNotificationToClient`

### Encoding conventions

- `,` separates fields inside a record
- `|` separates records
- `v1` is the first field of every body (schema version)
- `unix` timestamps are **UTC seconds** since epoch
- Strings must not contain `,` or `|` (server must sanitize/trim or replace with spaces)

### Auction keys

- `auctionKey` is a short base36 string representation of a 64-bit id.
- Client treats it as an opaque identifier and sends it back in `.auction view/bid/buy/cancel ...`.

### Bodies

#### `AuctionPageToClient` (eventId 8)

Body:

`v1,{category},{page},{pageCount},{total},{serverUnix}|{listing}|{listing}...`

Listing:

`{auctionKey},{cat},{name},{prefab},{qty},{curBid},{buyNow},{currencyPrefab},{expiresUnix},{flags}`

Flags:

Bitfield integer:
- bit0: `hasBids`
- bit1: `featured` (reserved)
- bit2: `sellerIsMe`
- bit3: `highestBidderIsMe`

#### `AuctionDetailToClient` (eventId 9)

Body:

`v1,{auctionKey},{sellerName},{itemName},{prefab},{qty},{startingBid},{currentBid},{buyNow},{currencyPrefab},{expiresUnix},{status},{minNextBid}`

Status is a short string: `active|sold|expired|cancelled`.

#### `AuctionNotificationToClient` (eventId 13)

Body:

`v1,{kind},{auctionKey},{amount},{currencyPrefab},{text}`

Kind examples: `outbid|won|sold|expired|error|info`.

