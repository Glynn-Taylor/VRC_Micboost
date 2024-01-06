A simple micboost system for VRChat.

Syncs who is boosted using a string limited to 128 characters to prevent network sync issues (as vrc names are capped to 15 characters this works out to roughly 8 max players, though you will probably only want 1-2 boosted)

If the code looks messy that's because 1. it is and 2. it is not actually C# but UdonScript and as such there are quite a few things that aren't possible or require messy workarounds.

Current issues
- Bubble sort seems to fail on some characters when sorting list alphabetically
- Haven't tested this prefab since I bundled it all into one folder so something may be missing.