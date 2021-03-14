# RevenantsRevenge
A Valheim mod for much larger dungeons and camps and optionally much more difficult mobs within them and in points of interest around the game world.

`This mod must be installed on a new world, as it affects world generation.`.

# Installation
1. Download and install BepInEx Valheim (https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download this mod and put the `RevenantsRevenge.dll` into the `BepInEx/plugins` folder
3. Generate the config file by launching the game
4. Make any desired changes to `BepInEx/config/spryto.revenantsrevenge.cfg` file _before starting a new world_.

# Configuration
For larger dungeons and camps, I recommend using these:

```
min_rooms = 45
max_rooms = 120
camp_min = 25
camp_max = 40
```

You can change them up to about the max (see the config file docs), but some of these will be laggy if they're too big.  

For default mob difficulty in dungeons and around points of interest, use these:
```
mob_min_lvl = 1
mob_max_lvl = 3
mob_lvl_chance = 15
```

Level 1 is 0 stars, Level 3 is 2 stars.  
If you want all encounters to be a lot harder, raise the `mob_lvl_chance`.  
If you want rare encounters to be VERY difficult, raise the `mob_max_lvl`.  
For a really bad time, raise the `mob_min_lvl` as well.

Loot drop is retained for mobs up to 2 stars of difficulty and scaled with the multipliers (`1,2,4,7,11,...`) rather than (`1,2,4,8,16,...`).

This has been done for a bit of balance and so the game doesn't crash if you somehow manage to kill a lvl 10 mob.
