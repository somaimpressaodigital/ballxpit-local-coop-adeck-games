# Validation for 2.1.10

The production plugins compile with the repository build script. Seven game-independent rule suites passed, covering locale selection, aim updates at different frame rates, controller identities/reconnection, individual health/rescue, progression, confirmation gates, and solo/co-op lifecycle/input ownership. The built companion also passed 182 Portuguese/English text checks.

Native tests use an isolated save profile and direct game API fixtures. The final combined run completed two co-op battles, returned to the normal menu, and entered a solo battle with exactly one active `Player` object while two controllers were connected, with zero error records in the BepInEx log. It confirmed concurrent P1/P2 firing, resumed attacks after ball recall, P2 attacks while P1 is down, upgrades/fusions, individual rescue, damage and result accounting, victory/defeat rewards and progression, and removal of the second player when leaving co-op. Native Rewired input-read fixtures also verified that P1 excludes P2 immediately before axis, held, down and up reads.

The initial city in the test profile has no player-built progression. The solo fixture explicitly supplies first-stage selection data to enter the battle; it does not validate completion of the original city tutorial or elevator-building workflow. Victories in the cooperative flow are invoked through the native test API, not natural boss kills.

An additional attack stress fixture raised both actors' leadership to exercise 25–26 common-ball queues and repeatedly caught/recharged balls through the native return API over a 65-second firing window. Both actors completed repeated nonempty and full volleys, retained natural combat damage/kill accounting and defeat XP, and returned to the menu with zero BepInEx error records. Partial volleys are permitted by native readiness rules; the companion no longer forces a complete-inventory reload before every attack.

## Attributes affect native calculations

The native stat audit temporarily adds five points to each attribute, recalculates gameplay values, verifies a change, and restores the original stats. Observed effects:

| Attribute | Native gameplay values that changed |
| --- | --- |
| Endurance | Defense multiplier, maximum health |
| Strength | Hero damage multiplier |
| Leadership | Follower counts, multi-hero count, follower damage bounds |
| Speed | Movement speed, movement while shooting, ball base/peak speed |
| Dexterity | Acceleration, critical chance, fire rate, reload time |
| Intelligence | Area/status damage multipliers, passive power, ally health |

The native/companion speed comparison also checks matching character builds for movement, movement while shooting, fire rate, reload time, and ball base/peak speeds. The extra complete-reload wait was removed; native stat values have not been arbitrarily increased to hide the firing-state defect.

These checks establish behavior in the tested baseline, not perfect balancing, compatibility with every controller/virtual-input driver, every character build, or every boss pattern. Physical reconnection and movement verification remain distinct from input-ownership API fixtures and rule tests.
