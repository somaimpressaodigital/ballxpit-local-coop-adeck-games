# Changelog

## 2.1.10

- Removed the companion's full-follower-reload requirement. Native readiness, fire-rate and reload rules determine when the next attack may start.
- Routed end-of-volley cleanup to its owning actor and cleared stale interruption state before a new P2 launch, so recall or one player's launch state does not block the other's next attack. Native iterator context remains managed by the base plugin.
- Restricted the original base's second-player update, spawn, binding and HUD to an explicitly started co-op battle. Normal play keeps one player and its native character/upgrade menus and tutorials.
- Cleared the native second player, character/inventory snapshots, return queues and aim state between co-op runs and on exit.
- Rebuilt missing runtime hero-effect caches from authored data when returning to a solo battle, avoiding a null cache during native ball creation.
- Routed movement/aim/shoot-held reads to the actor's own device before legacy input suppression. A disconnected device supplies neutral input rather than borrowing its partner's controller.
- Preserved and restored the normal Rewired assignment settings when leaving co-op.
- Stopped intro-follow movement once gameplay starts, preventing P2 from following P1 during combat.
- Added solo/co-op lifecycle and actor-input regression rules.

## 2.1.9

- Added automatic Portuguese/English mod localization. Portuguese game languages use Portuguese; all other game languages use English.
- Published production source, portable compilation scripts and authorized base dependencies.
