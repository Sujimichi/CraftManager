## Change Log

### Versioning:
The Major version number will only change if there is an API change on KerbalX which would make previous versions incompatible.
The Minor version number is (a bit unconventionally) being used to indicate compatibility with KSP versions.
The Patch version number is incremented with each update.  
**1.0.x are builds for KSP 1.3.x | 1.1.x are builds for KSP 1.4.x**


#### 1.0.4 & 1.1.4
**Change:** Moved settings.cfg file into PluginData folder
**Change:** Moved craft_data.cache file into PluginData folder
**Fix:** The 'craft has unsaved changes' message should no longer be shown when loading a craft when the current craft is saved.
**Added:** Details panel shows the KSP version of the selected craft if it's not the same as the current game version.
**Added:** new "Mod Lookup" button on craft details panel (only available if KerbalX integration is enabled) which uses KerbalX's mod database to check which mods the selected craft uses.
**Fix:** Stop the 'edit description' window from expanding past the bottom of the screen when showing long descriptions. also fix to prevent newline chars from being lost from description.


#### 1.0.3 & 1.1.3
**Change:** The primary name shown is now the internal ship name, and (if different) the file name is shown in a smaller font.  
**Change:** Renaming craft now allows non OS safe chars to be used in the internal name, but those chars will be replaced with an underscore in the filename. [**note**, may break some existing tag associations if updating to this version].  
**Added:** Group Actions; You can now select multiple craft and perform actions on all of them; add/remove Tags, Transfer between craft type [SPH/VAB/Subs], Move/Copy between saves and Delete.  The RHS panel will show info for the combined mass, cost and crew capacity when a group is selected.  Hold ctrl while selecting, hold shift to select all craft between two craft, and ctrl+a to select all.  
**Added** QuickTag; lets you add/remove tags to the current craft in the editor without having to open the main Craft Manager interface. click the tags icon in the toolbar or press ctrl+t to open it (esc or ctrl+t again will close it). Open it, add/remove/create a tag, done (changes are saved right away).  
**Added:** Height of main UI can be adjusted in settings.  
**Added:** When updating a description via CM, if the selected craft matches the currently loaded craft the stock description field will be updated too. (note if multiple craft share the same name this won't happen as there's no way to tell which craft is loaded).  
**Added:** There is the option in settings to have a larger thumbnail shown in the details panel (disabled by default).  
**Tweak:** Thumbnail icon in list is slightly bigger.  
**Tweak:** Autosaved-Craft has a highlight in the list to make it more distingusable from the others.  
**Tweak:** If the cache was genererated by another version of CM will be reset.  
**Tweak:** If 1 or more craft are selected, clicking to drag the list will no longer deselect the selected craft.  
**Fix:** builds for KSP 1.4.x were not detecting ctrl + click (previously used for selecting multiple craft type filters, now also used in group selecting craft).  
**Fix:** Fixed issue where moving a craft between saves would fail unless the craft had a thumbnail already generated.  


#### 1.0.2 & 1.1.2
**Fix:** issue caused when creating list of installed parts when there are more than 1 parts with the same name.
**New:** CM can use KerbalX.com to lookup any missing parts on a craft and find out which mods they come from.


#### 1.0.1 & 1.1.1  
minnor bug fixes:  
Ensure ship/subassemblies folders exist before downloading craft  
Autopopulate new save wasn't offering most recent craft, fixed  
Download queue notification wasn't being cleared once craft had downloaded, fixed  


#### 1.1.0  
Initial full release for KSP 1.3.x

#### 1.0.0  
Initial full release for KSP 1.3.

